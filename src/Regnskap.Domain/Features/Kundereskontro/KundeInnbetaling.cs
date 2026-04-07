namespace Regnskap.Domain.Features.Kundereskontro;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Innbetaling fra kunde, koblet til en faktura.
/// Genererer bilag: Debet 1920 Bank, Kredit 1500 Kundefordringer.
/// </summary>
public class KundeInnbetaling : AuditableEntity
{
    public Guid KundeFakturaId { get; set; }
    public KundeFaktura KundeFaktura { get; set; } = default!;

    public DateOnly Innbetalingsdato { get; set; }
    public Belop Belop { get; set; }

    public Guid? BilagId { get; set; }
    public Bilag? Bilag { get; set; }

    public string? Bankreferanse { get; set; }
    public string? KidNummer { get; set; }
    public bool ErAutoMatchet { get; set; }
    public string Betalingsmetode { get; set; } = "Bank";
}
