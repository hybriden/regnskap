using FluentAssertions;
using Regnskap.Application.Features.Bank;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Bankavstemming;
using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Tests.Features.Bank;

public class FakeHovedbokRepoForBank : IHovedbokRepository
{
    public Dictionary<(string, int, int), KontoSaldo> Saldoer { get; } = new();

    public Task<KontoSaldo?> HentKontoSaldoAsync(string kontonummer, int ar, int periode, CancellationToken ct = default)
    {
        Saldoer.TryGetValue((kontonummer, ar, periode), out var saldo);
        return Task.FromResult(saldo);
    }

    // Unused
    public Task<Regnskapsperiode?> HentPeriodeAsync(int ar, int periode, CancellationToken ct = default) => Task.FromResult<Regnskapsperiode?>(null);
    public Task<Regnskapsperiode?> HentPeriodeForDatoAsync(DateOnly dato, CancellationToken ct = default) => Task.FromResult<Regnskapsperiode?>(null);
    public Task<List<Regnskapsperiode>> HentPerioderForArAsync(int ar, CancellationToken ct = default) => Task.FromResult(new List<Regnskapsperiode>());
    public Task<List<Regnskapsperiode>> HentApnePerioderAsync(CancellationToken ct = default) => Task.FromResult(new List<Regnskapsperiode>());
    public Task LeggTilPeriodeAsync(Regnskapsperiode periode, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> PeriodeFinnesAsync(int ar, int periode, CancellationToken ct = default) => Task.FromResult(false);
    public Task<Bilag?> HentBilagAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Bilag?>(null);
    public Task<Bilag?> HentBilagMedPosteringerAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Bilag?>(null);
    public Task<Bilag?> HentBilagMedNummerAsync(int ar, int bilagsnummer, CancellationToken ct = default) => Task.FromResult<Bilag?>(null);
    public Task<int> NestebilagsnummerAsync(int ar, CancellationToken ct = default) => Task.FromResult(1);
    public Task LeggTilBilagAsync(Bilag bilag, CancellationToken ct = default) => Task.CompletedTask;
    public Task<List<Bilag>> HentBilagForPeriodeAsync(int ar, int? periode = null, BilagType? type = null, int side = 1, int antall = 50, CancellationToken ct = default) => Task.FromResult(new List<Bilag>());
    public Task<int> TellBilagForPeriodeAsync(int ar, int? periode = null, BilagType? type = null, CancellationToken ct = default) => Task.FromResult(0);
    public Task<List<int>> HentBilagsnumreForArAsync(int ar, CancellationToken ct = default) => Task.FromResult(new List<int>());
    public Task<List<Postering>> HentPosteringerForKontoAsync(string kontonummer, DateOnly? fraDato = null, DateOnly? tilDato = null, int side = 1, int antall = 100, CancellationToken ct = default) => Task.FromResult(new List<Postering>());
    public Task<int> TellPosteringerForKontoAsync(string kontonummer, DateOnly? fraDato = null, DateOnly? tilDato = null, CancellationToken ct = default) => Task.FromResult(0);
    public Task<bool> PeriodeHarPosteringerAsync(int ar, int periode, CancellationToken ct = default) => Task.FromResult(false);
    public Task<List<KontoSaldo>> HentAlleSaldoerForPeriodeAsync(int ar, int periode, CancellationToken ct = default) => Task.FromResult(new List<KontoSaldo>());
    public Task<List<KontoSaldo>> HentSaldoHistorikkForKontoAsync(string kontonummer, int ar, CancellationToken ct = default) => Task.FromResult(new List<KontoSaldo>());
    public Task LeggTilKontoSaldoAsync(KontoSaldo saldo, CancellationToken ct = default) => Task.CompletedTask;
    public Task LagreEndringerAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public class BankavstemmingServiceTests
{
    private readonly FakeBankRepository _bankRepo;
    private readonly FakeHovedbokRepoForBank _hovedbokRepo;
    private readonly BankavstemmingService _sut;
    private readonly Guid _bankkontoId = Guid.NewGuid();

    public BankavstemmingServiceTests()
    {
        _bankRepo = new FakeBankRepository();
        _hovedbokRepo = new FakeHovedbokRepoForBank();
        _sut = new BankavstemmingService(_bankRepo, _hovedbokRepo);

        _bankRepo.Bankkontoer.Add(new Bankkonto
        {
            Id = _bankkontoId,
            Kontonummer = "12345678903",
            Banknavn = "DNB",
            Beskrivelse = "Drift",
            Hovedbokkontonummer = "1920",
            HovedbokkkontoId = Guid.NewGuid()
        });
    }

    [Fact]
    public async Task HentEllerOpprett_OppretterNyAvstemming()
    {
        _hovedbokRepo.Saldoer[("1920", 2026, 3)] = new KontoSaldo
        {
            InngaendeBalanse = new Belop(50000m),
            SumDebet = new Belop(10000m),
            SumKredit = new Belop(5000m)
        };

        var avstemming = await _sut.HentEllerOpprett(_bankkontoId, 2026, 3);

        avstemming.BankkontoId.Should().Be(_bankkontoId);
        avstemming.Ar.Should().Be(2026);
        avstemming.Periode.Should().Be(3);
        avstemming.SaldoHovedbok.Verdi.Should().Be(55000m); // 50000 + 10000 - 5000
        avstemming.Status.Should().Be(AvstemmingStatus.UnderArbeid);
    }

    [Fact]
    public async Task HentEllerOpprett_ReturnererEksisterende()
    {
        var eksisterende = new Bankavstemming
        {
            Id = Guid.NewGuid(),
            BankkontoId = _bankkontoId,
            Ar = 2026,
            Periode = 3,
            SaldoHovedbok = new Belop(55000m),
            SaldoBank = new Belop(55000m),
            Differanse = new Belop(0m)
        };
        _bankRepo.Avstemminger.Add(eksisterende);

        var resultat = await _sut.HentEllerOpprett(_bankkontoId, 2026, 3);
        resultat.Id.Should().Be(eksisterende.Id);
    }

    [Fact]
    public async Task Oppdater_SetteTidsavgrensninger()
    {
        var avstemming = new Bankavstemming
        {
            Id = Guid.NewGuid(),
            BankkontoId = _bankkontoId,
            Ar = 2026, Periode = 3,
            SaldoHovedbok = new Belop(50000m),
            SaldoBank = new Belop(52000m),
            Differanse = new Belop(2000m)
        };
        _bankRepo.Avstemminger.Add(avstemming);

        var oppdatert = await _sut.Oppdater(avstemming.Id, new OppdaterAvstemmingRequest(
            UtestaaendeBetalinger: 500m,
            InnbetalingerITransitt: 1500m,
            AndreDifferanser: 0m,
            DifferanseForklaring: null));

        oppdatert.UtestaaendeBetalinger.Verdi.Should().Be(500m);
        oppdatert.InnbetalingerITransitt.Verdi.Should().Be(1500m);
    }

    [Fact]
    public async Task Godkjenn_NullDifferanse_SetterAvstemtStatus()
    {
        var avstemming = new Bankavstemming
        {
            Id = Guid.NewGuid(),
            BankkontoId = _bankkontoId,
            Ar = 2026, Periode = 3,
            SaldoHovedbok = new Belop(50000m),
            SaldoBank = new Belop(50000m),
            Differanse = new Belop(0m)
        };
        _bankRepo.Avstemminger.Add(avstemming);

        var godkjent = await _sut.Godkjenn(avstemming.Id, "testbruker");

        godkjent.Status.Should().Be(AvstemmingStatus.Avstemt);
        godkjent.GodkjentAv.Should().Be("testbruker");
        godkjent.GodkjentTidspunkt.Should().NotBeNull();
    }

    [Fact]
    public async Task Godkjenn_UforklartDifferanseUtenForklaring_KasterException()
    {
        var avstemming = new Bankavstemming
        {
            Id = Guid.NewGuid(),
            BankkontoId = _bankkontoId,
            Ar = 2026, Periode = 3,
            SaldoHovedbok = new Belop(50000m),
            SaldoBank = new Belop(52000m),
            Differanse = new Belop(2000m)
        };
        _bankRepo.Avstemminger.Add(avstemming);

        var act = () => _sut.Godkjenn(avstemming.Id, "testbruker");
        await act.Should().ThrowAsync<AvstemmingUforklartException>();
    }
}
