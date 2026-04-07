using Regnskap.Domain.Features.Leverandorreskontro;
using LeverandorEntity = Regnskap.Domain.Features.Leverandorreskontro.Leverandor;

namespace Regnskap.Tests.Features.Bank;

/// <summary>
/// Minimal fake for bank matching tests. Only implements methods used by BankMatchingService.
/// </summary>
public class FakeLeverandorRepoForBank : ILeverandorReskontroRepository
{
    public List<LeverandorFaktura> Fakturaer { get; } = new();
    public List<LeverandorBetaling> Betalinger { get; } = new();

    public Task<List<LeverandorFaktura>> HentApnePosterAsync(DateOnly? dato = null, CancellationToken ct = default)
        => Task.FromResult(Fakturaer.Where(f => f.GjenstaendeBelop.Verdi > 0).ToList());

    public Task<LeverandorBetaling?> HentBetalingMedBankreferanseAsync(string bankreferanse, CancellationToken ct = default)
        => Task.FromResult(Betalinger.FirstOrDefault(b => b.Bankreferanse == bankreferanse));

    // --- Unused in bank tests ---
    public Task<LeverandorEntity?> HentLeverandorAsync(Guid id, CancellationToken ct = default) => Task.FromResult<LeverandorEntity?>(null);
    public Task<LeverandorEntity?> HentLeverandorMedNummerAsync(string leverandornummer, CancellationToken ct = default) => Task.FromResult<LeverandorEntity?>(null);
    public Task<bool> LeverandornummerEksistererAsync(string leverandornummer, CancellationToken ct = default) => Task.FromResult(false);
    public Task<bool> OrganisasjonsnummerEksistererAsync(string organisasjonsnummer, CancellationToken ct = default) => Task.FromResult(false);
    public Task LeggTilLeverandorAsync(LeverandorEntity leverandor, CancellationToken ct = default) => Task.CompletedTask;
    public Task OppdaterLeverandorAsync(LeverandorEntity leverandor, CancellationToken ct = default) => Task.CompletedTask;
    public Task<List<LeverandorEntity>> SokLeverandorerAsync(string? query, int side, int antall, CancellationToken ct = default)
        => Task.FromResult(new List<LeverandorEntity>());
    public Task<int> TellLeverandorerAsync(string? query, CancellationToken ct = default) => Task.FromResult(0);
    public Task<LeverandorFaktura?> HentFakturaAsync(Guid id, CancellationToken ct = default) => Task.FromResult<LeverandorFaktura?>(null);
    public Task<LeverandorFaktura?> HentFakturaMedLinjerAsync(Guid id, CancellationToken ct = default) => Task.FromResult<LeverandorFaktura?>(null);
    public Task<bool> EksternFakturaDuplikatAsync(Guid leverandorId, string eksternNummer, CancellationToken ct = default) => Task.FromResult(false);
    public Task LeggTilFakturaAsync(LeverandorFaktura faktura, CancellationToken ct = default) => Task.CompletedTask;
    public Task OppdaterFakturaAsync(LeverandorFaktura faktura, CancellationToken ct = default) => Task.CompletedTask;
    public Task<int> NesteInternNummerAsync(CancellationToken ct = default) => Task.FromResult(1);
    public Task<List<LeverandorFaktura>> HentForfalteFakturaerForBetalingAsync(DateOnly forfallTilOgMed, List<Guid>? leverandorIder = null, CancellationToken ct = default)
        => Task.FromResult(new List<LeverandorFaktura>());
    public Task<List<LeverandorFaktura>> HentFakturaerForLeverandorAsync(Guid leverandorId, DateOnly? fraDato = null, DateOnly? tilDato = null, CancellationToken ct = default)
        => Task.FromResult(new List<LeverandorFaktura>());
    public Task<Betalingsforslag?> HentBetalingsforslagAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Betalingsforslag?>(null);
    public Task<Betalingsforslag?> HentBetalingsforslagMedLinjerAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Betalingsforslag?>(null);
    public Task LeggTilBetalingsforslagAsync(Betalingsforslag forslag, CancellationToken ct = default) => Task.CompletedTask;
    public Task OppdaterBetalingsforslagAsync(Betalingsforslag forslag, CancellationToken ct = default) => Task.CompletedTask;
    public Task<int> NesteForslagsnummerAsync(CancellationToken ct = default) => Task.FromResult(1);
    public Task LeggTilBetalingAsync(LeverandorBetaling betaling, CancellationToken ct = default) => Task.CompletedTask;
    public Task LagreEndringerAsync(CancellationToken ct = default) => Task.CompletedTask;
}
