namespace Regnskap.Application.Features.Mva;

using Regnskap.Domain.Features.Mva;

// --- Termin ---

public record MvaTerminDto(
    Guid Id,
    int Ar,
    int Termin,
    string Type,
    DateOnly FraDato,
    DateOnly TilDato,
    DateOnly Frist,
    string Status,
    string Terminnavn,
    DateTime? AvsluttetTidspunkt,
    string? AvsluttetAv,
    Guid? OppgjorsBilagId,
    bool HarOppgjor,
    bool ErForfalt
);

public record GenererTerminerRequest(
    int Ar,
    MvaTerminType Type = MvaTerminType.Tomaaneders
);

// --- Oppgjor ---

public record MvaOppgjorDto(
    Guid Id,
    Guid MvaTerminId,
    string Terminnavn,
    DateTime BeregnetTidspunkt,
    string BeregnetAv,
    decimal SumUtgaendeMva,
    decimal SumInngaendeMva,
    decimal SumSnuddAvregningUtgaende,
    decimal SumSnuddAvregningInngaende,
    decimal MvaTilBetaling,
    bool ErLast,
    List<MvaOppgjorLinjeDto> Linjer
);

public record MvaOppgjorLinjeDto(
    string MvaKode,
    string StandardTaxCode,
    decimal Sats,
    string Retning,
    int RfPostnummer,
    decimal SumGrunnlag,
    decimal SumMvaBelop,
    int AntallPosteringer
);

// --- Melding (RF-0002) ---

public record MvaMeldingDto(
    Guid MvaTerminId,
    string Terminnavn,
    int Ar,
    int Termin,
    DateOnly FraDato,
    DateOnly TilDato,
    List<MvaMeldingPostDto> Poster,
    decimal SumUtgaendeMva,
    decimal SumInngaendeMva,
    decimal MvaGrunnlagHoySats,
    decimal MvaGrunnlagMiddelsSats,
    decimal MvaGrunnlagLavSats,
    decimal MvaTilBetaling
);

public record MvaMeldingPostDto(
    int Postnummer,
    string Beskrivelse,
    decimal Grunnlag,
    decimal MvaBelop,
    List<string> StandardTaxCodes
);

// --- Avstemming ---

public record MvaAvstemmingDto(
    Guid Id,
    Guid MvaTerminId,
    string Terminnavn,
    DateTime AvstemmingTidspunkt,
    string AvstemmingAv,
    bool ErGodkjent,
    string? Merknad,
    bool HarAvvik,
    decimal TotaltAvvik,
    List<MvaAvstemmingLinjeDto> Linjer
);

public record MvaAvstemmingLinjeDto(
    string Kontonummer,
    string Kontonavn,
    decimal SaldoIflgHovedbok,
    decimal BeregnetFraPosteringer,
    decimal Avvik,
    bool HarAvvik
);

// --- Sammenstilling ---

public record MvaSammenstillingDto(
    int Ar,
    int Termin,
    DateOnly FraDato,
    DateOnly TilDato,
    List<MvaSammenstillingGruppeDto> Grupper,
    decimal TotaltMvaGrunnlag,
    decimal TotaltMvaBelop
);

public record MvaSammenstillingGruppeDto(
    string MvaKode,
    string Beskrivelse,
    string StandardTaxCode,
    decimal Sats,
    string Retning,
    decimal SumGrunnlag,
    decimal SumMvaBelop,
    int AntallPosteringer
);

public record MvaSammenstillingDetaljDto(
    string MvaKode,
    string Beskrivelse,
    List<MvaPosteringDetaljDto> Posteringer,
    decimal SumGrunnlag,
    decimal SumMvaBelop,
    int TotaltAntall
);

public record MvaPosteringDetaljDto(
    Guid PosteringId,
    Guid BilagId,
    int Bilagsnummer,
    DateOnly Bilagsdato,
    string Kontonummer,
    string Beskrivelse,
    string Side,
    decimal Belop,
    decimal MvaGrunnlag,
    decimal MvaBelop,
    decimal MvaSats,
    bool ErAutoGenerertMva
);

// --- SAF-T TaxTable ---

public record SaftTaxTableDto(
    List<SaftTaxCodeDetailDto> TaxCodeDetails
);

public record SaftTaxCodeDetailDto(
    string TaxCode,
    string Description,
    string StandardTaxCode,
    decimal TaxPercentage,
    string Country,
    decimal? BaseRate
);
