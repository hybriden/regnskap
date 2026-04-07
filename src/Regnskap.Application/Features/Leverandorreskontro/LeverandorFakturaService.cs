namespace Regnskap.Application.Features.Leverandorreskontro;

using Regnskap.Application.Common;
using Regnskap.Application.Features.Bilagsregistrering;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Leverandorreskontro;

public class LeverandorFakturaService : ILeverandorFakturaService
{
    private readonly ILeverandorReskontroRepository _repo;
    private readonly IBilagRegistreringService _bilagService;

    public LeverandorFakturaService(
        ILeverandorReskontroRepository repo,
        IBilagRegistreringService bilagService)
    {
        _repo = repo;
        _bilagService = bilagService;
    }

    /// <summary>
    /// FR-L01: Registrering av inngaende faktura.
    /// Oppretter faktura og bilag med posteringer.
    /// </summary>
    public async Task<LeverandorFakturaDto> RegistrerFakturaAsync(RegistrerFakturaRequest request, CancellationToken ct = default)
    {
        // Valider leverandor
        var leverandor = await _repo.HentLeverandorAsync(request.LeverandorId, ct)
            ?? throw new LeverandorIkkeFunnetException(request.LeverandorId);

        if (!leverandor.ErAktiv)
            throw new LeverandorSperretException(leverandor.Leverandornummer);

        if (leverandor.ErSperret)
            throw new LeverandorSperretException(leverandor.Leverandornummer);

        // Valider linjer
        if (request.Linjer == null || request.Linjer.Count == 0)
            throw new ArgumentException("Faktura ma ha minimum 1 linje.");

        if (request.Linjer.Any(l => l.Belop <= 0))
            throw new ArgumentException("Alle linjer ma ha positivt belop.");

        // FR-L10: Duplikatkontroll
        if (await _repo.EksternFakturaDuplikatAsync(request.LeverandorId, request.EksternFakturanummer, ct))
            throw new LeverandorFakturaDuplikatException(request.LeverandorId, request.EksternFakturanummer);

        // Beregn MVA per linje
        var linjer = new List<LeverandorFakturaLinje>();
        var totalEksMva = Belop.Null;
        var totalMva = Belop.Null;

        for (int i = 0; i < request.Linjer.Count; i++)
        {
            var req = request.Linjer[i];
            var mvaSats = HentMvaSats(req.MvaKode);
            var mvaBelop = new Belop(Math.Round(req.Belop * mvaSats / 100m, 2));

            var linje = new LeverandorFakturaLinje
            {
                Id = Guid.NewGuid(),
                Linjenummer = i + 1,
                KontoId = req.KontoId,
                Kontonummer = "", // Will be set by EF/lookup - denormalized
                Beskrivelse = req.Beskrivelse,
                Belop = new Belop(req.Belop),
                MvaKode = req.MvaKode,
                MvaSats = mvaSats > 0 ? mvaSats : null,
                MvaBelop = mvaSats > 0 ? mvaBelop : null,
                Avdelingskode = req.Avdelingskode,
                Prosjektkode = req.Prosjektkode
            };

            linjer.Add(linje);
            totalEksMva = totalEksMva + new Belop(req.Belop);
            totalMva = totalMva + mvaBelop;
        }

        var totalInklMva = totalEksMva + totalMva;

        // FR-L02: Forfallsdato-beregning
        var forfallsdato = request.Forfallsdato ?? BeregnForfallsdato(request.Fakturadato, leverandor);

        var internNummer = await _repo.NesteInternNummerAsync(ct);

        var faktura = new LeverandorFaktura
        {
            Id = Guid.NewGuid(),
            LeverandorId = request.LeverandorId,
            EksternFakturanummer = request.EksternFakturanummer,
            InternNummer = internNummer,
            Type = request.Type,
            Fakturadato = request.Fakturadato,
            Mottaksdato = DateOnly.FromDateTime(DateTime.UtcNow),
            Forfallsdato = forfallsdato,
            Beskrivelse = request.Beskrivelse,
            BelopEksMva = totalEksMva,
            MvaBelop = totalMva,
            BelopInklMva = totalInklMva,
            GjenstaendeBelop = totalInklMva,
            Status = FakturaStatus.Registrert,
            KidNummer = request.KidNummer,
            Valutakode = request.Valutakode,
            Valutakurs = request.Valutakurs,
            Linjer = linjer
        };

        // Opprett bilag via bilagsregistrering
        var posteringer = ByggBilagPosteringer(request.Type, linjer, totalInklMva, leverandor.Id, request.Beskrivelse);

        var bilagType = request.Type == LeverandorTransaksjonstype.Kreditnota
            ? BilagType.Kreditnota
            : BilagType.InngaendeFaktura;

        var bilagRequest = new Bilagsregistrering.OpprettBilagRequest(
            bilagType,
            request.Fakturadato,
            $"{leverandor.Leverandornummer} - {request.Beskrivelse}",
            request.EksternFakturanummer,
            "IF", // Inngaende Faktura serie
            posteringer,
            BokforDirekte: true);

        var bilag = await _bilagService.OpprettOgBokforBilagAsync(bilagRequest, ct);
        faktura.BilagId = bilag.Id;

        await _repo.LeggTilFakturaAsync(faktura, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapToDto(faktura, leverandor);
    }

    public async Task<LeverandorFakturaDto> GodkjennAsync(Guid id, CancellationToken ct = default)
    {
        var faktura = await _repo.HentFakturaMedLinjerAsync(id, ct)
            ?? throw new LeverandorFakturaIkkeFunnetException(id);

        if (faktura.Status != FakturaStatus.Registrert)
            throw new FakturaStatusException($"Faktura kan kun godkjennes fra status Registrert. Navarende status: {faktura.Status}");

        if (faktura.ErSperret)
            throw new FakturaStatusException("Fakturaen er sperret og kan ikke godkjennes.");

        faktura.Status = FakturaStatus.Godkjent;
        await _repo.OppdaterFakturaAsync(faktura, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapToDto(faktura, faktura.Leverandor);
    }

    public async Task<LeverandorFakturaDto> SperrAsync(Guid id, string arsak, CancellationToken ct = default)
    {
        var faktura = await _repo.HentFakturaMedLinjerAsync(id, ct)
            ?? throw new LeverandorFakturaIkkeFunnetException(id);

        faktura.ErSperret = true;
        faktura.SperreArsak = arsak;
        faktura.Status = FakturaStatus.Sperret;

        await _repo.OppdaterFakturaAsync(faktura, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapToDto(faktura, faktura.Leverandor);
    }

    public async Task<LeverandorFakturaDto> OpphevSperringAsync(Guid id, CancellationToken ct = default)
    {
        var faktura = await _repo.HentFakturaMedLinjerAsync(id, ct)
            ?? throw new LeverandorFakturaIkkeFunnetException(id);

        if (!faktura.ErSperret)
            throw new FakturaStatusException("Fakturaen er ikke sperret.");

        faktura.ErSperret = false;
        faktura.SperreArsak = null;
        faktura.Status = FakturaStatus.Registrert;

        await _repo.OppdaterFakturaAsync(faktura, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapToDto(faktura, faktura.Leverandor);
    }

    public async Task<LeverandorFakturaDto> HentAsync(Guid id, CancellationToken ct = default)
    {
        var faktura = await _repo.HentFakturaMedLinjerAsync(id, ct)
            ?? throw new LeverandorFakturaIkkeFunnetException(id);
        return MapToDto(faktura, faktura.Leverandor);
    }

    public async Task<PagedResult<LeverandorFakturaDto>> SokAsync(FakturaSokRequest request, CancellationToken ct = default)
    {
        // TODO: Implementer full sok med filtrering i repository
        var apne = await _repo.HentApnePosterAsync(null, ct);
        var dtos = apne.Select(f => MapToDto(f, f.Leverandor)).ToList();
        return new PagedResult<LeverandorFakturaDto>(dtos, dtos.Count, request.Side, request.Antall);
    }

    /// <summary>
    /// FR-L07: Apne poster.
    /// </summary>
    public async Task<List<LeverandorFakturaDto>> HentApnePosterAsync(DateOnly? dato = null, CancellationToken ct = default)
    {
        var fakturaer = await _repo.HentApnePosterAsync(dato, ct);
        return fakturaer.Select(f => MapToDto(f, f.Leverandor)).ToList();
    }

    /// <summary>
    /// FR-L08: Aldersfordeling.
    /// </summary>
    public async Task<AldersfordelingDto> HentAldersfordelingAsync(DateOnly dato, CancellationToken ct = default)
    {
        var apnePoster = await _repo.HentApnePosterAsync(dato, ct);

        var perLeverandor = apnePoster
            .GroupBy(f => new { f.LeverandorId, f.Leverandor.Leverandornummer, f.Leverandor.Navn })
            .Select(g =>
            {
                var kategorisert = g.Select(f => new
                {
                    Kategori = f.HentAlderskategori(dato),
                    f.GjenstaendeBelop.Verdi
                }).ToList();

                return new AldersfordelingLeverandorDto(
                    g.Key.LeverandorId,
                    g.Key.Leverandornummer,
                    g.Key.Navn,
                    kategorisert.Where(k => k.Kategori == Alderskategori.IkkeForfalt).Sum(k => k.Verdi),
                    kategorisert.Where(k => k.Kategori == Alderskategori.Dager0Til30).Sum(k => k.Verdi),
                    kategorisert.Where(k => k.Kategori == Alderskategori.Dager31Til60).Sum(k => k.Verdi),
                    kategorisert.Where(k => k.Kategori == Alderskategori.Dager61Til90).Sum(k => k.Verdi),
                    kategorisert.Where(k => k.Kategori == Alderskategori.Over90Dager).Sum(k => k.Verdi),
                    kategorisert.Sum(k => k.Verdi)
                );
            }).ToList();

        var summary = new AldersfordelingSummaryDto(
            perLeverandor.Sum(l => l.IkkeForfalt),
            perLeverandor.Sum(l => l.Dager0Til30),
            perLeverandor.Sum(l => l.Dager31Til60),
            perLeverandor.Sum(l => l.Dager61Til90),
            perLeverandor.Sum(l => l.Over90Dager),
            perLeverandor.Sum(l => l.Totalt)
        );

        return new AldersfordelingDto(perLeverandor, summary, dato);
    }

    /// <summary>
    /// FR-L09: Leverandorutskrift.
    /// </summary>
    public async Task<LeverandorutskriftDto> HentUtskriftAsync(Guid leverandorId, DateOnly fraDato, DateOnly tilDato, CancellationToken ct = default)
    {
        var leverandor = await _repo.HentLeverandorAsync(leverandorId, ct)
            ?? throw new LeverandorIkkeFunnetException(leverandorId);

        var fakturaer = await _repo.HentFakturaerForLeverandorAsync(leverandorId, null, tilDato, ct);

        // Beregn inngaende saldo (alle fakturaer for fraDato)
        var forPerioden = fakturaer.Where(f => f.Fakturadato < fraDato).ToList();
        var inngaaendeSaldo = forPerioden.Sum(f =>
            f.Type == LeverandorTransaksjonstype.Kreditnota
                ? -f.BelopInklMva.Verdi
                : f.GjenstaendeBelop.Verdi);

        // Transaksjoner i perioden
        var iPerioden = fakturaer
            .Where(f => f.Fakturadato >= fraDato && f.Fakturadato <= tilDato)
            .OrderBy(f => f.Fakturadato)
            .ThenBy(f => f.InternNummer)
            .ToList();

        var transaksjoner = new List<LeverandorutskriftLinjeDto>();
        var lopendeSaldo = inngaaendeSaldo;

        foreach (var f in iPerioden)
        {
            decimal? debet = null;
            decimal? kredit = null;

            if (f.Type == LeverandorTransaksjonstype.Kreditnota)
            {
                debet = f.BelopInklMva.Verdi;
                lopendeSaldo -= f.BelopInklMva.Verdi;
            }
            else if (f.Type == LeverandorTransaksjonstype.Betaling)
            {
                debet = f.BelopInklMva.Verdi;
                lopendeSaldo -= f.BelopInklMva.Verdi;
            }
            else
            {
                kredit = f.BelopInklMva.Verdi;
                lopendeSaldo += f.BelopInklMva.Verdi;
            }

            transaksjoner.Add(new LeverandorutskriftLinjeDto(
                f.Fakturadato,
                f.BilagId.HasValue ? f.BilagId.Value.ToString()[..8] : f.InternNummer.ToString(),
                f.Beskrivelse,
                f.Type,
                debet,
                kredit,
                lopendeSaldo,
                f.EksternFakturanummer
            ));
        }

        return new LeverandorutskriftDto(
            leverandorId,
            leverandor.Leverandornummer,
            leverandor.Navn,
            inngaaendeSaldo,
            transaksjoner,
            lopendeSaldo,
            fraDato,
            tilDato
        );
    }

    // --- Hjelpemetoder ---

    /// <summary>
    /// FR-L02: Beregn forfallsdato. Lordag/sondag flyttes til neste mandag.
    /// </summary>
    public static DateOnly BeregnForfallsdato(DateOnly fakturadato, Leverandor leverandor)
    {
        var dager = leverandor.HentBetalingsfristDager();
        var forfall = fakturadato.AddDays(dager);

        // Flytt lordag/sondag til neste mandag
        if (forfall.DayOfWeek == DayOfWeek.Saturday)
            forfall = forfall.AddDays(2);
        else if (forfall.DayOfWeek == DayOfWeek.Sunday)
            forfall = forfall.AddDays(1);

        return forfall;
    }

    /// <summary>
    /// Hent MVA-sats basert pa kode. Forenklet lookup.
    /// </summary>
    private static decimal HentMvaSats(string? mvaKode)
    {
        if (string.IsNullOrEmpty(mvaKode) || mvaKode == "0")
            return 0m;

        return mvaKode switch
        {
            "1" => 25m,
            "11" => 15m,
            "13" => 12m,
            "14" => 25m, // snudd avregning
            _ => 0m
        };
    }

    /// <summary>
    /// FR-L01/FR-L03: Bygg bilag posteringer for faktura/kreditnota.
    /// </summary>
    internal static List<Bilagsregistrering.OpprettPosteringRequest> ByggBilagPosteringer(
        LeverandorTransaksjonstype type,
        List<LeverandorFakturaLinje> linjer,
        Belop totalInklMva,
        Guid leverandorId,
        string beskrivelse)
    {
        var posteringer = new List<Bilagsregistrering.OpprettPosteringRequest>();
        var erKreditnota = type == LeverandorTransaksjonstype.Kreditnota;

        // Kostnadsposteringer per linje
        foreach (var linje in linjer)
        {
            posteringer.Add(new Bilagsregistrering.OpprettPosteringRequest(
                linje.Kontonummer.Length > 0 ? linje.Kontonummer : "6300", // fallback
                erKreditnota ? BokforingSide.Kredit : BokforingSide.Debet,
                linje.Belop.Verdi,
                linje.Beskrivelse,
                linje.MvaKode,
                linje.Avdelingskode,
                linje.Prosjektkode,
                KundeId: null,
                LeverandorId: leverandorId));

            // MVA-postering
            if (linje.MvaBelop.HasValue && linje.MvaBelop.Value.Verdi > 0)
            {
                posteringer.Add(new Bilagsregistrering.OpprettPosteringRequest(
                    "2710", // Inngaende MVA
                    erKreditnota ? BokforingSide.Kredit : BokforingSide.Debet,
                    linje.MvaBelop.Value.Verdi,
                    $"Inng. MVA {linje.MvaSats}%",
                    null,
                    null,
                    null,
                    KundeId: null,
                    LeverandorId: leverandorId));
            }
        }

        // Leverandorgjeld-postering (2400)
        posteringer.Add(new Bilagsregistrering.OpprettPosteringRequest(
            "2400", // Leverandorgjeld
            erKreditnota ? BokforingSide.Debet : BokforingSide.Kredit,
            totalInklMva.Verdi,
            beskrivelse,
            null,
            null,
            null,
            KundeId: null,
            LeverandorId: leverandorId));

        return posteringer;
    }

    internal static LeverandorFakturaDto MapToDto(LeverandorFaktura f, Leverandor? l) => new(
        f.Id,
        f.LeverandorId,
        l?.Navn ?? "",
        l?.Leverandornummer ?? "",
        f.EksternFakturanummer,
        f.InternNummer,
        f.Type,
        f.Fakturadato,
        f.Forfallsdato,
        f.Beskrivelse,
        f.BelopEksMva.Verdi,
        f.MvaBelop.Verdi,
        f.BelopInklMva.Verdi,
        f.GjenstaendeBelop.Verdi,
        f.Status,
        f.KidNummer,
        f.BilagId,
        f.ErSperret,
        f.SperreArsak,
        f.Linjer.Select(lj => new FakturaLinjeDto(
            lj.Id,
            lj.Linjenummer,
            lj.KontoId,
            lj.Kontonummer,
            lj.Beskrivelse,
            lj.Belop.Verdi,
            lj.MvaKode,
            lj.MvaSats,
            lj.MvaBelop?.Verdi,
            lj.Avdelingskode,
            lj.Prosjektkode
        )).ToList()
    );
}
