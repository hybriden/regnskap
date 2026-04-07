namespace Regnskap.Api.Features.Fakturering;

using Dtos;
using Regnskap.Domain.Features.Fakturering;

public static class FakturaMapper
{
    public static FakturaResponse TilResponse(Faktura f) => new(
        Id: f.Id,
        FakturaId: f.FakturaId,
        Dokumenttype: f.Dokumenttype,
        Status: f.Status,
        KundeId: f.KundeId,
        KundeNavn: f.Kunde?.Navn ?? "",
        Kundenummer: f.Kunde?.Kundenummer,
        Fakturadato: f.Fakturadato,
        Forfallsdato: f.Forfallsdato,
        Leveringsdato: f.Leveringsdato,
        BelopEksMva: f.BelopEksMva.Verdi,
        MvaBelop: f.MvaBelop.Verdi,
        BelopInklMva: f.BelopInklMva.Verdi,
        KidNummer: f.KidNummer,
        Valutakode: f.Valutakode,
        Bestillingsnummer: f.Bestillingsnummer,
        KjopersReferanse: f.KjopersReferanse,
        Leveringsformat: f.Leveringsformat,
        KreditertFakturaId: f.KreditertFakturaId,
        Krediteringsaarsak: f.Krediteringsaarsak,
        EhfGenerert: f.EhfGenerert,
        Linjer: f.Linjer.OrderBy(l => l.Linjenummer).Select(TilLinjeResponse).ToList(),
        MvaLinjer: f.MvaLinjer.Select(TilMvaLinjeResponse).ToList()
    );

    public static FakturaLinjeResponse TilLinjeResponse(FakturaLinje l) => new(
        Id: l.Id,
        Linjenummer: l.Linjenummer,
        Beskrivelse: l.Beskrivelse,
        Antall: l.Antall,
        Enhet: l.Enhet,
        Enhetspris: l.Enhetspris.Verdi,
        RabattType: l.RabattType,
        RabattProsent: l.RabattProsent,
        RabattBelop: l.RabattBelop?.Verdi,
        Nettobelop: l.Nettobelop.Verdi,
        MvaKode: l.MvaKode,
        MvaSats: l.MvaSats,
        MvaBelop: l.MvaBelop.Verdi,
        Bruttobelop: l.Bruttobelop.Verdi,
        Kontonummer: l.Kontonummer
    );

    public static FakturaMvaLinjeResponse TilMvaLinjeResponse(FakturaMvaLinje m) => new(
        MvaKode: m.MvaKode,
        MvaSats: m.MvaSats,
        Grunnlag: m.Grunnlag.Verdi,
        MvaBelop: m.MvaBelop.Verdi,
        EhfTaxCategoryId: m.EhfTaxCategoryId
    );
}
