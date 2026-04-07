namespace Regnskap.Domain.Features.Rapportering;

using Regnskap.Domain.Common;

/// <summary>
/// Logger alle genererte rapporter for sporbarhet og revisjon.
/// </summary>
public class RapportLogg : AuditableEntity
{
    public RapportType Type { get; set; }
    public int Ar { get; set; }
    public int? FraPeriode { get; set; }
    public int? TilPeriode { get; set; }
    public DateTime GenererTidspunkt { get; set; }
    public string GenererAv { get; set; } = default!;
    public string? Parametre { get; set; }
    public string? Kontrollsum { get; set; }
}
