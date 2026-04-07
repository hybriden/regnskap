namespace Regnskap.Application.Features.Kundereskontro;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kundereskontro;

// --- Kunde ---

public record KundeDto(
    Guid Id,
    string Kundenummer,
    string Navn,
    bool ErBedrift,
    string? Organisasjonsnummer,
    string? Fodselsnummer,
    string? Adresse1,
    string? Adresse2,
    string? Postnummer,
    string? Poststed,
    string Landkode,
    string? Kontaktperson,
    string? Telefon,
    string? Epost,
    KundeBetalingsbetingelse Betalingsbetingelse,
    int? EgendefinertBetalingsfrist,
    Guid? StandardKontoId,
    string? StandardMvaKode,
    decimal Kredittgrense,
    string? PeppolId,
    bool KanMottaEhf,
    bool ErAktiv,
    bool ErSperret);

public record OpprettKundeRequest(
    string Kundenummer,
    string Navn,
    bool ErBedrift,
    string? Organisasjonsnummer,
    string? Fodselsnummer,
    string? Adresse1,
    string? Adresse2,
    string? Postnummer,
    string? Poststed,
    string Landkode,
    string? Kontaktperson,
    string? Telefon,
    string? Epost,
    KundeBetalingsbetingelse Betalingsbetingelse,
    int? EgendefinertBetalingsfrist,
    Guid? StandardKontoId,
    string? StandardMvaKode,
    decimal? Kredittgrense,
    string? PeppolId,
    bool KanMottaEhf);

public record OppdaterKundeRequest(
    string Navn,
    string? Organisasjonsnummer,
    string? Fodselsnummer,
    string? Adresse1,
    string? Adresse2,
    string? Postnummer,
    string? Poststed,
    string Landkode,
    string? Kontaktperson,
    string? Telefon,
    string? Epost,
    KundeBetalingsbetingelse Betalingsbetingelse,
    int? EgendefinertBetalingsfrist,
    Guid? StandardKontoId,
    string? StandardMvaKode,
    decimal? Kredittgrense,
    string? PeppolId,
    bool KanMottaEhf,
    bool ErAktiv,
    bool ErSperret);

public record KundeSokRequest(
    string? Query,
    int Side = 1,
    int Antall = 50);

// --- Faktura ---

public record KundeFakturaDto(
    Guid Id,
    Guid KundeId,
    string Kundenummer,
    string KundeNavn,
    int Fakturanummer,
    KundeTransaksjonstype Type,
    DateOnly Fakturadato,
    DateOnly Forfallsdato,
    string Beskrivelse,
    decimal BelopEksMva,
    decimal MvaBelop,
    decimal BelopInklMva,
    decimal GjenstaendeBelop,
    KundeFakturaStatus Status,
    string? KidNummer,
    string? EksternReferanse,
    int AntallPurringer);

public record RegistrerKundeFakturaRequest(
    Guid KundeId,
    KundeTransaksjonstype Type,
    DateOnly Fakturadato,
    DateOnly? Forfallsdato,
    DateOnly? Leveringsdato,
    string Beskrivelse,
    string? EksternReferanse,
    string? Bestillingsnummer,
    string Valutakode,
    decimal? Valutakurs,
    List<KundeFakturaLinjeRequest> Linjer);

public record KundeFakturaLinjeRequest(
    Guid KontoId,
    string Beskrivelse,
    decimal Antall,
    decimal Enhetspris,
    string? MvaKode,
    decimal Rabatt,
    string? Avdelingskode,
    string? Prosjektkode);

public record KundeFakturaSokRequest(
    Guid? KundeId,
    KundeFakturaStatus? Status,
    int Side = 1,
    int Antall = 50);

// --- Innbetaling ---

public record KundeInnbetalingDto(
    Guid Id,
    Guid KundeFakturaId,
    int Fakturanummer,
    DateOnly Innbetalingsdato,
    decimal Belop,
    string? Bankreferanse,
    string? KidNummer,
    bool ErAutoMatchet,
    string Betalingsmetode);

public record RegistrerInnbetalingRequest(
    Guid KundeFakturaId,
    DateOnly Innbetalingsdato,
    decimal Belop,
    string? Bankreferanse,
    string? KidNummer,
    string Betalingsmetode);

public record MatchKidRequest(
    string KidNummer,
    decimal Belop,
    DateOnly Innbetalingsdato,
    string? Bankreferanse);

public record UmatchetInnbetalingDto(
    string? KidNummer,
    decimal Belop,
    DateOnly Dato,
    string? Bankreferanse,
    string Status);

// --- Purring ---

public record PurringDto(
    Guid Id,
    Guid KundeFakturaId,
    int Fakturanummer,
    string KundeNavn,
    PurringType Type,
    DateOnly Purringsdato,
    DateOnly NyForfallsdato,
    decimal Gebyr,
    decimal Forsinkelsesrente,
    bool ErSendt);

public record PurreforslagDto(
    Guid FakturaId,
    int Fakturanummer,
    Guid KundeId,
    string KundeNavn,
    decimal GjenstaendeBelop,
    DateOnly Forfallsdato,
    int DagerForfalt,
    PurringType ForeslattType,
    decimal ForeslattGebyr);

public record PurreforslagRequest(
    DateOnly Dato,
    int MinimumDagerForfalt = 14,
    bool InkluderPurring1 = true,
    bool InkluderPurring2 = true,
    bool InkluderPurring3 = true,
    List<Guid>? KundeIder = null);

// --- Rapporter ---

public record KundeAldersfordelingDto(
    List<AldersfordelingKundeDto> Kunder,
    AldersfordelingSummaryDto Totalt,
    DateOnly Dato);

public record AldersfordelingKundeDto(
    Guid KundeId,
    string Kundenummer,
    string Navn,
    decimal IkkeForfalt,
    decimal Dager0Til30,
    decimal Dager31Til60,
    decimal Dager61Til90,
    decimal Over90Dager,
    decimal Totalt);

public record AldersfordelingSummaryDto(
    decimal IkkeForfalt,
    decimal Dager0Til30,
    decimal Dager31Til60,
    decimal Dager61Til90,
    decimal Over90Dager,
    decimal Totalt);

public record KundeutskriftDto(
    Guid KundeId,
    string Kundenummer,
    string Navn,
    decimal InngaaendeSaldo,
    List<KundeutskriftLinjeDto> Transaksjoner,
    decimal UtgaaendeSaldo,
    DateOnly FraDato,
    DateOnly TilDato);

public record KundeutskriftLinjeDto(
    DateOnly Dato,
    string BilagsId,
    string Beskrivelse,
    KundeTransaksjonstype Type,
    decimal? Debet,
    decimal? Kredit,
    decimal Saldo,
    int? Fakturanummer,
    string? KidNummer);

// --- CAMT.053 ---

public record Camt053ImportResultDto(
    int TotaltAntall,
    int AutoMatchet,
    int ManuellBehandling,
    int Feilet,
    List<Camt053TransaksjonDto> Transaksjoner);

public record Camt053TransaksjonDto(
    string Bankreferanse,
    decimal Belop,
    DateOnly Dato,
    string? KidNummer,
    Guid? MatchetFakturaId,
    string? MatchetKundenavn,
    string Status);
