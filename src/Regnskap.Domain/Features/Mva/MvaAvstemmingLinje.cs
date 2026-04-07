namespace Regnskap.Domain.Features.Mva;

using Regnskap.Domain.Common;

/// <summary>
/// En linje i MVA-avstemmingen, en per MVA-relatert konto.
/// </summary>
public class MvaAvstemmingLinje : AuditableEntity
{
    /// <summary>
    /// FK til avstemmingen.
    /// </summary>
    public Guid MvaAvstemmingId { get; set; }
    public MvaAvstemming MvaAvstemming { get; set; } = default!;

    /// <summary>
    /// Kontonummer (f.eks. "2700", "2710", "1600").
    /// </summary>
    public string Kontonummer { get; set; } = default!;

    /// <summary>
    /// Kontonavn.
    /// </summary>
    public string Kontonavn { get; set; } = default!;

    /// <summary>
    /// Saldo pa kontoen ifg. KontoSaldo (for de aktuelle periodene i terminen).
    /// </summary>
    public decimal SaldoIflgHovedbok { get; set; }

    /// <summary>
    /// Beregnet MVA-belop fra posteringer med MVA-kode for denne kontoen.
    /// </summary>
    public decimal BeregnetFraPosteringer { get; set; }

    /// <summary>
    /// Avvik mellom saldo og beregnet belop.
    /// </summary>
    public decimal Avvik { get; set; }

    /// <summary>
    /// Om linjen har avvik over terskel (0.01 NOK).
    /// </summary>
    public bool HarAvvik => Math.Abs(Avvik) >= 0.01m;
}
