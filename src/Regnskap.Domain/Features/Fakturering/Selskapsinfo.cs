namespace Regnskap.Domain.Features.Fakturering;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kundereskontro;

/// <summary>
/// Selskapsinnstillinger for fakturering.
/// Inneholder alle felter som kraeves paa utgaaende faktura ihht
/// bokforingsforskriften 5-1-1 nr. 2-3 og EHF BT-27..BT-35.
/// </summary>
public class Selskapsinfo : AuditableEntity
{
    public string Navn { get; set; } = default!;
    public string Organisasjonsnummer { get; set; } = default!;
    public bool ErMvaRegistrert { get; set; }

    /// <summary>
    /// "Foretaksregisteret" -- obligatorisk for AS/ASA/NUF.
    /// EHF BT-35.
    /// </summary>
    public string? Foretaksregister { get; set; }

    // --- Adresse ---
    public string Adresse1 { get; set; } = default!;
    public string? Adresse2 { get; set; }
    public string Postnummer { get; set; } = default!;
    public string Poststed { get; set; } = default!;
    public string Landkode { get; set; } = "NO";

    // --- Kontakt ---
    public string? Telefon { get; set; }
    public string? Epost { get; set; }
    public string? Nettside { get; set; }

    // --- Bank ---
    public string Bankkontonummer { get; set; } = default!;
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public string? Banknavn { get; set; }

    // --- KID ---
    public KidAlgoritme StandardKidAlgoritme { get; set; } = KidAlgoritme.MOD10;

    // --- PDF ---
    public string? LogoFilsti { get; set; }

    // --- PEPPOL ---
    public string? PeppolId { get; set; }
}
