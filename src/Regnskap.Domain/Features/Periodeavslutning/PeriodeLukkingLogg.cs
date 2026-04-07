namespace Regnskap.Domain.Features.Periodeavslutning;

using Regnskap.Domain.Common;

/// <summary>
/// Logger alle periodeavslutningssteg for etterprovbarhet.
/// </summary>
public class PeriodeLukkingLogg : AuditableEntity
{
    public int Ar { get; set; }
    public int Periode { get; set; }
    public PeriodeLukkingSteg Steg { get; set; }
    public string Beskrivelse { get; set; } = default!;
    public string Status { get; set; } = default!; // "OK", "ADVARSEL", "FEIL"
    public string? Detaljer { get; set; }
    public DateTime Tidspunkt { get; set; }
    public string UtfortAv { get; set; } = default!;
}
