using Microsoft.EntityFrameworkCore;
using Regnskap.Application.Features.Periodeavslutning;
using Regnskap.Domain.Features.Periodeavslutning;
using Regnskap.Infrastructure.Persistence;

namespace Regnskap.Infrastructure.Features.Periodeavslutning;

public class PeriodeavslutningRepository : IPeriodeavslutningRepository
{
    private readonly RegnskapDbContext _db;

    public PeriodeavslutningRepository(RegnskapDbContext db)
    {
        _db = db;
    }

    // --- Anleggsmidler ---

    public async Task<Anleggsmiddel?> HentAnleggsmiddelAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Anleggsmidler
            .Include(a => a.Avskrivninger)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<List<Anleggsmiddel>> HentAnleggsmidlerAsync(
        bool? aktive = null, string? kontonummer = null, CancellationToken ct = default)
    {
        var query = _db.Anleggsmidler
            .Include(a => a.Avskrivninger)
            .AsQueryable();

        if (aktive.HasValue)
            query = query.Where(a => a.ErAktivt == aktive.Value);

        if (!string.IsNullOrEmpty(kontonummer))
            query = query.Where(a => a.BalanseKontonummer == kontonummer);

        return await query.OrderBy(a => a.Navn).ToListAsync(ct);
    }

    public async Task LeggTilAnleggsmiddelAsync(Anleggsmiddel anleggsmiddel, CancellationToken ct = default)
    {
        await _db.Anleggsmidler.AddAsync(anleggsmiddel, ct);
    }

    public async Task<bool> AvskrivningFinnesAsync(Guid anleggsmiddelId, int ar, int periode, CancellationToken ct = default)
    {
        return await _db.AvskrivningHistorikker
            .AnyAsync(a => a.AnleggsmiddelId == anleggsmiddelId && a.Ar == ar && a.Periode == periode, ct);
    }

    public async Task LeggTilAvskrivningHistorikkAsync(AvskrivningHistorikk historikk, CancellationToken ct = default)
    {
        await _db.AvskrivningHistorikker.AddAsync(historikk, ct);
    }

    // --- Periodiseringer ---

    public async Task<Periodisering?> HentPeriodiseringAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Periodiseringer
            .Include(p => p.Posteringer)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<List<Periodisering>> HentPeriodiseringerAsync(bool? aktive = null, CancellationToken ct = default)
    {
        var query = _db.Periodiseringer
            .Include(p => p.Posteringer)
            .AsQueryable();

        if (aktive.HasValue)
            query = query.Where(p => p.ErAktiv == aktive.Value);

        return await query.OrderBy(p => p.FraAr).ThenBy(p => p.FraPeriode).ToListAsync(ct);
    }

    public async Task<List<Periodisering>> HentAktivePeriodiseringerForPeriodeAsync(
        int ar, int periode, CancellationToken ct = default)
    {
        var periodeNr = ar * 12 + periode;

        return await _db.Periodiseringer
            .Include(p => p.Posteringer)
            .Where(p => p.ErAktiv
                && (p.FraAr * 12 + p.FraPeriode) <= periodeNr
                && (p.TilAr * 12 + p.TilPeriode) >= periodeNr)
            .ToListAsync(ct);
    }

    public async Task LeggTilPeriodiseringAsync(Periodisering periodisering, CancellationToken ct = default)
    {
        await _db.Periodiseringer.AddAsync(periodisering, ct);
    }

    public async Task<bool> PeriodiseringsHistorikkFinnesAsync(
        Guid periodiseringId, int ar, int periode, CancellationToken ct = default)
    {
        return await _db.PeriodiseringsHistorikker
            .AnyAsync(h => h.PeriodiseringId == periodiseringId && h.Ar == ar && h.Periode == periode, ct);
    }

    public async Task LeggTilPeriodiseringsHistorikkAsync(
        PeriodiseringsHistorikk historikk, CancellationToken ct = default)
    {
        await _db.PeriodiseringsHistorikker.AddAsync(historikk, ct);
    }

    // --- Logg ---

    public async Task LeggTilPeriodeLukkingLoggAsync(PeriodeLukkingLogg logg, CancellationToken ct = default)
    {
        await _db.PeriodeLukkingLogger.AddAsync(logg, ct);
    }

    public async Task<List<PeriodeLukkingLogg>> HentPeriodeLukkingLoggerAsync(
        int ar, int periode, CancellationToken ct = default)
    {
        return await _db.PeriodeLukkingLogger
            .Where(l => l.Ar == ar && l.Periode == periode)
            .OrderBy(l => l.Tidspunkt)
            .ToListAsync(ct);
    }

    // --- Arsavslutning ---

    public async Task<ArsavslutningStatus?> HentArsavslutningStatusAsync(int ar, CancellationToken ct = default)
    {
        return await _db.ArsavslutningStatuser
            .FirstOrDefaultAsync(s => s.Ar == ar, ct);
    }

    public async Task LagreArsavslutningStatusAsync(ArsavslutningStatus status, CancellationToken ct = default)
    {
        var existing = await _db.ArsavslutningStatuser
            .FirstOrDefaultAsync(s => s.Ar == status.Ar, ct);

        if (existing == null)
            await _db.ArsavslutningStatuser.AddAsync(status, ct);
        // else: tracked entity will be saved on SaveChanges
    }

    public async Task LagreEndringerAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
