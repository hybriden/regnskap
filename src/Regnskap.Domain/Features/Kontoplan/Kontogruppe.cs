using Regnskap.Domain.Common;

namespace Regnskap.Domain.Features.Kontoplan;

/// <summary>
/// Kontogruppe i NS 4102. Hierarki: Kontoklasse (1-8) -> Kontogruppe (10-89) -> Konto (1000-8999).
/// </summary>
public class Kontogruppe : AuditableEntity
{
    /// <summary>
    /// Tosifret gruppekode (10-89). Forste siffer = kontoklasse.
    /// </summary>
    public int Gruppekode { get; set; }

    /// <summary>
    /// Norsk navn pa gruppen.
    /// </summary>
    public string Navn { get; set; } = default!;

    /// <summary>
    /// Engelsk navn for SAF-T eksport.
    /// </summary>
    public string? NavnEn { get; set; }

    /// <summary>
    /// Avledet fra forste siffer i Gruppekode.
    /// </summary>
    public Kontoklasse Kontoklasse => (Kontoklasse)(Gruppekode / 10);

    /// <summary>
    /// Kontotype som gjelder for kontoer i denne gruppen.
    /// </summary>
    public Kontotype Kontotype { get; set; }

    /// <summary>
    /// Normalbalanse for gruppen.
    /// </summary>
    public Normalbalanse Normalbalanse { get; set; }

    /// <summary>
    /// Kontoene som tilhorer denne gruppen.
    /// </summary>
    public ICollection<Konto> Kontoer { get; set; } = new List<Konto>();

    /// <summary>
    /// Systemgruppe kan ikke slettes eller fa endret gruppekode.
    /// </summary>
    public bool ErSystemgruppe { get; set; }
}
