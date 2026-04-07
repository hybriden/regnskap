namespace Regnskap.Domain.Features.Bankavstemming;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kontoplan;

/// <summary>
/// En bankkonto registrert i systemet, koblet til en hovedbokkonto.
/// NBS 5: Bankkontoer skal avstemmes mot kontoutskrifter.
/// Mapper til SAF-T: Header > Company > BankAccount.
/// </summary>
public class Bankkonto : AuditableEntity
{
    /// <summary>
    /// Norsk bankkontonummer (11 siffer, format: XXXX.XX.XXXXX).
    /// </summary>
    public string Kontonummer { get; set; } = default!;

    /// <summary>
    /// IBAN (brukes i CAMT.053 for identifisering).
    /// </summary>
    public string? Iban { get; set; }

    /// <summary>
    /// BIC/SWIFT-kode for banken.
    /// </summary>
    public string? Bic { get; set; }

    /// <summary>
    /// Banknavn.
    /// </summary>
    public string Banknavn { get; set; } = default!;

    /// <summary>
    /// Beskrivelse / kallenavn (f.eks. "Driftskonto", "Skattetrekk").
    /// </summary>
    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// Valutakode for kontoen.
    /// </summary>
    public string Valutakode { get; set; } = "NOK";

    /// <summary>
    /// FK til hovedbokkonto (typisk 1920, 1930 etc.).
    /// NS 4102: 1920 = Bankinnskudd, 1930 = Skattetrekk.
    /// </summary>
    public Guid HovedbokkkontoId { get; set; }
    public Konto Hovedbokkonto { get; set; } = default!;

    /// <summary>
    /// Kontonummer fra hovedbok (denormalisert).
    /// </summary>
    public string Hovedbokkontonummer { get; set; } = default!;

    /// <summary>
    /// Om denne bankontoen er aktiv.
    /// </summary>
    public bool ErAktiv { get; set; } = true;

    /// <summary>
    /// Om dette er standardkonto for utbetalinger.
    /// </summary>
    public bool ErStandardUtbetaling { get; set; }

    /// <summary>
    /// Om dette er standardkonto for innbetalinger.
    /// </summary>
    public bool ErStandardInnbetaling { get; set; }

    // --- Navigasjon ---

    public ICollection<Kontoutskrift> Kontoutskrifter { get; set; } = new List<Kontoutskrift>();
    public ICollection<Bankavstemming> Avstemminger { get; set; } = new List<Bankavstemming>();
}
