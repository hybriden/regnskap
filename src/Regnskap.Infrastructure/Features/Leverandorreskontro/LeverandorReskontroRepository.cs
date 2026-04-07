using Microsoft.EntityFrameworkCore;
using Regnskap.Domain.Features.Leverandorreskontro;
using Regnskap.Infrastructure.Persistence;

namespace Regnskap.Infrastructure.Features.Leverandorreskontro;

public class LeverandorReskontroRepository : ILeverandorReskontroRepository
{
    private readonly RegnskapDbContext _db;

    public LeverandorReskontroRepository(RegnskapDbContext db)
    {
        _db = db;
    }

    // --- Leverandor ---

    public async Task<Leverandor?> HentLeverandorAsync(Guid id, CancellationToken ct = default)
        => await _db.Leverandorer.FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<Leverandor?> HentLeverandorMedNummerAsync(string leverandornummer, CancellationToken ct = default)
        => await _db.Leverandorer.FirstOrDefaultAsync(l => l.Leverandornummer == leverandornummer, ct);

    public async Task<bool> LeverandornummerEksistererAsync(string leverandornummer, CancellationToken ct = default)
        => await _db.Leverandorer.AnyAsync(l => l.Leverandornummer == leverandornummer, ct);

    public async Task<bool> OrganisasjonsnummerEksistererAsync(string organisasjonsnummer, CancellationToken ct = default)
        => await _db.Leverandorer.AnyAsync(l => l.Organisasjonsnummer == organisasjonsnummer, ct);

    public async Task LeggTilLeverandorAsync(Leverandor leverandor, CancellationToken ct = default)
        => await _db.Leverandorer.AddAsync(leverandor, ct);

    public Task OppdaterLeverandorAsync(Leverandor leverandor, CancellationToken ct = default)
    {
        _db.Leverandorer.Update(leverandor);
        return Task.CompletedTask;
    }

    public async Task<List<Leverandor>> SokLeverandorerAsync(string? query, int side, int antall, CancellationToken ct = default)
    {
        var q = _db.Leverandorer.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(l =>
                l.Navn.Contains(query) ||
                l.Leverandornummer.Contains(query) ||
                (l.Organisasjonsnummer != null && l.Organisasjonsnummer.Contains(query)));
        }

