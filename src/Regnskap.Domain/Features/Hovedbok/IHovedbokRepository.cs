namespace Regnskap.Domain.Features.Hovedbok;

public interface IHovedbokRepository
{
    // --- Regnskapsperioder ---
    Task<Regnskapsperiode?> HentPeriodeAsync(int ar, int periode, CancellationToken ct = default);
    Task<Regnskapsperiode?> HentPeriodeForDatoAsync(DateOnly dato, CancellationToken ct = default);
    Task<List<Regnskapsperiode>> HentPerioderForArAsync(int ar, CancellationToken ct = default);
    Task<List<Regnskapsperiode>> HentApnePerioderAsync(CancellationToken ct = default);
    Task LeggTilPeriodeAsync(Regnskapsperiode periode, CancellationToken ct = default);
    Task<bool> PeriodeFinnesAsync(int ar, int periode, CancellationToken ct = default);

    // --- Bilag ---
    Task<Bilag?> HentBilagAsync(Guid id, CancellationToken ct = default);
    Task<Bilag?> HentBilagMedPosteringerAsync(Guid id, CancellationToken ct = default);
    Task<Bilag?> HentBilagMedNummerAsync(int ar, int bilagsnummer, CancellationToken ct = default);
    Task<int> NestebilagsnummerAsync(int ar, CancellationToken ct = default);
    Task LeggTilBilagAsync(Bilag bilag, CancellationToken ct = default);
    Task<List<Bilag>> HentBilagForPeriodeAsync(
        int ar, int? periode = null,
        BilagType? type = null,
        int side = 1, int antall = 50,
        CancellationToken ct = default);
    Task<int> TellBilagForPeriodeAsync(int ar, int? periode = null, BilagType? type = null, CancellationToken ct = default);
    Task<List<int>> HentBilagsnumreForArAsync(int ar, CancellationToken ct = default);

    // --- Posteringer ---
    Task<List<Postering>> HentPosteringerForKontoAsync(
        string kontonummer,
        DateOnly? fraDato = null,
        DateOnly? tilDato = null,
        int side = 1,
        int antall = 100,
        CancellationToken ct = default);
    Task<int> TellPosteringerForKontoAsync(
        string kontonummer,
        DateOnly? fraDato = null,
        DateOnly? tilDato = null,
        CancellationToken ct = default);
    Task<bool> PeriodeHarPosteringerAsync(int ar, int periode, CancellationToken ct = default);

    // --- KontoSaldo ---
    Task<KontoSaldo?> HentKontoSaldoAsync(
        string kontonummer, int ar, int periode, CancellationToken ct = default);
    Task<List<KontoSaldo>> HentAlleSaldoerForPeriodeAsync(
        int ar, int periode, CancellationToken ct = default);
    Task<List<KontoSaldo>> HentSaldoHistorikkForKontoAsync(
        string kontonummer, int ar, CancellationToken ct = default);
    Task LeggTilKontoSaldoAsync(KontoSaldo saldo, CancellationToken ct = default);

    // --- Generelt ---
    Task LagreEndringerAsync(CancellationToken ct = default);
}
