namespace Regnskap.Domain.Features.Kundereskontro;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Purring (betalingspaaminnelse) sendt til kunde for forfalt faktura.
/// Inkassoloven 9 regulerer purregebyrer.
/// </summary>
public class Purring : AuditableEntity
{
    public Guid KundeFakturaId { get; set; }
    public KundeFaktura KundeFaktura { get; set; } = default!;

    public PurringType Type { get; set; }
    public DateOnly Purringsdato { get; set; }
    public DateOnly NyForfallsdato { get; set; }
    public Belop Gebyr { get; set; } = Belop.Null;
    public Belop Forsinkelsesrente { get; set; } = Belop.Null;

    public Guid? GebyrBilagId { get; set; }
    public Bilag? GebyrBilag { get; set; }

    public bool ErSendt { get; set; }
    public DateTime? SendtTidspunkt { get; set; }
    public string? Sendemetode { get; set; }
}
