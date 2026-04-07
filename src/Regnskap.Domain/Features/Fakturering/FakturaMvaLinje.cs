namespace Regnskap.Domain.Features.Fakturering;

using Regnskap.Domain.Common;

/// <summary>
/// MVA-oppsummering per sats paa en faktura.
/// Bokforingsforskriften 5-1-1 nr. 11: "MVA-belop spesifisert per sats".
/// Mapper til EHF TaxSubtotal (BT-116..BT-119).
/// </summary>
public class FakturaMvaLinje : AuditableEntity
{
    public Guid FakturaId { get; set; }
    public Faktura Faktura { get; set; } = default!;

    /// <summary>
    /// MVA-kode (f.eks. "3" for 25% utgaaende).
    /// </summary>
    public string MvaKode { get; set; } = default!;

    /// <summary>
    /// MVA-sats i prosent.
    /// </summary>
    public decimal MvaSats { get; set; }

    /// <summary>
    /// Sum grunnlag (netto) for denne satsen. EHF BT-116.
    /// </summary>
    public Belop Grunnlag { get; set; }

    /// <summary>
    /// Sum MVA for denne satsen. EHF BT-117.
    /// </summary>
    public Belop MvaBelop { get; set; }

    /// <summary>
    /// EHF TaxCategory ID (S=standard, Z=zero, E=exempt).
    /// </summary>
    public string EhfTaxCategoryId { get; set; } = "S";
}
