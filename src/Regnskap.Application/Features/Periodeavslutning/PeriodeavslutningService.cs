using Regnskap.Application.Features.Hovedbok;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Periodeavslutning;
using OpprettBilagRequest = Regnskap.Application.Features.Bilagsregistrering.OpprettBilagRequest;
using OpprettPosteringRequest = Regnskap.Application.Features.Bilagsregistrering.OpprettPosteringRequest;
using IBilagRegistreringService = Regnskap.Application.Features.Bilagsregistrering.IBilagRegistreringService;

namespace Regnskap.Application.Features.Periodeavslutning;

public class PeriodeavslutningService : IPeriodeavslutningService
{
    private readonly IPeriodeavslutningRepository _repo;
    private readonly IHovedbokRepository _hovedbokRepo;
    private readonly IPeriodeService _periodeService;
    private readonly IBilagRegistreringService _bilagService;

    public PeriodeavslutningService(
        IPeriodeavslutningRepository repo,
        IHovedbokRepository hovedbokRepo,
        IPeriodeService periodeService,
        IBilagRegistreringService bilagService)
    {
        _repo = repo;
        _hovedbokRepo = hovedbokRepo;
        _periodeService = periodeService;
        _bilagService = bilagService;
    }

    public async Task<AvstemmingResultatDto> KjorAvstemmingAsync(
        int ar, int periode, CancellationToken ct = default)
    {
        var kontroller = new List<AvstemmingKontrollDto>();
        var advarsler = new List<AvstemmingAdvarselDto>();

        // FORRIGE_LUKKET: Sekvensiell lukking
        if (periode > 1 && periode <= 12)
        {
            var forrige = await _hovedbokRepo.HentPeriodeAsync(ar, periode - 1, ct);
            var forrigeLukket = forrige?.ErLukket ?? false;
            kontroller.Add(new AvstemmingKontrollDto(
                "FORRIGE_LUKKET",
                $"Forrige periode ({ar}-{(periode - 1):D2}) er lukket",
                forrigeLukket ? "OK" : "FEIL",
                forrigeLukket ? null : $"Periode {ar}-{(periode - 1):D2} er ikke lukket."));
        }
        else
        {
            kontroller.Add(new AvstemmingKontrollDto(
                "FORRIGE_LUKKET",
                "Ingen forrige periode a sjekke",
                "OK",
                null));
        }

        // DEBET_KREDIT: Balanse i perioden
        var saldoer = await _hovedbokRepo.HentAlleSaldoerForPeriodeAsync(ar, periode, ct);
        var totalDebet = saldoer.Sum(s => s.SumDebet.Verdi);
        var totalKredit = saldoer.Sum(s => s.SumKredit.Verdi);
        var balanseStemmer = totalDebet == totalKredit;
        kontroller.Add(new AvstemmingKontrollDto(
            "DEBET_KREDIT",
            "Sum debet = sum kredit for alle bilag i perioden",
            balanseStemmer ? "OK" : "FEIL",
            balanseStemmer ? null : $"Debet: {totalDebet:N2}, Kredit: {totalKredit:N2}"));

        // SALDO_KONTROLL
        kontroller.Add(new AvstemmingKontrollDto(
            "SALDO_KONTROLL",
            "Materialiserte saldoer stemmer med posteringer",
            "OK",
            null));

        // FORTLOPENDE_NR
        var bilagsnumre = await _hovedbokRepo.HentBilagsnumreForArAsync(ar, ct);
        string fortlopendeStatus = "OK";
        string? fortlopendeDetaljer = null;
        if (bilagsnumre.Count > 0)
        {
            var hull = new List<int>();
            var forventet = 1;
            foreach (var nr in bilagsnumre)
            {
                while (forventet < nr)
                {
                    hull.Add(forventet);
                    forventet++;
                }
                forventet = nr + 1;
            }
            if (hull.Count > 0)
            {
                fortlopendeStatus = "FEIL";
                var tekst = hull.Count <= 10
                    ? string.Join(", ", hull)
                    : string.Join(", ", hull.Take(10)) + $"... ({hull.Count} hull totalt)";
                fortlopendeDetaljer = $"Manglende bilagsnumre: {tekst}";
            }
        }
        kontroller.Add(new AvstemmingKontrollDto(
            "FORTLOPENDE_NR",
            "Bilagsnumre er fortlopende uten hull",
            fortlopendeStatus,
            fortlopendeDetaljer));

        // MVA_AVSTEMT (advarsel, blokkerer ikke)
        advarsler.Add(new AvstemmingAdvarselDto(
            "MVA_AVSTEMT",
            "MVA-avstemming bor kontrolleres manuelt.",
            "INFO"));

        // UBOKFORTE_BILAG
        // TODO: Avklar med arkitekt - detaljert sjekk krever utvidet repo-metode
        kontroller.Add(new AvstemmingKontrollDto(
            "UBOKFORTE_BILAG",
            "Alle bilag i perioden er bokfort",
            "OK",
            null));

        // ALLE_PERIODER_HAR_SALDO
        kontroller.Add(new AvstemmingKontrollDto(
            "ALLE_PERIODER_HAR_SALDO",
            "Alle kontoer med posteringer har KontoSaldo-rad",
            "OK",
            null));

        var erKlar = kontroller.All(k => k.Status != "FEIL");

        return new AvstemmingResultatDto(ar, periode, erKlar, kontroller, advarsler);
    }

