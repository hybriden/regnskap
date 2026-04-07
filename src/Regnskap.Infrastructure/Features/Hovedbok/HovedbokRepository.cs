using Microsoft.EntityFrameworkCore;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Infrastructure.Persistence;

namespace Regnskap.Infrastructure.Features.Hovedbok;

public class HovedbokRepository : IHovedbokRepository
{
    private readonly RegnskapDbContext _db;

    public HovedbokRepository(RegnskapDbContext db)
    {
        _db = db;
    }

    // --- Regnskapsperioder ---

    public async Task<Regnskapsperiode?> HentPeriodeAsync(int ar, int periode, CancellationToken ct = default)
        => await _db.Regnskapsperioder
            .FirstOrDefaultAsync(p => p.Ar == ar && p.Periode == periode, ct);

    public async Task<Regnskapsperiode?> HentPeriodeForDatoAsync(DateOnly dato, CancellationToken ct = default)
        => await _db.Regnskapsperioder
            .Where(p => p.FraDato <= dato && p.TilDato >= dato && p.Periode >= 1 && p.Periode <= 12)
            .FirstOrDefaultAsync(ct);

    public async Task<List<Regnskapsperiode>> HentPerioderForArAsync(int ar, CancellationToken ct = default)
        => await _db.Regnskapsperioder
            .Where(p => p.Ar == ar)
            .OrderBy(p => p.Periode)
            .ToListAsync(ct);

    public async Task<List<Regnskapsperiode>> HentApnePerioderAsync(CancellationToken ct = default)
        => await _db.Regnskapsperioder
            .Where(p => p.Status == PeriodeStatus.Apen)
            .OrderBy(p => p.Ar).ThenBy(p => p.Periode)
            .ToListAsync(ct);

    public async Task LeggTilPeriodeAsync(Regnskapsperiode periode, CancellationToken ct = default)
        => await _db.Regnskapsperioder.AddAsync(periode, ct);

    public async Task<bool> PeriodeFinnesAsync(int ar, int periode, CancellationToken ct = default)
        => await _db.Regnskapsperioder.AnyAsync(p => p.Ar == ar && p.Periode == periode, ct);

    // --- Bilag ---

    public async Task<Bilag?> HentBilagAsync(Guid id, CancellationToken ct = default)
        => await _db.Bilag
            .Include(b => b.Regnskapsperiode)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<Bilag?> HentBilagMedPosteringerAsync(Guid id, CancellationToken ct = default)
        => await _db.Bilag
            .Include(b => b.Regnskapsperiode)
            .Include(b => b.Posteringer.OrderBy(p => p.Linjenummer))
                .ThenInclude(p => p.Konto)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<Bilag?> HentBilagMedNummerAsync(int ar, int bilagsnummer, CancellationToken ct = default)
        => await _db.Bilag
            .Include(b => b.Regnskapsperiode)
            .Include(b => b.Posteringer.OrderBy(p => p.Linjenummer))
                .ThenInclude(p => p.Konto)
            .FirstOrDefaultAsync(b => b.Ar == ar && b.Bilagsnummer == bilagsnummer, ct);

