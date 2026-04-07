namespace Regnskap.Domain.Features.Bankavstemming;

/// <summary>
/// Repository for bankavstemming.
/// </summary>
public interface IBankRepository
{
    Task<Bankkonto?> HentBankkonto(Guid id);
    Task<Bankkonto?> HentBankkontoMedIban(string iban);
    Task<Bankkonto?> HentBankkontoMedKontonummer(string kontonummer);
    Task<IReadOnlyList<Bankkonto>> HentAlleBankkontoer(bool kunAktive = true);
    Task LeggTilBankkonto(Bankkonto bankkonto);
    Task OppdaterBankkonto(Bankkonto bankkonto);

    Task<Bankbevegelse?> HentBevegelse(Guid id);
    Task<IReadOnlyList<Bankbevegelse>> HentUmatchedeBevegelser(Guid bankkontoId);
    Task<IReadOnlyList<Bankbevegelse>> HentBevegelser(Guid bankkontoId, BankbevegelseStatus? status = null, DateOnly? fraDato = null, DateOnly? tilDato = null);
    Task LeggTilBevegelse(Bankbevegelse bevegelse);

    Task<Kontoutskrift?> HentKontoutskrift(Guid id);
    Task<IReadOnlyList<Kontoutskrift>> HentKontoutskrifter(Guid bankkontoId);
    Task<bool> KontoutskriftFinnes(Guid bankkontoId, string meldingsId);
    Task LeggTilKontoutskrift(Kontoutskrift kontoutskrift);

    Task LeggTilMatch(BankbevegelseMatch match);
    Task FjernMatchinger(Guid bankbevegelseId);

    Task<Bankavstemming?> HentAvstemming(Guid bankkontoId, int aar, int periode);
    Task<Bankavstemming?> HentAvstemmingMedId(Guid id);
    Task LeggTilAvstemming(Bankavstemming avstemming);

    Task LagreEndringerAsync(CancellationToken ct = default);
}
