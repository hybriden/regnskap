namespace Regnskap.Domain.Features.Leverandorreskontro;

using Regnskap.Domain.Common;

/// <summary>
/// Leverandor (supplier). Grunndata for leverandorreskontro.
/// Mapper til SAF-T: MasterFiles > Suppliers > Supplier.
/// Bokforingsforskriften 3-1: leverandorspesifikasjon krever leverandorkode, navn og org.nr.
/// </summary>
public class Leverandor : AuditableEntity
{
    /// <summary>
    /// Unikt leverandornummer. Brukes som intern referanse.
    /// Mapper til SAF-T SupplierID.
    /// </summary>
    public string Leverandornummer { get; set; } = default!;

    /// <summary>
    /// Leverandorens fulle navn.
    /// Mapper til SAF-T Supplier/Name.
    /// </summary>
    public string Navn { get; set; } = default!;

    /// <summary>
    /// Organisasjonsnummer (9 siffer). Obligatorisk for norske foretak.
    /// Mapper til SAF-T Supplier/TaxRegistration/TaxRegistrationNumber.
    /// </summary>
    public string? Organisasjonsnummer { get; set; }

    /// <summary>
    /// Om leverandoren er MVA-registrert.
    /// </summary>
    public bool ErMvaRegistrert { get; set; }

    // --- Adresse ---
    public string? Adresse1 { get; set; }
    public string? Adresse2 { get; set; }
    public string? Postnummer { get; set; }
    public string? Poststed { get; set; }
    public string Landkode { get; set; } = "NO";

    // --- Kontakt ---
    public string? Kontaktperson { get; set; }
    public string? Telefon { get; set; }
    public string? Epost { get; set; }

    // --- Betaling ---
    public Betalingsbetingelse Betalingsbetingelse { get; set; } = Betalingsbetingelse.Netto30;
    public int? EgendefinertBetalingsfrist { get; set; }
    public string? Bankkontonummer { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public string? Banknavn { get; set; }

    // --- Bokforing ---
    public Guid? StandardKontoId { get; set; }
    public string? StandardMvaKode { get; set; }
    public string Valutakode { get; set; } = "NOK";

    public bool ErAktiv { get; set; } = true;
    public bool ErSperret { get; set; }
    public string? Notat { get; set; }

    // --- SAF-T ---
    public string SaftSupplierId => Leverandornummer;

    // --- Navigasjon ---
    public ICollection<LeverandorFaktura> Fakturaer { get; set; } = new List<LeverandorFaktura>();

    /// <summary>
    /// Beregn antall dager for betalingsbetingelse.
    /// </summary>
    public int HentBetalingsfristDager()
    {
        return Betalingsbetingelse switch
        {
            Betalingsbetingelse.Netto10 => 10,
            Betalingsbetingelse.Netto14 => 14,
            Betalingsbetingelse.Netto20 => 20,
            Betalingsbetingelse.Netto30 => 30,
            Betalingsbetingelse.Netto45 => 45,
            Betalingsbetingelse.Netto60 => 60,
            Betalingsbetingelse.Netto90 => 90,
            Betalingsbetingelse.Kontant => 0,
            Betalingsbetingelse.Egendefinert => EgendefinertBetalingsfrist ?? 30,
            _ => 30
        };
    }
}
