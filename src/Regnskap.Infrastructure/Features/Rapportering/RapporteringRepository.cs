using Microsoft.EntityFrameworkCore;
using Regnskap.Application.Features.Rapportering;
using Regnskap.Domain.Features.Rapportering;
using Regnskap.Infrastructure.Persistence;

namespace Regnskap.Infrastructure.Features.Rapportering;

public class RapporteringRepository : IRapporteringRepository
{
    private readonly RegnskapDbContext _db;

    public RapporteringRepository(RegnskapDbContext db)
    {
        _db = db;
    }

    // --- Budsjett ---

    public async Task<Budsjett?> HentBudsjettLinjeAsync(
        string kontonummer, int ar, int periode, string versjon, CancellationToken ct = default)
    {
        return await _db.Budsjetter
            .FirstOrDefaultAsync(b =>
                b.Kontonummer == kontonummer &&
                b.Ar == ar &&
                b.Periode == periode &&
                b.Versjon == versjon, ct);
    }

    public async Task<List<Budsjett>> HentBudsjettForArAsync(int ar, string versjon, CancellationToken ct = default)
    {
        return await _db.Budsjetter
            .Where(b => b.Ar == ar && b.Versjon == versjon)
            .OrderBy(b => b.Kontonummer)
            .ThenBy(b => b.Periode)
            .ToListAsync(ct);
    }

    public async Task LeggTilBudsjettAsync(Budsjett budsjett, CancellationToken ct = default)
    {
        await _db.Budsjetter.AddAsync(budsjett, ct);
    }

    public async Task SlettBudsjettForArAsync(int ar, string versjon, CancellationToken ct = default)
    {
        var budsjetter = await _db.Budsjetter
            .Where(b => b.Ar == ar && b.Versjon == versjon)
            .ToListAsync(ct);

        _db.Budsjetter.RemoveRange(budsjetter);
    }

    // --- Konfigurasjon ---

    public async Task<RapportKonfigurasjon?> HentKonfigurasjonAsync(CancellationToken ct = default)
    {
        return await _db.RapportKonfigurasjoner.FirstOrDefaultAsync(ct);
    }

    public async Task LagreKonfigurasjonAsync(RapportKonfigurasjon konfigurasjon, CancellationToken ct = default)
    {
        var existing = await _db.RapportKonfigurasjoner.FirstOrDefaultAsync(ct);
        if (existing == null)
        {
            await _db.RapportKonfigurasjoner.AddAsync(konfigurasjon, ct);
        }
        else
        {
            _db.Entry(existing).CurrentValues.SetValues(konfigurasjon);
        }
    }

    // --- Rapportlogg ---

    public async Task LeggTilRapportLoggAsync(RapportLogg logg, CancellationToken ct = default)
    {
        await _db.RapportLogger.AddAsync(logg, ct);
    }

    public async Task<List<RapportLogg>> HentRapportLoggerAsync(
        int ar, RapportType? type = null, CancellationToken ct = default)
    {
        var query = _db.RapportLogger.Where(l => l.Ar == ar);

        if (type.HasValue)
            query = query.Where(l => l.Type == type.Value);

        return await query
            .OrderByDescending(l => l.GenererTidspunkt)
            .ToListAsync(ct);
    }

    // --- Aggregeringssporringer ---

