namespace Regnskap.Domain.Features.Rapportering;

using Regnskap.Domain.Common;

/// <summary>
/// Konfigurasjon for firmaets rapporteringsoppsett.
/// Lagrer firmainfo som brukes i SAF-T header og rapportoverskrifter.
/// </summary>
public class RapportKonfigurasjon : AuditableEntity
{
    public string Firmanavn { get; set; } = default!;
    public string Organisasjonsnummer { get; set; } = default!;
    public string Adresse { get; set; } = default!;
    public string Postnummer { get; set; } = default!;
    public string Poststed { get; set; } = default!;
    public string Landskode { get; set; } = "NO";
    public bool ErMvaRegistrert { get; set; }
    public string? Kontaktperson { get; set; }
    public string? Telefon { get; set; }
    public string? Epost { get; set; }
    public string? Bankkontonummer { get; set; }
    public string? Iban { get; set; }
    public bool ErSmaForetak { get; set; }
    public string Valuta { get; set; } = "NOK";
}
