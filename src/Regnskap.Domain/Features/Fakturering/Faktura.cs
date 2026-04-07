namespace Regnskap.Domain.Features.Fakturering;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kundereskontro;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Utgaaende faktura. Kjerneentiteten i faktureringsmodulen.
/// Representerer hele livssyklusen fra utkast til utstedt/kreditert.
///
/// Bokforingsforskriften 5-1-1: alle obligatoriske felter for salgsdokument.
/// Mapper til SAF-T: SourceDocuments > SalesInvoices > Invoice.
/// EHF: UBL 2.1 Invoice (typeCode 380) eller CreditNote (typeCode 381).
/// </summary>
public class Faktura : AuditableEntity
{
    // --- Identifikasjon ---

    /// <summary>
    /// Fakturanummer. Fortlopende per aar.
    /// Bokforingsforskriften 5-1-1 nr. 1: "fortlopende nummer".
    /// Tildeles forst ved overgang fra Utkast til Godkjent/Utstedt.
    /// </summary>
    public int? Fakturanummer { get; set; }

    /// <summary>
    /// Aaret fakturanummeret tilhorer. Nummerserie per aar.
    /// </summary>
    public int? FakturanummerAr { get; set; }

    /// <summary>
    /// Unik fakturaidentifikator for visning, f.eks. "2026-00042".
    /// </summary>
    public string? FakturaId => Fakturanummer.HasValue && FakturanummerAr.HasValue
        ? $"{FakturanummerAr}-{Fakturanummer:D5}"
        : null;

    /// <summary>
    /// Type dokument (Faktura eller Kreditnota).
    /// </summary>
    public FakturaDokumenttype Dokumenttype { get; set; } = FakturaDokumenttype.Faktura;

    /// <summary>
    /// Status i faktureringsflyten.
    /// </summary>
    public FakturaStatus Status { get; set; } = FakturaStatus.Utkast;

    // --- Kunde ---

    /// <summary>
    /// FK til kunden.
    /// Bokforingsforskriften 5-1-1 nr. 4: "kjoperens navn/adresse".
    /// </summary>
    public Guid KundeId { get; set; }
    public Kunde Kunde { get; set; } = default!;

    // --- Datoer ---

    /// <summary>
    /// Fakturadato. Bokforingsforskriften 5-1-1 nr. 8.
    /// Settes ved utstedelse.
    /// </summary>
    public DateOnly? Fakturadato { get; set; }

    /// <summary>
    /// Forfallsdato. Bokforingsforskriften 5-1-1 nr. 9.
    /// Beregnes fra kundens betalingsbetingelse.
    /// </summary>
    public DateOnly? Forfallsdato { get; set; }

    /// <summary>
    /// Leveringsdato / ytelsesperiode. Bokforingsforskriften 5-1-1 nr. 7.
    /// </summary>
    public DateOnly? Leveringsdato { get; set; }

    /// <summary>
    /// Periodens sluttdato (for tjenester over tid).
    /// EHF BT-73/BT-74: InvoicePeriod.
    /// </summary>
    public DateOnly? LeveringsperiodeSlutt { get; set; }

    // --- Belop (beregnet fra linjer) ---

    /// <summary>
    /// Sum ex. MVA. Bokforingsforskriften 5-1-1 nr. 10.
    /// </summary>
    public Belop BelopEksMva { get; set; } = Belop.Null;

    /// <summary>
    /// Sum MVA. Bokforingsforskriften 5-1-1 nr. 11.
    /// </summary>
    public Belop MvaBelop { get; set; } = Belop.Null;

    /// <summary>
    /// Sum inkl. MVA. Bokforingsforskriften 5-1-1 nr. 12.
    /// </summary>
    public Belop BelopInklMva { get; set; } = Belop.Null;

    // --- Betaling og KID ---

    public string? KidNummer { get; set; }
    public string? Bankkontonummer { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }

    // --- Valuta ---

    public string Valutakode { get; set; } = "NOK";
    public decimal? Valutakurs { get; set; }

    // --- Leveringsformat ---

    public FakturaLeveringsformat Leveringsformat { get; set; } = FakturaLeveringsformat.Epost;

    // --- Referanser ---

    public string? Bestillingsnummer { get; set; }
    public string? KjopersReferanse { get; set; }
    public string? VaarReferanse { get; set; }
    public string? EksternReferanse { get; set; }
    public string? Merknad { get; set; }

    // --- Kreditnota-kobling ---

    public Guid? KreditertFakturaId { get; set; }
    public Faktura? KreditertFaktura { get; set; }
    public ICollection<Faktura> Kreditnotaer { get; set; } = new List<Faktura>();
    public string? Krediteringsaarsak { get; set; }

    // --- Bokforing ---

    public Guid? BilagId { get; set; }
    public Bilag? Bilag { get; set; }
    public Guid? KundeFakturaId { get; set; }
    public KundeFaktura? KundeFaktura { get; set; }

    // --- EHF ---

    public bool EhfGenerert { get; set; }
    public DateTime? EhfSendtTidspunkt { get; set; }
    public string? PeppolLeveringsId { get; set; }

    // --- PDF ---

    public string? PdfFilsti { get; set; }

    // --- Navigasjon ---

    public ICollection<FakturaLinje> Linjer { get; set; } = new List<FakturaLinje>();
    public ICollection<FakturaMvaLinje> MvaLinjer { get; set; } = new List<FakturaMvaLinje>();
}
