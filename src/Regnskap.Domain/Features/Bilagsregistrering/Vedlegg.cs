namespace Regnskap.Domain.Features.Bilagsregistrering;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Vedlegg knyttet til et bilag (kvittering, fakturakopi, kontrakt etc.).
/// Lagrer kun metadata og filsti -- selve filen lagres i filsystem eller blob storage.
/// Bokforingsloven 6 og 10: dokumentasjonskrav for bokforte transaksjoner.
/// </summary>
public class Vedlegg : AuditableEntity
{
    /// <summary>
    /// FK til bilaget vedlegget tilhorer.
    /// </summary>
    public Guid BilagId { get; set; }
    public Bilag Bilag { get; set; } = default!;

    /// <summary>
    /// Opprinnelig filnavn ved opplasting.
    /// </summary>
    public string Filnavn { get; set; } = default!;

    /// <summary>
    /// MIME-type (f.eks. "application/pdf", "image/jpeg").
    /// </summary>
    public string MimeType { get; set; } = default!;

    /// <summary>
    /// Filstorrelse i bytes.
    /// </summary>
    public long Storrelse { get; set; }

    /// <summary>
    /// Sti til filen i lagring (relativ sti, blob-referanse, eller full sti).
    /// </summary>
    public string LagringSti { get; set; } = default!;

    /// <summary>
    /// SHA-256 hash av filinnholdet for integritetskontroll.
    /// </summary>
    public string HashSha256 { get; set; } = default!;

    /// <summary>
    /// Valgfri beskrivelse av vedlegget.
    /// </summary>
    public string? Beskrivelse { get; set; }

    /// <summary>
    /// Sorteringsrekkefolge nar et bilag har flere vedlegg.
    /// </summary>
    public int Rekkefolge { get; set; }

    // --- Tillatte MIME-typer ---

    public static readonly HashSet<string> TillateMimeTyper = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/tiff",
        "application/xml"
    };

    /// <summary>
    /// Maks filstorrelse: 25 MB.
    /// </summary>
    public const long MaksStorrelse = 25 * 1024 * 1024;
}
