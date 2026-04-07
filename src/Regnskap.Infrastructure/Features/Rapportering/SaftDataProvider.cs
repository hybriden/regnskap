using Microsoft.EntityFrameworkCore;
using Regnskap.Application.Features.Rapportering;
using Regnskap.Domain.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kontoplan;
using Regnskap.Domain.Features.Kundereskontro;
using Regnskap.Domain.Features.Leverandorreskontro;
using Regnskap.Infrastructure.Persistence;

namespace Regnskap.Infrastructure.Features.Rapportering;

public class SaftDataProvider : ISaftDataProvider
{
    private readonly RegnskapDbContext _db;

    public SaftDataProvider(RegnskapDbContext db)
    {
        _db = db;
    }

    public async Task<List<Konto>> HentKontoerAsync(CancellationToken ct = default)
    {
        return await _db.Kontoer
            .Include(k => k.Kontogruppe)
            .Where(k => k.ErAktiv)
            .OrderBy(k => k.Kontonummer)
            .ToListAsync(ct);
    }

    public async Task<List<Kunde>> HentKunderAsync(CancellationToken ct = default)
    {
        return await _db.Kunder
            .Where(k => k.ErAktiv)
            .OrderBy(k => k.Kundenummer)
            .ToListAsync(ct);
    }

    public async Task<List<Leverandor>> HentLeverandorerAsync(CancellationToken ct = default)
    {
        return await _db.Leverandorer
            .Where(l => l.ErAktiv)
            .OrderBy(l => l.Leverandornummer)
            .ToListAsync(ct);
    }

    public async Task<List<MvaKode>> HentMvaKoderAsync(CancellationToken ct = default)
    {
        return await _db.MvaKoder
            .Where(m => m.ErAktiv)
            .OrderBy(m => m.Kode)
            .ToListAsync(ct);
    }

    public async Task<List<BilagSerie>> HentBilagSerierAsync(CancellationToken ct = default)
    {
        return await _db.BilagSerier
            .Where(s => s.ErAktiv)
            .OrderBy(s => s.Kode)
            .ToListAsync(ct);
    }

    public async Task<List<Bilag>> HentBilagMedPosteringerAsync(
        int ar, int fraPeriode, int tilPeriode, CancellationToken ct = default)
    {
        return await _db.Bilag
            .Include(b => b.Posteringer)
            .Where(b => b.Ar == ar
                        && b.ErBokfort
                        && b.Bilagsdato.Month >= fraPeriode
                        && b.Bilagsdato.Month <= tilPeriode)
            .OrderBy(b => b.Bilagsnummer)
            .ToListAsync(ct);
    }
}
