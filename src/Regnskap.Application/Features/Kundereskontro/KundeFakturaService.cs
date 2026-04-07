namespace Regnskap.Application.Features.Kundereskontro;

using Regnskap.Application.Features.Bilagsregistrering;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kundereskontro;

public class KundeFakturaService : IKundeFakturaService
{
    private readonly IKundeReskontroRepository _repo;
    private readonly IKidService _kidService;
    private readonly IBilagRegistreringService _bilagService;

    public KundeFakturaService(
        IKundeReskontroRepository repo,
        IKidService kidService,
        IBilagRegistreringService bilagService)
    {
        _repo = repo;
        _kidService = kidService;
        _bilagService = bilagService;
    }

    public async Task<KundeFakturaDto> RegistrerFakturaAsync(RegistrerKundeFakturaRequest request, CancellationToken ct = default)
    {
        var kunde = await _repo.HentKundeAsync(request.KundeId, ct)
            ?? throw new KundeIkkeFunnetException(request.KundeId);

        if (!kunde.ErAktiv)
            throw new KundeSperretException(kunde.Kundenummer);
        if (kunde.ErSperret)
            throw new KundeSperretException(kunde.Kundenummer);

        if (request.Linjer == null || request.Linjer.Count == 0)
            throw new ArgumentException("Minimum 1 fakturalinje er pakreves.");

        // Beregn forfallsdato
        var forfallsdato = request.Forfallsdato ?? BeregnForfallsdato(request.Fakturadato, kunde.Betalingsbetingelse, kunde.EgendefinertBetalingsfrist);

        // Flytt forfallsdato til neste mandag hvis helg
        forfallsdato = FlyttFraHelg(forfallsdato);

        // Beregn belop
        var linjer = new List<KundeFakturaLinje>();
        decimal totalEksMva = 0;
        decimal totalMva = 0;

        for (int i = 0; i < request.Linjer.Count; i++)
        {
            var l = request.Linjer[i];
            if (l.Antall <= 0)
                throw new ArgumentException($"Linje {i + 1}: Antall ma vaere > 0.");
            if (l.Enhetspris <= 0)
                throw new ArgumentException($"Linje {i + 1}: Enhetspris ma vaere > 0.");
            if (l.Rabatt < 0 || l.Rabatt > 100)
                throw new ArgumentException($"Linje {i + 1}: Rabatt ma vaere mellom 0 og 100.");

            var nettoBelop = Math.Round(l.Antall * l.Enhetspris * (1 - l.Rabatt / 100m), 2);
            decimal mvaSats = HentMvaSats(l.MvaKode);
            var mvaBelop = Math.Round(nettoBelop * mvaSats / 100m, 2);

            totalEksMva += nettoBelop;
            totalMva += mvaBelop;

            linjer.Add(new KundeFakturaLinje
            {
                Id = Guid.NewGuid(),
                Linjenummer = i + 1,
                KontoId = l.KontoId,
                Kontonummer = "", // TODO: Hent fra kontoplan
                Beskrivelse = l.Beskrivelse,
                Antall = l.Antall,
                Enhetspris = new Belop(l.Enhetspris),
                Belop = new Belop(nettoBelop),
                MvaKode = l.MvaKode,
                MvaSats = mvaSats,
                MvaBelop = new Belop(mvaBelop),
                Rabatt = l.Rabatt,
                Avdelingskode = l.Avdelingskode,
                Prosjektkode = l.Prosjektkode
            });
        }

        var totalInklMva = totalEksMva + totalMva;

        // Kredittgrensekontroll (FR-K13)
        if (kunde.Kredittgrense.Verdi > 0 && request.Type == KundeTransaksjonstype.Faktura)
        {
            var apnePoster = await _repo.HentApnePosterAsync(null, ct);
            var utstaaende = apnePoster
                .Where(f => f.KundeId == kunde.Id)
                .Sum(f => f.GjenstaendeBelop.Verdi);

            if (utstaaende + totalInklMva > kunde.Kredittgrense.Verdi)
                throw new KredittgrenseOverskredetException(utstaaende, totalInklMva, kunde.Kredittgrense.Verdi);
        }

        var fakturanummer = await _repo.NesteNummer(ct);

        // KID generering (FR-K04)
        var kidAlgoritme = kunde.KidAlgoritme ?? KidAlgoritme.MOD10;
        string? kidNummer = null;
        try
        {
            kidNummer = _kidService.Generer(kunde.Kundenummer, fakturanummer, kidAlgoritme);
        }
        catch (InvalidOperationException)
        {
            // MOD11 ugyldig kontrollsiffer - kan ikke generere KID for dette nummeret
            // TODO: Avklar med arkitekt om retry med neste nummer
        }

        var faktura = new KundeFaktura
        {
            Id = Guid.NewGuid(),
            KundeId = kunde.Id,
            Fakturanummer = fakturanummer,
            Type = request.Type,
            Fakturadato = request.Fakturadato,
            Forfallsdato = forfallsdato,
            Leveringsdato = request.Leveringsdato,
            Beskrivelse = request.Beskrivelse,
            BelopEksMva = new Belop(totalEksMva),
            MvaBelop = new Belop(totalMva),
            BelopInklMva = new Belop(totalInklMva),
            GjenstaendeBelop = new Belop(totalInklMva),
            Status = KundeFakturaStatus.Utstedt,
            KidNummer = kidNummer,
            Valutakode = request.Valutakode,
            Valutakurs = request.Valutakurs,
            EksternReferanse = request.EksternReferanse,
            Bestillingsnummer = request.Bestillingsnummer,
            Linjer = linjer
        };

        // Kreditnota: GjenstaendeBelop er negativ (motregning)
        if (request.Type == KundeTransaksjonstype.Kreditnota)
        {
            faktura.GjenstaendeBelop = new Belop(-totalInklMva);
        }

        foreach (var linje in linjer)
        {
            linje.KundeFakturaId = faktura.Id;
        }

        // FR-K01/FR-K03: Opprett bilag med posteringer for dobbelt bokholderi
        var posteringer = ByggFakturaBilagPosteringer(request.Type, linjer, new Belop(totalInklMva), kunde.Id, request.Beskrivelse);

        var bilagType = request.Type == KundeTransaksjonstype.Kreditnota
            ? BilagType.Kreditnota
            : BilagType.UtgaendeFaktura;

        var bilagRequest = new OpprettBilagRequest(
            bilagType,
            request.Fakturadato,
            $"{kunde.Kundenummer} - {request.Beskrivelse}",
            faktura.Fakturanummer.ToString(),
            "UF", // Utgaende Faktura serie
            posteringer,
            BokforDirekte: true);

        var bilag = await _bilagService.OpprettOgBokforBilagAsync(bilagRequest, ct);
        faktura.BilagId = bilag.Id;

        await _repo.LeggTilFakturaAsync(faktura, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapToDto(faktura, kunde);
    }

    public async Task<KundeFakturaDto> HentAsync(Guid id, CancellationToken ct = default)
    {
        var faktura = await _repo.HentFakturaAsync(id, ct)
            ?? throw new KundeFakturaIkkeFunnetException(id);
        return MapToDto(faktura, faktura.Kunde);
    }

    public async Task<(List<KundeFakturaDto> Data, int TotaltAntall)> SokAsync(KundeFakturaSokRequest request, CancellationToken ct = default)
    {
        var (data, totalt) = await _repo.SokFakturaerAsync(request.KundeId, request.Status, request.Side, request.Antall, ct);
        return (data.Select(f => MapToDto(f, f.Kunde)).ToList(), totalt);
    }

    public async Task<KundeInnbetalingDto> RegistrerInnbetalingAsync(RegistrerInnbetalingRequest request, CancellationToken ct = default)
    {
        var faktura = await _repo.HentFakturaAsync(request.KundeFakturaId, ct)
            ?? throw new KundeFakturaIkkeFunnetException(request.KundeFakturaId);

        if (request.Belop <= 0)
            throw new ArgumentException("Belop ma vaere > 0.");

        return await RegistrerInnbetalingInternal(faktura, request.Innbetalingsdato, request.Belop,
            request.Bankreferanse, request.KidNummer, request.Betalingsmetode, false, ct);
    }

    public async Task<KundeInnbetalingDto> MatchKidAsync(MatchKidRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.KidNummer))
            throw new ArgumentException("KidNummer er pakreves.");

