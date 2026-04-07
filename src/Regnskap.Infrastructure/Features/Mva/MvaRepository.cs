using Microsoft.EntityFrameworkCore;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kontoplan;
using Regnskap.Domain.Features.Mva;
using Regnskap.Infrastructure.Persistence;

namespace Regnskap.Infrastructure.Features.Mva;

public class MvaRepository : IMvaRepository
{
    private readonly RegnskapDbContext _db;

    public MvaRepository(RegnskapDbContext db)
    {
        _db = db;
    }

    // --- Terminer ---

    public async Task<List<MvaTermin>> HentTerminerForArAsync(int ar, CancellationToken ct = default)
    {
        return await _db.MvaTerminer
            .Where(t => t.Ar == ar)
            .OrderBy(t => t.Termin)
            .ToListAsync(ct);
    }

    public async Task<MvaTermin?> HentTerminAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.MvaTerminer.FindAsync(new object[] { id }, ct);
    }

    public async Task<MvaTermin?> HentTerminForDatoAsync(DateOnly dato, CancellationToken ct = default)
    {
        return await _db.MvaTerminer
            .FirstOrDefaultAsync(t => t.FraDato <= dato && t.TilDato >= dato, ct);
    }

    public async Task<MvaTermin?> HentTerminAsync(int ar, int termin, CancellationToken ct = default)
    {
        return await _db.MvaTerminer
            .FirstOrDefaultAsync(t => t.Ar == ar && t.Termin == termin, ct);
    }

    public async Task<bool> TerminerFinnesForArAsync(int ar, CancellationToken ct = default)
    {
        return await _db.MvaTerminer.AnyAsync(t => t.Ar == ar, ct);
    }

    public async Task LeggTilTerminAsync(MvaTermin termin, CancellationToken ct = default)
    {
        await _db.MvaTerminer.AddAsync(termin, ct);
    }

    public async Task LeggTilTerminerAsync(IEnumerable<MvaTermin> terminer, CancellationToken ct = default)
    {
        await _db.MvaTerminer.AddRangeAsync(terminer, ct);
    }

    // --- Oppgjor ---

    public async Task<MvaOppgjor?> HentOppgjorForTerminAsync(Guid terminId, CancellationToken ct = default)
    {
        return await _db.MvaOppgjorSet
            .Include(o => o.Linjer.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(o => o.MvaTerminId == terminId, ct);
    }

    public async Task<MvaOppgjor?> HentOppgjorMedLinjerAsync(Guid oppgjorId, CancellationToken ct = default)
    {
        return await _db.MvaOppgjorSet
            .Include(o => o.Linjer.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == oppgjorId, ct);
    }

    public async Task LeggTilOppgjorAsync(MvaOppgjor oppgjor, CancellationToken ct = default)
    {
        await _db.MvaOppgjorSet.AddAsync(oppgjor, ct);
    }

    // --- Avstemming ---

    public async Task<MvaAvstemming?> HentSisteAvstemmingForTerminAsync(Guid terminId, CancellationToken ct = default)
    {
        return await _db.MvaAvstemminger
            .Include(a => a.Linjer.Where(l => !l.IsDeleted))
            .Where(a => a.MvaTerminId == terminId)
            .OrderByDescending(a => a.AvstemmingTidspunkt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<MvaAvstemming>> HentAvstemmingshistorikkAsync(Guid terminId, CancellationToken ct = default)
    {
        return await _db.MvaAvstemminger
            .Include(a => a.Linjer.Where(l => !l.IsDeleted))
            .Where(a => a.MvaTerminId == terminId)
            .OrderByDescending(a => a.AvstemmingTidspunkt)
            .ToListAsync(ct);
    }

    public async Task LeggTilAvstemmingAsync(MvaAvstemming avstemming, CancellationToken ct = default)
    {
        await _db.MvaAvstemminger.AddAsync(avstemming, ct);
    }

    // --- Posteringsaggregering ---

    public async Task<List<MvaAggregeringDto>> HentMvaAggregertForPeriodeAsync(
        DateOnly fraDato, DateOnly tilDato, CancellationToken ct = default)
    {
        // Hent posteringer med MVA-kode i perioden
        var query = from p in _db.Posteringer
                    join b in _db.Bilag on p.BilagId equals b.Id
                    where b.Bilagsdato >= fraDato
                       && b.Bilagsdato <= tilDato
                       && p.MvaKode != null
                       && p.ErAutoGenerertMva == false // Kilde-posteringene, ikke auto-genererte
                       && b.ErBokfort
                    join mk in _db.MvaKoder on p.MvaKode equals mk.Kode into mvaKodes
                    from mk in mvaKodes.DefaultIfEmpty()
                    group new { p, mk } by new
                    {
                        p.MvaKode,
                        StandardTaxCode = mk != null ? mk.StandardTaxCode : p.MvaKode!,
                        Sats = p.MvaSats ?? 0m,
                        Retning = mk != null ? mk.Retning : MvaRetning.Ingen
                    } into g
                    select new MvaAggregeringDto(
                        g.Key.MvaKode!,
                        g.Key.StandardTaxCode,
                        g.Key.Sats,
                        g.Key.Retning,
                        g.Sum(x => x.p.MvaGrunnlag != null ? x.p.MvaGrunnlag.Value.Verdi : 0m),
                        g.Sum(x => x.p.MvaBelop != null ? x.p.MvaBelop.Value.Verdi : 0m),
                        g.Count()
                    );

        return await query.ToListAsync(ct);
    }

    public async Task<List<MvaPosteringDetalj>> HentMvaPosteringerForPeriodeAsync(
        DateOnly fraDato, DateOnly tilDato, string? mvaKode = null, CancellationToken ct = default)
    {
        var query = from p in _db.Posteringer
                    join b in _db.Bilag on p.BilagId equals b.Id
                    where b.Bilagsdato >= fraDato
                       && b.Bilagsdato <= tilDato
                       && p.MvaKode != null
                       && b.ErBokfort
                    select new { p, b };

        if (mvaKode != null)
            query = query.Where(x => x.p.MvaKode == mvaKode);

        return await query.Select(x => new MvaPosteringDetalj(
            x.p.Id,
            x.b.Id,
            x.b.Bilagsnummer,
            x.b.Bilagsdato,
            x.p.Kontonummer,
            x.p.Beskrivelse,
            x.p.Side,
            x.p.Belop.Verdi,
            x.p.MvaKode!,
            x.p.MvaGrunnlag != null ? x.p.MvaGrunnlag.Value.Verdi : 0m,
            x.p.MvaBelop != null ? x.p.MvaBelop.Value.Verdi : 0m,
            x.p.MvaSats ?? 0m,
            x.p.ErAutoGenerertMva
        )).ToListAsync(ct);
    }

    public async Task<List<MvaKontoSaldoDto>> HentMvaKontoSaldoerAsync(
        int ar, int fraPeriode, int tilPeriode, CancellationToken ct = default)
    {
        // MVA-kontoer: typisk 2600-2799 serien og 1600-serien
        var query = from ks in _db.KontoSaldoer
                    join k in _db.Kontoer on ks.KontoId equals k.Id
                    where ks.Ar == ar
                       && ks.Periode >= fraPeriode
                       && ks.Periode <= tilPeriode
                       && (k.Kontonummer.StartsWith("26") || k.Kontonummer.StartsWith("27") || k.Kontonummer.StartsWith("16"))
                    group new { ks, k } by new { ks.Kontonummer, k.Navn } into g
                    select new MvaKontoSaldoDto(
                        g.Key.Kontonummer,
                        g.Key.Navn,
                        g.Sum(x => x.ks.InngaendeBalanse.Verdi),
                        g.Sum(x => x.ks.SumDebet.Verdi),
                        g.Sum(x => x.ks.SumKredit.Verdi),
                        g.Sum(x => x.ks.InngaendeBalanse.Verdi + x.ks.SumDebet.Verdi - x.ks.SumKredit.Verdi)
                    );

        return await query.ToListAsync(ct);
    }

    public async Task<List<MvaKontoBeregnetDto>> HentBeregnetMvaPerKontoAsync(
        DateOnly fraDato, DateOnly tilDato, CancellationToken ct = default)
    {
        // Hent auto-genererte MVA-posteringer gruppert per kontonummer.
        // Disse posteringene er bokfort pa MVA-kontoer (2700, 2710, etc.)
        // og representerer det som "burde vaere" pa kontoen fra MVA-beregning.
        var query = from p in _db.Posteringer
                    join b in _db.Bilag on p.BilagId equals b.Id
                    where b.Bilagsdato >= fraDato
                       && b.Bilagsdato <= tilDato
                       && p.ErAutoGenerertMva == true
                       && b.ErBokfort
                    group p by p.Kontonummer into g
                    select new MvaKontoBeregnetDto(
                        g.Key,
                        g.Where(x => x.Side == BokforingSide.Debet).Sum(x => x.Belop.Verdi),
                        g.Where(x => x.Side == BokforingSide.Kredit).Sum(x => x.Belop.Verdi),
                        g.Where(x => x.Side == BokforingSide.Debet).Sum(x => x.Belop.Verdi)
                        - g.Where(x => x.Side == BokforingSide.Kredit).Sum(x => x.Belop.Verdi)
                    );

        return await query.ToListAsync(ct);
    }

    public async Task LagreEndringerAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
