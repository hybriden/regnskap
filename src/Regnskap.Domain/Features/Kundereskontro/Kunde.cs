namespace Regnskap.Domain.Features.Kundereskontro;

using Regnskap.Domain.Common;

/// <summary>
/// Kunde (customer). Grunndata for kundereskontro.
/// Mapper til SAF-T: MasterFiles > Customers > Customer.
/// Bokforingsforskriften 3-1: kundespesifikasjon krever kundekode og navn.
/// </summary>
public class Kunde : AuditableEntity
{
    /// <summary>
    /// Unikt kundenummer. Brukes som intern referanse.
    /// Mapper til SAF-T CustomerID.
    /// </summary>
    public string Kundenummer { get; set; } = default!;

    /// <summary>
    /// Kundens fulle navn.
    /// Mapper til SAF-T Customer/Name.
    /// </summary>
    public string Navn { get; set; } = default!;

    /// <summary>
    /// Organisasjonsnummer (9 siffer) for bedriftskunder.
    /// </summary>
    public string? Organisasjonsnummer { get; set; }

    /// <summary>
    /// Fodselsnummer/D-nummer (11 siffer) for privatpersoner.
    /// </summary>
    public string? Fodselsnummer { get; set; }

    /// <summary>
    /// Om kunden er en bedrift (true) eller privatperson (false).
    /// </summary>
    public bool ErBedrift { get; set; } = true;

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

    public KundeBetalingsbetingelse Betalingsbetingelse { get; set; } = KundeBetalingsbetingelse.Netto14;
    public int? EgendefinertBetalingsfrist { get; set; }

    // --- KID ---

    public KidAlgoritme? KidAlgoritme { get; set; }

    // --- Bokforing ---

    public Guid? StandardKontoId { get; set; }
    public string? StandardMvaKode { get; set; }
    public string Valutakode { get; set; } = "NOK";
    public Belop Kredittgrense { get; set; } = Belop.Null;
    public bool ErAktiv { get; set; } = true;
    public bool ErSperret { get; set; }
    public string? Notat { get; set; }

    // --- EHF / PEPPOL ---

    public string? PeppolId { get; set; }
    public bool KanMottaEhf { get; set; }

    // --- SAF-T ---

    public string SaftCustomerId => Kundenummer;

    // --- Navigasjon ---

    public ICollection<KundeFaktura> Fakturaer { get; set; } = new List<KundeFaktura>();
}