        var faktura = await _repo.HentFakturaMedKidAsync(request.KidNummer, ct)
            ?? throw new ArgumentException($"Ingen faktura funnet med KID {request.KidNummer}.");

        return await RegistrerInnbetalingInternal(faktura, request.Innbetalingsdato, request.Belop,
            request.Bankreferanse, request.KidNummer, "Bank", true, ct);
    }

    public async Task<KundeFakturaDto> AvskrivTapAsync(Guid fakturaId, string begrunnelse, CancellationToken ct = default)
    {
        var faktura = await _repo.HentFakturaAsync(fakturaId, ct)
            ?? throw new KundeFakturaIkkeFunnetException(fakturaId);

        // FR-K14: Krever minimum 3 purringer
        if (faktura.AntallPurringer < 3)
            throw new TapAvskrivningException("Minst 3 purringer kreves for tap pa fordringer.");

        if (faktura.GjenstaendeBelop.Verdi <= 0)
            throw new TapAvskrivningException("Fakturaen har ingen utstaaende belop.");

        // FR-K14: Opprett bilag for tap pa fordringer
        var tapBelop = faktura.GjenstaendeBelop.Verdi;
        var tapPosteringer = new List<OpprettPosteringRequest>
        {
            new("7830", BokforingSide.Debet, tapBelop, $"Tap pa fordringer faktura #{faktura.Fakturanummer}",
                null, null, null, KundeId: faktura.KundeId, LeverandorId: null),
            new("1500", BokforingSide.Kredit, tapBelop, $"Tap pa fordringer faktura #{faktura.Fakturanummer}",
                null, null, null, KundeId: faktura.KundeId, LeverandorId: null)
        };

        // Tilbakeforing av utgaende MVA hvis faktura har MVA
        if (faktura.MvaBelop.Verdi > 0)
        {
            tapPosteringer.Add(new OpprettPosteringRequest(
                "2700", BokforingSide.Debet, faktura.MvaBelop.Verdi,
                $"Tilbakeforing utg. MVA tap faktura #{faktura.Fakturanummer}",
                null, null, null, KundeId: faktura.KundeId, LeverandorId: null));
            tapPosteringer.Add(new OpprettPosteringRequest(
                "7830", BokforingSide.Kredit, faktura.MvaBelop.Verdi,
                $"Tilbakeforing utg. MVA tap faktura #{faktura.Fakturanummer}",
                null, null, null, KundeId: faktura.KundeId, LeverandorId: null));
        }

        var tapBilagRequest = new OpprettBilagRequest(
            BilagType.Manuelt,
            DateOnly.FromDateTime(DateTime.UtcNow),
            $"Tap pa fordringer {faktura.Kunde.Kundenummer} faktura #{faktura.Fakturanummer}",
            null,
            "UF",
            tapPosteringer,
            BokforDirekte: true);

        var tapBilag = await _bilagService.OpprettOgBokforBilagAsync(tapBilagRequest, ct);

        faktura.Status = KundeFakturaStatus.Tap;
        faktura.GjenstaendeBelop = Belop.Null;

        await _repo.OppdaterFakturaAsync(faktura, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapToDto(faktura, faktura.Kunde);
    }

    public async Task<List<KundeFakturaDto>> HentApnePosterAsync(DateOnly? dato = null, CancellationToken ct = default)
    {
        var poster = await _repo.HentApnePosterAsync(dato, ct);
        return poster.Select(f => MapToDto(f, f.Kunde)).ToList();
    }

    public async Task<KundeAldersfordelingDto> HentAldersfordelingAsync(DateOnly dato, CancellationToken ct = default)
    {
        var poster = await _repo.HentApnePosterAsync(dato, ct);

        var kundeGrupper = poster
            .GroupBy(f => new { f.KundeId, f.Kunde.Kundenummer, f.Kunde.Navn })
            .Select(g =>
            {
                var ikkeForfalt = g.Where(f => f.HentAlderskategori(dato) == Alderskategori.IkkeForfalt).Sum(f => f.GjenstaendeBelop.Verdi);
                var d0t30 = g.Where(f => f.HentAlderskategori(dato) == Alderskategori.Dager0Til30).Sum(f => f.GjenstaendeBelop.Verdi);
                var d31t60 = g.Where(f => f.HentAlderskategori(dato) == Alderskategori.Dager31Til60).Sum(f => f.GjenstaendeBelop.Verdi);
                var d61t90 = g.Where(f => f.HentAlderskategori(dato) == Alderskategori.Dager61Til90).Sum(f => f.GjenstaendeBelop.Verdi);
                var over90 = g.Where(f => f.HentAlderskategori(dato) == Alderskategori.Over90Dager).Sum(f => f.GjenstaendeBelop.Verdi);

                return new AldersfordelingKundeDto(
                    g.Key.KundeId,
                    g.Key.Kundenummer,
                    g.Key.Navn,
                    ikkeForfalt, d0t30, d31t60, d61t90, over90,
                    ikkeForfalt + d0t30 + d31t60 + d61t90 + over90);
            })
            .ToList();

        var totalt = new AldersfordelingSummaryDto(
            kundeGrupper.Sum(k => k.IkkeForfalt),
            kundeGrupper.Sum(k => k.Dager0Til30),
            kundeGrupper.Sum(k => k.Dager31Til60),
            kundeGrupper.Sum(k => k.Dager61Til90),
            kundeGrupper.Sum(k => k.Over90Dager),
            kundeGrupper.Sum(k => k.Totalt));

        return new KundeAldersfordelingDto(kundeGrupper, totalt, dato);
    }

    public async Task<KundeutskriftDto> HentUtskriftAsync(Guid kundeId, DateOnly fraDato, DateOnly tilDato, CancellationToken ct = default)
    {
        var kunde = await _repo.HentKundeAsync(kundeId, ct)
            ?? throw new KundeIkkeFunnetException(kundeId);

        var fakturaer = await _repo.HentFakturaerForKundeAsync(kundeId, fraDato, tilDato, ct);

        // Beregn inngaaende saldo (fakturaer for fraDato)
        var alleFakturaer = await _repo.HentFakturaerForKundeAsync(kundeId, null, null, ct);
        var inngaaendeSaldo = alleFakturaer
            .Where(f => f.Fakturadato < fraDato)
            .Sum(f => f.GjenstaendeBelop.Verdi);

        var linjer = new List<KundeutskriftLinjeDto>();
        var lopendeSaldo = inngaaendeSaldo;

        foreach (var f in fakturaer.OrderBy(f => f.Fakturadato).ThenBy(f => f.Fakturanummer))
        {
            decimal? debet = null;
            decimal? kredit = null;

            if (f.Type == KundeTransaksjonstype.Faktura || f.Type == KundeTransaksjonstype.Purregebyr)
            {
                debet = f.BelopInklMva.Verdi;
                lopendeSaldo += f.BelopInklMva.Verdi;
            }
            else if (f.Type == KundeTransaksjonstype.Kreditnota)
            {
                kredit = f.BelopInklMva.Verdi;
                lopendeSaldo -= f.BelopInklMva.Verdi;
            }

            linjer.Add(new KundeutskriftLinjeDto(
                f.Fakturadato,
                f.BilagId.HasValue ? $"BIL-{f.BilagId.Value.ToString()[..8]}" : "",
                f.Beskrivelse,
                f.Type,
                debet,
                kredit,
                lopendeSaldo,
                f.Fakturanummer,
                f.KidNummer));

            // Innbetalinger for denne fakturaen
            foreach (var inn in f.Innbetalinger.Where(i => i.Innbetalingsdato >= fraDato && i.Innbetalingsdato <= tilDato).OrderBy(i => i.Innbetalingsdato))
            {
                lopendeSaldo -= inn.Belop.Verdi;
                linjer.Add(new KundeutskriftLinjeDto(
                    inn.Innbetalingsdato,
                    inn.BilagId.HasValue ? $"BIL-{inn.BilagId.Value.ToString()[..8]}" : "",
                    $"Innbetaling faktura #{f.Fakturanummer}",
                    KundeTransaksjonstype.Innbetaling,
                    null,
                    inn.Belop.Verdi,
                    lopendeSaldo,
                    f.Fakturanummer,
                    inn.KidNummer));
            }
        }

        return new KundeutskriftDto(
            kunde.Id,
            kunde.Kundenummer,
            kunde.Navn,
            inngaaendeSaldo,
            linjer,
            lopendeSaldo,
            fraDato,
            tilDato);
    }

    // --- Hjelpemetoder ---

    private async Task<KundeInnbetalingDto> RegistrerInnbetalingInternal(
        KundeFaktura faktura,
        DateOnly innbetalingsdato,
        decimal belop,
        string? bankreferanse,
        string? kidNummer,
        string betalingsmetode,
        bool erAutoMatchet,
        CancellationToken ct)
    {
        var innbetaling = new KundeInnbetaling
        {
            Id = Guid.NewGuid(),
            KundeFakturaId = faktura.Id,
            Innbetalingsdato = innbetalingsdato,
            Belop = new Belop(belop),
            Bankreferanse = bankreferanse,
            KidNummer = kidNummer,
            ErAutoMatchet = erAutoMatchet,
            Betalingsmetode = betalingsmetode
        };

        // Oppdater gjenstaaende belop (FR-K05)
        faktura.GjenstaendeBelop = faktura.GjenstaendeBelop - new Belop(belop);

        if (faktura.GjenstaendeBelop.Verdi <= 0)
            faktura.Status = KundeFakturaStatus.Betalt;
        else
            faktura.Status = KundeFakturaStatus.DelvisBetalt;

        // FR-K05: Opprett bilag for innbetaling (Debet 1920 Bank, Kredit 1500 Kundefordringer)
        var innbetalingPosteringer = new List<OpprettPosteringRequest>
        {
            new("1920", BokforingSide.Debet, belop, $"Innbetaling faktura #{faktura.Fakturanummer}",
                null, null, null, KundeId: faktura.KundeId, LeverandorId: null),
            new("1500", BokforingSide.Kredit, belop, $"Innbetaling faktura #{faktura.Fakturanummer}",
                null, null, null, KundeId: faktura.KundeId, LeverandorId: null)
        };

        var innbetalingBilagRequest = new OpprettBilagRequest(
            BilagType.Bank,
            innbetalingsdato,
            $"Innbetaling {faktura.Kunde.Kundenummer} faktura #{faktura.Fakturanummer}",
            bankreferanse,
            "IB", // Innbetaling serie
            innbetalingPosteringer,
            BokforDirekte: true);

        var innbetalingBilag = await _bilagService.OpprettOgBokforBilagAsync(innbetalingBilagRequest, ct);
        innbetaling.BilagId = innbetalingBilag.Id;

        await _repo.LeggTilInnbetalingAsync(innbetaling, ct);
        await _repo.OppdaterFakturaAsync(faktura, ct);
        await _repo.LagreEndringerAsync(ct);

        return new KundeInnbetalingDto(
            innbetaling.Id,
            innbetaling.KundeFakturaId,
            faktura.Fakturanummer,
            innbetaling.Innbetalingsdato,
            innbetaling.Belop.Verdi,
            innbetaling.Bankreferanse,
            innbetaling.KidNummer,
            innbetaling.ErAutoMatchet,
            innbetaling.Betalingsmetode);
    }

    public static DateOnly BeregnForfallsdato(DateOnly fakturadato, KundeBetalingsbetingelse betingelse, int? egendefinert)
    {
        var dager = betingelse switch
        {
            KundeBetalingsbetingelse.Netto10 => 10,
            KundeBetalingsbetingelse.Netto14 => 14,
            KundeBetalingsbetingelse.Netto20 => 20,
            KundeBetalingsbetingelse.Netto30 => 30,
            KundeBetalingsbetingelse.Netto45 => 45,
            KundeBetalingsbetingelse.Netto60 => 60,
            KundeBetalingsbetingelse.Kontant => 0,
            KundeBetalingsbetingelse.Forskudd => 0,
            KundeBetalingsbetingelse.Egendefinert => egendefinert ?? 30,
            _ => 30
        };

        return fakturadato.AddDays(dager);
    }

    public static DateOnly FlyttFraHelg(DateOnly dato)
    {
        var dayOfWeek = dato.DayOfWeek;
        if (dayOfWeek == DayOfWeek.Saturday)
            return dato.AddDays(2);
        if (dayOfWeek == DayOfWeek.Sunday)
            return dato.AddDays(1);
        return dato;
    }

    /// <summary>
    /// Hent MVA-sats fra MVA-kode. Forenklet - i produksjon brukes MvaKode-entitet.
    /// </summary>
    private static decimal HentMvaSats(string? mvaKode)
    {
        // TODO: Hent fra MvaKode-entitet via repository
        return mvaKode switch
        {
            "3" => 25m,
            "31" => 15m,
            "33" => 12m,
            "5" or "6" or "0" => 0m,
            null => 0m,
            _ => 0m
        };
    }

    /// <summary>
    /// FR-K01/FR-K03: Bygg bilag posteringer for utgaende faktura/kreditnota.
    /// Faktura: Debet 1500 Kundefordringer, Kredit 3xxx Salgsinntekt, Kredit 2700 Utg. MVA
    /// Kreditnota: Speilvendt av faktura.
    /// </summary>
    internal static List<OpprettPosteringRequest> ByggFakturaBilagPosteringer(
        KundeTransaksjonstype type,
        List<KundeFakturaLinje> linjer,
        Belop totalInklMva,
        Guid kundeId,
        string beskrivelse)
    {
        var posteringer = new List<OpprettPosteringRequest>();
        var erKreditnota = type == KundeTransaksjonstype.Kreditnota;

        // Inntektsposteringer per linje (kredit for faktura, debet for kreditnota)
        foreach (var linje in linjer)
        {
            posteringer.Add(new OpprettPosteringRequest(
                linje.Kontonummer.Length > 0 ? linje.Kontonummer : "3000", // fallback salgsinntekt
                erKreditnota ? BokforingSide.Debet : BokforingSide.Kredit,
                linje.Belop.Verdi,
                linje.Beskrivelse,
                linje.MvaKode,
                linje.Avdelingskode,
                linje.Prosjektkode,
                KundeId: kundeId,
                LeverandorId: null));

            // MVA-postering per linje
            if (linje.MvaBelop.HasValue && linje.MvaBelop.Value.Verdi > 0)
            {
                posteringer.Add(new OpprettPosteringRequest(
                    HentMvaKonto(linje.MvaKode),
                    erKreditnota ? BokforingSide.Debet : BokforingSide.Kredit,
                    linje.MvaBelop.Value.Verdi,
                    $"Utg. MVA {linje.MvaSats}%",
                    null,
                    null,
                    null,
                    KundeId: kundeId,
                    LeverandorId: null));
            }
        }

        // Kundefordringer-postering (1500) - debet for faktura, kredit for kreditnota
        posteringer.Add(new OpprettPosteringRequest(
            "1500", // Kundefordringer
            erKreditnota ? BokforingSide.Kredit : BokforingSide.Debet,
            totalInklMva.Verdi,
            beskrivelse,
            null,
            null,
            null,
            KundeId: kundeId,
            LeverandorId: null));

        return posteringer;
    }

    /// <summary>
    /// Hent MVA-konto basert pa MvaKode for utgaende MVA.
    /// </summary>
    private static string HentMvaKonto(string? mvaKode)
    {
        return mvaKode switch
        {
            "3" => "2700",  // Utg. MVA 25%
            "31" => "2710", // Utg. MVA 15%
            "33" => "2710", // Utg. MVA 12%
            _ => "2700"     // Fallback
        };
    }

    internal static KundeFakturaDto MapToDto(KundeFaktura f, Kunde kunde) => new(
        f.Id,
        f.KundeId,
        kunde.Kundenummer,
        kunde.Navn,
        f.Fakturanummer,
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
        f.EksternReferanse,
        f.AntallPurringer);
}
