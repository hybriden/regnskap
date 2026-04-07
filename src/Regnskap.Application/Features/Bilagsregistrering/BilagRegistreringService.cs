namespace Regnskap.Application.Features.Bilagsregistrering;

using Regnskap.Application.Features.Hovedbok;
using Regnskap.Application.Features.Kontoplan;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kontoplan;

/// <summary>
/// Implementerer IBilagService og IBilagRegistreringService med all forretningslogikk for bilagsregistrering.
/// </summary>
public class BilagRegistreringService : IBilagService, IBilagRegistreringService
{
    private readonly IHovedbokRepository _hovedbokRepo;
    private readonly IBilagRepository _bilagRepo;
    private readonly IKontoplanRepository _kontoplanRepo;
    private readonly IMvaKodeService _mvaKodeService;
    private readonly ITransactionManager _transactionManager;

    private const int MaksAntallRetryForsok = 3;

    public BilagRegistreringService(
        IHovedbokRepository hovedbokRepo,
        IBilagRepository bilagRepo,
        IKontoplanRepository kontoplanRepo,
        IMvaKodeService mvaKodeService,
        ITransactionManager transactionManager)
    {
        _hovedbokRepo = hovedbokRepo;
        _bilagRepo = bilagRepo;
        _kontoplanRepo = kontoplanRepo;
        _mvaKodeService = mvaKodeService;
        _transactionManager = transactionManager;
    }

    // --- IBilagService implementation ---

    public async Task<Application.Features.Hovedbok.BilagDto> OpprettBilagAsync(
        Application.Features.Hovedbok.OpprettBilagRequest request, CancellationToken ct = default)
    {
        // Adapter fra Hovedbok-DTO til Bilag-DTO
        var bilagRequest = new OpprettBilagRequest(
            request.Type,
            request.Bilagsdato,
            request.Beskrivelse,
            request.EksternReferanse,
            null, // SerieKode
            request.Posteringer.Select(p => new OpprettPosteringRequest(
                p.Kontonummer, p.Side, p.Belop, p.Beskrivelse, p.MvaKode,
                p.Avdelingskode, p.Prosjektkode, null, null)).ToList(),
            true);

        var result = await OpprettOgBokforBilagAsync(bilagRequest, ct);

        // Map tilbake til Hovedbok DTO format
        return MapTilHovedbokBilagDto(result);
    }

    public async Task<Application.Features.Hovedbok.BilagDto> HentBilagAsync(Guid id, CancellationToken ct = default)
    {
        var bilag = await _hovedbokRepo.HentBilagMedPosteringerAsync(id, ct)
            ?? throw new BilagIkkeFunnetException(id);
        return MapTilHovedbokBilagDto(MapTilBilagDto(bilag));
    }

    public async Task<Application.Features.Hovedbok.BilagDto> HentBilagMedNummerAsync(
        int ar, int bilagsnummer, CancellationToken ct = default)
    {
        var bilag = await _hovedbokRepo.HentBilagMedNummerAsync(ar, bilagsnummer, ct)
            ?? throw new BilagIkkeFunnetException(Guid.Empty);
        return MapTilHovedbokBilagDto(MapTilBilagDto(bilag));
    }

    public async Task<(List<Application.Features.Hovedbok.BilagDto> Data, int TotaltAntall)> HentBilagListeAsync(
        int ar, int? periode = null, BilagType? type = null,
        int side = 1, int antall = 50, CancellationToken ct = default)
    {
        var data = await _hovedbokRepo.HentBilagForPeriodeAsync(ar, periode, type, side, antall, ct);
        var totalt = await _hovedbokRepo.TellBilagForPeriodeAsync(ar, periode, type, ct);
        return (data.Select(b => MapTilHovedbokBilagDto(MapTilBilagDto(b))).ToList(), totalt);
    }

    // --- Bilag-modul spesifikke operasjoner ---

