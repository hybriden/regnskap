using Regnskap.Domain.Features.Kundereskontro;

namespace Regnskap.Api.Features.Kundereskontro.Dtos;

public record OpprettKundeApiRequest(
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

public record OppdaterKundeApiRequest(
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

public record RegistrerFakturaApiRequest(
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
    List<FakturaLinjeApiRequest> Linjer);

public record FakturaLinjeApiRequest(
    Guid KontoId,
    string Beskrivelse,
    decimal Antall,
    decimal Enhetspris,
    string? MvaKode,
    decimal Rabatt,
    string? Avdelingskode,
    string? Prosjektkode);

public record RegistrerInnbetalingApiRequest(
    Guid KundeFakturaId,
    DateOnly Innbetalingsdato,
    decimal Belop,
    string? Bankreferanse,
    string? KidNummer,
    string Betalingsmetode);

public record MatchKidApiRequest(
    string KidNummer,
    decimal Belop,
    DateOnly Innbetalingsdato,
    string? Bankreferanse);

public record OpprettPurringerApiRequest(
    List<Guid> FakturaIder,
    PurringType Type);

public record ValiderKidApiRequest(
    string KidNummer,
    KidAlgoritme Algoritme);