        return await q
            .OrderBy(l => l.Leverandornummer)
            .Skip((side - 1) * antall)
            .Take(antall)
            .ToListAsync(ct);
    }

    public async Task<int> TellLeverandorerAsync(string? query, CancellationToken ct = default)
    {
        var q = _db.Leverandorer.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(l =>
                l.Navn.Contains(query) ||
                l.Leverandornummer.Contains(query) ||
                (l.Organisasjonsnummer != null && l.Organisasjonsnummer.Contains(query)));
        }

        return await q.CountAsync(ct);
    }

    // --- Faktura ---

    public async Task<LeverandorFaktura?> HentFakturaAsync(Guid id, CancellationToken ct = default)
        => await _db.LeverandorFakturaer
            .Include(f => f.Leverandor)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<LeverandorFaktura?> HentFakturaMedLinjerAsync(Guid id, CancellationToken ct = default)
        => await _db.LeverandorFakturaer
            .Include(f => f.Leverandor)
            .Include(f => f.Linjer)
            .Include(f => f.Betalinger)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<bool> EksternFakturaDuplikatAsync(Guid leverandorId, string eksternNummer, CancellationToken ct = default)
        => await _db.LeverandorFakturaer.AnyAsync(
            f => f.LeverandorId == leverandorId && f.EksternFakturanummer == eksternNummer, ct);

    public async Task LeggTilFakturaAsync(LeverandorFaktura faktura, CancellationToken ct = default)
        => await _db.LeverandorFakturaer.AddAsync(faktura, ct);

    public Task OppdaterFakturaAsync(LeverandorFaktura faktura, CancellationToken ct = default)
    {
        _db.LeverandorFakturaer.Update(faktura);
        return Task.CompletedTask;
    }

    public async Task<int> NesteInternNummerAsync(CancellationToken ct = default)
    {
        var max = await _db.LeverandorFakturaer
            .IgnoreQueryFilters()
            .MaxAsync(f => (int?)f.InternNummer, ct) ?? 0;
        return max + 1;
    }

    public async Task<List<LeverandorFaktura>> HentApnePosterAsync(DateOnly? dato, CancellationToken ct = default)
    {
        var q = _db.LeverandorFakturaer
            .Include(f => f.Leverandor)
            .Include(f => f.Linjer)
            .Where(f => f.GjenstaendeBelop > new Domain.Common.Belop(0m))
            .Where(f => f.Status != FakturaStatus.Betalt && f.Status != FakturaStatus.Kreditert);

        return await q.OrderBy(f => f.Forfallsdato).ToListAsync(ct);
    }

    public async Task<List<LeverandorFaktura>> HentForfalteFakturaerForBetalingAsync(
        DateOnly forfallTilOgMed, List<Guid>? leverandorIder = null, CancellationToken ct = default)
    {
        var q = _db.LeverandorFakturaer
            .Include(f => f.Leverandor)
            .Where(f => f.Forfallsdato <= forfallTilOgMed)
            .Where(f => f.GjenstaendeBelop > new Domain.Common.Belop(0m));

        if (leverandorIder != null && leverandorIder.Count > 0)
            q = q.Where(f => leverandorIder.Contains(f.LeverandorId));

        return await q.OrderBy(f => f.Forfallsdato).ToListAsync(ct);
    }

    public async Task<List<LeverandorFaktura>> HentFakturaerForLeverandorAsync(
        Guid leverandorId, DateOnly? fraDato = null, DateOnly? tilDato = null, CancellationToken ct = default)
    {
        var q = _db.LeverandorFakturaer
            .Include(f => f.Leverandor)
            .Where(f => f.LeverandorId == leverandorId);

        if (fraDato.HasValue)
            q = q.Where(f => f.Fakturadato >= fraDato.Value);
        if (tilDato.HasValue)
            q = q.Where(f => f.Fakturadato <= tilDato.Value);

        return await q.OrderBy(f => f.Fakturadato).ThenBy(f => f.InternNummer).ToListAsync(ct);
    }

    // --- Betalingsforslag ---

    public async Task<Betalingsforslag?> HentBetalingsforslagAsync(Guid id, CancellationToken ct = default)
        => await _db.Betalingsforslag.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<Betalingsforslag?> HentBetalingsforslagMedLinjerAsync(Guid id, CancellationToken ct = default)
        => await _db.Betalingsforslag
            .Include(b => b.Linjer).ThenInclude(l => l.Leverandor)
            .Include(b => b.Linjer).ThenInclude(l => l.LeverandorFaktura)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task LeggTilBetalingsforslagAsync(Betalingsforslag forslag, CancellationToken ct = default)
        => await _db.Betalingsforslag.AddAsync(forslag, ct);

    public Task OppdaterBetalingsforslagAsync(Betalingsforslag forslag, CancellationToken ct = default)
    {
        _db.Betalingsforslag.Update(forslag);
        return Task.CompletedTask;
    }

    public async Task<int> NesteForslagsnummerAsync(CancellationToken ct = default)
    {
        var max = await _db.Betalingsforslag
            .IgnoreQueryFilters()
            .MaxAsync(b => (int?)b.Forslagsnummer, ct) ?? 0;
        return max + 1;
    }

    // --- Betaling ---

    public async Task LeggTilBetalingAsync(LeverandorBetaling betaling, CancellationToken ct = default)
        => await _db.LeverandorBetalinger.AddAsync(betaling, ct);

    public async Task<LeverandorBetaling?> HentBetalingMedBankreferanseAsync(string bankreferanse, CancellationToken ct = default)
        => await _db.LeverandorBetalinger
            .Include(b => b.LeverandorFaktura)
            .FirstOrDefaultAsync(b => b.Bankreferanse == bankreferanse, ct);

    public async Task LagreEndringerAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
