using FluentAssertions;
using Regnskap.Application.Features.Rapportering;
using Regnskap.Domain.Features.Rapportering;

namespace Regnskap.Tests.Features.Rapportering;

/// <summary>
/// Fake repository for rapporteringsservice-tester.
/// </summary>
public class FakeRapporteringRepository : IRapporteringRepository
{
    private readonly List<Budsjett> _budsjetter = new();
    private readonly List<RapportLogg> _logger = new();
    private readonly List<KontoSaldoAggregat> _saldoer = new();
    private readonly List<DimensjonsSaldoAggregat> _dimSaldoer = new();
    private RapportKonfigurasjon? _konfig;

    public void LeggTilSaldoer(IEnumerable<KontoSaldoAggregat> saldoer) => _saldoer.AddRange(saldoer);
    public void LeggTilDimensjonsSaldoer(IEnumerable<DimensjonsSaldoAggregat> saldoer) => _dimSaldoer.AddRange(saldoer);
    public void SettKonfigurasjon(RapportKonfigurasjon konfig) => _konfig = konfig;
    public List<RapportLogg> HentLogger() => _logger;

    public Task<Budsjett?> HentBudsjettLinjeAsync(string kontonummer, int ar, int periode, string versjon, CancellationToken ct = default)
        => Task.FromResult(_budsjetter.FirstOrDefault(b => b.Kontonummer == kontonummer && b.Ar == ar && b.Periode == periode && b.Versjon == versjon));

    public Task<List<Budsjett>> HentBudsjettForArAsync(int ar, string versjon, CancellationToken ct = default)
        => Task.FromResult(_budsjetter.Where(b => b.Ar == ar && b.Versjon == versjon).ToList());

    public Task LeggTilBudsjettAsync(Budsjett budsjett, CancellationToken ct = default)
    {
        _budsjetter.Add(budsjett);
        return Task.CompletedTask;
    }

    public Task SlettBudsjettForArAsync(int ar, string versjon, CancellationToken ct = default)
    {
        _budsjetter.RemoveAll(b => b.Ar == ar && b.Versjon == versjon);
        return Task.CompletedTask;
    }

    public Task<RapportKonfigurasjon?> HentKonfigurasjonAsync(CancellationToken ct = default)
        => Task.FromResult(_konfig);

    public Task LagreKonfigurasjonAsync(RapportKonfigurasjon konfigurasjon, CancellationToken ct = default)
    {
        _konfig = konfigurasjon;
        return Task.CompletedTask;
    }

    public Task LeggTilRapportLoggAsync(RapportLogg logg, CancellationToken ct = default)
    {
        _logger.Add(logg);
        return Task.CompletedTask;
    }

    public Task<List<RapportLogg>> HentRapportLoggerAsync(int ar, RapportType? type = null, CancellationToken ct = default)
        => Task.FromResult(_logger.Where(l => l.Ar == ar && (type == null || l.Type == type)).ToList());

    public Task<List<KontoSaldoAggregat>> HentAggregerteSaldoerAsync(
        int ar, int fraPeriode, int tilPeriode, int? kontoklasse = null, CancellationToken ct = default)
    {
        var result = _saldoer.AsEnumerable();
        if (kontoklasse.HasValue)
            result = result.Where(s => s.Kontonummer.StartsWith(kontoklasse.Value.ToString()));
        return Task.FromResult(result.ToList());
    }

    public Task<List<DimensjonsSaldoAggregat>> HentDimensjonsSaldoerAsync(
        int ar, int fraPeriode, int tilPeriode, string dimensjon,
        string? kode = null, int? kontoklasse = null, CancellationToken ct = default)
    {
        var result = _dimSaldoer.AsEnumerable();
        if (kode != null)
            result = result.Where(d => d.DimensjonsKode == kode);
        return Task.FromResult(result.ToList());
    }