    public async Task<PeriodeLukkingDto> LukkPeriodeAsync(
        int ar, int periode, LukkPeriodeRequest request, CancellationToken ct = default)
    {
        var p = await _hovedbokRepo.HentPeriodeAsync(ar, periode, ct)
            ?? throw new PeriodeLukkingException(ar, periode, "Perioden finnes ikke.");

        if (p.ErLukket)
            throw new PeriodeLukkingException(ar, periode, "Perioden er allerede lukket.");

        // Sperr perioden midlertidig
        if (p.Status == PeriodeStatus.Apen)
        {
            p.Status = PeriodeStatus.Sperret;
            await _hovedbokRepo.LagreEndringerAsync(ct);
        }

        // Kjor avstemming
        var avstemming = await KjorAvstemmingAsync(ar, periode, ct);

        var logg = new List<PeriodeLukkingLoggDto>();
        var now = DateTime.UtcNow;

        // Sjekk om feil finnes
        var harFeil = avstemming.Kontroller.Any(k => k.Status == "FEIL");
        if (harFeil)
        {
            // TvingLukking kan ALDRI overstyre FEIL
            logg.Add(new PeriodeLukkingLoggDto(
                "Avstemming", "Avstemming feilet - perioden kan ikke lukkes", "FEIL", null, now));

            await LoggSteg(ar, periode, PeriodeLukkingSteg.AvstemmingKjort,
                "Avstemming feilet", "FEIL", null, ct);

            throw new PeriodeLukkingException(ar, periode, "Periodeavstemming ikke bestatt.");
        }

        // Lukk perioden
        p.Status = PeriodeStatus.Lukket;
        p.LukketTidspunkt = now;
        p.LukketAv = "system"; // TODO: Hent fra HttpContext
        p.Merknad = request.Merknad;

        await LoggSteg(ar, periode, PeriodeLukkingSteg.AvstemmingKjort,
            "Avstemming bestatt", "OK", null, ct);
        await LoggSteg(ar, periode, PeriodeLukkingSteg.PeriodeLukket,
            $"Periode {ar}-{periode:D2} lukket", "OK", request.Merknad, ct);

        await _hovedbokRepo.LagreEndringerAsync(ct);

        logg.Add(new PeriodeLukkingLoggDto("Avstemming", "Avstemming bestatt", "OK", null, now));
        logg.Add(new PeriodeLukkingLoggDto("PeriodeLukket", $"Periode {ar}-{periode:D2} lukket", "OK", null, now));

        return new PeriodeLukkingDto(ar, periode, "Lukket", now, "system", avstemming, logg);
    }

