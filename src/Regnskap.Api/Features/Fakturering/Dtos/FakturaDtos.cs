namespace Regnskap.Api.Features.Fakturering.Dtos;

using Regnskap.Domain.Features.Fakturering;

public record FakturaResponse(
    Guid Id,
    string? FakturaId,
    FakturaDokumenttype Dokumenttype,
    FakturaStatus Status,
    Guid KundeId,
    string KundeNavn,
    string? Kundenummer,
    DateOnly? Fakturadato,
    DateOnly? Forfallsdato,
    DateOnly? Leveringsdato,
    decimal BelopEksMva,
    decimal MvaBelop,
    decimal BelopInklMva,
    string? KidNummer,
    string Valutakode,
    string? Bestillingsnummer,
    string? KjopersReferanse,
    FakturaLeveringsformat Leveringsformat,
    Guid? KreditertFakturaId,
    string? Krediteringsaarsak,
    bool EhfGenerert,
    List<FakturaLinjeResponse> Linjer,
    List<FakturaMvaLinjeResponse> MvaLinjer
);

public record FakturaLinjeResponse(
    Guid Id,
    int Linjenummer,
    string Beskrivelse,
    decimal Antall,
    Enhet Enhet,
    decimal Enhetspris,
    RabattType? RabattType,
    decimal? RabattProsent,
    decimal? RabattBelop,
    decimal Nettobelop,
    string MvaKode,
    decimal MvaSats,
    decimal MvaBelop,
    decimal Bruttobelop,
    string Kontonummer
);

public record FakturaMvaLinjeResponse(
    string MvaKode,
    decimal MvaSats,
    decimal Grunnlag,
    decimal MvaBelop,
    string EhfTaxCategoryId
);

public record FakturaListeResponse(
    List<FakturaResponse> Fakturaer,
    int TotaltAntall,
    int Side,
    int Antall
);
