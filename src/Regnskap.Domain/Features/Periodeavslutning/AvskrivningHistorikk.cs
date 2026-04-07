namespace Regnskap.Domain.Features.Periodeavslutning;

using Regnskap.Domain.Common;

/// <summary>
/// Historikkrad for en enkelt avskrivningspostering.
/// </summary>
public class AvskrivningHistorikk : AuditableEntity
{
    public Guid AnleggsmiddelId { get; set; }
    public Anleggsmiddel Anleggsmiddel { get; set; } = default!;
    public int Ar { get; set; }
    public int Periode { get; set; }
    public decimal Belop { get; set; }
    public decimal AkkumulertEtter { get; set; }
    public decimal BokfortVerdiEtter { get; set; }
    public Guid BilagId { get; set; }
}