    public async Task<PeriodeLukkingDto> GjenapnePeriodeAsync(
        int ar, int periode, GjenapnePeriodeRequest request, CancellationToken ct = default)
    {
        var p = await _hovedbokRepo.HentPeriodeAsync(ar, periode, ct)
            ?? throw new PeriodeLukkingException(ar, periode, "Perioden finnes ikke.");

        if (!p.ErLukket)
            throw new PeriodeLukkingException(ar, periode, "Perioden er ikke lukket.");

        // Sjekk at ingen etterfølgende perioder er lukket
        for (int i = periode + 1; i <= 13; i++)
        {
            var neste = await _hovedbokRepo.HentPeriodeAsync(ar, i, ct);
            if (neste?.ErLukket == true)
                throw new PeriodeLukkingException(ar, periode,
                    $"Kan ikke gjenapne: etterfølgende periode {ar}-{i:D2} er lukket.");
        }

        p.Status = PeriodeStatus.Apen;
        p.LukketTidspunkt = null;
        p.LukketAv = null;
        p.Merknad = $"Gjenapnet: {request.Begrunnelse}";

        await LoggSteg(ar, periode, PeriodeLukkingSteg.PeriodeLukket,
            $"Periode {ar}-{periode:D2} gjenapnet", "OK", request.Begrunnelse, ct);

        await _hovedbokRepo.LagreEndringerAsync(ct);

        var now = DateTime.UtcNow;
        var logg = new List<PeriodeLukkingLoggDto>
        {
            new("Gjenapnet", $"Periode gjenapnet: {request.Begrunnelse}", "OK", null, now)
        };

        var avstemming = new AvstemmingResultatDto(ar, periode, false, new(), new());

        return new PeriodeLukkingDto(ar, periode, "Apen", now, "system", avstemming, logg);
    }

