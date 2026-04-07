using FluentAssertions;
using Regnskap.Application.Features.Hovedbok;
using Regnskap.Application.Features.Kontoplan;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Tests.Features.Hovedbok;

/// <summary>
/// Minimal fake for IKontoService i tester.
/// </summary>
public class FakeKontoService : IKontoService
{
    private readonly Dictionary<string, Konto> _kontoer = new();

    public void LeggTilKonto(Konto konto) => _kontoer[konto.Kontonummer] = konto;

    public Task<Konto?> HentKontoAsync(string kontonummer, CancellationToken ct = default)
        => Task.FromResult(_kontoer.GetValueOrDefault(kontonummer));

    public Task<bool> KontoFinnesOgErAktivAsync(string kontonummer, CancellationToken ct = default)
        => Task.FromResult(_kontoer.ContainsKey(kontonummer) && _kontoer[kontonummer].ErAktiv);

    public Task<Konto> HentKontoEllerKastAsync(string kontonummer, CancellationToken ct = default)
        => _kontoer.TryGetValue(kontonummer, out var k)
            ? Task.FromResult(k)
            : throw new KontoIkkeFunnetException(kontonummer);

    public Task<IReadOnlyList<Konto>> HentKontoerForGruppeAsync(int gruppekode, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Konto>>(new List<Konto>());

    public Task<Kontotype> HentKontotypeAsync(string kontonummer, CancellationToken ct = default)
        => Task.FromResult(_kontoer[kontonummer].Kontotype);

    public Task<Normalbalanse> HentNormalbalanseAsync(string kontonummer, CancellationToken ct = default)
        => Task.FromResult(_kontoer[kontonummer].Normalbalanse);

    // Ikke brukt i disse testene
    public Task<Konto> OpprettKontoAsync(OpprettKontoRequest request, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<Konto> OppdaterKontoAsync(string kontonummer, OppdaterKontoRequest request, CancellationToken ct = default) => throw new NotImplementedException();
    public Task SlettKontoAsync(string kontonummer, CancellationToken ct = default) => throw new NotImplementedException();
    public Task DeaktiverKontoAsync(string kontonummer, CancellationToken ct = default) => throw new NotImplementedException();
    public Task AktiverKontoAsync(string kontonummer, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<(List<Konto> Data, int TotaltAntall)> HentKontoerAsync(int? kontoklasse = null, Kontotype? kontotype = null, int? gruppekode = null, bool? erAktiv = null, bool? erBokforbar = null, string? sok = null, int side = 1, int antall = 50, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<Konto?> HentKontoMedDetaljerAsync(string kontonummer, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<List<Konto>> SokKontoerAsync(string query, int antall = 10, CancellationToken ct = default) => throw new NotImplementedException();
}

public class HovedbokServiceTests
{
    private readonly FakeHovedbokRepository _repo;
    private readonly FakeKontoService _kontoService;
    private readonly HovedbokService _sut;

    public HovedbokServiceTests()
    {
        _repo = new FakeHovedbokRepository();
        _kontoService = new FakeKontoService();
        _sut = new HovedbokService(_repo, _kontoService);

        // Sett opp standard testkontoer
        _kontoService.LeggTilKonto(LagKonto("1920", "Bankinnskudd", Kontotype.Eiendel, Normalbalanse.Debet));
        _kontoService.LeggTilKonto(LagKonto("2400", "Leverandorgjeld", Kontotype.Gjeld, Normalbalanse.Kredit));
        _kontoService.LeggTilKonto(LagKonto("4000", "Varekostnad", Kontotype.Kostnad, Normalbalanse.Debet));
        _kontoService.LeggTilKonto(LagKonto("6540", "Inventar", Kontotype.Kostnad, Normalbalanse.Debet));
        _kontoService.LeggTilKonto(LagKonto("3000", "Salgsinntekt", Kontotype.Inntekt, Normalbalanse.Kredit));
    }

    // --- Saldobalanse ---

    [Fact]
    public async Task HentSaldobalanse_ReturnererKontoerMedAktivitet()
    {
        var periodeId = Guid.NewGuid();
        _repo.Perioder.Add(LagPeriode(2026, 1, periodeId));

        _repo.SaldoListe.Add(LagSaldo("1920", 2026, 1, periodeId, 500000m, 100000m, 50000m));
        _repo.SaldoListe.Add(LagSaldo("2400", 2026, 1, periodeId, -120000m, 60000m, 95000m));

        var result = await _sut.HentSaldobalanseAsync(2026, 1);

        result.Kontoer.Should().HaveCount(2);
        result.Ar.Should().Be(2026);
        result.Periode.Should().Be(1);
    }

    [Fact]
    public async Task HentSaldobalanse_TotalDebetLikTotalKredit()
    {
        var periodeId = Guid.NewGuid();
        _repo.Perioder.Add(LagPeriode(2026, 1, periodeId));

        _repo.SaldoListe.Add(LagSaldo("1920", 2026, 1, periodeId, 0m, 10000m, 0m));
        _repo.SaldoListe.Add(LagSaldo("2400", 2026, 1, periodeId, 0m, 0m, 10000m));

        var result = await _sut.HentSaldobalanseAsync(2026, 1);

        result.TotalSumDebet.Should().Be(10000m);
        result.TotalSumKredit.Should().Be(10000m);
        result.ErIBalanse.Should().BeTrue();
    }

    [Fact]
    public async Task HentSaldobalanse_FiltrererPaKontoklasse()
    {
        var periodeId = Guid.NewGuid();
        _repo.Perioder.Add(LagPeriode(2026, 1, periodeId));

        _repo.SaldoListe.Add(LagSaldo("1920", 2026, 1, periodeId, 0m, 10000m, 0m));
        _repo.SaldoListe.Add(LagSaldo("4000", 2026, 1, periodeId, 0m, 5000m, 0m));

        var result = await _sut.HentSaldobalanseAsync(2026, 1, kontoklasse: 4);

        result.Kontoer.Should().HaveCount(1);
        result.Kontoer[0].Kontonummer.Should().Be("4000");
    }

    [Fact]
    public async Task HentSaldobalanse_UtelaterNullsaldoSomStandard()
    {
        var periodeId = Guid.NewGuid();
        _repo.Perioder.Add(LagPeriode(2026, 1, periodeId));

        _repo.SaldoListe.Add(LagSaldo("1920", 2026, 1, periodeId, 0m, 10000m, 0m));
        _repo.SaldoListe.Add(LagSaldo("4000", 2026, 1, periodeId, 0m, 0m, 0m)); // null-saldo

        var result = await _sut.HentSaldobalanseAsync(2026, 1, inkluderNullsaldo: false);

        result.Kontoer.Should().HaveCount(1);
    }

    [Fact]
    public async Task HentSaldobalanse_KasterHvisPeriodeIkkeFinnes()
    {
        var act = () => _sut.HentSaldobalanseAsync(2026, 1);

        await act.Should().ThrowAsync<PeriodeIkkeFunnetException>();
    }

    // --- Saldooppslag ---

    [Fact]
    public async Task HentKontoSaldo_ReturnererPeriodeHistorikk()
    {
        var p1Id = Guid.NewGuid();
        var p2Id = Guid.NewGuid();

        _repo.SaldoListe.Add(LagSaldo("1920", 2026, 1, p1Id, 500000m, 100000m, 50000m));
        _repo.SaldoListe.Add(LagSaldo("1920", 2026, 2, p2Id, 550000m, 80000m, 60000m));

        var result = await _sut.HentKontoSaldoAsync("1920", 2026);

        result.Kontonummer.Should().Be("1920");
        result.Perioder.Should().HaveCount(2);
        result.TotalInngaendeBalanse.Should().Be(500000m);
        result.TotalUtgaendeBalanse.Should().Be(570000m);
    }

    [Fact]
    public async Task HentKontoSaldo_FiltrererPaPeriodespenn()
    {
        var p1Id = Guid.NewGuid();
        var p2Id = Guid.NewGuid();
        var p3Id = Guid.NewGuid();

        _repo.SaldoListe.Add(LagSaldo("1920", 2026, 1, p1Id, 500000m, 100000m, 50000m));
        _repo.SaldoListe.Add(LagSaldo("1920", 2026, 2, p2Id, 550000m, 80000m, 60000m));
        _repo.SaldoListe.Add(LagSaldo("1920", 2026, 3, p3Id, 570000m, 50000m, 30000m));

        var result = await _sut.HentKontoSaldoAsync("1920", 2026, fraPeriode: 2, tilPeriode: 2);

        result.Perioder.Should().HaveCount(1);
        result.Perioder[0].Periode.Should().Be(2);
    }

    [Fact]
    public async Task HentKontoSaldo_KasterHvisKontoIkkeFinnes()
    {
        var act = () => _sut.HentKontoSaldoAsync("9999", 2026);

        await act.Should().ThrowAsync<KontoIkkeFunnetException>();
    }

    // --- Kontoutskrift ---

    [Fact]
    public async Task HentKontoutskrift_ReturnererPosteringerMedLopendeBalanse()
    {
        var bilag = new Bilag
        {
            Id = Guid.NewGuid(),
            Bilagsnummer = 1,
            Ar = 2026,
            Bilagsdato = new DateOnly(2026, 1, 15),
            Beskrivelse = "Test bilag"
        };

        _repo.PosteringListe.Add(new Postering
        {
            Id = Guid.NewGuid(),
            BilagId = bilag.Id,
            Bilag = bilag,
            Linjenummer = 1,
            Kontonummer = "1920",
            Side = BokforingSide.Debet,
            Belop = new Belop(5000m),
            Beskrivelse = "Innbetaling",
            Bilagsdato = new DateOnly(2026, 1, 15)
        });

        var result = await _sut.HentKontoutskriftAsync(
            "1920", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        result.Kontonummer.Should().Be("1920");
        result.Posteringer.Should().HaveCount(1);
        result.Posteringer[0].LopendeBalanse.Should().Be(5000m);
        result.UtgaendeBalanse.Should().Be(5000m);
    }

    // --- Hjelpemetoder ---

    private static Konto LagKonto(string kontonummer, string navn, Kontotype type, Normalbalanse normalbalanse)
    {
        return new Konto
        {
            Id = Guid.NewGuid(),
            Kontonummer = kontonummer,
            Navn = navn,
            Kontotype = type,
            Normalbalanse = normalbalanse,
            StandardAccountId = kontonummer,
            ErAktiv = true,
            ErBokforbar = true,
            KontogruppeId = Guid.NewGuid()
        };
    }

    private static Regnskapsperiode LagPeriode(int ar, int periode, Guid id)
    {
        return new Regnskapsperiode
        {
            Id = id,
            Ar = ar,
            Periode = periode,
            FraDato = new DateOnly(ar, periode, 1),
            TilDato = new DateOnly(ar, periode, 1).AddMonths(1).AddDays(-1),
            Status = PeriodeStatus.Apen
        };
    }

    private static KontoSaldo LagSaldo(
        string kontonummer, int ar, int periode, Guid periodeId,
        decimal ib, decimal debet, decimal kredit)
    {
        return new KontoSaldo
        {
            Id = Guid.NewGuid(),
            Kontonummer = kontonummer,
            KontoId = Guid.NewGuid(),
            Konto = new Konto
            {
                Kontonummer = kontonummer,
                Navn = kontonummer,
                Kontotype = kontonummer.StartsWith('1') ? Kontotype.Eiendel :
                            kontonummer.StartsWith('2') ? Kontotype.Gjeld :
                            kontonummer.StartsWith('3') ? Kontotype.Inntekt : Kontotype.Kostnad,
                Normalbalanse = kontonummer.StartsWith('1') || kontonummer.StartsWith('4')
                    ? Normalbalanse.Debet : Normalbalanse.Kredit,
                StandardAccountId = kontonummer,
                KontogruppeId = Guid.NewGuid()
            },
            Ar = ar,
            Periode = periode,
            RegnskapsperiodeId = periodeId,
            InngaendeBalanse = new Belop(ib),
            SumDebet = new Belop(debet),
            SumKredit = new Belop(kredit),
            AntallPosteringer = 1
        };
    }
}
