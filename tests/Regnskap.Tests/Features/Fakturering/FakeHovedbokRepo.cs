using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Tests.Features.Fakturering;

/// <summary>
/// Minimal fake for IHovedbokRepository, only implements methods needed by FaktureringService.
/// </summary>
public class FakeHovedbokRepo : IHovedbokRepository
{
    public Regnskapsperiode? PeriodeForDato { get; set; }

    public Task<Regnskapsperiode?> HentPeriodeForDatoAsync(DateOnly dato, CancellationToken ct = default)
        => Task.FromResult(PeriodeForDato);

    // Unused methods - minimal implementation
    public Task<Regnskapsperiode?> HentPeriodeAsync(int ar, int periode, CancellationToken ct = default) => Task.FromResult<Regnskapsperiode?>(null);
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
    public Task<KontoSaldo?> HentKontoSaldoAsync(string kontonummer, int ar, int periode, CancellationToken ct = default) => Task.FromResult<KontoSaldo?>(null);
    public Task<List<KontoSaldo>> HentAlleSaldoerForPeriodeAsync(int ar, int periode, CancellationToken ct = default) => Task.FromResult(new List<KontoSaldo>());
    public Task<List<KontoSaldo>> HentSaldoHistorikkForKontoAsync(string kontonummer, int ar, CancellationToken ct = default) => Task.FromResult(new List<KontoSaldo>());
    public Task LeggTilKontoSaldoAsync(KontoSaldo saldo, CancellationToken ct = default) => Task.CompletedTask;
    public Task LagreEndringerAsync(CancellationToken ct = default) => Task.CompletedTask;
}
