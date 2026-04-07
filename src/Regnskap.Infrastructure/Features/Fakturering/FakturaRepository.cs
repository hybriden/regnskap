using Microsoft.EntityFrameworkCore;
using Regnskap.Domain.Features.Fakturering;
using Regnskap.Infrastructure.Persistence;

namespace Regnskap.Infrastructure.Features.Fakturering;

public class FakturaRepository : IFakturaRepository
{
    private readonly RegnskapDbContext _db;

    public FakturaRepository(RegnskapDbContext db)
    {
        _db = db;
    }

    public async Task<Faktura?> HentAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Fakturaer
            .Include(f => f.Kunde)
            .FirstOrDefaultAsync(f => f.Id == id, ct);
    }

    public async Task<Faktura?> HentMedLinjerAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Fakturaer
            .Include(f => f.Kunde)
            .Include(f => f.Linjer.OrderBy(l => l.Linjenummer))
            .Include(f => f.MvaLinjer)
            .Include(f => f.Kreditnotaer)
            .Include(f => f.KreditertFaktura)
            .FirstOrDefaultAsync(f => f.Id == id, ct);
    }

    public async Task LeggTilAsync(Faktura faktura, CancellationToken ct = default)
    {
        await _db.Fakturaer.AddAsync(faktura, ct);
    }

    public async Task<int> NesteNummerAsync(int aar, FakturaDokumenttype type, CancellationToken ct = default)
    {
        // M-2: Bruk serializable isolation for aa sikre ubrudt nummerserie under samtidige foresporsler.
        // Samme moenster som Hovedbok bilagsnummer-fix (FR-F01).
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async (cancellation) =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellation);
            try
            {
                var serie = await _db.FakturaNummerserie
                    .FirstOrDefaultAsync(n => n.Ar == aar && n.Dokumenttype == type, cancellation);

                if (serie == null)
                {
                    serie = new FakturaNummerserie
                    {
                        Id = Guid.NewGuid(),
                        Ar = aar,
                        Dokumenttype = type,
                        SisteNummer = 0,
                        Prefiks = type == FakturaDokumenttype.Faktura ? "F" : "K"
                    };
                    await _db.FakturaNummerserie.AddAsync(serie, cancellation);
                }

                serie.SisteNummer++;
                await _db.SaveChangesAsync(cancellation);
                await transaction.CommitAsync(cancellation);
                return serie.SisteNummer;
            }
            catch
            {
                await transaction.RollbackAsync(cancellation);
                throw;
            }
        }, ct);
    }

    public async Task<Selskapsinfo?> HentSelskapsinfoAsync(CancellationToken ct = default)
    {
        return await _db.Selskapsinfo.FirstOrDefaultAsync(ct);
    }

    public async Task<List<Faktura>> SokAsync(FakturaSokFilter filter, CancellationToken ct = default)
    {
        var query = BygQuery(filter);

        return await query
            .OrderByDescending(f => f.Fakturadato ?? DateOnly.MinValue)
            .ThenByDescending(f => f.CreatedAt)
            .Skip((filter.Side - 1) * filter.Antall)
            .Take(filter.Antall)
            .Include(f => f.Kunde)
            .Include(f => f.Linjer)
            .Include(f => f.MvaLinjer)
            .ToListAsync(ct);
    }

    public async Task<int> TellAsync(FakturaSokFilter filter, CancellationToken ct = default)
    {
        return await BygQuery(filter).CountAsync(ct);
    }

    public async Task LagreEndringerAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }

    private IQueryable<Faktura> BygQuery(FakturaSokFilter filter)
    {
        var query = _db.Fakturaer.AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(f => f.Status == filter.Status.Value);

        if (filter.Dokumenttype.HasValue)
            query = query.Where(f => f.Dokumenttype == filter.Dokumenttype.Value);

        if (filter.KundeId.HasValue)
            query = query.Where(f => f.KundeId == filter.KundeId.Value);

        if (filter.FraDato.HasValue)
            query = query.Where(f => f.Fakturadato >= filter.FraDato.Value);

        if (filter.TilDato.HasValue)
            query = query.Where(f => f.Fakturadato <= filter.TilDato.Value);

        if (!string.IsNullOrWhiteSpace(filter.Sok))
        {
            var sok = filter.Sok.ToLower();
            query = query.Where(f =>
                (f.KidNummer != null && f.KidNummer.Contains(sok)) ||
                (f.Merknad != null && f.Merknad.ToLower().Contains(sok)) ||
                (f.Bestillingsnummer != null && f.Bestillingsnummer.ToLower().Contains(sok)));
        }

        return query;
    }
}
