namespace Regnskap.Api.Features.Kontoplan.Dtos;

public record MvaKodeListeDto(
    Guid Id,
    string Kode,
    string Beskrivelse,
    string? BeskrivelseEn,
    string StandardTaxCode,
    decimal Sats,
    string Retning,
    string? UtgaendeKontonummer,
    string? InngaendeKontonummer,
    bool ErAktiv,
    bool ErSystemkode);

public record MvaKodeOpprettRequest(
    string Kode,
    string Beskrivelse,
    string? BeskrivelseEn,
    string StandardTaxCode,
    decimal Sats,
    string Retning,
    string? UtgaendeKontonummer,
    string? InngaendeKontonummer);

public record MvaKodeOppdaterRequest(
    string Beskrivelse,
    string? BeskrivelseEn,
    decimal Sats,
    bool ErAktiv,
    string? UtgaendeKontonummer,
    string? InngaendeKontonummer);
