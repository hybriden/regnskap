using Regnskap.Domain.Features.Kundereskontro;

namespace Regnskap.Tests.Features.Bank;

/// <summary>
/// Minimal fake for bank matching tests. Only implements methods used by BankMatchingService.
/// </summary>
public class FakeKundeRepoForBank : IKundeReskontroRepository
{
    public List<KundeFaktura> Fakturaer { get; } = new();

    public Task<KundeFaktura?> HentFakturaMedKidAsync(string kidNummer, CancellationToken ct = default)
        => Task.FromResult(Fakturaer.FirstOrDefault(f => f.KidNummer == kidNummer));

    public Task<List<KundeFaktura>> HentApnePosterAsync(DateOnly? dato = null, CancellationToken ct = default)
        => Task.FromResult(Fakturaer.Where(f => f.GjenstaendeBelop.Verdi > 0).ToList());

    // --- Unused in bank tests ---
    public Task<Kunde?> HentKundeAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Kunde?>(null);
    public Task<Kunde?> HentKundeMedNummerAsync(string kundenummer, CancellationToken ct = default) => Task.FromResult<Kunde?>(null);
    public Task<bool> KundenummerEksistererAsync(string kundenummer, CancellationToken ct = default) => Task.FromResult(false);
    public Task LeggTilKundeAsync(Kunde kunde, CancellationToken ct = default) => Task.CompletedTask;
    public Task OppdaterKundeAsync(Kunde kunde, CancellationToken ct = default) => Task.CompletedTask;
    public Task<(List<Kunde> Data, int TotaltAntall)> SokKunderAsync(string? query, int side, int antall, CancellationToken ct = default)
        => Task.FromResult((new List<Kunde>(), 0));
    public Task<KundeFaktura?> HentFakturaAsync(Guid id, CancellationToken ct = default) => Task.FromResult<KundeFaktura?>(null);
    public Task<int> NesteNummer(CancellationToken ct = default) => Task.FromResult(1);
    public Task LeggTilFakturaAsync(KundeFaktura faktura, CancellationToken ct = default) => Task.CompletedTask;
    public Task OppdaterFakturaAsync(KundeFaktura faktura, CancellationToken ct = default) => Task.CompletedTask;
    public Task<List<KundeFaktura>> HentForfalteFakturaerAsync(DateOnly dato, int minimumDagerForfalt, CancellationToken ct = default)
        => Task.FromResult(new List<KundeFaktura>());
    public Task<(List<KundeFaktura> Data, int TotaltAntall)> SokFakturaerAsync(Guid? kundeId, KundeFakturaStatus? status, int side, int antall, CancellationToken ct = default)
        => Task.FromResult((new List<KundeFaktura>(), 0));
    public Task<List<KundeFaktura>> HentFakturaerForKundeAsync(Guid kundeId, DateOnly? fraDato, DateOnly? tilDato, CancellationToken ct = default)
        => Task.FromResult(new List<KundeFaktura>());
    public Task LeggTilInnbetalingAsync(KundeInnbetaling innbetaling, CancellationToken ct = default) => Task.CompletedTask;
    public Task LeggTilPurringAsync(Purring purring, CancellationToken ct = default) => Task.CompletedTask;
    public Task<Purring?> HentSistePurringAsync(Guid fakturaId, CancellationToken ct = default) => Task.FromResult<Purring?>(null);
    public Task<List<Purring>> HentPurringerAsync(int side, int antall, CancellationToken ct = default) => Task.FromResult(new List<Purring>());
    public Task LagreEndringerAsync(CancellationToken ct = default) => Task.CompletedTask;
}
