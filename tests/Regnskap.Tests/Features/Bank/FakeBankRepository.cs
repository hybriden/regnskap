using Regnskap.Domain.Features.Bankavstemming;

namespace Regnskap.Tests.Features.Bank;

public class FakeBankRepository : IBankRepository
{
    public List<Bankkonto> Bankkontoer { get; } = new();
    public List<Kontoutskrift> Kontoutskrifter { get; } = new();
    public List<Bankbevegelse> Bevegelser { get; } = new();
    public List<BankbevegelseMatch> Matchinger { get; } = new();
    public List<Bankavstemming> Avstemminger { get; } = new();

    public Task<Bankkonto?> HentBankkonto(Guid id)
        => Task.FromResult(Bankkontoer.FirstOrDefault(b => b.Id == id));

    public Task<Bankkonto?> HentBankkontoMedIban(string iban)
        => Task.FromResult(Bankkontoer.FirstOrDefault(b => b.Iban == iban));

    public Task<Bankkonto?> HentBankkontoMedKontonummer(string kontonummer)
        => Task.FromResult(Bankkontoer.FirstOrDefault(b => b.Kontonummer == kontonummer));

    public Task<IReadOnlyList<Bankkonto>> HentAlleBankkontoer(bool kunAktive = true)
        => Task.FromResult<IReadOnlyList<Bankkonto>>(
            Bankkontoer.Where(b => !kunAktive || b.ErAktiv).ToList());

    public Task LeggTilBankkonto(Bankkonto bankkonto)
    {
        Bankkontoer.Add(bankkonto);
        return Task.CompletedTask;
    }

    public Task OppdaterBankkonto(Bankkonto bankkonto) => Task.CompletedTask;

    public Task<Bankbevegelse?> HentBevegelse(Guid id)
        => Task.FromResult(Bevegelser.FirstOrDefault(b => b.Id == id));

    public Task<IReadOnlyList<Bankbevegelse>> HentUmatchedeBevegelser(Guid bankkontoId)
        => Task.FromResult<IReadOnlyList<Bankbevegelse>>(
            Bevegelser.Where(b => b.BankkontoId == bankkontoId && b.Status == BankbevegelseStatus.IkkeMatchet).ToList());

    public Task<IReadOnlyList<Bankbevegelse>> HentBevegelser(Guid bankkontoId, BankbevegelseStatus? status, DateOnly? fraDato, DateOnly? tilDato)
        => Task.FromResult<IReadOnlyList<Bankbevegelse>>(
            Bevegelser.Where(b => b.BankkontoId == bankkontoId).ToList());

    public Task LeggTilBevegelse(Bankbevegelse bevegelse)
    {
        Bevegelser.Add(bevegelse);
        return Task.CompletedTask;
    }

    public Task<Kontoutskrift?> HentKontoutskrift(Guid id)
        => Task.FromResult(Kontoutskrifter.FirstOrDefault(k => k.Id == id));

    public Task<IReadOnlyList<Kontoutskrift>> HentKontoutskrifter(Guid bankkontoId)
        => Task.FromResult<IReadOnlyList<Kontoutskrift>>(
            Kontoutskrifter.Where(k => k.BankkontoId == bankkontoId).ToList());

    public Task<bool> KontoutskriftFinnes(Guid bankkontoId, string meldingsId)
        => Task.FromResult(Kontoutskrifter.Any(k => k.BankkontoId == bankkontoId && k.MeldingsId == meldingsId));

    public Task LeggTilKontoutskrift(Kontoutskrift kontoutskrift)
    {
        Kontoutskrifter.Add(kontoutskrift);
        // Also add bevegelser to the flat list
        foreach (var bev in kontoutskrift.Bevegelser)
            Bevegelser.Add(bev);
        return Task.CompletedTask;
    }

    public Task LeggTilMatch(BankbevegelseMatch match)
    {
        Matchinger.Add(match);
        return Task.CompletedTask;
    }

    public Task FjernMatchinger(Guid bankbevegelseId)
    {
        Matchinger.RemoveAll(m => m.BankbevegelseId == bankbevegelseId);
        return Task.CompletedTask;
    }

    public Task<Bankavstemming?> HentAvstemming(Guid bankkontoId, int aar, int periode)
        => Task.FromResult(Avstemminger.FirstOrDefault(a => a.BankkontoId == bankkontoId && a.Ar == aar && a.Periode == periode));

    public Task<Bankavstemming?> HentAvstemmingMedId(Guid id)
        => Task.FromResult(Avstemminger.FirstOrDefault(a => a.Id == id));

    public Task LeggTilAvstemming(Bankavstemming avstemming)
    {
        Avstemminger.Add(avstemming);
        return Task.CompletedTask;
    }

    public Task LagreEndringerAsync(CancellationToken ct = default) => Task.CompletedTask;
}
