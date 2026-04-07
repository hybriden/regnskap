namespace Regnskap.Application.Features.Mva;

using Regnskap.Application.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kontoplan;
using Regnskap.Domain.Features.Mva;

public class MvaOppgjorService : IMvaOppgjorService
{
    private readonly IMvaRepository _repo;
    private readonly IKontoplanRepository _kontoplanRepo;
    private readonly IBilagRegistreringService _bilagService;

    /// <summary>
    /// Oppgjorskonto for MVA (2740). Differansen mellom utgaende og inngaende posteres hit.
    /// </summary>
    private const string OppgjorsKontonummer = "2740";

    public MvaOppgjorService(
        IMvaRepository repo,
        IKontoplanRepository kontoplanRepo,
        IBilagRegistreringService bilagService)
    {
        _repo = repo;
        _kontoplanRepo = kontoplanRepo;
        _bilagService = bilagService;
    }

    public async Task<MvaOppgjorDto> BeregnOppgjorAsync(Guid terminId, CancellationToken ct = default)
    {
        var termin = await _repo.HentTerminAsync(terminId, ct)
            ?? throw new MvaTerminIkkeFunnetException(terminId);

        if (termin.Status != MvaTerminStatus.Apen && termin.Status != MvaTerminStatus.Beregnet)
            throw new MvaTerminIkkeApenException(terminId);

        // Sjekk om det finnes et eksisterende oppgjor (reberegning)
        var eksisterende = await _repo.HentOppgjorForTerminAsync(terminId, ct);
        if (eksisterende != null)
        {
            if (eksisterende.ErLast)
                throw new MvaOppgjorAlleredeLastException(terminId);

            // Soft-delete eksisterende
            eksisterende.IsDeleted = true;
        }

        // Hent aggregerte MVA-data fra posteringer
        var aggregeringer = await _repo.HentMvaAggregertForPeriodeAsync(
            termin.FraDato, termin.TilDato, ct);

        // Hent MVA-koder for mapping
        var mvaKoder = await _kontoplanRepo.HentAlleMvaKoderAsync(erAktiv: true, ct: ct);
        var kodeMap = mvaKoder.ToDictionary(k => k.Kode);

        // Bygg oppgjorslinjer
        var linjer = new List<MvaOppgjorLinje>();
        foreach (var agg in aggregeringer)
        {
            kodeMap.TryGetValue(agg.MvaKode, out var mvaKode);

            var rfPost = TilordneRfPostnummer(agg.StandardTaxCode);

            linjer.Add(new MvaOppgjorLinje
            {
                Id = Guid.NewGuid(),
                MvaKode = agg.MvaKode,
                StandardTaxCode = agg.StandardTaxCode,
                Sats = agg.Sats,
                Retning = agg.Retning,
                RfPostnummer = rfPost,
                SumGrunnlag = agg.SumGrunnlag,
                SumMvaBelop = agg.SumMvaBelop,
                AntallPosteringer = agg.AntallPosteringer
            });
        }

        // Beregn totaler
        decimal sumUtgaende = linjer
            .Where(l => l.Retning == MvaRetning.Utgaende)
            .Sum(l => l.SumMvaBelop);

        decimal sumInngaende = linjer
            .Where(l => l.Retning == MvaRetning.Inngaende)
            .Sum(l => l.SumMvaBelop);

        // Snudd avregning: utgaende-del (koder 82, 87, 92) og inngaende-del (koder 81, 86, 91)
        decimal sumSnuddUtg = linjer
            .Where(l => l.Retning == MvaRetning.SnuddAvregning && IsSnuddAvregningUtgaende(l.StandardTaxCode))
            .Sum(l => l.SumMvaBelop);

        decimal sumSnuddIng = linjer
            .Where(l => l.Retning == MvaRetning.SnuddAvregning && IsSnuddAvregningInngaende(l.StandardTaxCode))
            .Sum(l => l.SumMvaBelop);

        decimal mvaTilBetaling = sumUtgaende + sumSnuddUtg - sumInngaende - sumSnuddIng;

        var oppgjor = new MvaOppgjor
        {
            Id = Guid.NewGuid(),
            MvaTerminId = terminId,
            BeregnetTidspunkt = DateTime.UtcNow,
            BeregnetAv = "system", // TODO: Hent fra HTTP-kontekst
            SumUtgaendeMva = sumUtgaende,
            SumInngaendeMva = sumInngaende,
            SumSnuddAvregningUtgaende = sumSnuddUtg,
            SumSnuddAvregningInngaende = sumSnuddIng,
            MvaTilBetaling = mvaTilBetaling,
            ErLast = false,
            Linjer = linjer
        };

        await _repo.LeggTilOppgjorAsync(oppgjor, ct);

        // Oppdater terminstatus
        termin.Status = MvaTerminStatus.Beregnet;
        await _repo.LagreEndringerAsync(ct);

        return MapOppgjor(oppgjor, termin.Terminnavn);
    }