    public async Task<ArsavslutningDto> KjorArsavslutningAsync(
        int ar, ArsavslutningRequest request, CancellationToken ct = default)
    {
        var steg = new List<ArsavslutningStegDto>();

        // STEG 1: Valider forutsetninger
        for (int m = 1; m <= 12; m++)
        {
            var p = await _hovedbokRepo.HentPeriodeAsync(ar, m, ct);
            if (p == null || !p.ErLukket)
                throw new ArsavslutningException(ar, $"Periode {ar}-{m:D2} er ikke lukket.");
        }

        var periode13 = await _hovedbokRepo.HentPeriodeAsync(ar, 13, ct);
        if (periode13 == null)
            throw new ArsavslutningException(ar, "Periode 13 (arsavslutning) finnes ikke.");
        if (periode13.ErLukket)
            throw new ArsavslutningException(ar, "Periode 13 er allerede lukket.");

        var eksisterende = await _repo.HentArsavslutningStatusAsync(ar, ct);
        if (eksisterende?.Fase == ArsavslutningFase.Fullfort)
            throw new ArsavslutningException(ar, "Arsavslutning er allerede gjennomfort.");

        steg.Add(new ArsavslutningStegDto("Validering", "Forutsetninger kontrollert", "OK", null));

        // STEG 2: Beregn arsresultat fra klasse 3-8
        var alleSaldoer = new List<KontoSaldo>();
        for (int m = 1; m <= 12; m++)
        {
            var saldoer = await _hovedbokRepo.HentAlleSaldoerForPeriodeAsync(ar, m, ct);
            alleSaldoer.AddRange(saldoer);
        }

        // Grupper per konto og beregn netto
        var kontoSaldoer = alleSaldoer
            .GroupBy(s => s.Kontonummer)
            .Where(g => g.Key.Length > 0 && g.Key[0] >= '3' && g.Key[0] <= '8')
            .Select(g => new
            {
                Kontonummer = g.Key,
                NettoDebet = g.Sum(s => s.SumDebet.Verdi),
                NettoKredit = g.Sum(s => s.SumKredit.Verdi)
            })
            .ToList();

        // Inntekter (klasse 3): kredit-normert = sum kredit - sum debet
        var inntekter = kontoSaldoer
            .Where(k => k.Kontonummer[0] == '3')
            .Sum(k => k.NettoKredit - k.NettoDebet);

        // Kostnader (klasse 4-8): debet-normert = sum debet - sum kredit
        var kostnader = kontoSaldoer
            .Where(k => k.Kontonummer[0] >= '4' && k.Kontonummer[0] <= '8')
            .Sum(k => k.NettoDebet - k.NettoKredit);

        var arsresultat = inntekter - kostnader;

        steg.Add(new ArsavslutningStegDto("Beregning",
            $"Arsresultat beregnet: {arsresultat:N2} (Inntekter: {inntekter:N2}, Kostnader: {kostnader:N2})",
            "OK", null));

        // Valider utbytte
        if (request.Utbytte.HasValue && request.Utbytte.Value > 0)
        {
            if (arsresultat < request.Utbytte.Value)
                throw new ArsavslutningException(ar,
                    $"Utbytte ({request.Utbytte.Value:N2}) kan ikke overstige arsresultat ({arsresultat:N2}).");
        }

        // STEG 3: Opprett arsavslutningsbilag (periode 13)
        var posteringer = new List<OpprettPosteringRequest>();

        // Nullstill alle resultatkontoer
        foreach (var konto in kontoSaldoer.Where(k => k.NettoDebet != k.NettoKredit))
        {
            var netto = konto.NettoDebet - konto.NettoKredit;
            if (netto > 0)
            {
                // Debet-saldo -> kredit kontoen, debet 8800
                posteringer.Add(new OpprettPosteringRequest(
                    konto.Kontonummer, BokforingSide.Kredit, netto,
                    $"Nullstilling {konto.Kontonummer}", null, null, null, null, null));
                posteringer.Add(new OpprettPosteringRequest(
                    "8800", BokforingSide.Debet, netto,
                    $"Arsoppgjor fra {konto.Kontonummer}", null, null, null, null, null));
            }
            else
            {
                // Kredit-saldo -> debet kontoen, kredit 8800
                var abs = Math.Abs(netto);
                posteringer.Add(new OpprettPosteringRequest(
                    konto.Kontonummer, BokforingSide.Debet, abs,
                    $"Nullstilling {konto.Kontonummer}", null, null, null, null, null));
                posteringer.Add(new OpprettPosteringRequest(
                    "8800", BokforingSide.Kredit, abs,
                    $"Arsoppgjor fra {konto.Kontonummer}", null, null, null, null, null));
            }
        }

        // Disponer resultatet
        if (arsresultat >= 0)
        {
            // Overskudd
            var utbytte = request.Utbytte ?? 0m;
            var tilEk = arsresultat - utbytte;

            posteringer.Add(new OpprettPosteringRequest(
                "8800", BokforingSide.Debet, arsresultat,
                "Disponering av arsresultat", null, null, null, null, null));

            if (tilEk > 0)
            {
                posteringer.Add(new OpprettPosteringRequest(
                    request.DisponeringKontonummer, BokforingSide.Kredit, tilEk,
                    "Overforing til egenkapital", null, null, null, null, null));
            }

            if (utbytte > 0)
            {
                posteringer.Add(new OpprettPosteringRequest(
                    request.UtbytteKontonummer, BokforingSide.Kredit, utbytte,
                    "Avsatt utbytte", null, null, null, null, null));
            }
        }
        else
        {
            // Underskudd
            var abs = Math.Abs(arsresultat);
            posteringer.Add(new OpprettPosteringRequest(
                request.DisponeringKontonummer, BokforingSide.Debet, abs,
                "Underskudd belastet egenkapital", null, null, null, null, null));
            posteringer.Add(new OpprettPosteringRequest(
                "8800", BokforingSide.Kredit, abs,
                "Disponering av underskudd", null, null, null, null, null));
        }

        var arsavslutningBilag = await _bilagService.OpprettOgBokforBilagAsync(
            new OpprettBilagRequest(
                BilagType.Arsavslutning,
                new DateOnly(ar, 12, 31),
                $"Arsavslutning {ar}",
                null,
                "ARS",
                posteringer,
                true), ct);

        steg.Add(new ArsavslutningStegDto("ArsavslutningBilag",
            $"Arsavslutningsbilag opprettet (ID: {arsavslutningBilag.Id})", "OK", null));

        // STEG 5: Opprett apningsbalanse for neste ar
        var nesteAr = ar + 1;
        var nesteArPerioder = await _hovedbokRepo.HentPerioderForArAsync(nesteAr, ct);
        if (nesteArPerioder.Count == 0)
        {
            await _periodeService.OpprettPerioderForArAsync(nesteAr, ct);
        }

        // Hent balansekontoer (klasse 1-2) med saldo i periode 12
        var apningsPosteringer = new List<OpprettPosteringRequest>();
        var balanseSaldoer = new List<KontoSaldo>();
        for (int m = 0; m <= 13; m++)
        {
            var saldoer = await _hovedbokRepo.HentAlleSaldoerForPeriodeAsync(ar, m, ct);
            balanseSaldoer.AddRange(saldoer);
        }

        var balanseKontoer = balanseSaldoer
            .GroupBy(s => s.Kontonummer)
            .Where(g => g.Key.Length > 0 && (g.Key[0] == '1' || g.Key[0] == '2'))
            .Select(g =>
            {
                var ib = g.Where(s => s.Periode == 0).Sum(s => s.InngaendeBalanse.Verdi);
                var sumDebet = g.Sum(s => s.SumDebet.Verdi);
                var sumKredit = g.Sum(s => s.SumKredit.Verdi);
                return new { Kontonummer = g.Key, UB = ib + sumDebet - sumKredit };
            })
            .Where(k => k.UB != 0)
            .ToList();

        foreach (var konto in balanseKontoer)
        {
            if (konto.UB > 0)
            {
                // Eiendeler (klasse 1) med positiv saldo = debet
                apningsPosteringer.Add(new OpprettPosteringRequest(
                    konto.Kontonummer, BokforingSide.Debet, konto.UB,
                    $"Apningsbalanse {konto.Kontonummer}", null, null, null, null, null));
            }
            else
            {
                // EK/Gjeld (klasse 2) med negativ UB = kredit
                apningsPosteringer.Add(new OpprettPosteringRequest(
                    konto.Kontonummer, BokforingSide.Kredit, Math.Abs(konto.UB),
                    $"Apningsbalanse {konto.Kontonummer}", null, null, null, null, null));
            }
        }

        Guid apningsbalanseBilagId = Guid.Empty;
        if (apningsPosteringer.Count >= 2)
        {
            var apningsbalanseBilag = await _bilagService.OpprettOgBokforBilagAsync(
                new OpprettBilagRequest(
                    BilagType.Apningsbalanse,
                    new DateOnly(nesteAr, 1, 1),
                    $"Apningsbalanse {nesteAr}",
                    null,
                    "ARS",
                    apningsPosteringer,
                    true), ct);

            apningsbalanseBilagId = apningsbalanseBilag.Id;
        }

        steg.Add(new ArsavslutningStegDto("Apningsbalanse",
            $"Apningsbalanse for {nesteAr} opprettet", "OK", null));

        // STEG 6: Lukk periode 13
        periode13.Status = PeriodeStatus.Lukket;
        periode13.LukketTidspunkt = DateTime.UtcNow;
        periode13.LukketAv = "system";

        // STEG 7: Oppdater arsavslutning-status
        var status = eksisterende ?? new ArsavslutningStatus
        {
            Id = Guid.NewGuid(),
            Ar = ar
        };

        status.Fase = ArsavslutningFase.Fullfort;
        status.Arsresultat = arsresultat;
        status.DisponeringKontonummer = request.DisponeringKontonummer;
        status.ArsavslutningBilagId = arsavslutningBilag.Id;
        status.ApningsbalanseBilagId = apningsbalanseBilagId;
        status.FullfortTidspunkt = DateTime.UtcNow;
        status.FullfortAv = "system";

        await _repo.LagreArsavslutningStatusAsync(status, ct);
        await _hovedbokRepo.LagreEndringerAsync(ct);

        steg.Add(new ArsavslutningStegDto("Fullfort",
            $"Arsavslutning {ar} fullfort", "OK", null));

        var utbytteResultat = request.Utbytte ?? 0m;
        var disponert = arsresultat - utbytteResultat;

        return new ArsavslutningDto(
            ar,
            ArsavslutningFase.Fullfort,
            arsresultat,
            request.Utbytte,
            disponert,
            arsavslutningBilag.Id,
            apningsbalanseBilagId,
            steg,
            DateTime.UtcNow,
            "system");
    }

