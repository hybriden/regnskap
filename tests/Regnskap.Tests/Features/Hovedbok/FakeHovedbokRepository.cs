using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Tests.Features.Hovedbok;

/// <summary>
/// In-memory fake repository for unit testing uten database.
/// </summary>
public class FakeHovedbokRepository : IHovedbokRepository
{
    public List<Regnskapsperiode> Perioder { get; } = new();
    public List<Bilag> BilagListe { get; } = new();
    public List<Postering> PosteringListe { get; } = new();
    public List<KontoSaldo> SaldoListe { get; } = new();
    public int SaveCount { get; private set; }

    // --- Regnskapsperioder ---

    public Task<Regnskapsperiode?> HentPeriodeAsync(int ar, int periode, CancellationToken ct = default)
        => Task.FromResult(Perioder.FirstOrDefault(p => p.Ar == ar && p.Periode == periode));

    public Task<Regnskapsperiode?> HentPeriodeForDatoAsync(DateOnly dato, CancellationToken ct = default)
        => Task.FromResult(Perioder.FirstOrDefault(p =>
            p.FraDato <= dato && p.TilDato >= dato && p.Periode >= 1 && p.Periode <= 12));

    public Task<List<Regnskapsperiode>> HentPerioderForArAsync(int ar, CancellationToken ct = default)
        => Task.FromResult(Perioder.Where(p => p.Ar == ar).OrderBy(p => p.Periode).ToList());

    public Task<List<Regnskapsperiode>> HentApnePerioderAsync(CancellationToken ct = default)
        => Task.FromResult(Perioder.Where(p => p.Status == PeriodeStatus.Apen).ToList());

    public Task LeggTilPeriodeAsync(Regnskapsperiode periode, CancellationToken ct = default)
    {
        Perioder.Add(periode);
        return Task.CompletedTask;
    }

    public Task<bool> PeriodeFinnesAsync(int ar, int periode, CancellationToken ct = default)
        => Task.FromResult(Perioder.Any(p => p.Ar == ar && p.Periode == periode));

    // --- Bilag ---

    public Task<Bilag?> HentBilagAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(BilagListe.FirstOrDefault(b => b.Id == id));

    public Task<Bilag?> HentBilagMedPosteringerAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(BilagListe.FirstOrDefault(b => b.Id == id));

    public Task<Bilag?> HentBilagMedNummerAsync(int ar, int bilagsnummer, CancellationToken ct = default)
        => Task.FromResult(BilagListe.FirstOrDefault(b => b.Ar == ar && b.Bilagsnummer == bilagsnummer));

    public Task<int> NestebilagsnummerAsync(int ar, CancellationToken ct = default)
    {
        var max = BilagListe.Where(b => b.Ar == ar).Select(b => b.Bilagsnummer).DefaultIfEmpty(0).Max();
        return Task.FromResult(max + 1);
    }

    public Task LeggTilBilagAsync(Bilag bilag, CancellationToken ct = default)
    {
        BilagListe.Add(bilag);
        return Task.CompletedTask;
    }

    public Task<List<Bilag>> HentBilagForPeriodeAsync(
        int ar, int? periode = null, BilagType? type = null,
        int side = 1, int antall = 50, CancellationToken ct = default)
    {
        var query = BilagListe.Where(b => b.Ar == ar).AsEnumerable();
        if (periode.HasValue)
            query = query.Where(b => b.Regnskapsperiode?.Periode == periode.Value);
        if (type.HasValue)
            query = query.Where(b => b.Type == type.Value);
        return Task.FromResult(query.Skip((side - 1) * antall).Take(antall).ToList());
    }

    public Task<List<int>> HentBilagsnumreForArAsync(int ar, CancellationToken ct = default)
        => Task.FromResult(BilagListe.Where(b => b.Ar == ar)
            .Select(b => b.Bilagsnummer).OrderBy(n => n).ToList());

    public Task<int> TellBilagForPeriodeAsync(int ar, int? periode = null, BilagType? type = null, CancellationToken ct = default)
    {
        var query = BilagListe.Where(b => b.Ar == ar).AsEnumerable();
        if (periode.HasValue)
            query = query.Where(b => b.Regnskapsperiode?.Periode == periode.Value);
        if (type.HasValue)
            query = query.Where(b => b.Type == type.Value);
        return Task.FromResult(query.Count());
    }

    // --- Posteringer ---

    public Task<List<Postering>> HentPosteringerForKontoAsync(
        string kontonummer, DateOnly? fraDato = null, DateOnly? tilDato = null,
        int side = 1, int antall = 100, CancellationToken ct = default)
    {
        var query = PosteringListe.Where(p => p.Kontonummer == kontonummer).AsEnumerable();
        if (fraDato.HasValue)
            query = query.Where(p => p.Bilagsdato >= fraDato.Value);
        if (tilDato.HasValue)
            query = query.Where(p => p.Bilagsdato <= tilDato.Value);
        return Task.FromResult(query.OrderBy(p => p.Bilagsdato).Skip((side - 1) * antall).Take(antall).ToList());
    }

    public Task<int> TellPosteringerForKontoAsync(
        string kontonummer, DateOnly? fraDato = null, DateOnly? tilDato = null,
        CancellationToken ct = default)
    {
        var query = PosteringListe.Where(p => p.Kontonummer == kontonummer).AsEnumerable();
        if (fraDato.HasValue)
            query = query.Where(p => p.Bilagsdato >= fraDato.Value);
        if (tilDato.HasValue)
            query = query.Where(p => p.Bilagsdato <= tilDato.Value);
        return Task.FromResult(query.Count());
    }

    public Task<bool> PeriodeHarPosteringerAsync(int ar, int periode, CancellationToken ct = default)
        => Task.FromResult(PosteringListe.Any());

    // --- KontoSaldo ---

    public Task<KontoSaldo?> HentKontoSaldoAsync(
        string kontonummer, int ar, int periode, CancellationToken ct = default)
        => Task.FromResult(SaldoListe.FirstOrDefault(s =>
            s.Kontonummer == kontonummer && s.Ar == ar && s.Periode == periode));

    public Task<List<KontoSaldo>> HentAlleSaldoerForPeriodeAsync(
        int ar, int periode, CancellationToken ct = default)
        => Task.FromResult(SaldoListe.Where(s => s.Ar == ar && s.Periode == periode)
            .OrderBy(s => s.Kontonummer).ToList());

    public Task<List<KontoSaldo>> HentSaldoHistorikkForKontoAsync(
        string kontonummer, int ar, CancellationToken ct = default)
        => Task.FromResult(SaldoListe.Where(s => s.Kontonummer == kontonummer && s.Ar == ar)
            .OrderBy(s => s.Periode).ToList());

    public Task LeggTilKontoSaldoAsync(KontoSaldo saldo, CancellationToken ct = default)
    {
        SaldoListe.Add(saldo);
        return Task.CompletedTask;
    }

    // --- Generelt ---

    public Task LagreEndringerAsync(CancellationToken ct = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
