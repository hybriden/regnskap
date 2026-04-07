namespace Regnskap.Application.Features.Fakturering;

using System.Text;
using System.Xml.Linq;
using Regnskap.Domain.Features.Fakturering;

/// <summary>
/// Genererer EHF/PEPPOL BIS Billing 3.0 UBL 2.1 XML.
/// </summary>
public class EhfService : IEhfService
{
    private readonly IFakturaRepository _fakturaRepo;

    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace InvoiceNs = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace CreditNoteNs = "urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2";

    private const string CustomizationId = "urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0";
    private const string ProfileId = "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0";

    public EhfService(IFakturaRepository fakturaRepo)
    {
        _fakturaRepo = fakturaRepo;
    }

    public async Task<byte[]> GenererEhfXmlAsync(Guid fakturaId, CancellationToken ct = default)
    {
        var faktura = await _fakturaRepo.HentMedLinjerAsync(fakturaId, ct)
            ?? throw new FakturaKundeIkkeFunnetException(fakturaId);

        if (faktura.Status != FakturaStatus.Utstedt && faktura.Status != FakturaStatus.Kreditert)
            throw new FakturaException("EHF_IKKE_UTSTEDT", "Kun utstedte fakturaer kan genereres som EHF.");

        if (faktura.Leveringsformat == FakturaLeveringsformat.Ehf)
        {
            if (string.IsNullOrWhiteSpace(faktura.KjopersReferanse) && string.IsNullOrWhiteSpace(faktura.Bestillingsnummer))
                throw new EhfManglerKjopersRefException();
        }

        var selskapsinfo = await _fakturaRepo.HentSelskapsinfoAsync(ct);

        XDocument doc;
        if (faktura.Dokumenttype == FakturaDokumenttype.Kreditnota)
            doc = GenererCreditNote(faktura, selskapsinfo);
        else
            doc = GenererInvoice(faktura, selskapsinfo);

        faktura.EhfGenerert = true;
        await _fakturaRepo.LagreEndringerAsync(ct);

        using var stream = new MemoryStream();
        doc.Save(stream);
        return stream.ToArray();
    }