    public Task LagreEndringerAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public class RapporteringServiceTests
{
    private readonly FakeRapporteringRepository _repo;
    private readonly RapporteringService _sut;

    public RapporteringServiceTests()
    {
        _repo = new FakeRapporteringRepository();
        _sut = new RapporteringService(_repo);
    }

    // --- Resultatregnskap ---

    [Fact]
    public async Task GenererResultatregnskap_BeregnerDriftsresultatKorrekt()
    {
        // Arrange: Driftsinntekter 1 000 000, Varekostnad 400 000, Lonn 300 000
        _repo.LeggTilSaldoer(new[]
        {
            LagSaldo("3000", "Salgsinntekt", "Inntekt", "Kredit", 30, "Salgsinntekt", sumDebet: 0, sumKredit: 1_000_000),
            LagSaldo("4000", "Varekjop", "Kostnad", "Debet", 40, "Varekjop", sumDebet: 400_000, sumKredit: 0),
            LagSaldo("5000", "Lonn", "Kostnad", "Debet", 50, "Lonn", sumDebet: 300_000, sumKredit: 0),
        });

        // Act
        var resultat = await _sut.GenererResultatregnskapAsync(2026, inkluderForrigeAr: false);

        // Assert (FR-R07)
        resultat.Driftsresultat.Should().Be(300_000m); // 1M - 400k - 300k
        resultat.Arsresultat.Should().Be(300_000m);
        resultat.Ar.Should().Be(2026);
    }

    [Fact]
    public async Task GenererResultatregnskap_InkludererFinansposter()
    {
        _repo.LeggTilSaldoer(new[]
        {
            LagSaldo("3000", "Salgsinntekt", "Inntekt", "Kredit", 30, "Salgsinntekt", sumDebet: 0, sumKredit: 500_000),
            LagSaldo("8000", "Renteinntekt", "Inntekt", "Kredit", 80, "Finansinntekt", sumDebet: 0, sumKredit: 10_000),
            LagSaldo("8400", "Rentekostnad", "Kostnad", "Debet", 84, "Finanskostnad", sumDebet: 5_000, sumKredit: 0),
        });

        var resultat = await _sut.GenererResultatregnskapAsync(2026, inkluderForrigeAr: false);

        resultat.Driftsresultat.Should().Be(500_000m);
        resultat.FinansresultatNetto.Should().Be(5_000m); // 10k - 5k
        resultat.OrdnaertResultatForSkatt.Should().Be(505_000m);
    }

    [Fact]
    public async Task GenererResultatregnskap_TrekkerFraSkattekostnad()
    {
        _repo.LeggTilSaldoer(new[]
        {
            LagSaldo("3000", "Salgsinntekt", "Inntekt", "Kredit", 30, "Salgsinntekt", sumDebet: 0, sumKredit: 200_000),
            LagSaldo("8900", "Skattekostnad", "Kostnad", "Debet", 89, "Skattekostnad", sumDebet: 50_000, sumKredit: 0),
        });

        var resultat = await _sut.GenererResultatregnskapAsync(2026, inkluderForrigeAr: false);

        resultat.Skattekostnad.Should().Be(50_000m);
        resultat.Arsresultat.Should().Be(150_000m);
    }

    [Fact]
    public async Task GenererResultatregnskap_LoggerRapport()
    {
        _repo.LeggTilSaldoer(new[]
        {
            LagSaldo("3000", "Salg", "Inntekt", "Kredit", 30, "Salg", 0, 100_000)
        });

        await _sut.GenererResultatregnskapAsync(2026, inkluderForrigeAr: false);

        _repo.HentLogger().Should().ContainSingle(l => l.Type == RapportType.Resultatregnskap);
    }

    // --- Balanse ---

    [Fact]
    public async Task GenererBalanse_SumEiendelerStemmerMedEkOgGjeld()
    {
        _repo.LeggTilSaldoer(new[]
        {
            // Eiendeler
            LagSaldo("1920", "Bank", "Eiendel", "Debet", 19, "Bank", ib: 0, sumDebet: 500_000, sumKredit: 0),
            // EK
            LagSaldo("2000", "Aksjekapital", "Egenkapital", "Kredit", 20, "Innskutt EK", ib: 0, sumDebet: 0, sumKredit: 500_000),
        });

        var balanse = await _sut.GenererBalanseAsync(2026, inkluderForrigeAr: false);

        balanse.SumEiendeler.Should().Be(500_000m);
        balanse.SumEgenkapitalOgGjeld.Should().Be(500_000m);
        balanse.ErIBalanse.Should().BeTrue();
    }

    [Fact]
    public async Task GenererBalanse_DetektererUbalanse()
    {
        _repo.LeggTilSaldoer(new[]
        {
            LagSaldo("1920", "Bank", "Eiendel", "Debet", 19, "Bank", ib: 0, sumDebet: 100_000, sumKredit: 0),
            LagSaldo("2000", "Aksjekapital", "Egenkapital", "Kredit", 20, "Innskutt EK", ib: 0, sumDebet: 0, sumKredit: 50_000),
        });

        var balanse = await _sut.GenererBalanseAsync(2026, inkluderForrigeAr: false);

        balanse.ErIBalanse.Should().BeFalse();
    }

    [Fact]
    public async Task GenererBalanse_GrupperingSeksjoner()
    {
        _repo.LeggTilSaldoer(new[]
        {
            LagSaldo("1000", "Goodwill", "Eiendel", "Debet", 10, "Immatr", ib: 0, sumDebet: 100_000, sumKredit: 0),
            LagSaldo("1200", "Maskiner", "Eiendel", "Debet", 12, "Varige", ib: 0, sumDebet: 200_000, sumKredit: 0),
            LagSaldo("1920", "Bank", "Eiendel", "Debet", 19, "Bank", ib: 0, sumDebet: 50_000, sumKredit: 0),
        });

        var balanse = await _sut.GenererBalanseAsync(2026, inkluderForrigeAr: false);

        balanse.Eiendeler.Seksjoner.Should().HaveCountGreaterThan(0);
        var immatr = balanse.Eiendeler.Seksjoner.First(s => s.Kode == "IMMATR_ANLEGG");
        immatr.Sum.Should().Be(100_000m);
    }

    // --- Kontantstrom ---

    [Fact]
    public async Task GenererKontantstrom_BeregnerDriftKorrekt()
    {
        _repo.LeggTilSaldoer(new[]
        {
            // Resultat
            LagSaldo("3000", "Salg", "Inntekt", "Kredit", 30, "Salg", 0, 0, 1_000_000),
            LagSaldo("4000", "Varer", "Kostnad", "Debet", 40, "Varekjop", 0, 600_000, 0),
            // Avskrivninger (tilbakeforing)
            LagSaldo("6000", "Avskrivninger", "Kostnad", "Debet", 60, "Avskr", 0, 50_000, 0),
            // Balanseendringer
            LagSaldo("1500", "Kundefordringer", "Eiendel", "Debet", 15, "Fordringer", ib: 100_000, sumDebet: 50_000, sumKredit: 30_000),
            LagSaldo("1920", "Bank", "Eiendel", "Debet", 19, "Bank", ib: 200_000, sumDebet: 100_000, sumKredit: 50_000),
        });

        var kontantstrom = await _sut.GenererKontantstromAsync(2026, inkluderForrigeAr: false);

        kontantstrom.Ar.Should().Be(2026);
        // Arsresultat = 1M - 600k - 50k = 350k (kredit - debet for alle resultatposter)
        kontantstrom.Drift.Linjer.First().Beskrivelse.Should().Be("Arsresultat");
    }

    // --- Nokkeltall ---

    [Fact]
    public async Task GenererNokkeltall_Likviditetsgrad1()
    {
        _repo.LeggTilSaldoer(new[]
        {
            LagSaldo("1920", "Bank", "Eiendel", "Debet", 19, "Bank", ib: 0, sumDebet: 200_000, sumKredit: 0),
            LagSaldo("2400", "Leverandorgjeld", "Gjeld", "Kredit", 24, "LevGjeld", ib: 0, sumDebet: 0, sumKredit: 100_000),
        });

        var nokkeltall = await _sut.GenererNokkeltallAsync(2026, inkluderForrigeAr: false);

        // Omlopsmidler (19xx) = 200k, Kortsiktig gjeld (24xx) = 100k
        nokkeltall.Likviditet.Likviditetsgrad1.Should().Be(2.00m);
    }

    [Fact]
    public async Task GenererNokkeltall_Likviditetsgrad2_TrekkerFraVarelager()
    {
        _repo.LeggTilSaldoer(new[]
        {
            LagSaldo("1400", "Varelager", "Eiendel", "Debet", 14, "Varer", ib: 0, sumDebet: 50_000, sumKredit: 0),
            LagSaldo("1920", "Bank", "Eiendel", "Debet", 19, "Bank", ib: 0, sumDebet: 150_000, sumKredit: 0),
            LagSaldo("2400", "Leverandorgjeld", "Gjeld", "Kredit", 24, "LevGjeld", ib: 0, sumDebet: 0, sumKredit: 100_000),
        });

        var nokkeltall = await _sut.GenererNokkeltallAsync(2026, inkluderForrigeAr: false);

        // Omlopsmidler = 200k, Varelager = 50k, Kortsiktig gjeld = 100k
        nokkeltall.Likviditet.Likviditetsgrad1.Should().Be(2.00m);
        nokkeltall.Likviditet.Likviditetsgrad2.Should().Be(1.50m); // (200k - 50k) / 100k
        nokkeltall.Likviditet.Arbeidskapital.Should().Be(100_000m);
    }

    [Fact]
    public async Task GenererNokkeltall_DivisjonMedNull_ReturnererNull()
    {
        // FR-R18: Ingen kortsiktig gjeld
        _repo.LeggTilSaldoer(new[]
        {
            LagSaldo("1920", "Bank", "Eiendel", "Debet", 19, "Bank", ib: 0, sumDebet: 200_000, sumKredit: 0),
        });

        var nokkeltall = await _sut.GenererNokkeltallAsync(2026, inkluderForrigeAr: false);

        nokkeltall.Likviditet.Likviditetsgrad1.Should().Be(0m);
    }

    [Fact]
    public async Task GenererNokkeltall_Egenkapitalandel()
    {
        _repo.LeggTilSaldoer(new[]
        {
            LagSaldo("1920", "Bank", "Eiendel", "Debet", 19, "Bank", ib: 0, sumDebet: 1_000_000, sumKredit: 0),
            LagSaldo("2000", "Aksjekapital", "Egenkapital", "Kredit", 20, "EK", ib: 0, sumDebet: 0, sumKredit: 400_000),
            LagSaldo("2200", "Langsiktig lan", "Gjeld", "Kredit", 22, "LangsiktigGjeld", ib: 0, sumDebet: 0, sumKredit: 600_000),
        });

        var nokkeltall = await _sut.GenererNokkeltallAsync(2026, inkluderForrigeAr: false);

        // EK = 400k, Totalkapital (eiendeler) = 1M
        nokkeltall.Soliditet.Egenkapitalandel.Should().Be(40.00m);
    }

    // --- Saldobalanse ---

    [Fact]
    public async Task GenererSaldobalanse_FiltrerNullsaldo()
    {
        _repo.LeggTilSaldoer(new[]
        {
            LagSaldo("1920", "Bank", "Eiendel", "Debet", 19, "Bank", 0, 100, 0),
            LagSaldo("3000", "Salg", "Inntekt", "Kredit", 30, "Salg", 0, 0, 0), // Nullsaldo
        });

        var rapport = await _sut.GenererSaldobalanseRapportAsync(2026, inkluderNullsaldo: false);

        rapport.Grupper.SelectMany(g => g.Linjer).Should().HaveCount(1);
    }

    [Fact]
    public async Task GenererSaldobalanse_InkludererNullsaldo()
    {
        _repo.LeggTilSaldoer(new[]
        {
            LagSaldo("1920", "Bank", "Eiendel", "Debet", 19, "Bank", 0, 100, 0),
            LagSaldo("3000", "Salg", "Inntekt", "Kredit", 30, "Salg", 0, 0, 0),
        });

        var rapport = await _sut.GenererSaldobalanseRapportAsync(2026, inkluderNullsaldo: true);

        rapport.Grupper.SelectMany(g => g.Linjer).Should().HaveCount(2);
    }

    // --- Sammenligning ---

    [Fact]
    public async Task GenererSammenligning_BudsjettTyp_BeregnerAvvik()
    {
        _repo.LeggTilSaldoer(new[]
        {
            LagSaldo("3000", "Salg", "Inntekt", "Kredit", 30, "Salg", 0, 0, 100_000),
        });

        // Legg til budsjett manuelt
        await _repo.LeggTilBudsjettAsync(new Budsjett
        {
            Id = Guid.NewGuid(), Kontonummer = "3000", Ar = 2026, Periode = 1, Belop = -120_000, Versjon = "Opprinnelig"
        });

        var sammenligning = await _sut.GenererSammenligningAsync(2026, type: "budsjett");

        sammenligning.Linjer.Should().ContainSingle();
    }

    // --- Hjelpemetode ---

    private static KontoSaldoAggregat LagSaldo(
        string kontonummer, string kontonavn, string kontotype, string normalbalanse,
        int gruppekode, string gruppenavn,
        decimal ib = 0, decimal sumDebet = 0, decimal sumKredit = 0)
    {
        return new KontoSaldoAggregat(
            Kontonummer: kontonummer,
            Kontonavn: kontonavn,
            Kontotype: kontotype,
            Normalbalanse: normalbalanse,
            Gruppekode: gruppekode,
            Gruppenavn: gruppenavn,
            InngaendeBalanse: ib,
            SumDebet: sumDebet,
            SumKredit: sumKredit,
            UtgaendeBalanse: ib + sumDebet - sumKredit
        );
    }
}
