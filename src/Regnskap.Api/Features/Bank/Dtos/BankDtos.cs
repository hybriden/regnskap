namespace Regnskap.Api.Features.Bank.Dtos;

using Regnskap.Domain.Features.Bankavstemming;

// --- Bankkonto ---
public record OpprettBankkontoRequest(
    string Kontonummer,
    string? Iban,
    string? Bic,
    string Banknavn,
    string Beskrivelse,
    string Valutakode,
    Guid HovedbokkkontoId,
    bool ErStandardUtbetaling = false,
    bool ErStandardInnbetaling = false
);

public record OppdaterBankkontoRequest(
    string Banknavn,
    string Beskrivelse,
    string? Iban,
    string? Bic,
    bool ErStandardUtbetaling,
    bool ErStandardInnbetaling
);

public record BankkontoResponse(
    Guid Id,
    string Kontonummer,
    string? Iban,
    string? Bic,
    string Banknavn,
    string Beskrivelse,
    string Valutakode,
    string Hovedbokkontonummer,
    bool ErAktiv,
    bool ErStandardUtbetaling,
    bool ErStandardInnbetaling
);

// --- Import ---
public record ImportKontoutskriftResponse(
    Guid KontoutskriftId,
    string MeldingsId,
    DateOnly PeriodeFra,
    DateOnly PeriodeTil,
    decimal InngaendeSaldo,
    decimal UtgaendeSaldo,
    int AntallBevegelser,
    int AntallAutoMatchet,
    int AntallIkkeMatchet
);

public record KontoutskriftResponse(
    Guid Id,
    string MeldingsId,
    DateOnly PeriodeFra,
    DateOnly PeriodeTil,
    decimal InngaendeSaldo,
    decimal UtgaendeSaldo,
    int AntallBevegelser,
    KontoutskriftStatus Status
);

// --- Bevegelser ---
public record BankbevegelseResponse(
    Guid Id,
    DateOnly Bokforingsdato,
    DateOnly? Valuteringsdato,
    BankbevegelseRetning Retning,
    decimal Belop,
    string? KidNummer,
    string? Motpart,
    string? Beskrivelse,
    BankbevegelseStatus Status,
    MatcheType? MatcheType,
    decimal? MatcheKonfidens,
    List<BankbevegelseMatchResponse> Matchinger
);

public record BankbevegelseMatchResponse(
    Guid Id,
    decimal Belop,
    MatcheType MatcheType,
    string? Beskrivelse,
    Guid? KundeFakturaId,
    Guid? LeverandorFakturaId,
    Guid? BilagId
);

// --- Avstemming ---
public record AvstemmingResponse(
    Guid Id,
    Guid BankkontoId,
    DateOnly Avstemmingsdato,
    decimal SaldoHovedbok,
    decimal SaldoBank,
    decimal Differanse,
    decimal UtestaaendeBetalinger,
    decimal InnbetalingerITransitt,
    decimal AndreDifferanser,
    decimal UforklartDifferanse,
    AvstemmingStatus Status,
    string? GodkjentAv,
    DateTime? GodkjentTidspunkt
);

// --- Bokfor ---
public record BokforBankbevegelseRequest(
    Guid MotkontoId,
    string Motkontonummer,
    string? MvaKode,
    string Beskrivelse,
    string? Avdelingskode,
    string? Prosjektkode
);

public record BokforBankbevegelseResponse(
    Guid BilagId
);