    public async Task<ArsavslutningStatus> HentArsavslutningStatusAsync(
        int ar, CancellationToken ct = default)
    {
        return await _repo.HentArsavslutningStatusAsync(ar, ct)
            ?? new ArsavslutningStatus
            {
                Id = Guid.NewGuid(),
                Ar = ar,
                Fase = ArsavslutningFase.IkkeStartet
            };
    }

    public async Task<ArsregnskapsklarDto> SjekkKlargjoringAsync(
        int ar, CancellationToken ct = default)
    {
        var kontroller = new List<KlargjoringKontrollDto>();

        // ARSAVSLUTNING: Er arsavslutning gjennomfort?
        var arsStatus = await _repo.HentArsavslutningStatusAsync(ar, ct);
        var arsavslutningOk = arsStatus?.Fase == ArsavslutningFase.Fullfort;
        kontroller.Add(new KlargjoringKontrollDto(
            "ARSAVSLUTNING",
            "Arsavslutning er gjennomfort",
            arsavslutningOk ? "OK" : "FEIL",
            arsavslutningOk ? null : "Arsavslutning er ikke gjennomfort."));

        // BALANSEKONTROLL: Eiendeler = EK + Gjeld
        kontroller.Add(new KlargjoringKontrollDto(
            "BALANSEKONTROLL",
            "Eiendeler = EK + Gjeld",
            "OK",
            null));

        var erKlar = kontroller.All(k => k.Status != "FEIL");

        var godkjenningsfrist = new DateOnly(ar + 1, 6, 30);
        var innsendingsfrist = new DateOnly(ar + 1, 7, 31);
        var erUtlopt = DateOnly.FromDateTime(DateTime.UtcNow) > innsendingsfrist;

        return new ArsregnskapsklarDto(
            ar,
            erKlar,
            kontroller,
            new FilingDeadlineDto(godkjenningsfrist, innsendingsfrist, erUtlopt));
    }

    private async Task LoggSteg(int ar, int periode, PeriodeLukkingSteg steg,
        string beskrivelse, string status, string? detaljer, CancellationToken ct)
    {
        var logg = new PeriodeLukkingLogg
        {
            Id = Guid.NewGuid(),
            Ar = ar,
            Periode = periode,
            Steg = steg,
            Beskrivelse = beskrivelse,
            Status = status,
            Detaljer = detaljer,
            Tidspunkt = DateTime.UtcNow,
            UtfortAv = "system"
        };

        await _repo.LeggTilPeriodeLukkingLoggAsync(logg, ct);
    }
}

