using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Application.Features.Hovedbok;

public record RegnskapsperiodeDto(
    Guid Id,
    int Ar,
    int Periode,
    string Periodenavn,
    DateOnly FraDato,
    DateOnly TilDato,
    string Status,
    DateTime? LukketTidspunkt,
    string? LukketAv,
    string? Merknad);

public record BilagDto(
    Guid Id,
    string BilagsId,
    int Bilagsnummer,
    int Ar,
    string Type,
    DateOnly Bilagsdato,
    DateTime Registreringsdato,
    string Beskrivelse,
    string? EksternReferanse,
    RegnskapsperiodeDto Periode,
    List<PosteringDto> Posteringer,
    decimal SumDebet,
    decimal SumKredit);

public record PosteringDto(
    Guid Id,
    int Linjenummer,
    string Kontonummer,
    string Kontonavn,
    string Side,
    decimal Belop,
    string Beskrivelse,
    string? MvaKode,
    decimal? MvaBelop,
    decimal? MvaGrunnlag,
    decimal? MvaSats,
    string? Avdelingskode,
    string? Prosjektkode);

public record OpprettBilagRequest(
    BilagType Type,
    DateOnly Bilagsdato,
    string Beskrivelse,
    string? EksternReferanse,
    List<OpprettPosteringRequest> Posteringer);

public record OpprettPosteringRequest(
    string Kontonummer,
    BokforingSide Side,
    decimal Belop,
    string Beskrivelse,
    string? MvaKode,
    string? Avdelingskode,
    string? Prosjektkode);

public record KontoutskriftDto(
    string Kontonummer,
    string Kontonavn,
    string Kontotype,
    string Normalbalanse,
    DateOnly FraDato,
    DateOnly TilDato,
    decimal InngaendeBalanse,
    List<KontoutskriftLinjeDto> Posteringer,
    decimal SumDebet,
    decimal SumKredit,
    decimal UtgaendeBalanse,
    int TotaltAntall,
    int Side,
    int Antall);

public record KontoutskriftLinjeDto(
    DateOnly Bilagsdato,
    string BilagsId,
    string BilagBeskrivelse,
    int Linjenummer,
    string Beskrivelse,
    string Side,
    decimal Belop,
    decimal LopendeBalanse);

public record SaldobalanseDto(
    int Ar,
    int Periode,
    string Periodenavn,
    List<SaldobalanseLinjeDto> Kontoer,
    decimal TotalSumDebet,
    decimal TotalSumKredit,
    bool ErIBalanse);

public record SaldobalanseLinjeDto(
    string Kontonummer,
    string Kontonavn,
    string Kontotype,
    decimal InngaendeBalanse,
    decimal SumDebet,
    decimal SumKredit,
    decimal Endring,
    decimal UtgaendeBalanse);

public record KontoSaldoOppslagDto(
    string Kontonummer,
    string Kontonavn,
    int Ar,
    List<KontoSaldoPeriodeDto> Perioder,
    decimal TotalInngaendeBalanse,
    decimal TotalSumDebet,
    decimal TotalSumKredit,
    decimal TotalUtgaendeBalanse);

public record KontoSaldoPeriodeDto(
    int Periode,
    decimal InngaendeBalanse,
    decimal SumDebet,
    decimal SumKredit,
    decimal UtgaendeBalanse,
    int AntallPosteringer);

public record PeriodeavstemmingDto(
    int Ar,
    int Periode,
    bool ErKlarForLukking,
    List<AvstemmingKontrollDto> Kontroller);

public record AvstemmingKontrollDto(
    string Navn,
    string Beskrivelse,
    string Status,
    string? Detaljer);

public record EndrePeriodeStatusRequest(
    PeriodeStatus NyStatus,
    string? Merknad);

public record OpprettPerioderRequest(
    int Ar);
