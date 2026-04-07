namespace Regnskap.Application.Features.Leverandorreskontro;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Leverandorreskontro;

// --- Leverandor DTOs ---

public record LeverandorDto(
    Guid Id,
    string Leverandornummer,
    string Navn,
    string? Organisasjonsnummer,
    bool ErMvaRegistrert,
    string? Adresse1,
    string? Postnummer,
    string? Poststed,
    string Landkode,
    string? Kontaktperson,
    string? Epost,
    Betalingsbetingelse Betalingsbetingelse,
    string? Bankkontonummer,
    string? Iban,
    bool ErAktiv,
    bool ErSperret
);

public record OpprettLeverandorRequest(
    string Leverandornummer,
    string Navn,
    string? Organisasjonsnummer,
    bool ErMvaRegistrert,
    string? Adresse1,
    string? Adresse2,
    string? Postnummer,
    string? Poststed,
    string Landkode,
    string? Kontaktperson,
    string? Telefon,
    string? Epost,
    Betalingsbetingelse Betalingsbetingelse,
    int? EgendefinertBetalingsfrist,
    string? Bankkontonummer,
    string? Iban,
    string? Bic,
    Guid? StandardKontoId,
    string? StandardMvaKode
);

public record OppdaterLeverandorRequest(
    string Navn,
    string? Organisasjonsnummer,
    bool ErMvaRegistrert,
    string? Adresse1,
    string? Adresse2,
    string? Postnummer,
    string? Poststed,
    string Landkode,
    string? Kontaktperson,
    string? Telefon,
    string? Epost,
    Betalingsbetingelse Betalingsbetingelse,
    int? EgendefinertBetalingsfrist,
    string? Bankkontonummer,
    string? Iban,
    string? Bic,
    Guid? StandardKontoId,
    string? StandardMvaKode,
    bool ErAktiv,
    bool ErSperret,
    string? Notat
);

public record LeverandorSokRequest(string? Query, int Side = 1, int Antall = 50);

// --- Faktura DTOs ---

public record LeverandorFakturaDto(
    Guid Id,
    Guid LeverandorId,
    string LeverandorNavn,
    string LeverandorNummer,
    string EksternFakturanummer,
    int InternNummer,
    LeverandorTransaksjonstype Type,
    DateOnly Fakturadato,
    DateOnly Forfallsdato,
    string Beskrivelse,
    decimal BelopEksMva,
    decimal MvaBelop,
    decimal BelopInklMva,
    decimal GjenstaendeBelop,
    FakturaStatus Status,
    string? KidNummer,
    Guid? BilagId,
    bool ErSperret,
    string? SperreArsak,
    List<FakturaLinjeDto> Linjer
);

public record FakturaLinjeDto(
    Guid Id,
    int Linjenummer,
    Guid KontoId,
    string Kontonummer,
    string Beskrivelse,
    decimal Belop,
    string? MvaKode,
    decimal? MvaSats,
    decimal? MvaBelop,
    string? Avdelingskode,
    string? Prosjektkode
);

public record RegistrerFakturaRequest(
    Guid LeverandorId,
    string EksternFakturanummer,
    LeverandorTransaksjonstype Type,
    DateOnly Fakturadato,
    DateOnly? Forfallsdato,
    string Beskrivelse,
    string? KidNummer,
    string Valutakode,
    decimal? Valutakurs,
    List<FakturaLinjeRequest> Linjer
);

public record FakturaLinjeRequest(
    Guid KontoId,
    string Beskrivelse,
    decimal Belop,
    string? MvaKode,
    string? Avdelingskode,
    string? Prosjektkode
);

public record FakturaSokRequest(
    Guid? LeverandorId = null,
    FakturaStatus? Status = null,
    DateOnly? FraDato = null,
    DateOnly? TilDato = null,
    int Side = 1,
    int Antall = 50
);

// --- Betalingsforslag DTOs ---

public record BetalingsforslagDto(
    Guid Id,
    int Forslagsnummer,
    string Beskrivelse,
    DateOnly Opprettdato,
    DateOnly Betalingsdato,
    DateOnly ForfallTilOgMed,
    BetalingsforslagStatus Status,
    decimal TotalBelop,
    int AntallBetalinger,
    string? FraKontonummer,
    string? BetalingsfilReferanse,
    string? GodkjentAv,
    DateTime? GodkjentTidspunkt,
    List<BetalingsforslagLinjeDto> Linjer
);

public record BetalingsforslagLinjeDto(
    Guid Id,
    Guid LeverandorFakturaId,
    string LeverandorNavn,
    string LeverandorNummer,
    string EksternFakturanummer,
    decimal Belop,
    string? MottakerKontonummer,
    string? KidNummer,
    bool ErInkludert,
    bool? ErUtfort,
    string? Feilmelding
);

public record GenererBetalingsforslagRequest(
    DateOnly ForfallTilOgMed,
    DateOnly Betalingsdato,
    Guid? FraBankkontoId,
    string? FraKontonummer,
    bool InkluderKunGodkjente,
    List<Guid>? LeverandorIder
);

// --- Rapport DTOs ---

public record AldersfordelingDto(
    List<AldersfordelingLeverandorDto> Leverandorer,
    AldersfordelingSummaryDto Totalt,
    DateOnly Dato
);

public record AldersfordelingLeverandorDto(
    Guid LeverandorId,
    string Leverandornummer,
    string Navn,
    decimal IkkeForfalt,
    decimal Dager0Til30,
    decimal Dager31Til60,
    decimal Dager61Til90,
    decimal Over90Dager,
    decimal Totalt
);

public record AldersfordelingSummaryDto(
    decimal IkkeForfalt,
    decimal Dager0Til30,
    decimal Dager31Til60,
    decimal Dager61Til90,
    decimal Over90Dager,
    decimal Totalt
);

public record LeverandorutskriftDto(
    Guid LeverandorId,
    string Leverandornummer,
    string Navn,
    decimal InngaaendeSaldo,
    List<LeverandorutskriftLinjeDto> Transaksjoner,
    decimal UtgaaendeSaldo,
    DateOnly FraDato,
    DateOnly TilDato
);

public record LeverandorutskriftLinjeDto(
    DateOnly Dato,
    string BilagsId,
    string Beskrivelse,
    LeverandorTransaksjonstype Type,
    decimal? Debet,
    decimal? Kredit,
    decimal Saldo,
    string? EksternFakturanummer
);
