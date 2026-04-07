using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Api.Features.Kontoplan.Dtos;

public record KontoListeDto(
    Guid Id,
    string Kontonummer,
    string Navn,
    string? NavnEn,
    string Kontotype,
    string Normalbalanse,
    int Kontoklasse,
    int Gruppekode,
    string GruppeNavn,
    string StandardAccountId,
    string? GrupperingsKategori,
    string? GrupperingsKode,
    bool ErAktiv,
    bool ErSystemkonto,
    bool ErBokforbar,
    string? StandardMvaKode,
    bool KreverAvdeling,
    bool KreverProsjekt,
    bool HarUnderkontoer,
    string? OverordnetKontonummer);

public record KontoDetaljerDto(
    Guid Id,
    string Kontonummer,
    string Navn,
    string? NavnEn,
    string Kontotype,
    string Normalbalanse,
    int Kontoklasse,
    int Gruppekode,
    string GruppeNavn,
    string StandardAccountId,
    string? GrupperingsKategori,
    string? GrupperingsKode,
    bool ErAktiv,
    bool ErSystemkonto,
    bool ErBokforbar,
    string? StandardMvaKode,
    string? Beskrivelse,
    bool KreverAvdeling,
    bool KreverProsjekt,
    List<UnderkontoDto> Underkontoer);

public record UnderkontoDto(
    string Kontonummer,
    string Navn,
    bool ErAktiv);

public record KontoOpprettRequest(
    string Kontonummer,
    string Navn,
    string? NavnEn,
    string Kontotype,
    int Gruppekode,
    string StandardAccountId,
    string? GrupperingsKategori,
    string? GrupperingsKode,
    bool ErBokforbar,
    string? StandardMvaKode,
    string? Beskrivelse,
    string? OverordnetKontonummer,
    bool KreverAvdeling,
    bool KreverProsjekt);

public record KontoOppdaterRequest(
    string Navn,
    string? NavnEn,
    bool ErAktiv,
    bool ErBokforbar,
    string? StandardMvaKode,
    string? Beskrivelse,
    string? GrupperingsKategori,
    string? GrupperingsKode,
    bool KreverAvdeling,
    bool KreverProsjekt);

public record KontoOpprettetDto(
    Guid Id,
    string Kontonummer,
    string Navn);

public record PaginertResultat<T>(
    List<T> Data,
    int Side,
    int Antall,
    int TotaltAntall);

public record KontoOppslagDto(
    string Kontonummer,
    string Navn,
    string Kontotype,
    string? StandardMvaKode);
