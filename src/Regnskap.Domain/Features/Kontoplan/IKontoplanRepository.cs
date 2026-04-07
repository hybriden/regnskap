namespace Regnskap.Domain.Features.Kontoplan;

/// <summary>
/// Repository for kontoplan-entiteter.
/// </summary>
public interface IKontoplanRepository
{
    // Kontogrupper
    Task<List<Kontogruppe>> HentAlleKontogrupperAsync(CancellationToken ct = default);
    Task<Kontogruppe?> HentKontogruppeAsync(int gruppekode, CancellationToken ct = default);

    // Kontoer
    Task<Konto?> HentKontoAsync(string kontonummer, CancellationToken ct = default);
    Task<Konto?> HentKontoMedDetaljerAsync(string kontonummer, CancellationToken ct = default);
    Task<bool> KontoFinnesAsync(string kontonummer, CancellationToken ct = default);
    Task<List<Konto>> HentKontoerAsync(
        int? kontoklasse = null,
        Kontotype? kontotype = null,
        int? gruppekode = null,
        bool? erAktiv = null,
        bool? erBokforbar = null,
        string? sok = null,
        int side = 1,
        int antall = 50,
        CancellationToken ct = default);
    Task<int> TellKontoerAsync(
        int? kontoklasse = null,
        Kontotype? kontotype = null,
        int? gruppekode = null,
        bool? erAktiv = null,
        bool? erBokforbar = null,
        string? sok = null,
        CancellationToken ct = default);
    Task<List<Konto>> SokKontoerAsync(string query, int antall = 10, CancellationToken ct = default);
    Task LeggTilKontoAsync(Konto konto, CancellationToken ct = default);
    Task<bool> KontoHarPosteringerAsync(string kontonummer, CancellationToken ct = default);
    Task<bool> KontoHarAktiveUnderkontoerAsync(string kontonummer, CancellationToken ct = default);

    // MVA-koder
    Task<List<MvaKode>> HentAlleMvaKoderAsync(bool? erAktiv = null, MvaRetning? retning = null, CancellationToken ct = default);
    Task<MvaKode?> HentMvaKodeAsync(string kode, CancellationToken ct = default);
    Task<bool> MvaKodeFinnesAsync(string kode, CancellationToken ct = default);
    Task LeggTilMvaKodeAsync(MvaKode mvaKode, CancellationToken ct = default);

    Task LagreEndringerAsync(CancellationToken ct = default);
}
