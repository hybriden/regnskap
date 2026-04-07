using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Application.Features.Kontoplan;

/// <summary>
/// Service for a hente og validere kontoer. Brukes av andre moduler.
/// </summary>
public interface IKontoService
{
    Task<Konto?> HentKontoAsync(string kontonummer, CancellationToken ct = default);
    Task<bool> KontoFinnesOgErAktivAsync(string kontonummer, CancellationToken ct = default);
    Task<Konto> HentKontoEllerKastAsync(string kontonummer, CancellationToken ct = default);
    Task<IReadOnlyList<Konto>> HentKontoerForGruppeAsync(int gruppekode, CancellationToken ct = default);
    Task<Kontotype> HentKontotypeAsync(string kontonummer, CancellationToken ct = default);
    Task<Normalbalanse> HentNormalbalanseAsync(string kontonummer, CancellationToken ct = default);

    // CRUD operations
    Task<Konto> OpprettKontoAsync(OpprettKontoRequest request, CancellationToken ct = default);
    Task<Konto> OppdaterKontoAsync(string kontonummer, OppdaterKontoRequest request, CancellationToken ct = default);
    Task SlettKontoAsync(string kontonummer, CancellationToken ct = default);
    Task DeaktiverKontoAsync(string kontonummer, CancellationToken ct = default);
    Task AktiverKontoAsync(string kontonummer, CancellationToken ct = default);

    // Query operations
    Task<(List<Konto> Data, int TotaltAntall)> HentKontoerAsync(
        int? kontoklasse = null, Kontotype? kontotype = null, int? gruppekode = null,
        bool? erAktiv = null, bool? erBokforbar = null, string? sok = null,
        int side = 1, int antall = 50, CancellationToken ct = default);
    Task<Konto?> HentKontoMedDetaljerAsync(string kontonummer, CancellationToken ct = default);
    Task<List<Konto>> SokKontoerAsync(string query, int antall = 10, CancellationToken ct = default);
}

public record OpprettKontoRequest(
    string Kontonummer,
    string Navn,
    string? NavnEn,
    Kontotype Kontotype,
    int Gruppekode,
    string StandardAccountId,
    GrupperingsKategori? GrupperingsKategori,
    string? GrupperingsKode,
    bool ErBokforbar,
    string? StandardMvaKode,
    string? Beskrivelse,
    string? OverordnetKontonummer,
    bool KreverAvdeling,
    bool KreverProsjekt);

public record OppdaterKontoRequest(
    string Navn,
    string? NavnEn,
    bool ErAktiv,
    bool ErBokforbar,
    string? StandardMvaKode,
    string? Beskrivelse,
    GrupperingsKategori? GrupperingsKategori,
    string? GrupperingsKode,
    bool KreverAvdeling,
    bool KreverProsjekt,
    // Beskyttede felter -- disse kan sendes inn, men vil avvises for systemkontoer
    Kontotype? Kontotype = null,
    int? Gruppekode = null,
    bool? ErSystemkonto = null);
