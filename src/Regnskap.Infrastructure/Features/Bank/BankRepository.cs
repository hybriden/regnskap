using Microsoft.EntityFrameworkCore;
using Regnskap.Domain.Features.Bankavstemming;
using Regnskap.Infrastructure.Persistence;

namespace Regnskap.Infrastructure.Features.Bank;

public class BankRepository : IBankRepository
{
    private readonly RegnskapDbContext _db;

    public BankRepository(RegnskapDbContext db)
    {
        _db = db;
    }

    // --- Bankkonto ---

    public async Task<Bankkonto?> HentBankkonto(Guid id)
        => await _db.Bankkontoer
            .Include(b => b.Hovedbokkonto)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<Bankkonto?> HentBankkontoMedIban(string iban)
        => await _db.Bankkontoer.FirstOrDefaultAsync(b => b.Iban == iban);

    public async Task<Bankkonto?> HentBankkontoMedKontonummer(string kontonummer)
        => await _db.Bankkontoer.FirstOrDefaultAsync(b => b.Kontonummer == kontonummer);

    public async Task<IReadOnlyList<Bankkonto>> HentAlleBankkontoer(bool kunAktive = true)
    {
        var q = _db.Bankkontoer.Include(b => b.Hovedbokkonto).AsQueryable();
        if (kunAktive) q = q.Where(b => b.ErAktiv);
        return await q.OrderBy(b => b.Kontonummer).ToListAsync();
    }

    public async Task LeggTilBankkonto(Bankkonto bankkonto)
        => await _db.Bankkontoer.AddAsync(bankkonto);

    public Task OppdaterBankkonto(Bankkonto bankkonto)
    {
        _db.Bankkontoer.Update(bankkonto);
        return Task.CompletedTask;
    }

    // --- Bankbevegelse ---

    public async Task<Bankbevegelse?> HentBevegelse(Guid id)
        => await _db.Bankbevegelser
            .Include(b => b.Matchinger)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<IReadOnlyList<Bankbevegelse>> HentUmatchedeBevegelser(Guid bankkontoId)
        => await _db.Bankbevegelser
            .Where(b => b.BankkontoId == bankkontoId && b.Status == BankbevegelseStatus.IkkeMatchet)
            .OrderBy(b => b.Bokforingsdato)
            .ToListAsync();

    public async Task<IReadOnlyList<Bankbevegelse>> HentBevegelser(Guid bankkontoId, BankbevegelseStatus? status, DateOnly? fraDato, DateOnly? tilDato)
    {
        var q = _db.Bankbevegelser
            .Include(b => b.Matchinger)
            .Where(b => b.BankkontoId == bankkontoId);

        if (status.HasValue) q = q.Where(b => b.Status == status.Value);
        if (fraDato.HasValue) q = q.Where(b => b.Bokforingsdato >= fraDato.Value);
        if (tilDato.HasValue) q = q.Where(b => b.Bokforingsdato <= tilDato.Value);

        return await q.OrderBy(b => b.Bokforingsdato).ToListAsync();
    }

    public async Task LeggTilBevegelse(Bankbevegelse bevegelse)
        => await _db.Bankbevegelser.AddAsync(bevegelse);

    // --- Kontoutskrift ---

    public async Task<Kontoutskrift?> HentKontoutskrift(Guid id)
        => await _db.Kontoutskrifter
            .Include(k => k.Bevegelser)
            .FirstOrDefaultAsync(k => k.Id == id);

    public async Task<IReadOnlyList<Kontoutskrift>> HentKontoutskrifter(Guid bankkontoId)
        => await _db.Kontoutskrifter
            .Where(k => k.BankkontoId == bankkontoId)
            .OrderByDescending(k => k.PeriodeTil)
            .ToListAsync();

    public async Task<bool> KontoutskriftFinnes(Guid bankkontoId, string meldingsId)
        => await _db.Kontoutskrifter
            .AnyAsync(k => k.BankkontoId == bankkontoId && k.MeldingsId == meldingsId);

    public async Task LeggTilKontoutskrift(Kontoutskrift kontoutskrift)
        => await _db.Kontoutskrifter.AddAsync(kontoutskrift);

    // --- Match ---

    public async Task LeggTilMatch(BankbevegelseMatch match)
        => await _db.BankbevegelseMatchinger.AddAsync(match);

    public async Task FjernMatchinger(Guid bankbevegelseId)
    {
        var matchinger = await _db.BankbevegelseMatchinger
            .Where(m => m.BankbevegelseId == bankbevegelseId)
            .ToListAsync();
        _db.BankbevegelseMatchinger.RemoveRange(matchinger);
    }

    // --- Avstemming ---

    public async Task<Bankavstemming?> HentAvstemming(Guid bankkontoId, int aar, int periode)
        => await _db.Bankavstemminger
            .FirstOrDefaultAsync(a => a.BankkontoId == bankkontoId && a.Ar == aar && a.Periode == periode);

    public async Task<Bankavstemming?> HentAvstemmingMedId(Guid id)
        => await _db.Bankavstemminger
            .Include(a => a.Bankkonto)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task LeggTilAvstemming(Bankavstemming avstemming)
        => await _db.Bankavstemminger.AddAsync(avstemming);

    public async Task LagreEndringerAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