    public async Task<BilagDto> OpprettOgBokforBilagAsync(OpprettBilagRequest request, CancellationToken ct = default)
    {
        // 1. Valider og generer MVA-posteringer
        var valideringResultat = await ValiderBilagInternAsync(request, ct);
        if (!valideringResultat.ErGyldig)
        {
            var forsteFeil = valideringResultat.Feil.First();
            throw new BilagValideringException(
                "ny", $"{forsteFeil.Kode}: {forsteFeil.Melding}");
        }

        // M-3: Eksplisitt transaksjon rundt hele bokforingsoperasjonen
        // Sikrer atomisitet for nummerering + bilag + KontoSaldo-oppdatering
        await using var transaction = await _transactionManager.BeginTransactionAsync(ct);
        try
        {
            // 2. Finn regnskapsperiode
            var periode = await _hovedbokRepo.HentPeriodeForDatoAsync(request.Bilagsdato, ct)
                ?? throw new PeriodeIkkeFunnetException(request.Bilagsdato.Year, request.Bilagsdato.Month);

            if (periode.Status == PeriodeStatus.Lukket)
                throw new PeriodeLukketException(periode.Ar, periode.Periode);
            if (periode.Status == PeriodeStatus.Sperret)
                throw new PeriodeSperretException(periode.Ar, periode.Periode);

            // 3. Bestem bilagserie
            var serieKode = request.SerieKode ?? "MAN";
            var serie = await _bilagRepo.HentBilagSerieAsync(serieKode, ct)
                ?? throw new SerieIkkeFunnetException(serieKode);
            if (!serie.ErAktiv)
                throw new SerieInaktivException(serieKode);

            // 4. Tildel bilagsnummer
            var bilagsnummer = await _hovedbokRepo.NestebilagsnummerAsync(request.Bilagsdato.Year, ct);

            // 5. Tildel serienummer (med retry ved concurrency-konflikt)
            var serieNummer = await TildelSerieNummerAsync(serie, request.Bilagsdato.Year, ct);

            // 6. Opprett bilag-entitet
            var bilag = new Bilag
            {
                Id = Guid.NewGuid(),
                Bilagsnummer = bilagsnummer,
                Ar = request.Bilagsdato.Year,
                Type = request.Type,
                Bilagsdato = request.Bilagsdato,
                Registreringsdato = DateTime.UtcNow,
                Beskrivelse = request.Beskrivelse,
                EksternReferanse = request.EksternReferanse,
                RegnskapsperiodeId = periode.Id,
                BilagSerieId = serie.Id,
                SerieKode = serieKode,
                SerieNummer = serieNummer,
                ErBokfort = false
            };

            // 7. Opprett posteringer (bruker + MVA-auto)
            var linjenummerTeller = new LinjenummerTeller { Verdi = 1 };
            var allePosteringer = new List<Postering>();

            foreach (var req in request.Posteringer)
            {
                var konto = await _kontoplanRepo.HentKontoAsync(req.Kontonummer, ct)
                    ?? throw new KontoIkkeFunnetException(req.Kontonummer);

                var postering = new Postering
                {
                    Id = Guid.NewGuid(),
                    BilagId = bilag.Id,
                    Linjenummer = linjenummerTeller.NesteOgInkrementer(),
                    KontoId = konto.Id,
                    Kontonummer = konto.Kontonummer,
                    Side = req.Side,
                    Belop = new Belop(req.Belop),
                    Beskrivelse = req.Beskrivelse,
                    MvaKode = req.MvaKode,
                    Avdelingskode = req.Avdelingskode,
                    Prosjektkode = req.Prosjektkode,
                    KundeId = req.KundeId,
                    LeverandorId = req.LeverandorId,
                    Bilagsdato = request.Bilagsdato,
                    ErAutoGenerertMva = false
                };

                // MVA-beregning pa linjen
                if (!string.IsNullOrEmpty(req.MvaKode) && req.MvaKode != "0")
                {
                    var mvaKode = await _mvaKodeService.HentMvaKodeEllerKastAsync(req.MvaKode, ct);
                    var mvaBelop = Math.Round(req.Belop * mvaKode.Sats / 100m, 2, MidpointRounding.ToEven);
                    postering.MvaGrunnlag = new Belop(req.Belop);
                    postering.MvaBelop = new Belop(mvaBelop);
                    postering.MvaSats = mvaKode.Sats;

                    // Generer MVA-autoposteringer
                    var mvaPosteringer = await GenererMvaPosteringerAsync(
                        bilag.Id, mvaKode, req.Belop, request.Bilagsdato, linjenummerTeller, ct);
                    allePosteringer.AddRange(mvaPosteringer);
                }

                allePosteringer.Add(postering);
            }

            // Sett posteringer pa bilaget
            foreach (var p in allePosteringer.OrderBy(p => p.Linjenummer))
                bilag.Posteringer.Add(p);

            // 8. Valider balanse
            bilag.ValiderBalanse();

            // 9. Lagre bilag
            await _hovedbokRepo.LeggTilBilagAsync(bilag, ct);

            // 10. Bokfor hvis direkte
            if (request.BokforDirekte)
            {
                await BokforBilagInternAsync(bilag, periode, ct);
            }

            await _hovedbokRepo.LagreEndringerAsync(ct);
            await transaction.CommitAsync(ct);

            return MapTilBilagDto(bilag);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<BilagDto> HentBilagDetaljertAsync(Guid id, CancellationToken ct = default)
    {
        var bilag = await _hovedbokRepo.HentBilagMedPosteringerAsync(id, ct)
            ?? throw new BilagIkkeFunnetException(id);
        return MapTilBilagDto(bilag);
    }

    public async Task<BilagDto> HentBilagMedSerieAsync(
        string serieKode, int ar, int serieNummer, CancellationToken ct = default)
    {
        var bilag = await _bilagRepo.HentBilagMedSerieAsync(serieKode, ar, serieNummer, ct)
            ?? throw new BilagIkkeFunnetException(Guid.Empty);
        return MapTilBilagDto(bilag);
    }

    public async Task<BilagDto> BokforBilagAsync(Guid id, CancellationToken ct = default)
    {
        var bilag = await _hovedbokRepo.HentBilagMedPosteringerAsync(id, ct)
            ?? throw new BilagIkkeFunnetException(id);

        if (bilag.ErBokfort)
            throw new BilagAlleredeBokfortException(id);

        var periode = await _hovedbokRepo.HentPeriodeForDatoAsync(bilag.Bilagsdato, ct)
            ?? throw new PeriodeIkkeFunnetException(bilag.Ar, bilag.Bilagsdato.Month);

        if (periode.Status != PeriodeStatus.Apen)
            throw new PeriodeLukketException(periode.Ar, periode.Periode);

        // M-3: Eksplisitt transaksjon for bokforing + KontoSaldo-oppdatering
        await using var transaction = await _transactionManager.BeginTransactionAsync(ct);
        try
        {
            await BokforBilagInternAsync(bilag, periode, ct);
            await _hovedbokRepo.LagreEndringerAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return MapTilBilagDto(bilag);
    }

    public async Task<BilagDto> TilbakeforBilagAsync(TilbakeforBilagRequest request, CancellationToken ct = default)
    {
        // 1. Hent og valider originalbilaget
        var original = await _hovedbokRepo.HentBilagMedPosteringerAsync(request.OriginalBilagId, ct)
            ?? throw new BilagIkkeFunnetException(request.OriginalBilagId);

        if (!original.ErBokfort)
            throw new BilagIkkeBokfortForTilbakeforingException(request.OriginalBilagId);
        if (original.ErTilbakfort)
            throw new BilagAlleredeTilbakfortException(request.OriginalBilagId);

        // 2. Finn apen periode for tilbakeforingsdato
        var periode = await _hovedbokRepo.HentPeriodeForDatoAsync(request.Tilbakeforingsdato, ct)
            ?? throw new PeriodeIkkeFunnetException(request.Tilbakeforingsdato.Year, request.Tilbakeforingsdato.Month);

        if (periode.Status != PeriodeStatus.Apen)
            throw new PeriodeLukketException(periode.Ar, periode.Periode);

        // M-3: Eksplisitt transaksjon for tilbakeforing + nummerering + KontoSaldo
        await using var transaction = await _transactionManager.BeginTransactionAsync(ct);
        try
        {
            // 3. Finn KOR-serien
            var korSerie = await _bilagRepo.HentBilagSerieAsync("KOR", ct)
                ?? throw new SerieIkkeFunnetException("KOR");

            // 4. Tildel numre (med retry ved concurrency-konflikt)
            var bilagsnummer = await _hovedbokRepo.NestebilagsnummerAsync(request.Tilbakeforingsdato.Year, ct);
            var serieNummer = await TildelSerieNummerAsync(korSerie, request.Tilbakeforingsdato.Year, ct);

            // 5. Opprett tilbakeforingsbilag
            var tilbakeforBilag = new Bilag
            {
                Id = Guid.NewGuid(),
                Bilagsnummer = bilagsnummer,
                Ar = request.Tilbakeforingsdato.Year,
                Type = BilagType.Korreksjon,
                Bilagsdato = request.Tilbakeforingsdato,
                Registreringsdato = DateTime.UtcNow,
                Beskrivelse = $"Tilbakeforing av {original.BilagsId}: {request.Beskrivelse}",
                RegnskapsperiodeId = periode.Id,
                BilagSerieId = korSerie.Id,
                SerieKode = "KOR",
                SerieNummer = serieNummer,
                TilbakefortFraBilagId = original.Id,
                ErBokfort = false
            };

            // 6. Speilvendte posteringer
            var linjenummer = 1;
            foreach (var orig in original.Posteringer.OrderBy(p => p.Linjenummer))
            {
                var motSide = orig.Side == BokforingSide.Debet ? BokforingSide.Kredit : BokforingSide.Debet;
                tilbakeforBilag.Posteringer.Add(new Postering
                {
                    Id = Guid.NewGuid(),
                    BilagId = tilbakeforBilag.Id,
                    Linjenummer = linjenummer++,
                    KontoId = orig.KontoId,
                    Kontonummer = orig.Kontonummer,
                    Side = motSide,
                    Belop = orig.Belop,
                    Beskrivelse = $"Tilbakeforing: {orig.Beskrivelse}",
                    MvaKode = orig.MvaKode,
                    MvaBelop = orig.MvaBelop,
                    MvaGrunnlag = orig.MvaGrunnlag,
                    MvaSats = orig.MvaSats,
                    Avdelingskode = orig.Avdelingskode,
                    Prosjektkode = orig.Prosjektkode,
                    KundeId = orig.KundeId,
                    LeverandorId = orig.LeverandorId,
                    Bilagsdato = request.Tilbakeforingsdato,
                    ErAutoGenerertMva = orig.ErAutoGenerertMva
                });
            }

            // 7. Valider balanse
            tilbakeforBilag.ValiderBalanse();

            // 8. Link tilbake
            original.TilbakefortAvBilagId = tilbakeforBilag.Id;
            original.ErTilbakfort = true;

            // 9. Lagre og bokfor umiddelbart
            await _hovedbokRepo.LeggTilBilagAsync(tilbakeforBilag, ct);
            await BokforBilagInternAsync(tilbakeforBilag, periode, ct);
            await _hovedbokRepo.LagreEndringerAsync(ct);
            await transaction.CommitAsync(ct);

            return MapTilBilagDto(tilbakeforBilag);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<BilagValideringResultatDto> ValiderBilagAsync(
        ValiderBilagRequest request, CancellationToken ct = default)
    {
        var opprettRequest = new OpprettBilagRequest(
            request.Type, request.Bilagsdato, request.Beskrivelse,
            null, request.SerieKode, request.Posteringer, false);
        return await ValiderBilagInternAsync(opprettRequest, ct);
    }

    // --- Vedlegg ---

    public async Task<VedleggDto> LeggTilVedleggAsync(LeggTilVedleggRequest request, CancellationToken ct = default)
    {
        var bilag = await _hovedbokRepo.HentBilagAsync(request.BilagId, ct)
            ?? throw new BilagIkkeFunnetException(request.BilagId);

        if (!Domain.Features.Bilagsregistrering.Vedlegg.TillateMimeTyper.Contains(request.MimeType))
            throw new UgyldigMimeTypeException(request.MimeType);

        if (request.Storrelse > Domain.Features.Bilagsregistrering.Vedlegg.MaksStorrelse)
            throw new VedleggForStortException(request.Storrelse);

        var eksisterende = await _bilagRepo.HentVedleggForBilagAsync(request.BilagId, ct);

        var vedlegg = new Domain.Features.Bilagsregistrering.Vedlegg
        {
            Id = Guid.NewGuid(),
            BilagId = request.BilagId,
            Filnavn = request.Filnavn,
            MimeType = request.MimeType,
            Storrelse = request.Storrelse,
            LagringSti = request.LagringSti,
            HashSha256 = request.HashSha256,
            Beskrivelse = request.Beskrivelse,
            Rekkefolge = eksisterende.Count + 1
        };

        await _bilagRepo.LeggTilVedleggAsync(vedlegg, ct);
        await _bilagRepo.LagreEndringerAsync(ct);

        return MapTilVedleggDto(vedlegg);
    }

    public async Task<List<VedleggDto>> HentVedleggForBilagAsync(Guid bilagId, CancellationToken ct = default)
    {
        _ = await _hovedbokRepo.HentBilagAsync(bilagId, ct)
            ?? throw new BilagIkkeFunnetException(bilagId);

        var vedlegg = await _bilagRepo.HentVedleggForBilagAsync(bilagId, ct);
        return vedlegg.Select(MapTilVedleggDto).ToList();
    }

    public async Task SlettVedleggAsync(Guid bilagId, Guid vedleggId, CancellationToken ct = default)
    {
        var bilag = await _hovedbokRepo.HentBilagAsync(bilagId, ct)
            ?? throw new BilagIkkeFunnetException(bilagId);

        if (bilag.ErBokfort)
            throw new VedleggPaBokfortBilagException();

        var vedlegg = await _bilagRepo.HentVedleggAsync(vedleggId, ct)
            ?? throw new VedleggIkkeFunnetException(vedleggId);

        vedlegg.IsDeleted = true;
        await _bilagRepo.LagreEndringerAsync(ct);
    }

    // --- Bilagserier ---

    public async Task<List<BilagSerieDto>> HentAlleBilagSerierAsync(CancellationToken ct = default)
    {
        var serier = await _bilagRepo.HentAlleBilagSerierAsync(ct);
        return serier.Select(MapTilBilagSerieDto).ToList();
    }

    public async Task<BilagSerieDto> HentBilagSerieAsync(string kode, CancellationToken ct = default)
    {
        var serie = await _bilagRepo.HentBilagSerieAsync(kode, ct)
            ?? throw new SerieIkkeFunnetException(kode);
        return MapTilBilagSerieDto(serie);
    }

    public async Task<BilagSerieDto> OpprettBilagSerieAsync(
        OpprettBilagSerieRequest request, CancellationToken ct = default)
    {
        var serie = new BilagSerie
        {
            Id = Guid.NewGuid(),
            Kode = request.Kode.ToUpperInvariant(),
            Navn = request.Navn,
            NavnEn = request.NavnEn,
            StandardType = request.StandardType,
            ErAktiv = true,
            ErSystemserie = false,
            SaftJournalId = request.SaftJournalId
        };

        await _bilagRepo.LeggTilBilagSerieAsync(serie, ct);
        await _bilagRepo.LagreEndringerAsync(ct);

        return MapTilBilagSerieDto(serie);
    }

    public async Task<BilagSerieDto> OppdaterBilagSerieAsync(
        string kode, OppdaterBilagSerieRequest request, CancellationToken ct = default)
    {
        var serie = await _bilagRepo.HentBilagSerieAsync(kode, ct)
            ?? throw new SerieIkkeFunnetException(kode);

        // M-4: Systemserier kan ikke deaktiveres (FR-5, BilagSerie.ErSystemserie)
        if (serie.ErSystemserie && !request.ErAktiv)
            throw new SystemserieKanIkkeDeaktiveresException(kode);

        serie.Navn = request.Navn;
        serie.NavnEn = request.NavnEn;
        serie.StandardType = request.StandardType;
        serie.ErAktiv = request.ErAktiv;
        serie.SaftJournalId = request.SaftJournalId;

        await _bilagRepo.LagreEndringerAsync(ct);
        return MapTilBilagSerieDto(serie);
    }

    // --- Private hjelpemetoder ---

    /// <summary>
    /// Tildeler neste serienummer med retry ved concurrency-konflikt (M-1, FR-4).
    /// Bruker RowVersion concurrency token pa BilagSerieNummer.
    /// Ved DbUpdateConcurrencyException: refresher entiteten og proever igjen (maks 3 forsok).
    /// </summary>
    private async Task<int> TildelSerieNummerAsync(BilagSerie serie, int ar, CancellationToken ct)
    {
        for (int attempt = 0; attempt < MaksAntallRetryForsok; attempt++)
        {
            try
            {
                var serieNummer = await _bilagRepo.HentSerieNummerAsync(serie.Kode, ar, ct);
                if (serieNummer == null)
                {
                    serieNummer = new BilagSerieNummer
                    {
                        Id = Guid.NewGuid(),
                        BilagSerieId = serie.Id,
                        SerieKode = serie.Kode,
                        Ar = ar,
                        NesteNummer = 1
                    };
                    await _bilagRepo.LeggTilSerieNummerAsync(serieNummer, ct);
                }

                var nummer = serieNummer.TildelNummer();
                await _bilagRepo.LagreEndringerAsync(ct);
                return nummer;
            }
            catch (ConcurrencyException) when (attempt < MaksAntallRetryForsok - 1)
            {
                // Concurrency-konflikt: en annen transaksjon oppdaterte NesteNummer.
                // Retry - hent ferske data pa neste iterasjon.
            }
        }

        throw new NummereringKonfliktException();
    }

    /// <summary>
    /// Wrapper for line number counter to allow passing to async methods.
    /// </summary>
    private class LinjenummerTeller
    {
        public int Verdi { get; set; }
        public int NesteOgInkrementer() => Verdi++;
    }

    private async Task<List<Postering>> GenererMvaPosteringerAsync(
        Guid bilagId, MvaKode mvaKode, decimal grunnlag, DateOnly bilagsdato,
        LinjenummerTeller linjenummer, CancellationToken ct)
    {
        var posteringer = new List<Postering>();
        var mvaBelop = Math.Round(grunnlag * mvaKode.Sats / 100m, 2, MidpointRounding.ToEven);

        if (mvaBelop == 0m) return posteringer;

        switch (mvaKode.Retning)
        {
            case MvaRetning.Inngaende:
            {
                var konto = await HentMvaKontoAsync(mvaKode.InngaendeKontoId, mvaKode.Kode, "inngaende", ct);
                posteringer.Add(LagMvaPostering(bilagId, konto, BokforingSide.Debet, mvaBelop, grunnlag, mvaKode, bilagsdato, linjenummer.NesteOgInkrementer()));
                break;
            }
            case MvaRetning.Utgaende:
            {
                var konto = await HentMvaKontoAsync(mvaKode.UtgaendeKontoId, mvaKode.Kode, "utgaende", ct);
                posteringer.Add(LagMvaPostering(bilagId, konto, BokforingSide.Kredit, mvaBelop, grunnlag, mvaKode, bilagsdato, linjenummer.NesteOgInkrementer()));
                break;
            }
            case MvaRetning.SnuddAvregning:
            {
                var inngKonto = await HentMvaKontoAsync(mvaKode.InngaendeKontoId, mvaKode.Kode, "inngaende", ct);
                posteringer.Add(LagMvaPostering(bilagId, inngKonto, BokforingSide.Debet, mvaBelop, grunnlag, mvaKode, bilagsdato, linjenummer.NesteOgInkrementer()));

                var utgKonto = await HentMvaKontoAsync(mvaKode.UtgaendeKontoId, mvaKode.Kode, "utgaende", ct);
                posteringer.Add(LagMvaPostering(bilagId, utgKonto, BokforingSide.Kredit, mvaBelop, grunnlag, mvaKode, bilagsdato, linjenummer.NesteOgInkrementer()));
                break;
            }
            // MvaRetning.Ingen: ingen auto-postering
        }

        return posteringer;
    }

    private async Task<Konto> HentMvaKontoAsync(Guid? kontoId, string mvaKode, string retning, CancellationToken ct)
    {
        if (!kontoId.HasValue)
            throw new BilagValideringException("ny", $"MVA-kode {mvaKode} mangler {retning} konto.");

        // Finn konto via ID - bruk kontoplan repo
        // Vi henter alle kontoer og filtrerer, eller bruk en mer effektiv metode
        // TODO: Avklar med arkitekt - IKontoplanRepository mangler HentKontoMedIdAsync
        var kontoer = await _kontoplanRepo.HentKontoerAsync(erBokforbar: true, ct: ct);
        var konto = kontoer.FirstOrDefault(k => k.Id == kontoId.Value)
            ?? throw new BilagValideringException("ny", $"MVA-konto for kode {mvaKode} ({retning}) finnes ikke.");
        return konto;
    }

    private static Postering LagMvaPostering(
        Guid bilagId, Konto konto, BokforingSide side, decimal mvaBelop,
        decimal grunnlag, MvaKode mvaKode, DateOnly bilagsdato, int linjenummer)
    {
        return new Postering
        {
            Id = Guid.NewGuid(),
            BilagId = bilagId,
            Linjenummer = linjenummer,
            KontoId = konto.Id,
            Kontonummer = konto.Kontonummer,
            Side = side,
            Belop = new Belop(mvaBelop),
            Beskrivelse = $"MVA {mvaKode.Sats}% ({mvaKode.Kode})",
            MvaKode = mvaKode.Kode,
            MvaBelop = new Belop(mvaBelop),
            MvaGrunnlag = new Belop(grunnlag),
            MvaSats = mvaKode.Sats,
            Bilagsdato = bilagsdato,
            ErAutoGenerertMva = true
        };
    }

    private async Task BokforBilagInternAsync(Bilag bilag, Regnskapsperiode periode, CancellationToken ct)
    {
        foreach (var postering in bilag.Posteringer)
        {
            var saldo = await _hovedbokRepo.HentKontoSaldoAsync(
                postering.Kontonummer, periode.Ar, periode.Periode, ct);

            if (saldo == null)
            {
                saldo = new KontoSaldo
                {
                    Id = Guid.NewGuid(),
                    KontoId = postering.KontoId,
                    Kontonummer = postering.Kontonummer,
                    RegnskapsperiodeId = periode.Id,
                    Ar = periode.Ar,
                    Periode = periode.Periode,
                    InngaendeBalanse = Belop.Null,
                    SumDebet = Belop.Null,
                    SumKredit = Belop.Null
                };
                await _hovedbokRepo.LeggTilKontoSaldoAsync(saldo, ct);
            }

            saldo.LeggTilPostering(postering.Side, postering.Belop);
        }

        bilag.ErBokfort = true;
        bilag.BokfortTidspunkt = DateTime.UtcNow;
        bilag.BokfortAv = "system"; // TODO: Hent fra brukeridentitet
    }

    private async Task<BilagValideringResultatDto> ValiderBilagInternAsync(
        OpprettBilagRequest request, CancellationToken ct)
    {
        var feil = new List<BilagValideringFeilDto>();
        var advarsler = new List<BilagValideringAdvarselDto>();
        var mvaPosteringer = new List<PosteringDto>();

        // FR-2: Minimum posteringer
        if (request.Posteringer.Count < 2)
        {
            feil.Add(new BilagValideringFeilDto("BILAG_FOR_FA_LINJER",
                "Et bilag ma ha minimum 2 posteringer", null));
        }

        // FR-6: Periodevalidering
        var periode = await _hovedbokRepo.HentPeriodeForDatoAsync(request.Bilagsdato, ct);
        if (periode == null)
        {
            feil.Add(new BilagValideringFeilDto("PERIODE_IKKE_FUNNET",
                $"Regnskapsperiode for dato {request.Bilagsdato} finnes ikke", null));
        }
        else if (periode.Status == PeriodeStatus.Lukket)
        {
            feil.Add(new BilagValideringFeilDto("PERIODE_LUKKET",
                $"Perioden {periode.Ar}-{periode.Periode} er lukket for bokforing", null));
        }
        else if (periode.Status == PeriodeStatus.Sperret)
        {
            feil.Add(new BilagValideringFeilDto("PERIODE_SPERRET",
                $"Perioden {periode.Ar}-{periode.Periode} er sperret", null));
        }

        // FR-5: Serievalidering
        if (!string.IsNullOrEmpty(request.SerieKode))
        {
            var serie = await _bilagRepo.HentBilagSerieAsync(request.SerieKode, ct);
            if (serie == null)
                feil.Add(new BilagValideringFeilDto("SERIE_IKKE_FUNNET",
                    $"Bilagserie {request.SerieKode} finnes ikke", null));
            else if (!serie.ErAktiv)
                feil.Add(new BilagValideringFeilDto("SERIE_INAKTIV",
                    $"Bilagserie {request.SerieKode} er deaktivert", null));
        }

        // Valider per linje
        decimal sumDebet = 0m;
        decimal sumKredit = 0m;
        var linjenummer = 1;

        foreach (var linje in request.Posteringer)
        {
            var linjeNr = linjenummer++;

            // FR-3: Positive belop
            if (linje.Belop <= 0)
            {
                feil.Add(new BilagValideringFeilDto("BELOP_MA_VAERE_POSITIVT",
                    "Belop ma vaere storre enn 0", linjeNr));
                continue;
            }

            // FR-7: Kontovalidering
            var konto = await _kontoplanRepo.HentKontoAsync(linje.Kontonummer, ct);
            if (konto == null)
            {
                feil.Add(new BilagValideringFeilDto("KONTO_IKKE_FUNNET",
                    $"Konto {linje.Kontonummer} finnes ikke", linjeNr));
                continue;
            }
            if (!konto.ErAktiv)
            {
                feil.Add(new BilagValideringFeilDto("KONTO_INAKTIV",
                    $"Konto {linje.Kontonummer} er deaktivert", linjeNr));
            }
            if (!konto.ErBokforbar)
            {
                feil.Add(new BilagValideringFeilDto("KONTO_IKKE_BOKFORBAR",
                    $"Konto {linje.Kontonummer} er ikke bokforbar", linjeNr));
            }
            if (konto.KreverAvdeling && string.IsNullOrEmpty(linje.Avdelingskode))
            {
                feil.Add(new BilagValideringFeilDto("AVDELING_PAKREVD",
                    $"Konto {linje.Kontonummer} krever avdelingskode", linjeNr));
            }
            if (konto.KreverProsjekt && string.IsNullOrEmpty(linje.Prosjektkode))
            {
                feil.Add(new BilagValideringFeilDto("PROSJEKT_PAKREVD",
                    $"Konto {linje.Kontonummer} krever prosjektkode", linjeNr));
            }

            // Akkumuler belop
            if (linje.Side == BokforingSide.Debet)
                sumDebet += linje.Belop;
            else
                sumKredit += linje.Belop;

            // FR-8: MVA-validering
            if (!string.IsNullOrEmpty(linje.MvaKode) && linje.MvaKode != "0")
            {
                var mvaKode = await _mvaKodeService.HentMvaKodeAsync(linje.MvaKode, ct);
                if (mvaKode == null)
                {
                    feil.Add(new BilagValideringFeilDto("MVA_KODE_IKKE_FUNNET",
                        $"MVA-kode {linje.MvaKode} finnes ikke", linjeNr));
                }
                else
                {
                    if (!mvaKode.ErAktiv)
                    {
                        feil.Add(new BilagValideringFeilDto("MVA_KODE_INAKTIV",
                            $"MVA-kode {linje.MvaKode} er deaktivert", linjeNr));
                    }
                    else
                    {
                        // Beregn og rapporter MVA-posteringer
                        var mvaBelop = Math.Round(linje.Belop * mvaKode.Sats / 100m, 2, MidpointRounding.ToEven);
                        if (mvaBelop > 0)
                        {
                            switch (mvaKode.Retning)
                            {
                                case MvaRetning.Inngaende:
                                    sumDebet += mvaBelop;
                                    mvaPosteringer.Add(new PosteringDto(Guid.Empty, 0,
                                        mvaKode.InngaendeKonto?.Kontonummer ?? "?",
                                        mvaKode.InngaendeKonto?.Navn ?? "Inngaende MVA",
                                        "Debet", mvaBelop, $"MVA {mvaKode.Sats}%",
                                        mvaKode.Kode, mvaBelop, linje.Belop, mvaKode.Sats,
                                        null, null, null, null, true));
                                    break;
                                case MvaRetning.Utgaende:
                                    sumKredit += mvaBelop;
                                    mvaPosteringer.Add(new PosteringDto(Guid.Empty, 0,
                                        mvaKode.UtgaendeKonto?.Kontonummer ?? "?",
                                        mvaKode.UtgaendeKonto?.Navn ?? "Utgaende MVA",
                                        "Kredit", mvaBelop, $"MVA {mvaKode.Sats}%",
                                        mvaKode.Kode, mvaBelop, linje.Belop, mvaKode.Sats,
                                        null, null, null, null, true));
                                    break;
                                case MvaRetning.SnuddAvregning:
                                    sumDebet += mvaBelop;
                                    sumKredit += mvaBelop;
                                    mvaPosteringer.Add(new PosteringDto(Guid.Empty, 0,
                                        mvaKode.InngaendeKonto?.Kontonummer ?? "?",
                                        mvaKode.InngaendeKonto?.Navn ?? "Inngaende MVA",
                                        "Debet", mvaBelop, $"MVA {mvaKode.Sats}% (snudd avregning)",
                                        mvaKode.Kode, mvaBelop, linje.Belop, mvaKode.Sats,
                                        null, null, null, null, true));
                                    mvaPosteringer.Add(new PosteringDto(Guid.Empty, 0,
                                        mvaKode.UtgaendeKonto?.Kontonummer ?? "?",
                                        mvaKode.UtgaendeKonto?.Navn ?? "Utgaende MVA",
                                        "Kredit", mvaBelop, $"MVA {mvaKode.Sats}% (snudd avregning)",
                                        mvaKode.Kode, mvaBelop, linje.Belop, mvaKode.Sats,
                                        null, null, null, null, true));
                                    break;
                            }
                        }
                    }
                }
            }
        }

        // FR-1: Balansekontroll (inkl. MVA-posteringer)
        if (sumDebet != sumKredit && request.Posteringer.Count >= 2)
        {
            feil.Add(new BilagValideringFeilDto("BILAG_IKKE_I_BALANSE",
                $"Bilaget er ikke i balanse. Debet: {sumDebet:N2}, Kredit: {sumKredit:N2}, Differanse: {sumDebet - sumKredit:N2}",
                null));
        }

        return new BilagValideringResultatDto(
            feil.Count == 0,
            feil,
            advarsler,
            mvaPosteringer.Count > 0 ? mvaPosteringer : null);
    }

    // --- Mapping ---

    private static BilagDto MapTilBilagDto(Bilag bilag)
    {
        var periodeDto = bilag.Regnskapsperiode != null
            ? new Application.Features.Hovedbok.RegnskapsperiodeDto(
                bilag.Regnskapsperiode.Id,
                bilag.Regnskapsperiode.Ar,
                bilag.Regnskapsperiode.Periode,
                bilag.Regnskapsperiode.Periodenavn,
                bilag.Regnskapsperiode.FraDato,
                bilag.Regnskapsperiode.TilDato,
                bilag.Regnskapsperiode.Status.ToString(),
                bilag.Regnskapsperiode.LukketTidspunkt,
                bilag.Regnskapsperiode.LukketAv,
                bilag.Regnskapsperiode.Merknad)
            : new Application.Features.Hovedbok.RegnskapsperiodeDto(
                Guid.Empty, bilag.Ar, bilag.Bilagsdato.Month,
                $"{bilag.Ar}-{bilag.Bilagsdato.Month:D2}",
                default, default, "Ukjent", null, null, null);

        return new BilagDto(
            bilag.Id,
            bilag.BilagsId,
            bilag.SerieBilagsId,
            bilag.Bilagsnummer,
            bilag.SerieNummer,
            bilag.SerieKode,
            bilag.Ar,
            bilag.Type.ToString(),
            bilag.Bilagsdato,
            bilag.Registreringsdato,
            bilag.Beskrivelse,
            bilag.EksternReferanse,
            periodeDto,
            bilag.Posteringer.OrderBy(p => p.Linjenummer).Select(MapTilPosteringDto).ToList(),
            bilag.Vedlegg.Where(v => !v.IsDeleted).Select(MapTilVedleggDto).ToList(),
            bilag.SumDebet().Verdi,
            bilag.SumKredit().Verdi,
            bilag.ErBokfort,
            bilag.BokfortTidspunkt,
            bilag.ErTilbakfort,
            bilag.TilbakefortFraBilagId,
            bilag.TilbakefortAvBilagId);
    }

    private static PosteringDto MapTilPosteringDto(Postering p)
    {
        return new PosteringDto(
            p.Id,
            p.Linjenummer,
            p.Kontonummer,
            p.Konto?.Navn ?? "",
            p.Side.ToString(),
            p.Belop.Verdi,
            p.Beskrivelse,
            p.MvaKode,
            p.MvaBelop?.Verdi,
            p.MvaGrunnlag?.Verdi,
            p.MvaSats,
            p.Avdelingskode,
            p.Prosjektkode,
            p.KundeId,
            p.LeverandorId,
            p.ErAutoGenerertMva);
    }

    private static VedleggDto MapTilVedleggDto(Domain.Features.Bilagsregistrering.Vedlegg v)
    {
        return new VedleggDto(
            v.Id,
            v.Filnavn,
            v.MimeType,
            v.Storrelse,
            v.LagringSti,
            v.Beskrivelse,
            v.Rekkefolge,
            v.CreatedAt);
    }

    private static BilagSerieDto MapTilBilagSerieDto(BilagSerie s)
    {
        return new BilagSerieDto(
            s.Id,
            s.Kode,
            s.Navn,
            s.NavnEn,
            s.StandardType.ToString(),
            s.ErAktiv,
            s.ErSystemserie,
            s.SaftJournalId);
    }

    private static Application.Features.Hovedbok.BilagDto MapTilHovedbokBilagDto(BilagDto bilag)
    {
        return new Application.Features.Hovedbok.BilagDto(
            bilag.Id,
            bilag.BilagsId,
            bilag.Bilagsnummer,
            bilag.Ar,
            bilag.Type,
            bilag.Bilagsdato,
            bilag.Registreringsdato,
            bilag.Beskrivelse,
            bilag.EksternReferanse,
            bilag.Periode,
            bilag.Posteringer.Select(p => new Application.Features.Hovedbok.PosteringDto(
                p.Id, p.Linjenummer, p.Kontonummer, p.Kontonavn, p.Side, p.Belop,
                p.Beskrivelse, p.MvaKode, p.MvaBelop, p.MvaGrunnlag, p.MvaSats,
                p.Avdelingskode, p.Prosjektkode)).ToList(),
            bilag.SumDebet,
            bilag.SumKredit);
    }
}
