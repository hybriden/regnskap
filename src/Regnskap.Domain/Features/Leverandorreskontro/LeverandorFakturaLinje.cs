namespace Regnskap.Domain.Features.Leverandorreskontro;

using Regnskap.Domain.Common;

/// <summary>
/// Konteringslinje for en leverandorfaktura.
/// Hver linje representerer en kostnad som fordeles pa en konto med MVA-kode.
/// </summary>
public class LeverandorFakturaLinje : AuditableEntity
{
    public Guid LeverandorFakturaId { get; set; }
    public LeverandorFaktura LeverandorFaktura { get; set; } = default!;

    public int Linjenummer { get; set; }

    /// <summary>
    /// FK til kostnadskontoen (debet-konto).
    /// </summary>
    public Guid KontoId { get; set; }

    /// <summary>
    /// Kontonummer denormalisert.
    /// </summary>
    public string Kontonummer { get; set; } = default!;

    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// Nettobelop (eks. MVA) for denne linjen.
    /// </summary>
    public Belop Belop { get; set; }

    public string? MvaKode { get; set; }
    public decimal? MvaSats { get; set; }
    public Belop? MvaBelop { get; set; }

    public string? Avdelingskode { get; set; }
    public string? Prosjektkode { get; set; }
}
