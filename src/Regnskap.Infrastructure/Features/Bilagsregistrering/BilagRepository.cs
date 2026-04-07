using Microsoft.EntityFrameworkCore;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Infrastructure.Persistence;

namespace Regnskap.Infrastructure.Features.Bilagsregistrering;

public class BilagRepository : IBilagRepository
{
    private readonly RegnskapDbContext _db;

    public BilagRepository(RegnskapDbContext db)
    {
        _db = db;
    }

    // --- BilagSerie ---

    public async Task<BilagSerie?> HentBilagSerieAsync(string kode, CancellationToken ct = default)
        => await _db.BilagSerier.FirstOrDefaultAsync(s => s.Kode == kode, ct);

    public async Task<BilagSerie?> HentBilagSerieMedIdAsync(Guid id, CancellationToken ct = default)
        => await _db.BilagSerier.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<List<BilagSerie>> HentAlleBilagSerierAsync(CancellationToken ct = default)
        => await _db.BilagSerier.OrderBy(s => s.Kode).ToListAsync(ct);

    public async Task LeggTilBilagSerieAsync(BilagSerie serie, CancellationToken ct = default)
        => await _db.BilagSerier.AddAsync(serie, ct);

    // --- BilagSerieNummer ---

    public async Task<BilagSerieNummer?> HentSerieNummerAsync(string serieKode, int ar, CancellationToken ct = default)
        => await _db.BilagSerieNummer.FirstOrDefaultAsync(
            s => s.SerieKode == serieKode && s.Ar == ar, ct);

    public async Task LeggTilSerieNummerAsync(BilagSerieNummer serieNummer, CancellationToken ct = default)
        => await _db.BilagSerieNummer.AddAsync(serieNummer, ct);

    // --- Vedlegg ---

    public async Task<Vedlegg?> HentVedleggAsync(Guid id, CancellationToken ct = default)
        => await _db.Vedlegg.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<List<Vedlegg>> HentVedleggForBilagAsync(Guid bilagId, CancellationToken ct = default)
        => await _db.Vedlegg
            .Where(v => v.BilagId == bilagId)
            .OrderBy(v => v.Rekkefolge)
            .ToListAsync(ct);

    public async Task LeggTilVedleggAsync(Vedlegg vedlegg, CancellationToken ct = default)
        => await _db.Vedlegg.AddAsync(vedlegg, ct);

    // --- Utvidet Bilag ---

    public async Task<Bilag?> HentBilagMedSerieAsync(
        string serieKode, int ar, int serieNummer, CancellationToken ct = default)
        => await _db.Bilag
            .Include(b => b.Regnskapsperiode)
            .Include(b => b.Posteringer.OrderBy(p => p.Linjenummer))
                .ThenInclude(p => p.Konto)
            .Include(b => b.Vedlegg)
            .FirstOrDefaultAsync(b =>
                b.SerieKode == serieKode && b.Ar == ar && b.SerieNummer == serieNummer, ct);

    public async Task<(List<Bilag> Data, int TotaltAntall)> SokBilagAsync(
        BilagSokParametre p, CancellationToken ct = default)
    {
        var query = _db.Bilag
            .Include(b => b.Regnskapsperiode)
            .Include(b => b.Posteringer.OrderBy(post => post.Linjenummer))
                .ThenInclude(post => post.Konto)
            .Include(b => b.Vedlegg)
            .AsQueryable();

        if (p.Ar.HasValue)
            query = query.Where(b => b.Ar == p.Ar.Value);
        if (p.Periode.HasValue)
            query = query.Where(b => b.Regnskapsperiode.Periode == p.Periode.Value);
        if (p.Type.HasValue)
            query = query.Where(b => b.Type == p.Type.Value);
        if (!string.IsNullOrEmpty(p.SerieKode))
            query = query.Where(b => b.SerieKode == p.SerieKode);
        if (p.FraDato.HasValue)
            query = query.Where(b => b.Bilagsdato >= p.FraDato.Value);
        if (p.TilDato.HasValue)
            query = query.Where(b => b.Bilagsdato <= p.TilDato.Value);
        if (!string.IsNullOrEmpty(p.Kontonummer))
            query = query.Where(b => b.Posteringer.Any(post => post.Kontonummer == p.Kontonummer));
        if (p.MinBelop.HasValue)
            query = query.Where(b => b.Posteringer
                .Where(post => post.Side == BokforingSide.Debet)
                .Sum(post => post.Belop.Verdi) >= p.MinBelop.Value);
        if (p.MaxBelop.HasValue)
            query = query.Where(b => b.Posteringer
                .Where(post => post.Side == BokforingSide.Debet)
                .Sum(post => post.Belop.Verdi) <= p.MaxBelop.Value);
        if (!string.IsNullOrEmpty(p.Beskrivelse))
            query = query.Where(b => b.Beskrivelse.Contains(p.Beskrivelse));
        if (!string.IsNullOrEmpty(p.EksternReferanse))
            query = query.Where(b => b.EksternReferanse != null && b.EksternReferanse.Contains(p.EksternReferanse));
        if (p.Bilagsnummer.HasValue)
            query = query.Where(b => b.Bilagsnummer == p.Bilagsnummer.Value);
        if (p.ErBokfort.HasValue)
            query = query.Where(b => b.ErBokfort == p.ErBokfort.Value);
        if (p.ErTilbakfort.HasValue)
            query = query.Where(b => b.ErTilbakfort == p.ErTilbakfort.Value);

        var totalt = await query.CountAsync(ct);

        var data = await query
            .OrderByDescending(b => b.Bilagsnummer)
            .Skip((p.Side - 1) * p.Antall)
            .Take(p.Antall)
            .ToListAsync(ct);

        return (data, totalt);
    }

    // --- Generelt ---

    public async Task LagreEndringerAsync(CancellationToken ct = default)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("Concurrency-konflikt ved lagring.", ex);
        }
    }
}
