using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Regnskap.Domain.Features.Rapportering;

namespace Regnskap.Application.Features.Rapportering;

public class RapporteringService : IRapporteringService
{
    private readonly IRapporteringRepository _repo;

    public RapporteringService(IRapporteringRepository repo)
    {
        _repo = repo;
    }

    // --- Resultatregnskap (Regnskapsloven 3-2) ---

    public async Task<ResultatregnskapDto> GenererResultatregnskapAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        ResultatregnskapFormat format = ResultatregnskapFormat.Artsinndelt,
        bool inkluderForrigeAr = true,
        CancellationToken ct = default)
    {
        // Hent saldoer for resultatkontoer (kontoklasse 3-8)
        var saldoer = await _repo.HentAggregerteSaldoerAsync(ar, fraPeriode, tilPeriode, ct: ct);
        var resultatSaldoer = saldoer.Where(s => int.Parse(s.Kontonummer[..1]) >= 3).ToList();

        List<KontoSaldoAggregat>? forrigeSaldoer = null;
        if (inkluderForrigeAr)
        {
            var alle = await _repo.HentAggregerteSaldoerAsync(ar - 1, fraPeriode, tilPeriode, ct: ct);
            forrigeSaldoer = alle.Where(s => int.Parse(s.Kontonummer[..1]) >= 3).ToList();
        }

        var seksjoner = new List<ResultatregnskapSeksjonDto>();

        // FR-R07: Beregning av poster per seksjon
        var driftsinntekter = ByggResultatSeksjon("DRIFTSINNTEKTER", "Driftsinntekter",
            resultatSaldoer, forrigeSaldoer, "3", invertFortegn: true);
        seksjoner.Add(driftsinntekter);

        var varekostnad = ByggResultatSeksjon("VAREKOSTNAD", "Varekostnad",
            resultatSaldoer, forrigeSaldoer, "4", invertFortegn: false);
        seksjoner.Add(varekostnad);

        var lonnskostnad = ByggResultatSeksjon("LONNSKOSTNAD", "Lonnskostnad",
            resultatSaldoer, forrigeSaldoer, "5", invertFortegn: false);
        seksjoner.Add(lonnskostnad);

        var avskrivning = ByggResultatSeksjon("AVSKRIVNING", "Av- og nedskrivning",
            resultatSaldoer, forrigeSaldoer, "60", invertFortegn: false);
        seksjoner.Add(avskrivning);

        var annenDrift = ByggResultatSeksjonRange("ANNEN_DRIFT", "Annen driftskostnad",
            resultatSaldoer, forrigeSaldoer, 61, 79, invertFortegn: false);
        seksjoner.Add(annenDrift);

        var driftsresultat = driftsinntekter.Sum - varekostnad.Sum - lonnskostnad.Sum
                             - avskrivning.Sum - annenDrift.Sum;
        decimal? forrigeDriftsresultat = null;
        if (inkluderForrigeAr)
        {
            forrigeDriftsresultat = (driftsinntekter.ForrigeArSum ?? 0m)
                - (varekostnad.ForrigeArSum ?? 0m) - (lonnskostnad.ForrigeArSum ?? 0m)
                - (avskrivning.ForrigeArSum ?? 0m) - (annenDrift.ForrigeArSum ?? 0m);
        }

        seksjoner.Add(new ResultatregnskapSeksjonDto("DRIFTSRESULTAT", "Driftsresultat",
            new List<ResultatregnskapLinjeDto>
            {
                new("", "Driftsresultat", driftsresultat, forrigeDriftsresultat, true)
            },
            driftsresultat, forrigeDriftsresultat));

        var finansinntekter = ByggResultatSeksjonRange("FINANSINNTEKT", "Finansinntekter",
            resultatSaldoer, forrigeSaldoer, 80, 83, invertFortegn: true);
        seksjoner.Add(finansinntekter);

        var finanskostnader = ByggResultatSeksjonRange("FINANSKOSTNAD", "Finanskostnader",
            resultatSaldoer, forrigeSaldoer, 84, 87, invertFortegn: false);
        seksjoner.Add(finanskostnader);

        var finansNetto = finansinntekter.Sum - finanskostnader.Sum;
        decimal? forrigeFinansNetto = null;
        if (inkluderForrigeAr)
            forrigeFinansNetto = (finansinntekter.ForrigeArSum ?? 0m) - (finanskostnader.ForrigeArSum ?? 0m);

        seksjoner.Add(new ResultatregnskapSeksjonDto("FINANSNETTO", "Netto finans",
            new List<ResultatregnskapLinjeDto>
            {
                new("", "Netto finans", finansNetto, forrigeFinansNetto, true)
            },
            finansNetto, forrigeFinansNetto));

        var resultatForSkatt = driftsresultat + finansNetto;
        decimal? forrigeResultatForSkatt = null;
        if (inkluderForrigeAr)
            forrigeResultatForSkatt = forrigeDriftsresultat + forrigeFinansNetto;

        seksjoner.Add(new ResultatregnskapSeksjonDto("RESULTAT_FOR_SKATT", "Ordinaert resultat for skatt",
            new List<ResultatregnskapLinjeDto>
            {
                new("", "Ordinaert resultat for skatt", resultatForSkatt, forrigeResultatForSkatt, true)
            },
            resultatForSkatt, forrigeResultatForSkatt));

        var skattekostnad = ByggResultatSeksjon("SKATTEKOSTNAD", "Skattekostnad",
            resultatSaldoer, forrigeSaldoer, "89", invertFortegn: false);
        seksjoner.Add(skattekostnad);

        var arsresultat = resultatForSkatt - skattekostnad.Sum;
        decimal? forrigeArsresultat = null;
        if (inkluderForrigeAr)
            forrigeArsresultat = forrigeResultatForSkatt - (skattekostnad.ForrigeArSum ?? 0m);

        seksjoner.Add(new ResultatregnskapSeksjonDto("ARSRESULTAT", "Arsresultat",
            new List<ResultatregnskapLinjeDto>
            {
                new("", "Arsresultat", arsresultat, forrigeArsresultat, true)
            },
            arsresultat, forrigeArsresultat));

        var dto = new ResultatregnskapDto(
            Ar: ar,
            FraPeriode: fraPeriode,
            TilPeriode: tilPeriode,
            Format: format.ToString(),
            Seksjoner: seksjoner,
            Driftsresultat: driftsresultat,
            FinansresultatNetto: finansNetto,
            OrdnaertResultatForSkatt: resultatForSkatt,
            Skattekostnad: skattekostnad.Sum,
            Arsresultat: arsresultat,
            ForrigeArDriftsresultat: forrigeDriftsresultat,
            ForrigeArArsresultat: forrigeArsresultat
        );

        await LoggRapportAsync(RapportType.Resultatregnskap, ar, fraPeriode, tilPeriode, dto, ct);
        return dto;
    }

    // --- Balanse (Regnskapsloven 3-2a) ---

    public async Task<BalanseDto> GenererBalanseAsync(
        int ar, int periode = 12,
        bool inkluderForrigeAr = true,
        CancellationToken ct = default)
    {
        var saldoer = await _repo.HentAggregerteSaldoerAsync(ar, 1, periode, ct: ct);
        List<KontoSaldoAggregat>? forrigeSaldoer = null;
        if (inkluderForrigeAr)
            forrigeSaldoer = await _repo.HentAggregerteSaldoerAsync(ar - 1, 1, 12, ct: ct);

        // Eiendeler (kontoklasse 1)
        var eiendelerSaldoer = saldoer.Where(s => s.Kontonummer.StartsWith("1")).ToList();
        var forrigeEiendeler = forrigeSaldoer?.Where(s => s.Kontonummer.StartsWith("1")).ToList();

        var eiendelerSeksjoner = new List<BalanseSeksjonDto>
        {
            ByggBalanseSeksjon("IMMATR_ANLEGG", "Immaterielle eiendeler", eiendelerSaldoer, forrigeEiendeler, 10, 10),
            ByggBalanseSeksjon("VARIGE_ANLEGG", "Varige driftsmidler", eiendelerSaldoer, forrigeEiendeler, 11, 12),
            ByggBalanseSeksjon("FIN_ANLEGG", "Finansielle anleggsmidler", eiendelerSaldoer, forrigeEiendeler, 13, 13),
            ByggBalanseSeksjon("VARER", "Varer", eiendelerSaldoer, forrigeEiendeler, 14, 14),
            ByggBalanseSeksjon("FORDRINGER", "Fordringer", eiendelerSaldoer, forrigeEiendeler, 15, 17),
            ByggBalanseSeksjon("INVESTERING", "Investeringer", eiendelerSaldoer, forrigeEiendeler, 18, 18),
            ByggBalanseSeksjon("BANK_KONTANT", "Bankinnskudd, kontanter", eiendelerSaldoer, forrigeEiendeler, 19, 19),
        };

        var sumEiendeler = eiendelerSaldoer.Sum(s => s.UtgaendeBalanse);
        decimal? forrigeSumEiendeler = forrigeEiendeler?.Sum(s => s.UtgaendeBalanse);

        var eiendelerSide = new BalanseSideDto(eiendelerSeksjoner, sumEiendeler, forrigeSumEiendeler);

        // EK og gjeld (kontoklasse 2) -- kredit-normert, vises som absoluttverdi
        var ekGjeldSaldoer = saldoer.Where(s => s.Kontonummer.StartsWith("2")).ToList();
        var forrigeEkGjeld = forrigeSaldoer?.Where(s => s.Kontonummer.StartsWith("2")).ToList();

        var ekGjeldSeksjoner = new List<BalanseSeksjonDto>
        {
            ByggBalanseSeksjon("INNSKUTT_EK", "Innskutt egenkapital", ekGjeldSaldoer, forrigeEkGjeld, 20, 20, kredit: true),
            ByggBalanseSeksjon("OPPTJENT_EK", "Opptjent egenkapital", ekGjeldSaldoer, forrigeEkGjeld, 21, 21, kredit: true),
            ByggBalanseSeksjon("LANGSIKTIG_GJELD", "Langsiktig gjeld", ekGjeldSaldoer, forrigeEkGjeld, 22, 23, kredit: true),
            ByggBalanseSeksjon("LEVERANDOR_GJELD", "Leverandorgjeld", ekGjeldSaldoer, forrigeEkGjeld, 24, 24, kredit: true),
            ByggBalanseSeksjon("OFFENTLIG_GJELD", "Skattetrekk, offentlige avgifter", ekGjeldSaldoer, forrigeEkGjeld, 25, 27, kredit: true),
            ByggBalanseSeksjon("ANNEN_KORT_GJELD", "Annen kortsiktig gjeld", ekGjeldSaldoer, forrigeEkGjeld, 28, 29, kredit: true),
        };

        // FR-R03/FR-R09: EK og gjeld viser kredit-saldo som positiv
        var sumEkGjeld = Math.Abs(ekGjeldSaldoer.Sum(s => s.UtgaendeBalanse));
        decimal? forrigeSumEkGjeld = forrigeEkGjeld != null
            ? Math.Abs(forrigeEkGjeld.Sum(s => s.UtgaendeBalanse))
            : null;

        var ekGjeldSide = new BalanseSideDto(ekGjeldSeksjoner, sumEkGjeld, forrigeSumEkGjeld);

        // FR-R10: Balansekontroll
        var erIBalanse = Math.Abs(sumEiendeler - sumEkGjeld) < 0.01m;

        var dto = new BalanseDto(
            Ar: ar,
            Periode: periode,
            Eiendeler: eiendelerSide,
            EgenkapitalOgGjeld: ekGjeldSide,
            SumEiendeler: sumEiendeler,
            SumEgenkapitalOgGjeld: sumEkGjeld,
            ErIBalanse: erIBalanse,
            ForrigeArSumEiendeler: forrigeSumEiendeler,
            ForrigeArSumEgenkapitalOgGjeld: forrigeSumEkGjeld
        );

        await LoggRapportAsync(RapportType.Balanse, ar, null, periode, dto, ct);
        return dto;
    }

    // --- Kontantstrom (indirekte metode) ---

    public async Task<KontantstromDto> GenererKontantstromAsync(
        int ar,
        bool inkluderForrigeAr = true,
        CancellationToken ct = default)
    {
        var saldoer = await _repo.HentAggregerteSaldoerAsync(ar, 1, 12, ct: ct);
        List<KontoSaldoAggregat>? forrigeSaldoer = null;
        if (inkluderForrigeAr)
            forrigeSaldoer = await _repo.HentAggregerteSaldoerAsync(ar - 1, 1, 12, ct: ct);

        // Beregn arsresultat
        var resultatSaldoer = saldoer.Where(s => int.Parse(s.Kontonummer[..1]) >= 3).ToList();
        var arsresultat = BeregnetNetto(resultatSaldoer);
        decimal? forrigeArsresultat = forrigeSaldoer != null
            ? BeregnetNetto(forrigeSaldoer.Where(s => int.Parse(s.Kontonummer[..1]) >= 3).ToList())
            : null;

        // DRIFT
        var driftLinjer = new List<KontantstromLinjeDto>();
        driftLinjer.Add(new("Arsresultat", arsresultat, forrigeArsresultat));

        var avskrivninger = HentNetto(saldoer, "60");
        driftLinjer.Add(new("Avskrivninger og nedskrivninger", avskrivninger,
            forrigeSaldoer != null ? HentNetto(forrigeSaldoer, "60") : null));

        var endringKundefordringer = HentEndring(saldoer, "15");
        driftLinjer.Add(new("Endring kundefordringer", -endringKundefordringer,
            forrigeSaldoer != null ? -HentEndring(forrigeSaldoer, "15") : null));

        var endringVarelager = HentEndring(saldoer, "14");
        driftLinjer.Add(new("Endring varelager", -endringVarelager,
            forrigeSaldoer != null ? -HentEndring(forrigeSaldoer, "14") : null));

        var endringLevGjeld = HentEndring(saldoer, "24");
        driftLinjer.Add(new("Endring leverandorgjeld", endringLevGjeld,
            forrigeSaldoer != null ? HentEndring(forrigeSaldoer, "24") : null));

        var endringOffGjeld = HentEndringRange(saldoer, 25, 27);
        driftLinjer.Add(new("Endring offentlig gjeld", endringOffGjeld,
            forrigeSaldoer != null ? HentEndringRange(forrigeSaldoer, 25, 27) : null));

        var driftSum = driftLinjer.Sum(l => l.Belop);
        decimal? forrigeDriftSum = inkluderForrigeAr ? driftLinjer.Sum(l => l.ForrigeArBelop ?? 0m) : null;

        var drift = new KontantstromSeksjonDto("Kontantstrom fra drift", driftLinjer, driftSum, forrigeDriftSum);

        // INVESTERING
        var invLinjer = new List<KontantstromLinjeDto>();
        var endringDriftsmidler = HentEndringRange(saldoer, 10, 12);
        invLinjer.Add(new("Endring varige driftsmidler", -endringDriftsmidler,
            forrigeSaldoer != null ? -HentEndringRange(forrigeSaldoer, 10, 12) : null));

        var endringFinAnlegg = HentEndring(saldoer, "13");
        invLinjer.Add(new("Endring finansielle anleggsmidler", -endringFinAnlegg,
            forrigeSaldoer != null ? -HentEndring(forrigeSaldoer, "13") : null));

        var invSum = invLinjer.Sum(l => l.Belop);
        decimal? forrigeInvSum = inkluderForrigeAr ? invLinjer.Sum(l => l.ForrigeArBelop ?? 0m) : null;

        var investering = new KontantstromSeksjonDto("Kontantstrom fra investering", invLinjer, invSum, forrigeInvSum);

        // FINANSIERING
        var finLinjer = new List<KontantstromLinjeDto>();
        var endringLangsiktigGjeld = HentEndringRange(saldoer, 22, 23);
        finLinjer.Add(new("Endring langsiktig gjeld", endringLangsiktigGjeld,
            forrigeSaldoer != null ? HentEndringRange(forrigeSaldoer, 22, 23) : null));

        var endringEK = HentEndring(saldoer, "20");
        finLinjer.Add(new("Endring innskutt egenkapital", endringEK,
            forrigeSaldoer != null ? HentEndring(forrigeSaldoer, "20") : null));

        var finSum = finLinjer.Sum(l => l.Belop);
        decimal? forrigeFinSum = inkluderForrigeAr ? finLinjer.Sum(l => l.ForrigeArBelop ?? 0m) : null;

        var finansiering = new KontantstromSeksjonDto("Kontantstrom fra finansiering", finLinjer, finSum, forrigeFinSum);

        // FR-R12: Likvider = konto 19xx
        var likviderUB = saldoer.Where(s => s.Kontonummer.StartsWith("19")).Sum(s => s.UtgaendeBalanse);
        var likviderIB = saldoer.Where(s => s.Kontonummer.StartsWith("19")).Sum(s => s.InngaendeBalanse);
        var nettoEndring = driftSum + invSum + finSum;
        decimal? forrigeNettoEndring = inkluderForrigeAr
            ? (forrigeDriftSum ?? 0m) + (forrigeInvSum ?? 0m) + (forrigeFinSum ?? 0m)
            : null;

        var dto = new KontantstromDto(
            Ar: ar,
            Drift: drift,
            Investering: investering,
            Finansiering: finansiering,
            NettoEndringLikvider: nettoEndring,
            LikviderIB: likviderIB,
            LikviderUB: likviderUB,
            ForrigeArNettoEndring: forrigeNettoEndring
        );

        await LoggRapportAsync(RapportType.Kontantstromoppstilling, ar, 1, 12, dto, ct);
        return dto;
    }

    // --- Saldobalanse ---

    public async Task<SaldobalanseRapportDto> GenererSaldobalanseRapportAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        bool inkluderNullsaldo = false,
        int? kontoklasse = null,
        bool gruppert = false,
        CancellationToken ct = default)
    {
        var saldoer = await _repo.HentAggregerteSaldoerAsync(ar, fraPeriode, tilPeriode, kontoklasse, ct);

        if (!inkluderNullsaldo)
            saldoer = saldoer.Where(s => s.SumDebet != 0 || s.SumKredit != 0 || s.InngaendeBalanse != 0).ToList();

        var grupper = saldoer
            .GroupBy(s => new { s.Gruppekode, s.Gruppenavn })
            .Select(g => new SaldobalanseGruppeDto(
                Gruppekode: g.Key.Gruppekode,
                Gruppenavn: g.Key.Gruppenavn,
                Linjer: g.Select(s => new SaldobalanseRapportLinjeDto(
                    Kontonummer: s.Kontonummer,
                    Kontonavn: s.Kontonavn,
                    Kontotype: s.Kontotype,
                    InngaendeBalanse: s.InngaendeBalanse,
                    SumDebet: s.SumDebet,
                    SumKredit: s.SumKredit,
                    Endring: s.SumDebet - s.SumKredit,
                    UtgaendeBalanse: s.UtgaendeBalanse
                )).ToList(),
                GruppeIB: g.Sum(s => s.InngaendeBalanse),
                GruppeSumDebet: g.Sum(s => s.SumDebet),
                GruppeSumKredit: g.Sum(s => s.SumKredit),
                GruppeUB: g.Sum(s => s.UtgaendeBalanse)
            ))
            .OrderBy(g => g.Gruppekode)
            .ToList();

        var totalIB = saldoer.Sum(s => s.InngaendeBalanse);
        var totalDebet = saldoer.Sum(s => s.SumDebet);
        var totalKredit = saldoer.Sum(s => s.SumKredit);
        var totalUB = saldoer.Sum(s => s.UtgaendeBalanse);

        // Debet/kredit saldo: sum av UB der UB > 0 vs UB < 0
        var debetSaldo = saldoer.Where(s => s.UtgaendeBalanse > 0).Sum(s => s.UtgaendeBalanse);
        var kreditSaldo = Math.Abs(saldoer.Where(s => s.UtgaendeBalanse < 0).Sum(s => s.UtgaendeBalanse));

        var totaler = new SaldobalanseTotalerDto(totalIB, totalDebet, totalKredit, totalUB, debetSaldo, kreditSaldo);
        var erStemmer = Math.Abs(debetSaldo - kreditSaldo) < 0.01m;

        var dto = new SaldobalanseRapportDto(ar, fraPeriode, tilPeriode, grupper, totaler, erStemmer);

        await LoggRapportAsync(RapportType.Saldobalanse, ar, fraPeriode, tilPeriode, dto, ct);
        return dto;
    }

    // --- Hovedboksutskrift (Bokforingsforskriften 3-1) ---

    public async Task<HovedboksutskriftDto> GenererHovedboksutskriftAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        string? fraKonto = null, string? tilKonto = null,
        CancellationToken ct = default)
    {
        var saldoer = await _repo.HentAggregerteSaldoerAsync(ar, fraPeriode, tilPeriode, ct: ct);

        if (fraKonto != null)
            saldoer = saldoer.Where(s => string.Compare(s.Kontonummer, fraKonto, StringComparison.Ordinal) >= 0).ToList();
        if (tilKonto != null)
            saldoer = saldoer.Where(s => string.Compare(s.Kontonummer, tilKonto, StringComparison.Ordinal) <= 0).ToList();

        // NOTE: Full posteringsdetaljer krever direkte DB-sporing.
        // Her genererer vi oversiktsniva basert pa aggregerte saldoer.
        var kontoer = saldoer.Select(s => new KontoUtskriftDto(
            Kontonummer: s.Kontonummer,
            Kontonavn: s.Kontonavn,
            Kontotype: s.Kontotype,
            Normalbalanse: s.Normalbalanse,
            InngaendeBalanse: s.InngaendeBalanse,
            Posteringer: new List<PosteringUtskriftDto>(), // TODO: Avklar med arkitekt - krever utvidet repository-metode for posteringsdetaljer
            SumDebet: s.SumDebet,
            SumKredit: s.SumKredit,
            UtgaendeBalanse: s.UtgaendeBalanse,
            AntallPosteringer: 0
        )).ToList();

        var dto = new HovedboksutskriftDto(ar, fraPeriode, tilPeriode, kontoer);

        await LoggRapportAsync(RapportType.Hovedboksutskrift, ar, fraPeriode, tilPeriode, dto, ct);
        return dto;
    }

    // --- Dimensjonsrapport ---

    public async Task<DimensjonsrapportDto> GenererDimensjonsrapportAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        string dimensjon = "avdeling",
        string? kode = null,
        int? kontoklasse = null,
        CancellationToken ct = default)
    {
        var dimSaldoer = await _repo.HentDimensjonsSaldoerAsync(ar, fraPeriode, tilPeriode, dimensjon, kode, kontoklasse, ct);

        var grupper = dimSaldoer
            .GroupBy(d => d.DimensjonsKode)
            .Select(g => new DimensjonsGruppeDto(
                Kode: g.Key,
                Navn: g.Key, // FR-R20: "Uspesifisert" for tomme koder
                Linjer: g.Select(d => new DimensjonsLinjeDto(
                    Kontonummer: d.Kontonummer,
                    Kontonavn: d.Kontonavn,
                    SumDebet: d.SumDebet,
                    SumKredit: d.SumKredit,
                    Netto: d.Netto
                )).ToList(),
                SumDebet: g.Sum(d => d.SumDebet),
                SumKredit: g.Sum(d => d.SumKredit),
                Netto: g.Sum(d => d.Netto)
            ))
            .OrderBy(g => g.Kode)
            .ToList();

        var totaler = new DimensjonsTotalerDto(
            TotalDebet: dimSaldoer.Sum(d => d.SumDebet),
            TotalKredit: dimSaldoer.Sum(d => d.SumKredit),
            TotalNetto: dimSaldoer.Sum(d => d.Netto),
            AntallPosteringer: dimSaldoer.Count
        );

        var dto = new DimensjonsrapportDto(ar, fraPeriode, tilPeriode, dimensjon, grupper, totaler);

        await LoggRapportAsync(RapportType.Dimensjonsrapport, ar, fraPeriode, tilPeriode, dto, ct);
        return dto;
    }

    // --- Sammenligning ---

    public async Task<SammenligningDto> GenererSammenligningAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        string type = "forrige_ar",
        string budsjettVersjon = "Opprinnelig",
        int? kontoklasse = null,
        CancellationToken ct = default)
    {
        var saldoer = await _repo.HentAggregerteSaldoerAsync(ar, fraPeriode, tilPeriode, kontoklasse, ct);

        List<SammenligningLinjeDto> linjer;

        if (type == "budsjett")
        {
            var budsjett = await _repo.HentBudsjettForArAsync(ar, budsjettVersjon, ct);
            var budsjettDict = budsjett
                .GroupBy(b => b.Kontonummer)
                .ToDictionary(g => g.Key, g => g.Sum(b => b.Belop));

            linjer = saldoer.Select(s =>
            {
                var faktisk = s.SumDebet - s.SumKredit;
                var budsjettBelop = budsjettDict.GetValueOrDefault(s.Kontonummer, 0m);
                var avvik = faktisk - budsjettBelop;
                var avvikProsent = budsjettBelop != 0 ? (avvik / Math.Abs(budsjettBelop)) * 100m : 0m;

                return new SammenligningLinjeDto(
                    Kontonummer: s.Kontonummer,
                    Kontonavn: s.Kontonavn,
                    Kontotype: s.Kontotype,
                    Faktisk: faktisk,
                    Sammenligning: budsjettBelop,
                    AvvikBelop: avvik,
                    AvvikProsent: Math.Round(avvikProsent, 2)
                );
            }).ToList();
        }
        else
        {
            // forrige_ar
            var forrigeSaldoer = await _repo.HentAggregerteSaldoerAsync(ar - 1, fraPeriode, tilPeriode, kontoklasse, ct);
            var forrigeDict = forrigeSaldoer.ToDictionary(s => s.Kontonummer, s => s.SumDebet - s.SumKredit);

            linjer = saldoer.Select(s =>
            {
                var faktisk = s.SumDebet - s.SumKredit;
                var forrige = forrigeDict.GetValueOrDefault(s.Kontonummer, 0m);
                var avvik = faktisk - forrige;
                var avvikProsent = forrige != 0 ? (avvik / Math.Abs(forrige)) * 100m : 0m;

                return new SammenligningLinjeDto(
                    Kontonummer: s.Kontonummer,
                    Kontonavn: s.Kontonavn,
                    Kontotype: s.Kontotype,
                    Faktisk: faktisk,
                    Sammenligning: forrige,
                    AvvikBelop: avvik,
                    AvvikProsent: Math.Round(avvikProsent, 2)
                );
            }).ToList();
        }

        var totalFaktisk = linjer.Sum(l => l.Faktisk);
        var totalSammenligning = linjer.Sum(l => l.Sammenligning);
        var totalAvvik = totalFaktisk - totalSammenligning;
        var totalAvvikProsent = totalSammenligning != 0
            ? Math.Round((totalAvvik / Math.Abs(totalSammenligning)) * 100m, 2)
            : 0m;

        var totaler = new SammenligningTotalerDto(totalFaktisk, totalSammenligning, totalAvvik, totalAvvikProsent);
        var dto = new SammenligningDto(ar, fraPeriode, tilPeriode, type, linjer, totaler);

        await LoggRapportAsync(RapportType.Sammenligning, ar, fraPeriode, tilPeriode, dto, ct);
        return dto;
    }

    // --- Nokkeltall ---

    public async Task<NokkeltallRapportDto> GenererNokkeltallAsync(
        int ar, int periode = 12,
        bool inkluderForrigeAr = true,
        CancellationToken ct = default)
    {
        var saldoer = await _repo.HentAggregerteSaldoerAsync(ar, 1, periode, ct: ct);

        var nokkeltall = BeregnetNokkeltall(saldoer, ar, periode);

        NokkeltallRapportDto? forrigeAr = null;
        if (inkluderForrigeAr)
        {
            var forrigeSaldoer = await _repo.HentAggregerteSaldoerAsync(ar - 1, 1, 12, ct: ct);
            forrigeAr = BeregnetNokkeltall(forrigeSaldoer, ar - 1, 12);
        }

        var dto = new NokkeltallRapportDto(
            Ar: ar,
            Periode: periode,
            Likviditet: nokkeltall.Likviditet,
            Soliditet: nokkeltall.Soliditet,
            Lonnsomhet: nokkeltall.Lonnsomhet,
            ForrigeAr: forrigeAr
        );

        await LoggRapportAsync(RapportType.Nokkeltall, ar, null, periode, dto, ct);
        return dto;
    }

    // --- Private hjelpemetoder ---

    private static NokkeltallRapportDto BeregnetNokkeltall(
        List<KontoSaldoAggregat> saldoer, int ar, int periode)
    {
        // LIKVIDITET
        var omlopsmidler = saldoer
            .Where(s => s.Gruppekode >= 14 && s.Gruppekode <= 19)
            .Sum(s => s.UtgaendeBalanse);
        var varelager = saldoer
            .Where(s => s.Gruppekode == 14)
            .Sum(s => s.UtgaendeBalanse);
        var kortsiktigGjeld = Math.Abs(saldoer
            .Where(s => s.Gruppekode >= 24 && s.Gruppekode <= 29)
            .Sum(s => s.UtgaendeBalanse));

        // FR-R18: Ved divisjon med null
        var likviditetsgrad1 = kortsiktigGjeld != 0 ? Math.Round(omlopsmidler / kortsiktigGjeld, 2) : 0m;
        var likviditetsgrad2 = kortsiktigGjeld != 0 ? Math.Round((omlopsmidler - varelager) / kortsiktigGjeld, 2) : 0m;
        var arbeidskapital = omlopsmidler - kortsiktigGjeld;

        var likviditet = new LikviditetDto(likviditetsgrad1, likviditetsgrad2, arbeidskapital);

        // SOLIDITET
        var egenkapital = Math.Abs(saldoer
            .Where(s => s.Gruppekode >= 20 && s.Gruppekode <= 21)
            .Sum(s => s.UtgaendeBalanse));
        var totalGjeld = Math.Abs(saldoer
            .Where(s => s.Gruppekode >= 22 && s.Gruppekode <= 29)
            .Sum(s => s.UtgaendeBalanse));
        var totalkapital = saldoer
            .Where(s => s.Kontonummer.StartsWith("1"))
            .Sum(s => s.UtgaendeBalanse);

        var rentekostnader = Math.Abs(saldoer
            .Where(s => s.Kontonummer.StartsWith("84"))
            .Sum(s => s.SumDebet - s.SumKredit));

        // Resultat for skatt = netto konto 30xx-87xx
        var resultatForSkatt = BeregnetNettoRange(saldoer, 30, 87);

        var egenkapitalandel = totalkapital != 0 ? Math.Round(egenkapital / totalkapital * 100m, 2) : 0m;
        var gjeldsgrad = egenkapital != 0 ? Math.Round(totalGjeld / egenkapital, 2) : 0m;
        var rentedekningsgrad = rentekostnader != 0
            ? Math.Round((Math.Abs(resultatForSkatt) + rentekostnader) / rentekostnader, 2)
            : 0m;

        var soliditet = new SoliditetDto(egenkapitalandel, gjeldsgrad, rentedekningsgrad);

        // LONNSOMHET
        var driftsinntekter = Math.Abs(saldoer
            .Where(s => s.Gruppekode >= 30 && s.Gruppekode <= 39)
            .Sum(s => s.SumKredit - s.SumDebet));
        var driftskostnader = saldoer
            .Where(s => s.Gruppekode >= 40 && s.Gruppekode <= 79)
            .Sum(s => s.SumDebet - s.SumKredit);
        var driftsresultat = driftsinntekter - driftskostnader;
        var arsresultat = BeregnetNetto(saldoer.Where(s => int.Parse(s.Kontonummer[..1]) >= 3).ToList());

        // FR-R19: Gjennomsnittlig kapital
        var totalkapitalIB = saldoer
            .Where(s => s.Kontonummer.StartsWith("1"))
            .Sum(s => s.InngaendeBalanse);
        var gjSnittTotalkapital = (totalkapitalIB + totalkapital) / 2m;
        var egenkapitalIB = Math.Abs(saldoer
            .Where(s => s.Gruppekode >= 20 && s.Gruppekode <= 21)
            .Sum(s => s.InngaendeBalanse));
        var gjSnittEgenkapital = (egenkapitalIB + egenkapital) / 2m;

        var totalkapitalrentabilitet = gjSnittTotalkapital != 0
            ? Math.Round((Math.Abs(resultatForSkatt) + rentekostnader) / gjSnittTotalkapital * 100m, 2)
            : 0m;
        var egenkapitalrentabilitet = gjSnittEgenkapital != 0
            ? Math.Round(Math.Abs(arsresultat) / gjSnittEgenkapital * 100m, 2)
            : 0m;
        var resultatmargin = driftsinntekter != 0
            ? Math.Round(driftsresultat / driftsinntekter * 100m, 2)
            : 0m;
        var driftsmargin = driftsinntekter != 0
            ? Math.Round(driftsresultat / driftsinntekter * 100m, 2)
            : 0m;

        var lonnsomhet = new LonnsomhetDto(totalkapitalrentabilitet, egenkapitalrentabilitet, resultatmargin, driftsmargin);

        return new NokkeltallRapportDto(ar, periode, likviditet, soliditet, lonnsomhet, null);
    }

    private static decimal BeregnetNetto(List<KontoSaldoAggregat> saldoer)
    {
        // For resultatkontoer: inntekter (kredit-normert) vises positivt, kostnader trekkes fra
        // Netto = SumKredit - SumDebet for alle resultatkontoer
        return saldoer.Sum(s => s.SumKredit - s.SumDebet);
    }

    private static decimal BeregnetNettoRange(List<KontoSaldoAggregat> saldoer, int fraGruppe, int tilGruppe)
    {
        return saldoer
            .Where(s => s.Gruppekode >= fraGruppe && s.Gruppekode <= tilGruppe)
            .Sum(s => s.SumKredit - s.SumDebet);
    }

    private ResultatregnskapSeksjonDto ByggResultatSeksjon(
        string kode, string navn,
        List<KontoSaldoAggregat> saldoer,
        List<KontoSaldoAggregat>? forrigeSaldoer,
        string prefix,
        bool invertFortegn)
    {
        var kontoer = saldoer.Where(s => s.Kontonummer.StartsWith(prefix)).ToList();
        var forrigeKontoer = forrigeSaldoer?.Where(s => s.Kontonummer.StartsWith(prefix)).ToList();

        var linjer = kontoer.Select(s =>
        {
            var belop = invertFortegn
                ? (s.SumKredit - s.SumDebet)
                : (s.SumDebet - s.SumKredit);
            var forrigeBelop = forrigeKontoer?.FirstOrDefault(f => f.Kontonummer == s.Kontonummer);
            decimal? forrigeBelopVerdi = forrigeBelop != null
                ? (invertFortegn ? (forrigeBelop.SumKredit - forrigeBelop.SumDebet) : (forrigeBelop.SumDebet - forrigeBelop.SumKredit))
                : null;

            return new ResultatregnskapLinjeDto(
                s.Kontonummer, s.Kontonavn, belop, forrigeBelopVerdi, false);
        }).ToList();

        var sum = linjer.Sum(l => l.Belop);
        decimal? forrigeSum = forrigeKontoer != null
            ? (invertFortegn
                ? forrigeKontoer.Sum(s => s.SumKredit - s.SumDebet)
                : forrigeKontoer.Sum(s => s.SumDebet - s.SumKredit))
            : null;

        return new ResultatregnskapSeksjonDto(kode, navn, linjer, sum, forrigeSum);
    }

    private ResultatregnskapSeksjonDto ByggResultatSeksjonRange(
        string kode, string navn,
        List<KontoSaldoAggregat> saldoer,
        List<KontoSaldoAggregat>? forrigeSaldoer,
        int fraGruppe, int tilGruppe,
        bool invertFortegn)
    {
        var kontoer = saldoer.Where(s => s.Gruppekode >= fraGruppe && s.Gruppekode <= tilGruppe).ToList();
        var forrigeKontoer = forrigeSaldoer?.Where(s => s.Gruppekode >= fraGruppe && s.Gruppekode <= tilGruppe).ToList();

        var linjer = kontoer.Select(s =>
        {
            var belop = invertFortegn
                ? (s.SumKredit - s.SumDebet)
                : (s.SumDebet - s.SumKredit);
            var forrigeBelop = forrigeKontoer?.FirstOrDefault(f => f.Kontonummer == s.Kontonummer);
            decimal? forrigeBelopVerdi = forrigeBelop != null
                ? (invertFortegn ? (forrigeBelop.SumKredit - forrigeBelop.SumDebet) : (forrigeBelop.SumDebet - forrigeBelop.SumKredit))
                : null;

            return new ResultatregnskapLinjeDto(
                s.Kontonummer, s.Kontonavn, belop, forrigeBelopVerdi, false);
        }).ToList();

        var sum = linjer.Sum(l => l.Belop);
        decimal? forrigeSum = forrigeKontoer != null
            ? (invertFortegn
                ? forrigeKontoer.Sum(s => s.SumKredit - s.SumDebet)
                : forrigeKontoer.Sum(s => s.SumDebet - s.SumKredit))
            : null;

        return new ResultatregnskapSeksjonDto(kode, navn, linjer, sum, forrigeSum);
    }

    private BalanseSeksjonDto ByggBalanseSeksjon(
        string kode, string navn,
        List<KontoSaldoAggregat> saldoer,
        List<KontoSaldoAggregat>? forrigeSaldoer,
        int fraGruppe, int tilGruppe,
        bool kredit = false)
    {
        var kontoer = saldoer.Where(s => s.Gruppekode >= fraGruppe && s.Gruppekode <= tilGruppe).ToList();
        var forrigeKontoer = forrigeSaldoer?.Where(s => s.Gruppekode >= fraGruppe && s.Gruppekode <= tilGruppe).ToList();

        var linjer = kontoer.Select(s =>
        {
            // FR-R03: Absoluttverdi for balansen
            var belop = kredit ? Math.Abs(s.UtgaendeBalanse) : s.UtgaendeBalanse;
            var forrigeBelop = forrigeKontoer?.FirstOrDefault(f => f.Kontonummer == s.Kontonummer);
            decimal? forrigeVerdi = forrigeBelop != null
                ? (kredit ? Math.Abs(forrigeBelop.UtgaendeBalanse) : forrigeBelop.UtgaendeBalanse)
                : null;

            return new BalanseLinjeDto(s.Kontonummer, s.Kontonavn, belop, forrigeVerdi, false);
        }).ToList();

        var sum = linjer.Sum(l => l.Belop);
        decimal? forrigeSum = forrigeKontoer != null
            ? (kredit
                ? forrigeKontoer.Sum(s => Math.Abs(s.UtgaendeBalanse))
                : forrigeKontoer.Sum(s => s.UtgaendeBalanse))
            : null;

        return new BalanseSeksjonDto(kode, navn, linjer, sum, forrigeSum);
    }

    private static decimal HentNetto(List<KontoSaldoAggregat> saldoer, string prefix)
    {
        return saldoer
            .Where(s => s.Kontonummer.StartsWith(prefix))
            .Sum(s => s.SumDebet - s.SumKredit);
    }

    private static decimal HentEndring(List<KontoSaldoAggregat> saldoer, string prefix)
    {
        return saldoer
            .Where(s => s.Kontonummer.StartsWith(prefix))
            .Sum(s => s.UtgaendeBalanse - s.InngaendeBalanse);
    }

    private static decimal HentEndringRange(List<KontoSaldoAggregat> saldoer, int fraGruppe, int tilGruppe)
    {
        return saldoer
            .Where(s => s.Gruppekode >= fraGruppe && s.Gruppekode <= tilGruppe)
            .Sum(s => s.UtgaendeBalanse - s.InngaendeBalanse);
    }

    private async Task LoggRapportAsync(
        RapportType type, int ar, int? fraPeriode, int? tilPeriode,
        object data, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(data);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        var kontrollsum = Convert.ToHexStringLower(hash);

        var logg = new RapportLogg
        {
            Id = Guid.NewGuid(),
            Type = type,
            Ar = ar,
            FraPeriode = fraPeriode,
            TilPeriode = tilPeriode,
            GenererTidspunkt = DateTime.UtcNow,
            GenererAv = "system",
            Parametre = json.Length > 2000 ? json[..2000] : json,
            Kontrollsum = kontrollsum
        };

        await _repo.LeggTilRapportLoggAsync(logg, ct);
        await _repo.LagreEndringerAsync(ct);
    }
}
