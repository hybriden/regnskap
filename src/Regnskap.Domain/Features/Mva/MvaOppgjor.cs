namespace Regnskap.Domain.Features.Mva;

using Regnskap.Domain.Common;

/// <summary>
/// Beregnet MVA-oppgjor for en termin. Inneholder alle beregnede poster
/// og endelig MVA til betaling eller tilgode.
///
/// Opprettes ved beregning, oppdateres ved ny beregning, lases ved innsending.
/// </summary>
public class MvaOppgjor : AuditableEntity
{
    /// <summary>
    /// FK til MVA-terminen dette oppgjoret gjelder.
    /// </summary>
    public Guid MvaTerminId { get; set; }
    public MvaTermin MvaTermin { get; set; } = default!;

    /// <summary>
    /// Tidspunkt for siste beregning.
    /// </summary>
    public DateTime BeregnetTidspunkt { get; set; }

    /// <summary>
    /// Hvem som kjorte beregningen.
    /// </summary>
    public string BeregnetAv { get; set; } = default!;

    /// <summary>
    /// Sum utgaende MVA (skyldige belop, positiv = skyldig).
    /// </summary>
    public decimal SumUtgaendeMva { get; set; }

    /// <summary>
    /// Sum inngaende MVA (fradragsbelop, positiv = til fradrag).
    /// </summary>
    public decimal SumInngaendeMva { get; set; }

    /// <summary>
    /// Sum snudd avregning utgaende (reverse charge output).
    /// </summary>
    public decimal SumSnuddAvregningUtgaende { get; set; }

    /// <summary>
    /// Sum snudd avregning inngaende (reverse charge input = fradrag).
    /// </summary>
    public decimal SumSnuddAvregningInngaende { get; set; }

    /// <summary>
    /// MVA til betaling. Positivt = skyldig Skatteetaten. Negativt = tilgode.
    /// Beregning: SumUtgaendeMva + SumSnuddAvregningUtgaende - SumInngaendeMva - SumSnuddAvregningInngaende
    /// </summary>
    public decimal MvaTilBetaling { get; set; }

    /// <summary>
    /// Om oppgjoret er last og ikke kan endres.
    /// Settes til true ved innsending av MVA-melding.
    /// </summary>
    public bool ErLast { get; set; }

    /// <summary>
    /// Detaljlinjer per MVA-kode.
    /// </summary>
    public List<MvaOppgjorLinje> Linjer { get; set; } = new();
}
