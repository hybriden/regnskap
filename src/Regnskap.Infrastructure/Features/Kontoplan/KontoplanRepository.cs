using Microsoft.EntityFrameworkCore;
using Regnskap.Domain.Features.Kontoplan;
using Regnskap.Infrastructure.Persistence;

namespace Regnskap.Infrastructure.Features.Kontoplan;

public class KontoplanRepository : IKontoplanRepository
{
    private readonly RegnskapDbContext _db;

    public KontoplanRepository(RegnskapDbContext db)
    {
        _db = db;
    }

    // --- Kontogrupper ---

    public async Task<List<Kontogruppe>> HentAlleKontogrupperAsync(CancellationToken ct = default)
    {
        return await _db.Kontogrupper
            .Include(g => g.Kontoer)
            .OrderBy(g => g.Gruppekode)
            .ToListAsync(ct);
    }

    public async Task<Kontogruppe?> HentKontogruppeAsync(int gruppekode, CancellationToken ct = default)
    {
        return await _db.Kontogrupper
            .Include(g => g.Kontoer.Where(k => !k.IsDeleted))
            .FirstOrDefaultAsync(g => g.Gruppekode == gruppekode, ct);
    }

    // --- Kontoer ---

    public async Task<Konto?> HentKontoAsync(string kontonummer, CancellationToken ct = default)
    {
        return await _db.Kontoer
            .FirstOrDefaultAsync(k => k.Kontonummer == kontonummer, ct);
    }

    public async Task<Konto?> HentKontoMedDetaljerAsync(string kontonummer, CancellationToken ct = default)
    {
        return await _db.Kontoer
            .Include(k => k.Kontogruppe)
            .Include(k => k.Underkontoer.Where(u => !u.IsDeleted))
            .FirstOrDefaultAsync(k => k.Kontonummer == kontonummer, ct);
    }

    public async Task<bool> KontoFinnesAsync(string kontonummer, CancellationToken ct = default)
    {
        return await _db.Kontoer.AnyAsync(k => k.Kontonummer == kontonummer, ct);
    }

    public async Task<List<Konto>> HentKontoerAsync(
        int? kontoklasse = null,
        Kontotype? kontotype = null,
        int? gruppekode = null,
        bool? erAktiv = null,
        bool? erBokforbar = null,
        string? sok = null,
        int side = 1,
        int antall = 50,
        CancellationToken ct = default)
    {
        var query = BuildKontoQuery(kontoklasse, kontotype, gruppekode, erAktiv, erBokforbar, sok);
        return await query
            .Include(k => k.Kontogruppe)
            .OrderBy(k => k.Kontonummer)
            .Skip((side - 1) * antall)
            .Take(antall)
            .ToListAsync(ct);
    }

    public async Task<int> TellKontoerAsync(
        int? kontoklasse = null,
        Kontotype? kontotype = null,
        int? gruppekode = null,
        bool? erAktiv = null,
        bool? erBokforbar = null,
        string? sok = null,
        CancellationToken ct = default)
    {
        var query = BuildKontoQuery(kontoklasse, kontotype, gruppekode, erAktiv, erBokforbar, sok);
        return await query.CountAsync(ct);
    }

    public async Task<List<Konto>> SokKontoerAsync(string query, int antall = 10, CancellationToken ct = default)
    {
        return await _db.Kontoer
            .Where(k => k.ErAktiv && k.ErBokforbar)
            .Where(k => k.Kontonummer.StartsWith(query) || k.Navn.Contains(query))
            .OrderBy(k => k.Kontonummer)
            .Take(antall)
            .ToListAsync(ct);
    }

    public async Task LeggTilKontoAsync(Konto konto, CancellationToken ct = default)
    {
        await _db.Kontoer.AddAsync(konto, ct);
    }

    public async Task<bool> KontoHarPosteringerAsync(string kontonummer, CancellationToken ct = default)
    {
        // TODO: Implementer nar Hovedbok-modulen er pa plass.
        // For na returnerer vi alltid false.
        return await Task.FromResult(false);
    }

    public async Task<bool> KontoHarAktiveUnderkontoerAsync(string kontonummer, CancellationToken ct = default)
    {
        return await _db.Kontoer
            .AnyAsync(k => k.OverordnetKonto != null
                && k.OverordnetKonto.Kontonummer == kontonummer
                && k.ErAktiv, ct);
    }

    // --- MVA-koder ---

    public async Task<List<MvaKode>> HentAlleMvaKoderAsync(bool? erAktiv = null, MvaRetning? retning = null, CancellationToken ct = default)
    {
        var query = _db.MvaKoder.AsQueryable();

        if (erAktiv.HasValue)
            query = query.Where(m => m.ErAktiv == erAktiv.Value);

        if (retning.HasValue)
            query = query.Where(m => m.Retning == retning.Value);

        return await query
            .Include(m => m.UtgaendeKonto)
            .Include(m => m.InngaendeKonto)
            .OrderBy(m => m.Kode)
            .ToListAsync(ct);
    }

    public async Task<MvaKode?> HentMvaKodeAsync(string kode, CancellationToken ct = default)
    {
        return await _db.MvaKoder
            .Include(m => m.UtgaendeKonto)
            .Include(m => m.InngaendeKonto)
            .FirstOrDefaultAsync(m => m.Kode == kode, ct);
    }

    public async Task<bool> MvaKodeFinnesAsync(string kode, CancellationToken ct = default)
    {
        return await _db.MvaKoder.AnyAsync(m => m.Kode == kode, ct);
    }

    public async Task LeggTilMvaKodeAsync(MvaKode mvaKode, CancellationToken ct = default)
    {
        await _db.MvaKoder.AddAsync(mvaKode, ct);
    }

    public async Task LagreEndringerAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }

    // --- Private ---

    private IQueryable<Konto> BuildKontoQuery(
        int? kontoklasse, Kontotype? kontotype, int? gruppekode,
        bool? erAktiv, bool? erBokforbar, string? sok)
    {
        var query = _db.Kontoer.AsQueryable();

        if (kontoklasse.HasValue)
        {
            var prefix = kontoklasse.Value.ToString();
            query = query.Where(k => k.Kontonummer.StartsWith(prefix));
        }

        if (kontotype.HasValue)
            query = query.Where(k => k.Kontotype == kontotype.Value);

        if (gruppekode.HasValue)
            query = query.Where(k => k.Kontogruppe.Gruppekode == gruppekode.Value);

        if (erAktiv.HasValue)
            query = query.Where(k => k.ErAktiv == erAktiv.Value);

        if (erBokforbar.HasValue)
            query = query.Where(k => k.ErBokforbar == erBokforbar.Value);

        if (!string.IsNullOrWhiteSpace(sok))
            query = query.Where(k => k.Kontonummer.Contains(sok) || k.Navn.Contains(sok));

        return query;
    }
}
