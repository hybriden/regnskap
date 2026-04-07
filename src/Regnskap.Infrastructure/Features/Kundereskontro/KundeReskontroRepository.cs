using Microsoft.EntityFrameworkCore;
using Regnskap.Domain.Features.Kundereskontro;
using Regnskap.Infrastructure.Persistence;

namespace Regnskap.Infrastructure.Features.Kundereskontro;

public class KundeReskontroRepository : IKundeReskontroRepository
{
    private readonly RegnskapDbContext _db;

    public KundeReskontroRepository(RegnskapDbContext db)
    {
        _db = db;
    }

    // --- Kunde ---

    public async Task<Kunde?> HentKundeAsync(Guid id, CancellationToken ct = default)
        => await _db.Kunder.FirstOrDefaultAsync(k => k.Id == id, ct);

    public async Task<Kunde?> HentKundeMedNummerAsync(string kundenummer, CancellationToken ct = default)
        => await _db.Kunder.FirstOrDefaultAsync(k => k.Kundenummer == kundenummer, ct);

    public async Task<bool> KundenummerEksistererAsync(string kundenummer, CancellationToken ct = default)
        => await _db.Kunder.AnyAsync(k => k.Kundenummer == kundenummer, ct);

    public async Task LeggTilKundeAsync(Kunde kunde, CancellationToken ct = default)
        => await _db.Kunder.AddAsync(kunde, ct);

    public Task OppdaterKundeAsync(Kunde kunde, CancellationToken ct = default)
    {
        _db.Kunder.Update(kunde);
        return Task.CompletedTask;
    }

    public async Task<(List<Kunde> Data, int TotaltAntall)> SokKunderAsync(string? query, int side, int antall, CancellationToken ct = default)
    {
        var q = _db.Kunder.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(k =>
                k.Kundenummer.Contains(query) ||
                k.Navn.Contains(query) ||
                (k.Organisasjonsnummer != null && k.Organisasjonsnummer.Contains(query)));
        }

        var totalt = await q.CountAsync(ct);
        var data = await q
            .OrderBy(k => k.Kundenummer)
            .Skip((side - 1) * antall)
            .Take(antall)
            .ToListAsync(ct);

        return (data, totalt);
    }

    // --- Faktura ---

    public async Task<KundeFaktura?> HentFakturaAsync(Guid id, CancellationToken ct = default)
        => await _db.KundeFakturaer
            .Include(f => f.Kunde)
            .Include(f => f.Linjer)
            .Include(f => f.Innbetalinger)
            .Include(f => f.Purringer)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<KundeFaktura?> HentFakturaMedKidAsync(string kidNummer, CancellationToken ct = default)
        => await _db.KundeFakturaer
            .Include(f => f.Kunde)
            .FirstOrDefaultAsync(f => f.KidNummer == kidNummer, ct);

    public async Task<int> NesteNummer(CancellationToken ct = default)
    {
        // IgnoreQueryFilters() sikrer at soft-deleted fakturaer ikke skaper hull i sekvensen
        // (Bokforingsforskriften 5-1-1: kontrollerbar, sammenhengende nummersekvens)
        var maks = await _db.KundeFakturaer.IgnoreQueryFilters().MaxAsync(f => (int?)f.Fakturanummer, ct);
        return (maks ?? 0) + 1;
    }

    public async Task LeggTilFakturaAsync(KundeFaktura faktura, CancellationToken ct = default)
        => await _db.KundeFakturaer.AddAsync(faktura, ct);

    public Task OppdaterFakturaAsync(KundeFaktura faktura, CancellationToken ct = default)
    {
        _db.KundeFakturaer.Update(faktura);
        return Task.CompletedTask;
    }

    public async Task<List<KundeFaktura>> HentApnePosterAsync(DateOnly? dato = null, CancellationToken ct = default)
    {
        var q = _db.KundeFakturaer
            .Include(f => f.Kunde)
            .Where(f => f.GjenstaendeBelop.Verdi != 0 &&
                        f.Status != KundeFakturaStatus.Betalt &&
                        f.Status != KundeFakturaStatus.Tap);

        if (dato.HasValue)
            q = q.Where(f => f.Fakturadato <= dato.Value);

        return await q.ToListAsync(ct);
    }

    public async Task<List<KundeFaktura>> HentForfalteFakturaerAsync(DateOnly dato, int minimumDagerForfalt, CancellationToken ct = default)
    {
        var grenseDato = dato.AddDays(-minimumDagerForfalt);
        return await _db.KundeFakturaer
            .Include(f => f.Kunde)
            .Where(f => f.Forfallsdato <= grenseDato &&
                        f.GjenstaendeBelop.Verdi > 0 &&
                        f.Status != KundeFakturaStatus.Betalt &&
                        f.Status != KundeFakturaStatus.Tap)
            .ToListAsync(ct);
    }

    public async Task<(List<KundeFaktura> Data, int TotaltAntall)> SokFakturaerAsync(Guid? kundeId, KundeFakturaStatus? status, int side, int antall, CancellationToken ct = default)
    {
        var q = _db.KundeFakturaer
            .Include(f => f.Kunde)
            .AsQueryable();

        if (kundeId.HasValue)
            q = q.Where(f => f.KundeId == kundeId.Value);
        if (status.HasValue)
            q = q.Where(f => f.Status == status.Value);

        var totalt = await q.CountAsync(ct);
        var data = await q
            .OrderByDescending(f => f.Fakturanummer)
            .Skip((side - 1) * antall)
            .Take(antall)
            .ToListAsync(ct);

        return (data, totalt);
    }

    public async Task<List<KundeFaktura>> HentFakturaerForKundeAsync(Guid kundeId, DateOnly? fraDato, DateOnly? tilDato, CancellationToken ct = default)
    {
        var q = _db.KundeFakturaer
            .Include(f => f.Innbetalinger)
            .Where(f => f.KundeId == kundeId);

        if (fraDato.HasValue)
            q = q.Where(f => f.Fakturadato >= fraDato.Value);
        if (tilDato.HasValue)
            q = q.Where(f => f.Fakturadato <= tilDato.Value);

        return await q.OrderBy(f => f.Fakturadato).ThenBy(f => f.Fakturanummer).ToListAsync(ct);
    }

    // --- Innbetaling ---

    public async Task LeggTilInnbetalingAsync(KundeInnbetaling innbetaling, CancellationToken ct = default)
        => await _db.KundeInnbetalinger.AddAsync(innbetaling, ct);

    // --- Purring ---

    public async Task LeggTilPurringAsync(Purring purring, CancellationToken ct = default)
        => await _db.Purringer.AddAsync(purring, ct);

    public async Task<Purring?> HentSistePurringAsync(Guid fakturaId, CancellationToken ct = default)
        => await _db.Purringer
            .Where(p => p.KundeFakturaId == fakturaId)
            .OrderByDescending(p => p.Purringsdato)
            .FirstOrDefaultAsync(ct);

    public async Task<List<Purring>> HentPurringerAsync(int side, int antall, CancellationToken ct = default)
        => await _db.Purringer
            .Include(p => p.KundeFaktura)
                .ThenInclude(f => f.Kunde)
            .OrderByDescending(p => p.Purringsdato)
            .Skip((side - 1) * antall)
            .Take(antall)
            .ToListAsync(ct);

    public async Task LagreEndringerAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
