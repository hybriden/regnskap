namespace Regnskap.Domain.Features.Kundereskontro;

public interface IKundeReskontroRepository
{
    // Kunde
    Task<Kunde?> HentKundeAsync(Guid id, CancellationToken ct = default);
    Task<Kunde?> HentKundeMedNummerAsync(string kundenummer, CancellationToken ct = default);
    Task<bool> KundenummerEksistererAsync(string kundenummer, CancellationToken ct = default);
    Task LeggTilKundeAsync(Kunde kunde, CancellationToken ct = default);
    Task OppdaterKundeAsync(Kunde kunde, CancellationToken ct = default);

    // Faktura
    Task<KundeFaktura?> HentFakturaAsync(Guid id, CancellationToken ct = default);
    Task<KundeFaktura?> HentFakturaMedKidAsync(string kidNummer, CancellationToken ct = default);
    Task<int> NesteNummer(CancellationToken ct = default);
    Task LeggTilFakturaAsync(KundeFaktura faktura, CancellationToken ct = default);
    Task OppdaterFakturaAsync(KundeFaktura faktura, CancellationToken ct = default);
    Task<List<KundeFaktura>> HentApnePosterAsync(DateOnly? dato = null, CancellationToken ct = default);
    Task<List<KundeFaktura>> HentForfalteFakturaerAsync(DateOnly dato, int minimumDagerForfalt, CancellationToken ct = default);

    // Innbetaling
    Task LeggTilInnbetalingAsync(KundeInnbetaling innbetaling, CancellationToken ct = default);

    // Purring
    Task LeggTilPurringAsync(Purring purring, CancellationToken ct = default);
    Task<Purring?> HentSistePurringAsync(Guid fakturaId, CancellationToken ct = default);

    // Sok / lister
    Task<(List<Kunde> Data, int TotaltAntall)> SokKunderAsync(string? query, int side, int antall, CancellationToken ct = default);
    Task<(List<KundeFaktura> Data, int TotaltAntall)> SokFakturaerAsync(Guid? kundeId, KundeFakturaStatus? status, int side, int antall, CancellationToken ct = default);
    Task<List<KundeFaktura>> HentFakturaerForKundeAsync(Guid kundeId, DateOnly? fraDato, DateOnly? tilDato, CancellationToken ct = default);
    Task<List<Purring>> HentPurringerAsync(int side, int antall, CancellationToken ct = default);

    Task LagreEndringerAsync(CancellationToken ct = default);
}