    public async Task<MvaOppgjorDto> BokforOppgjorAsync(Guid terminId, CancellationToken ct = default)
    {
        var termin = await _repo.HentTerminAsync(terminId, ct)
            ?? throw new MvaTerminIkkeFunnetException(terminId);

        if (termin.OppgjorsBilagId.HasValue)
            throw new MvaOppgjorAlleredeBokfortException(terminId);

        var oppgjor = await _repo.HentOppgjorForTerminAsync(terminId, ct)
            ?? throw new MvaOppgjorManglerException(terminId);

        if (oppgjor.ErLast)
            throw new MvaOppgjorAlleredeLastException(terminId);

        // Hent MVA-koder for kontotilknytning
        var mvaKoder = await _kontoplanRepo.HentAlleMvaKoderAsync(erAktiv: true, ct: ct);
        var kodeMap = mvaKoder.ToDictionary(k => k.Kode);

        // Bygg oppgjorsbilag-posteringer
        // Prinsipp: nullstill alle MVA-kontoer, differansen mot oppgjorskonto 2740
        var posteringer = new List<OpprettPosteringRequest>();
        decimal sumDebetTotalt = 0m;
        decimal sumKreditTotalt = 0m;

        foreach (var linje in oppgjor.Linjer)
        {
            if (!kodeMap.TryGetValue(linje.MvaKode, out var mvaKode))
                continue;

            if (linje.SumMvaBelop == 0m)
                continue;

            // Utgaende MVA: normalt kredit-saldo pa 2700-serien, nullstilles med debet
            if (linje.Retning == MvaRetning.Utgaende || IsSnuddAvregningUtgaende(linje.StandardTaxCode))
            {
                var kontonummer = mvaKode.UtgaendeKonto?.Kontonummer ?? "2700";
                posteringer.Add(new OpprettPosteringRequest(
                    kontonummer, BokforingSide.Debet, Math.Abs(linje.SumMvaBelop),
                    $"Nullstill utgaende MVA kode {linje.MvaKode}",
                    null, null, null, null, null));
                sumDebetTotalt += Math.Abs(linje.SumMvaBelop);
            }
            // Inngaende MVA: normalt debet-saldo pa 2710-serien, nullstilles med kredit
            else if (linje.Retning == MvaRetning.Inngaende || IsSnuddAvregningInngaende(linje.StandardTaxCode))
            {
                var kontonummer = mvaKode.InngaendeKonto?.Kontonummer ?? "2710";
                posteringer.Add(new OpprettPosteringRequest(
                    kontonummer, BokforingSide.Kredit, Math.Abs(linje.SumMvaBelop),
                    $"Nullstill inngaende MVA kode {linje.MvaKode}",
                    null, null, null, null, null));
                sumKreditTotalt += Math.Abs(linje.SumMvaBelop);
            }
        }

        // Oppgjorskonto 2740: differansen (netto MVA til betaling)
        var nettoBelop = sumDebetTotalt - sumKreditTotalt;
        if (nettoBelop > 0)
        {
            // Skyldig Skatteetaten: kredit oppgjorskonto
            posteringer.Add(new OpprettPosteringRequest(
                OppgjorsKontonummer, BokforingSide.Kredit, nettoBelop,
                $"MVA-oppgjor termin {termin.Termin}/{termin.Ar}",
                null, null, null, null, null));
        }
        else if (nettoBelop < 0)
        {
            // Tilgode fra Skatteetaten: debet oppgjorskonto
            posteringer.Add(new OpprettPosteringRequest(
                OppgjorsKontonummer, BokforingSide.Debet, Math.Abs(nettoBelop),
                $"MVA-oppgjor termin {termin.Termin}/{termin.Ar} (tilgode)",
                null, null, null, null, null));
        }

        // Opprett og bokfor bilag via AUTO-serien
        var bilagRequest = new OpprettBilagRequest(
            Type: BilagType.Manuelt,
            Bilagsdato: termin.TilDato,
            Beskrivelse: $"MVA-oppgjorsbilag termin {termin.Termin}/{termin.Ar}",
            EksternReferanse: $"MVA-OPPGJOR-{termin.Ar}-{termin.Termin}",
            SerieKode: "AUTO",
            Posteringer: posteringer,
            BokforDirekte: true
        );

        var bilag = await _bilagService.OpprettOgBokforBilagAsync(bilagRequest, ct);

        // Lagre oppgjorsbilag-referanse pa terminen
        termin.OppgjorsBilagId = bilag.Id;
        await _repo.LagreEndringerAsync(ct);

        return MapOppgjor(oppgjor, termin.Terminnavn);
    }

