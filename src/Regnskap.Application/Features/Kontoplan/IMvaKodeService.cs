using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Application.Features.Kontoplan;

/// <summary>
/// Service for a hente og validere MVA-koder. Brukes av Bilag og MVA-modulen.
/// </summary>
public interface IMvaKodeService
{
    Task<MvaKode?> HentMvaKodeAsync(string kode, CancellationToken ct = default);
    Task<MvaKode> HentMvaKodeEllerKastAsync(string kode, CancellationToken ct = default);
    Task<IReadOnlyList<MvaKode>> HentAlleMvaKoderAsync(bool? erAktiv = null, MvaRetning? retning = null, CancellationToken ct = default);
    Task<string?> HentStandardMvaKodeForKontoAsync(string kontonummer, CancellationToken ct = default);

    // CRUD
    Task<MvaKode> OpprettMvaKodeAsync(OpprettMvaKodeRequest request, CancellationToken ct = default);
    Task<MvaKode> OppdaterMvaKodeAsync(string kode, OppdaterMvaKodeRequest request, CancellationToken ct = default);
}

public record OpprettMvaKodeRequest(
    string Kode,
    string Beskrivelse,
    string? BeskrivelseEn,
    string StandardTaxCode,
    decimal Sats,
    MvaRetning Retning,
    string? UtgaendeKontonummer,
    string? InngaendeKontonummer);

public record OppdaterMvaKodeRequest(
    string Beskrivelse,
    string? BeskrivelseEn,
    decimal Sats,
    bool ErAktiv,
    string? UtgaendeKontonummer,
    string? InngaendeKontonummer);