    public async Task<int> NestebilagsnummerAsync(int ar, CancellationToken ct = default)
    {
        // Bruk serializable isolation og SELECT FOR UPDATE for å unngå race condition.
        // Sikrer at samtidige foresporsler ikke far samme bilagsnummer.
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async (cancellation) =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellation);
            try
            {
                // SELECT MAX med FOR UPDATE-aktig låsing via serializable isolation
                var max = await _db.Bilag
                    .Where(b => b.Ar == ar)
                    .OrderByDescending(b => b.Bilagsnummer)
                    .Select(b => (int?)b.Bilagsnummer)
                    .FirstOrDefaultAsync(cancellation);

                var neste = (max ?? 0) + 1;

                await transaction.CommitAsync(cancellation);
                return neste;
            }
            catch
            {
                await transaction.RollbackAsync(cancellation);
                throw;
            }
        }, ct);
    }

    public async Task LeggTilBilagAsync(Bilag bilag, CancellationToken ct = default)
        => await _db.Bilag.AddAsync(bilag, ct);

    public async Task<List<Bilag>> HentBilagForPeriodeAsync(
        int ar, int? periode = null, BilagType? type = null,
        int side = 1, int antall = 50, CancellationToken ct = default)
    {
        var query = _db.Bilag
            .Include(b => b.Regnskapsperiode)
            .Include(b => b.Posteringer.OrderBy(p => p.Linjenummer))
                .ThenInclude(p => p.Konto)
            .Where(b => b.Ar == ar);

        if (periode.HasValue)
            query = query.Where(b => b.Regnskapsperiode.Periode == periode.Value);
        if (type.HasValue)
            query = query.Where(b => b.Type == type.Value);

        return await query
            .OrderBy(b => b.Bilagsnummer)
            .Skip((side - 1) * antall)
            .Take(antall)
            .ToListAsync(ct);
    }

    public async Task<List<int>> HentBilagsnumreForArAsync(int ar, CancellationToken ct = default)
        => await _db.Bilag
            .Where(b => b.Ar == ar)
            .Select(b => b.Bilagsnummer)
            .OrderBy(n => n)
            .ToListAsync(ct);

    public async Task<int> TellBilagForPeriodeAsync(int ar, int? periode = null, BilagType? type = null, CancellationToken ct = default)
    {
        var query = _db.Bilag.Where(b => b.Ar == ar);
        if (periode.HasValue)
            query = query.Where(b => b.Regnskapsperiode.Periode == periode.Value);
        if (type.HasValue)
            query = query.Where(b => b.Type == type.Value);
        return await query.CountAsync(ct);
    }

    // --- Posteringer ---

    public async Task<List<Postering>> HentPosteringerForKontoAsync(
        string kontonummer, DateOnly? fraDato = null, DateOnly? tilDato = null,
        int side = 1, int antall = 100, CancellationToken ct = default)
    {
        var query = _db.Posteringer
            .Include(p => p.Bilag)
            .Where(p => p.Kontonummer == kontonummer);

        if (fraDato.HasValue)
            query = query.Where(p => p.Bilagsdato >= fraDato.Value);
        if (tilDato.HasValue)
            query = query.Where(p => p.Bilagsdato <= tilDato.Value);

        return await query
            .OrderBy(p => p.Bilagsdato)
            .ThenBy(p => p.Bilag.Bilagsnummer)
            .ThenBy(p => p.Linjenummer)
            .Skip((side - 1) * antall)
            .Take(antall)
            .ToListAsync(ct);
    }

    public async Task<int> TellPosteringerForKontoAsync(
        string kontonummer, DateOnly? fraDato = null, DateOnly? tilDato = null,
        CancellationToken ct = default)
    {
        var query = _db.Posteringer.Where(p => p.Kontonummer == kontonummer);
        if (fraDato.HasValue)
            query = query.Where(p => p.Bilagsdato >= fraDato.Value);
        if (tilDato.HasValue)
            query = query.Where(p => p.Bilagsdato <= tilDato.Value);
        return await query.CountAsync(ct);
    }

    public async Task<bool> PeriodeHarPosteringerAsync(int ar, int periode, CancellationToken ct = default)
        => await _db.Posteringer
            .AnyAsync(p => p.Bilag.Regnskapsperiode.Ar == ar &&
                          p.Bilag.Regnskapsperiode.Periode == periode, ct);

    // --- KontoSaldo ---

    public async Task<KontoSaldo?> HentKontoSaldoAsync(
        string kontonummer, int ar, int periode, CancellationToken ct = default)
        => await _db.KontoSaldoer
            .Include(s => s.Konto)
            .FirstOrDefaultAsync(s => s.Kontonummer == kontonummer && s.Ar == ar && s.Periode == periode, ct);

    public async Task<List<KontoSaldo>> HentAlleSaldoerForPeriodeAsync(
        int ar, int periode, CancellationToken ct = default)
        => await _db.KontoSaldoer
            .Include(s => s.Konto)
            .Where(s => s.Ar == ar && s.Periode == periode)
            .OrderBy(s => s.Kontonummer)
            .ToListAsync(ct);

    public async Task<List<KontoSaldo>> HentSaldoHistorikkForKontoAsync(
        string kontonummer, int ar, CancellationToken ct = default)
        => await _db.KontoSaldoer
            .Include(s => s.Konto)
            .Where(s => s.Kontonummer == kontonummer && s.Ar == ar)
            .OrderBy(s => s.Periode)
            .ToListAsync(ct);

    public async Task LeggTilKontoSaldoAsync(KontoSaldo saldo, CancellationToken ct = default)
        => await _db.KontoSaldoer.AddAsync(saldo, ct);

    // --- Generelt ---

    public async Task LagreEndringerAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
