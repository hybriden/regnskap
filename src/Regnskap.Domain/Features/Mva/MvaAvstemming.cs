namespace Regnskap.Domain.Features.Mva;

using Regnskap.Domain.Common;

/// <summary>
/// Resultat av MVA-avstemming for en termin.
/// Sammenligner saldo pa MVA-kontoer med beregnede MVA-verdier fra posteringer.
/// </summary>
public class MvaAvstemming : AuditableEntity
{
    /// <summary>
    /// FK til MVA-terminen.
    /// </summary>
    public Guid MvaTerminId { get; set; }
    public MvaTermin MvaTermin { get; set; } = default!;

    /// <summary>
    /// Tidspunkt for avstemming.
    /// </summary>
    public DateTime AvstemmingTidspunkt { get; set; }

    /// <summary>
    /// Hvem som utforte avstemmingen.
    /// </summary>
    public string AvstemmingAv { get; set; } = default!;

    /// <summary>
    /// Om avstemmingen er godkjent (ingen avvik, eller avvik akseptert).
    /// </summary>
    public bool ErGodkjent { get; set; }

    /// <summary>
    /// Eventuell merknad/begrunnelse.
    /// </summary>
    public string? Merknad { get; set; }

    /// <summary>
    /// Detaljlinjer per MVA-konto.
    /// </summary>
    public List<MvaAvstemmingLinje> Linjer { get; set; } = new();
}
