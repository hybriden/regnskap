namespace Regnskap.Application.Features.Periodeavslutning;

using Regnskap.Domain.Features.Periodeavslutning;

// --- Avstemming ---

public record AvstemmingResultatDto(
    int Ar,
    int Periode,
    bool ErKlarForLukking,
    List<AvstemmingKontrollDto> Kontroller,
    List<AvstemmingAdvarselDto> Advarsler);

public record AvstemmingKontrollDto(
    string Kode,
    string Beskrivelse,
    string Status,  // "OK", "ADVARSEL", "FEIL"
    string? Detaljer);

public record AvstemmingAdvarselDto(
    string Kode,
    string Melding,
    string Alvorlighet);  // "INFO", "ADVARSEL", "KRITISK"

// --- Lukking ---

public record LukkPeriodeRequest(
    string? Merknad = null,
    bool TvingLukking = false);

public record PeriodeLukkingDto(
    int Ar,
    int Periode,
    string NyStatus,
    DateTime LukketTidspunkt,
    string LukketAv,
    AvstemmingResultatDto Avstemming,
    List<PeriodeLukkingLoggDto> Logg);

public record PeriodeLukkingLoggDto(
    string Steg,
    string Beskrivelse,
    string Status,
    string? Detaljer,
    DateTime Tidspunkt);

public record GjenapnePeriodeRequest(
    string Begrunnelse);

// --- Arsavslutning ---

public record ArsavslutningRequest(
    string DisponeringKontonummer = "2050",
    decimal? Utbytte = null,
    string UtbytteKontonummer = "2800");

public record ArsavslutningDto(
    int Ar,
    ArsavslutningFase Fase,
    decimal Arsresultat,
    decimal? Utbytte,
    decimal DisponertTilEgenkapital,
    Guid ArsavslutningBilagId,
    Guid ApningsbalanseBilagId,
    List<ArsavslutningStegDto> Steg,
    DateTime FullfortTidspunkt,
    string FullfortAv);

public record ArsavslutningStegDto(
    string Steg,
    string Beskrivelse,
    string Status,
    string? Detaljer);

// --- Anleggsmidler ---

public record OpprettAnleggsmiddelRequest(
    string Navn,
    string? Beskrivelse,
    DateOnly Anskaffelsesdato,
    decimal Anskaffelseskostnad,
    decimal Restverdi,
    int LevetidManeder,
    string BalanseKontonummer,
    string AvskrivningsKontonummer,
    string AkkumulertAvskrivningKontonummer,
    string? Avdelingskode,
    string? Prosjektkode);

public record AnleggsmiddelDto(
    Guid Id,
    string Navn,
    string? Beskrivelse,
    DateOnly Anskaffelsesdato,
    decimal Anskaffelseskostnad,
    decimal Restverdi,
    int LevetidManeder,
    string BalanseKontonummer,
    string AvskrivningsKontonummer,
    string AkkumulertAvskrivningKontonummer,
    string? Avdelingskode,
    string? Prosjektkode,
    bool ErAktivt,
    DateOnly? UtrangeringsDato,
    decimal Avskrivningsgrunnlag,
    decimal ManedligAvskrivning,
    decimal AkkumulertAvskrivning,
    decimal BokfortVerdi,
    decimal GjenvaerendeAvskrivning,
    bool ErFulltAvskrevet,
    List<AvskrivningHistorikkDto> Avskrivninger);

public record AvskrivningHistorikkDto(
    Guid Id,
    int Ar,
    int Periode,
    decimal Belop,
    decimal AkkumulertEtter,
    decimal BokfortVerdiEtter,
    Guid BilagId);

// --- Avskrivninger ---

public record BeregnAvskrivningerRequest(int Ar, int Periode);

public record AvskrivningBeregningDto(
    int Ar,
    int Periode,
    List<AvskrivningLinjeDto> Linjer,
    decimal TotalAvskrivning,
    int AntallAnleggsmidler);

public record AvskrivningLinjeDto(
    Guid AnleggsmiddelId,
    string Navn,
    string BalanseKontonummer,
    string AvskrivningsKontonummer,
    string AkkumulertAvskrivningKontonummer,
    decimal Belop,
    decimal AkkumulertFor,
    decimal AkkumulertEtter,
    decimal BokfortVerdiEtter,
    bool ErSisteAvskrivning);

public record BokforAvskrivningerRequest(int Ar, int Periode);

public record AvskrivningBokforingDto(
    int Ar,
    int Periode,
    List<AvskrivningLinjeDto> Linjer,
    decimal TotalAvskrivning,
    Guid BilagId);

// --- Periodiseringer ---

public record OpprettPeriodiseringRequest(
    string Beskrivelse,
    PeriodiseringsType Type,
    decimal TotalBelop,
    int FraAr,
    int FraPeriode,
    int TilAr,
    int TilPeriode,
    string BalanseKontonummer,
    string ResultatKontonummer,
    string? Avdelingskode,
    string? Prosjektkode,
    Guid? OpprinneligBilagId);

public record PeriodiseringDto(
    Guid Id,
    string Beskrivelse,
    PeriodiseringsType Type,
    decimal TotalBelop,
    int FraAr,
    int FraPeriode,
    int TilAr,
    int TilPeriode,
    string BalanseKontonummer,
    string ResultatKontonummer,
    bool ErAktiv,
    int AntallPerioder,
    decimal BelopPerPeriode,
    decimal SumPeriodisert,
    decimal GjenstaendeBelop,
    List<PeriodiseringsHistorikkDto> Posteringer);

public record PeriodiseringsHistorikkDto(
    Guid Id,
    int Ar,
    int Periode,
    decimal Belop,
    decimal AkkumulertEtter,
    decimal GjenstaarEtter,
    Guid BilagId);

public record BokforPeriodiseringerRequest(int Ar, int Periode);

public record PeriodiseringBokforingDto(
    int Ar,
    int Periode,
    List<PeriodiseringBokforingLinjeDto> Linjer,
    decimal TotalBelop,
    Guid BilagId);

public record PeriodiseringBokforingLinjeDto(
    Guid PeriodiseringId,
    string Beskrivelse,
    PeriodiseringsType Type,
    decimal Belop,
    decimal GjenstaarEtter);

// --- Arsregnskapsklargjoring ---

public record ArsregnskapsklarDto(
    int Ar,
    bool ErKlar,
    List<KlargjoringKontrollDto> Kontroller,
    FilingDeadlineDto Frister);

public record KlargjoringKontrollDto(
    string Kode,
    string Beskrivelse,
    string Status,
    string? Detaljer);

public record FilingDeadlineDto(
    DateOnly Godkjenningsfrist,
    DateOnly Innsendingsfrist,
    bool ErFristUtlopt);
