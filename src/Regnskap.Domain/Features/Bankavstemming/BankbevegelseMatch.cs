namespace Regnskap.Domain.Features.Bankavstemming;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kundereskontro;
using Regnskap.Domain.Features.Leverandorreskontro;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Kobling mellom en bankbevegelse og en aapen post (faktura, betaling, bilag).
/// En bevegelse kan matche flere poster (splitt), og en post kan matche flere bevegelser.
/// </summary>
public class BankbevegelseMatch : AuditableEntity
{
    /// <summary>
    /// FK til bankbevegelsen.
    /// </summary>
    public Guid BankbevegelseId { get; set; }
    public Bankbevegelse Bankbevegelse { get; set; } = default!;

    /// <summary>
    /// Belop som er matchet fra denne bevegelsen.
    /// Ved splitt: delbelop. Ellers: hele belopet.
    /// </summary>
    public Belop Belop { get; set; }

    // --- Matching mot ulike entiteter ---

    /// <summary>
    /// FK til kundefaktura (innbetaling fra kunde).
    /// </summary>
    public Guid? KundeFakturaId { get; set; }
    public KundeFaktura? KundeFaktura { get; set; }

    /// <summary>
    /// FK til leverandorfaktura (utbetaling til leverandor).
    /// </summary>
    public Guid? LeverandorFakturaId { get; set; }
    public LeverandorFaktura? LeverandorFaktura { get; set; }

    /// <summary>
    /// FK til eksisterende bilag (f.eks. lonnskjoring, manuelt bilag).
    /// </summary>
    public Guid? BilagId { get; set; }
    public Bilag? Bilag { get; set; }

    /// <summary>
    /// Beskrivelse av matching (automatisk generert eller brukerkommentar).
    /// </summary>
    public string? Beskrivelse { get; set; }

    /// <summary>
    /// Type match.
    /// </summary>
    public MatcheType MatcheType { get; set; }
}
