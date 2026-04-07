namespace Regnskap.Domain.Features.Periodeavslutning;

using Regnskap.Domain.Common;

public class PeriodiseringsHistorikk : AuditableEntity
{
    public Guid PeriodiseringId { get; set; }
    public Periodisering Periodisering { get; set; } = default!;
    public int Ar { get; set; }
    public int Periode { get; set; }
    public decimal Belop { get; set; }
    public decimal AkkumulertEtter { get; set; }
    public decimal GjenstaarEtter { get; set; }
    public Guid BilagId { get; set; }
}