    public async Task<MvaOppgjorDto> HentOppgjorAsync(Guid terminId, CancellationToken ct = default)
    {
        var termin = await _repo.HentTerminAsync(terminId, ct)
            ?? throw new MvaTerminIkkeFunnetException(terminId);

        var oppgjor = await _repo.HentOppgjorForTerminAsync(terminId, ct)
            ?? throw new MvaOppgjorManglerException(terminId);

        return MapOppgjor(oppgjor, termin.Terminnavn);
    }

    /// <summary>
    /// Tilordner RF-0002 postnummer basert pa StandardTaxCode.
    /// </summary>
    public static int TilordneRfPostnummer(string standardTaxCode)
    {
        return standardTaxCode switch
        {
            "3" => 1,   // Utgaende MVA, alminnelig sats (25%)
            "31" => 2,  // Utgaende MVA, middels sats (15%)
            "33" => 3,  // Utgaende MVA, lav sats (12%)
            "51" => 4,  // Innforsel av varer, alminnelig sats (25%)
            "52" => 5,  // Innforsel av varer, middels sats (15%)
            "82" => 6,  // Tjenester kjopt fra utlandet (snudd avregning, 25%)
            "1" => 7,   // Inngaende MVA, alminnelig sats (25%)
            "11" => 8,  // Inngaende MVA, middels sats (15%)
            "13" => 9,  // Inngaende MVA, lav sats (12%)
            "14" => 10, // Innforsel av varer, inngaende MVA (25%)
            "15" => 11, // Innforsel av varer, inngaende MVA (15%)
            "81" => 12, // Tjenester fra utlandet, inngaende MVA (snudd avregning, 25%)
            _ => 0      // Ukjent kode, ikke RF-0002-post
        };
    }

    /// <summary>
    /// Snudd avregning utgaende-del: StandardTaxCodes 82, 87, 92.
    /// </summary>
    public static bool IsSnuddAvregningUtgaende(string standardTaxCode)
    {
        return standardTaxCode is "82" or "87" or "92";
    }

    /// <summary>
    /// Snudd avregning inngaende-del: StandardTaxCodes 81, 86, 91.
    /// </summary>
    public static bool IsSnuddAvregningInngaende(string standardTaxCode)
    {
        return standardTaxCode is "81" or "86" or "91";
    }

    private static MvaOppgjorDto MapOppgjor(MvaOppgjor o, string terminnavn)
    {
        return new MvaOppgjorDto(
            o.Id, o.MvaTerminId, terminnavn,
            o.BeregnetTidspunkt, o.BeregnetAv,
            o.SumUtgaendeMva, o.SumInngaendeMva,
            o.SumSnuddAvregningUtgaende, o.SumSnuddAvregningInngaende,
            o.MvaTilBetaling, o.ErLast,
            o.Linjer.Select(l => new MvaOppgjorLinjeDto(
                l.MvaKode, l.StandardTaxCode, l.Sats,
                l.Retning.ToString(), l.RfPostnummer,
                l.SumGrunnlag, l.SumMvaBelop, l.AntallPosteringer
            )).ToList()
        );
    }
}
