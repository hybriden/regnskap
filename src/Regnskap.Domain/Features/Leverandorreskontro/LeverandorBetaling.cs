namespace Regnskap.Domain.Features.Leverandorreskontro;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Betaling knyttet til en leverandorfaktura.
/// Genererer bilag:
///   Debet: 2400 Leverandorgjeld
///   Kredit: 1920 Bank
/// </summary>
public class LeverandorBetaling : AuditableEntity
{
    public Guid LeverandorFakturaId { get; set; }
    public LeverandorFaktura LeverandorFaktura { get; set; } = default!;

    public Guid? BetalingsforslagId { get; set; }
    public Betalingsforslag? Betalingsforslag { get; set; }

    public DateOnly Betalingsdato { get; set; }
    public Belop Belop { get; set; }

    public Guid? BilagId { get; set; }
    public Bilag? Bilag { get; set; }

    public string? Bankreferanse { get; set; }
    public string Betalingsmetode { get; set; } = "Bank";
}