    public async Task<List<KontoSaldoAggregat>> HentAggregerteSaldoerAsync(
        int ar, int fraPeriode, int tilPeriode,
        int? kontoklasse = null,
        CancellationToken ct = default)
    {
        var query = from ks in _db.KontoSaldoer
                    join k in _db.Kontoer on ks.KontoId equals k.Id
                    join kg in _db.Kontogrupper on k.KontogruppeId equals kg.Id
                    where ks.Ar == ar
                          && ks.Periode >= fraPeriode
                          && ks.Periode <= tilPeriode
                    select new { ks, k, kg };

        if (kontoklasse.HasValue)
        {
            var prefix = kontoklasse.Value.ToString();
            query = query.Where(x => x.k.Kontonummer.StartsWith(prefix));
        }

        var rawData = await query.ToListAsync(ct);

        // Get IB (opening balance) from period 0 or from first period
        var ibQuery = from ks in _db.KontoSaldoer
                      join k in _db.Kontoer on ks.KontoId equals k.Id
                      where ks.Ar == ar && ks.Periode == 0
                      select new { ks.Kontonummer, IB = ks.InngaendeBalanse.Verdi + ks.SumDebet.Verdi - ks.SumKredit.Verdi };

        if (kontoklasse.HasValue)
        {
            var prefix = kontoklasse.Value.ToString();
            ibQuery = ibQuery.Where(x => x.Kontonummer.StartsWith(prefix));
        }

        var ibData = await ibQuery.ToListAsync(ct);
        var ibDict = ibData.GroupBy(x => x.Kontonummer)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.IB));

        // Also check if period fraPeriode has IB set
        var ibFromFirstPeriod = await (
            from ks in _db.KontoSaldoer
            join k in _db.Kontoer on ks.KontoId equals k.Id
            where ks.Ar == ar && ks.Periode == fraPeriode
            select new { ks.Kontonummer, IB = ks.InngaendeBalanse.Verdi }
        ).ToListAsync(ct);

        var ibFromFirstDict = ibFromFirstPeriod
            .GroupBy(x => x.Kontonummer)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.IB));

        var grouped = rawData
            .GroupBy(x => x.k.Kontonummer)
            .Select(g =>
            {
                var first = g.First();
                var sumDebet = g.Sum(x => x.ks.SumDebet.Verdi);
                var sumKredit = g.Sum(x => x.ks.SumKredit.Verdi);

                // Try IB from period 0, then from first period's IB
                var ib = ibDict.GetValueOrDefault(first.k.Kontonummer, 0m);
                if (ib == 0m && ibFromFirstDict.TryGetValue(first.k.Kontonummer, out var ibFirst))
                    ib = ibFirst;

                return new KontoSaldoAggregat(
                    Kontonummer: first.k.Kontonummer,
                    Kontonavn: first.k.Navn,
                    Kontotype: first.k.Kontotype.ToString(),
                    Normalbalanse: first.k.Normalbalanse.ToString(),
                    Gruppekode: first.kg.Gruppekode,
                    Gruppenavn: first.kg.Navn,
                    InngaendeBalanse: ib,
                    SumDebet: sumDebet,
                    SumKredit: sumKredit,
                    UtgaendeBalanse: ib + sumDebet - sumKredit
                );
            })
            .OrderBy(x => x.Kontonummer)
            .ToList();

        return grouped;
    }

    public async Task<List<DimensjonsSaldoAggregat>> HentDimensjonsSaldoerAsync(
        int ar, int fraPeriode, int tilPeriode,
        string dimensjon,
        string? kode = null,
        int? kontoklasse = null,
        CancellationToken ct = default)
    {
        var query = from p in _db.Posteringer
                    join b in _db.Bilag on p.BilagId equals b.Id
                    join k in _db.Kontoer on p.KontoId equals k.Id
                    where b.Ar == ar
                          && b.Bilagsdato.Month >= fraPeriode
                          && b.Bilagsdato.Month <= tilPeriode
                          && b.ErBokfort
                    select new { p, k };

        if (kontoklasse.HasValue)
        {
            var prefix = kontoklasse.Value.ToString();
            query = query.Where(x => x.k.Kontonummer.StartsWith(prefix));
        }

        var rawData = await query.ToListAsync(ct);

        var grouped = rawData
            .GroupBy(x => new
            {
                DimensjonsKode = dimensjon == "avdeling"
                    ? (x.p.Avdelingskode ?? "Uspesifisert")
                    : (x.p.Prosjektkode ?? "Uspesifisert"),
                x.k.Kontonummer,
                x.k.Navn
            })
            .Where(g => kode == null || g.Key.DimensjonsKode == kode)
            .Select(g => new DimensjonsSaldoAggregat(
                DimensjonsKode: g.Key.DimensjonsKode,
                Kontonummer: g.Key.Kontonummer,
                Kontonavn: g.Key.Navn,
                SumDebet: g.Where(x => x.p.Side == Domain.Features.Hovedbok.BokforingSide.Debet).Sum(x => x.p.Belop.Verdi),
                SumKredit: g.Where(x => x.p.Side == Domain.Features.Hovedbok.BokforingSide.Kredit).Sum(x => x.p.Belop.Verdi),
                Netto: g.Where(x => x.p.Side == Domain.Features.Hovedbok.BokforingSide.Debet).Sum(x => x.p.Belop.Verdi)
                     - g.Where(x => x.p.Side == Domain.Features.Hovedbok.BokforingSide.Kredit).Sum(x => x.p.Belop.Verdi)
            ))
            .OrderBy(x => x.DimensjonsKode)
            .ThenBy(x => x.Kontonummer)
            .ToList();

        return grouped;
    }

    public async Task LagreEndringerAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