    public Task<bool> ValiderEhfXml(byte[] xml)
    {
        try
        {
            var doc = XDocument.Load(new MemoryStream(xml));
            // Enkel validering: sjekk at root element og CustomizationID finnes
            var root = doc.Root;
            if (root == null) return Task.FromResult(false);

            var customId = root.Element(Cbc + "CustomizationID");
            return Task.FromResult(customId != null && customId.Value == CustomizationId);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private XDocument GenererInvoice(Faktura faktura, Selskapsinfo? selskap)
    {
        var root = new XElement(InvoiceNs + "Invoice",
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
            new XAttribute(XNamespace.Xmlns + "cac", Cac),
            new XElement(Cbc + "CustomizationID", CustomizationId),
            new XElement(Cbc + "ProfileID", ProfileId),
            new XElement(Cbc + "ID", faktura.FakturaId ?? faktura.Id.ToString()),
            new XElement(Cbc + "IssueDate", faktura.Fakturadato?.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "DueDate", faktura.Forfallsdato?.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "InvoiceTypeCode", "380"),
            new XElement(Cbc + "DocumentCurrencyCode", faktura.Valutakode)
        );

        if (!string.IsNullOrWhiteSpace(faktura.KjopersReferanse))
            root.Add(new XElement(Cbc + "BuyerReference", faktura.KjopersReferanse));

        if (!string.IsNullOrWhiteSpace(faktura.Bestillingsnummer))
            root.Add(new XElement(Cac + "OrderReference",
                new XElement(Cbc + "ID", faktura.Bestillingsnummer)));

        if (!string.IsNullOrWhiteSpace(faktura.Merknad))
            root.Add(new XElement(Cbc + "Note", faktura.Merknad));

        // Seller (supplier)
        root.Add(GenererSelger(selskap));

        // Buyer (customer)
        root.Add(GenererKjoper(faktura));

        // Payment means
        root.Add(GenererBetalingsinfo(faktura));

        // Tax total
        root.Add(GenererMvaTotaler(faktura));

        // Legal monetary total
        root.Add(GenererPengetotaler(faktura));

        // Invoice lines
        foreach (var linje in faktura.Linjer.OrderBy(l => l.Linjenummer))
        {
            root.Add(GenererInvoiceLine(linje));
        }

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
    }

    private XDocument GenererCreditNote(Faktura faktura, Selskapsinfo? selskap)
    {
        var root = new XElement(CreditNoteNs + "CreditNote",
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
            new XAttribute(XNamespace.Xmlns + "cac", Cac),
            new XElement(Cbc + "CustomizationID", CustomizationId),
            new XElement(Cbc + "ProfileID", ProfileId),
            new XElement(Cbc + "ID", faktura.FakturaId ?? faktura.Id.ToString()),
            new XElement(Cbc + "IssueDate", faktura.Fakturadato?.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "CreditNoteTypeCode", "381"),
            new XElement(Cbc + "DocumentCurrencyCode", faktura.Valutakode)
        );

        if (!string.IsNullOrWhiteSpace(faktura.KjopersReferanse))
            root.Add(new XElement(Cbc + "BuyerReference", faktura.KjopersReferanse));

        // BillingReference to original invoice
        if (faktura.KreditertFaktura != null)
        {
            root.Add(new XElement(Cac + "BillingReference",
                new XElement(Cac + "InvoiceDocumentReference",
                    new XElement(Cbc + "ID", faktura.KreditertFaktura.FakturaId ?? faktura.KreditertFakturaId.ToString()))));
        }

        root.Add(GenererSelger(selskap));
        root.Add(GenererKjoper(faktura));
        root.Add(GenererMvaTotaler(faktura));
        root.Add(GenererPengetotaler(faktura));

        foreach (var linje in faktura.Linjer.OrderBy(l => l.Linjenummer))
        {
            root.Add(GenererCreditNoteLine(linje));
        }

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
    }

    private XElement GenererSelger(Selskapsinfo? selskap)
    {
        var party = new XElement(Cac + "Party");

        if (selskap?.PeppolId != null)
        {
            party.Add(new XElement(Cac + "EndpointID",
                new XAttribute("schemeID", "0192"), selskap.PeppolId));
        }

        if (selskap != null)
        {
            party.Add(new XElement(Cac + "PartyName",
                new XElement(Cbc + "Name", selskap.Navn)));

            party.Add(new XElement(Cac + "PostalAddress",
                new XElement(Cbc + "StreetName", selskap.Adresse1),
                new XElement(Cbc + "CityName", selskap.Poststed),
                new XElement(Cbc + "PostalZone", selskap.Postnummer),
                new XElement(Cac + "Country",
                    new XElement(Cbc + "IdentificationCode", selskap.Landkode))));

            var partyTaxScheme = new XElement(Cac + "PartyTaxScheme",
                new XElement(Cbc + "CompanyID", $"{selskap.Organisasjonsnummer}MVA"),
                new XElement(Cac + "TaxScheme",
                    new XElement(Cbc + "ID", "VAT")));
            party.Add(partyTaxScheme);

            var partyLegalEntity = new XElement(Cac + "PartyLegalEntity",
                new XElement(Cbc + "RegistrationName", selskap.Navn),
                new XElement(Cbc + "CompanyID", selskap.Organisasjonsnummer));

            if (!string.IsNullOrWhiteSpace(selskap.Foretaksregister))
                partyLegalEntity.Add(new XElement(Cbc + "CompanyLegalForm", selskap.Foretaksregister));

            party.Add(partyLegalEntity);
        }

        return new XElement(Cac + "AccountingSupplierParty", party);
    }

    private XElement GenererKjoper(Faktura faktura)
    {
        var kunde = faktura.Kunde;
        var party = new XElement(Cac + "Party");

        if (kunde?.PeppolId != null)
        {
            party.Add(new XElement(Cac + "EndpointID",
                new XAttribute("schemeID", "0192"), kunde.PeppolId));
        }

        if (kunde != null)
        {
            party.Add(new XElement(Cac + "PartyName",
                new XElement(Cbc + "Name", kunde.Navn)));

            party.Add(new XElement(Cac + "PostalAddress",
                new XElement(Cbc + "StreetName", kunde.Adresse1 ?? ""),
                new XElement(Cbc + "CityName", kunde.Poststed ?? ""),
                new XElement(Cbc + "PostalZone", kunde.Postnummer ?? ""),
                new XElement(Cac + "Country",
                    new XElement(Cbc + "IdentificationCode", kunde.Landkode))));

            party.Add(new XElement(Cac + "PartyLegalEntity",
                new XElement(Cbc + "RegistrationName", kunde.Navn),
                new XElement(Cbc + "CompanyID", kunde.Organisasjonsnummer ?? "")));
        }

        return new XElement(Cac + "AccountingCustomerParty", party);
    }

    private XElement GenererBetalingsinfo(Faktura faktura)
    {
        var pm = new XElement(Cac + "PaymentMeans",
            new XElement(Cbc + "PaymentMeansCode", "30"), // Credit transfer
            new XElement(Cbc + "PaymentID", faktura.KidNummer ?? ""));

        if (!string.IsNullOrWhiteSpace(faktura.Iban))
        {
            pm.Add(new XElement(Cac + "PayeeFinancialAccount",
                new XElement(Cbc + "ID", faktura.Iban),
                new XElement(Cac + "FinancialInstitutionBranch",
                    new XElement(Cbc + "ID", faktura.Bic ?? ""))));
        }
        else if (!string.IsNullOrWhiteSpace(faktura.Bankkontonummer))
        {
            pm.Add(new XElement(Cac + "PayeeFinancialAccount",
                new XElement(Cbc + "ID", faktura.Bankkontonummer)));
        }

        return pm;
    }

    private XElement GenererMvaTotaler(Faktura faktura)
    {
        var taxTotal = new XElement(Cac + "TaxTotal",
            new XElement(Cbc + "TaxAmount",
                new XAttribute("currencyID", faktura.Valutakode),
                faktura.MvaBelop.Verdi.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)));

        foreach (var mvaLinje in faktura.MvaLinjer)
        {
            taxTotal.Add(new XElement(Cac + "TaxSubtotal",
                new XElement(Cbc + "TaxableAmount",
                    new XAttribute("currencyID", faktura.Valutakode),
                    mvaLinje.Grunnlag.Verdi.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                new XElement(Cbc + "TaxAmount",
                    new XAttribute("currencyID", faktura.Valutakode),
                    mvaLinje.MvaBelop.Verdi.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                new XElement(Cac + "TaxCategory",
                    new XElement(Cbc + "ID", mvaLinje.EhfTaxCategoryId),
                    new XElement(Cbc + "Percent", mvaLinje.MvaSats.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", "VAT")))));
        }

        return taxTotal;
    }

    private XElement GenererPengetotaler(Faktura faktura)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        return new XElement(Cac + "LegalMonetaryTotal",
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", faktura.Valutakode),
                faktura.BelopEksMva.Verdi.ToString("F2", inv)),
            new XElement(Cbc + "TaxExclusiveAmount",
                new XAttribute("currencyID", faktura.Valutakode),
                faktura.BelopEksMva.Verdi.ToString("F2", inv)),
            new XElement(Cbc + "TaxInclusiveAmount",
                new XAttribute("currencyID", faktura.Valutakode),
                faktura.BelopInklMva.Verdi.ToString("F2", inv)),
            new XElement(Cbc + "PayableAmount",
                new XAttribute("currencyID", faktura.Valutakode),
                faktura.BelopInklMva.Verdi.ToString("F2", inv)));
    }

    private XElement GenererInvoiceLine(FakturaLinje linje)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        return new XElement(Cac + "InvoiceLine",
            new XElement(Cbc + "ID", linje.Linjenummer.ToString()),
            new XElement(Cbc + "InvoicedQuantity",
                new XAttribute("unitCode", FakturaBeregning.EnhetTilUblKode(linje.Enhet)),
                linje.Antall.ToString("F2", inv)),
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", "NOK"),
                linje.Nettobelop.Verdi.ToString("F2", inv)),
            new XElement(Cac + "Item",
                new XElement(Cbc + "Name", linje.Beskrivelse),
                new XElement(Cac + "ClassifiedTaxCategory",
                    new XElement(Cbc + "ID", FakturaBeregning.MapTilEhfTaxCategory(linje.MvaKode, linje.MvaSats)),
                    new XElement(Cbc + "Percent", linje.MvaSats.ToString("F2", inv)),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", "VAT")))),
            new XElement(Cac + "Price",
                new XElement(Cbc + "PriceAmount",
                    new XAttribute("currencyID", "NOK"),
                    linje.Enhetspris.Verdi.ToString("F2", inv))));
    }

    private XElement GenererCreditNoteLine(FakturaLinje linje)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        return new XElement(Cac + "CreditNoteLine",
            new XElement(Cbc + "ID", linje.Linjenummer.ToString()),
            new XElement(Cbc + "CreditedQuantity",
                new XAttribute("unitCode", FakturaBeregning.EnhetTilUblKode(linje.Enhet)),
                linje.Antall.ToString("F2", inv)),
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", "NOK"),
                linje.Nettobelop.Verdi.ToString("F2", inv)),
            new XElement(Cac + "Item",
                new XElement(Cbc + "Name", linje.Beskrivelse),
                new XElement(Cac + "ClassifiedTaxCategory",
                    new XElement(Cbc + "ID", FakturaBeregning.MapTilEhfTaxCategory(linje.MvaKode, linje.MvaSats)),
                    new XElement(Cbc + "Percent", linje.MvaSats.ToString("F2", inv)),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", "VAT")))),
            new XElement(Cac + "Price",
                new XElement(Cbc + "PriceAmount",
                    new XAttribute("currencyID", "NOK"),
                    linje.Enhetspris.Verdi.ToString("F2", inv))));
    }
}
