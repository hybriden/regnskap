namespace Regnskap.Domain.Features.Leverandorreskontro;

public interface ILeverandorReskontroRepository
{
    // Leverandor
    Task<Leverandor?> HentLeverandorAsync(Guid id, CancellationToken ct = default);
    Task<Leverandor?> HentLeverandorMedNummerAsync(string leverandornummer, CancellationToken ct = default);
    Task<bool> LeverandornummerEksistererAsync(string leverandornummer, CancellationToken ct = default);
    Task<bool> OrganisasjonsnummerEksistererAsync(string organisasjonsnummer, CancellationToken ct = default);
    Task LeggTilLeverandorAsync(Leverandor leverandor, CancellationToken ct = default);
    Task OppdaterLeverandorAsync(Leverandor leverandor, CancellationToken ct = default);
    Task<List<Leverandor>> SokLeverandorerAsync(string? query, int side, int antall, CancellationToken ct = default);
    Task<int> TellLeverandorerAsync(string? query, CancellationToken ct = default);

    // Faktura
    Task<LeverandorFaktura?> HentFakturaAsync(Guid id, CancellationToken ct = default);
    Task<LeverandorFaktura?> HentFakturaMedLinjerAsync(Guid id, CancellationToken ct = default);
    Task<bool> EksternFakturaDuplikatAsync(Guid leverandorId, string eksternNummer, CancellationToken ct = default);
    Task LeggTilFakturaAsync(LeverandorFaktura faktura, CancellationToken ct = default);
    Task OppdaterFakturaAsync(LeverandorFaktura faktura, CancellationToken ct = default);
    Task<int> NesteInternNummerAsync(CancellationToken ct = default);
    Task<List<LeverandorFaktura>> HentApnePosterAsync(DateOnly? dato = null, CancellationToken ct = default);
    Task<List<LeverandorFaktura>> HentForfalteFakturaerForBetalingAsync(
        DateOnly forfallTilOgMed, List<Guid>? leverandorIder = null, CancellationToken ct = default);
    Task<List<LeverandorFaktura>> HentFakturaerForLeverandorAsync(
        Guid leverandorId, DateOnly? fraDato = null, DateOnly? tilDato = null, CancellationToken ct = default);

    // Betalingsforslag
    Task<Betalingsforslag?> HentBetalingsforslagAsync(Guid id, CancellationToken ct = default);
    Task<Betalingsforslag?> HentBetalingsforslagMedLinjerAsync(Guid id, CancellationToken ct = default);
    Task LeggTilBetalingsforslagAsync(Betalingsforslag forslag, CancellationToken ct = default);
    Task OppdaterBetalingsforslagAsync(Betalingsforslag forslag, CancellationToken ct = default);
    Task<int> NesteForslagsnummerAsync(CancellationToken ct = default);

    // Betaling
    Task LeggTilBetalingAsync(LeverandorBetaling betaling, CancellationToken ct = default);
    Task<LeverandorBetaling?> HentBetalingMedBankreferanseAsync(string bankreferanse, CancellationToken ct = default);

    Task LagreEndringerAsync(CancellationToken ct = default);
}
