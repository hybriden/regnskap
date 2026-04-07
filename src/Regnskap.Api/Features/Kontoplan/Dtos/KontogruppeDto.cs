namespace Regnskap.Api.Features.Kontoplan.Dtos;

public record KontogruppeListeDto(
    Guid Id,
    int Gruppekode,
    string Navn,
    string? NavnEn,
    int Kontoklasse,
    string KontoklasseNavn,
    string Kontotype,
    string Normalbalanse,
    bool ErSystemgruppe,
    int AntallKontoer);

public record KontogruppeDetaljerDto(
    Guid Id,
    int Gruppekode,
    string Navn,
    string? NavnEn,
    int Kontoklasse,
    string KontoklasseNavn,
    string Kontotype,
    string Normalbalanse,
    bool ErSystemgruppe,
    List<KontoKortDto> Kontoer);

public record KontoKortDto(
    Guid Id,
    string Kontonummer,
    string Navn,
    string Kontotype,
    bool ErAktiv,
    string? StandardMvaKode);
