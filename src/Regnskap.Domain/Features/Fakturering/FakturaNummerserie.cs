namespace Regnskap.Domain.Features.Fakturering;

using Regnskap.Domain.Common;

/// <summary>
/// Nummerserie for fakturering per aar.
/// Bokforingsforskriften 5-1-1 / 5-2-1: "kontrollbar ubrudt nummerserie".
/// </summary>
public class FakturaNummerserie : AuditableEntity
{
    /// <summary>
    /// Regnskapsaaret.
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Dokumenttype (Faktura eller Kreditnota).
    /// Separat serie per type.
    /// </summary>
    public FakturaDokumenttype Dokumenttype { get; set; }

    /// <summary>
    /// Siste brukte nummer i serien.
    /// Neste nummer = SisteNummer + 1.
    /// </summary>
    public int SisteNummer { get; set; }

    /// <summary>
    /// Prefiks for visning (f.eks. "F" for faktura, "K" for kreditnota).
    /// </summary>
    public string? Prefiks { get; set; }
}
