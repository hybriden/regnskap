namespace Regnskap.Domain.Features.Periodeavslutning;

using Regnskap.Domain.Common;

/// <summary>
/// Sporer statusen for en arsavslutning.
/// </summary>
public class ArsavslutningStatus : AuditableEntity
{
    public int Ar { get; set; }
    public ArsavslutningFase Fase { get; set; } = ArsavslutningFase.IkkeStartet;

    /// <summary>
    /// Arsresultat som skal disponeres.
    /// Beregnes som netto resultat for kontoklasse 3-8.
    /// </summary>
    public decimal? Arsresultat { get; set; }

    /// <summary>
    /// Konto for disponering av overskudd/underskudd.
    /// Standard: 2050 (Annen innskutt EK) eller 2100 (Annen opptjent EK).
    /// </summary>
    public string? DisponeringKontonummer { get; set; }

    /// <summary>
    /// Bilag-ID for arsavslutningsbilaget.
    /// </summary>
    public Guid? ArsavslutningBilagId { get; set; }

    /// <summary>
    /// Bilag-ID for apningsbalanse neste ar.
    /// </summary>
    public Guid? ApningsbalanseBilagId { get; set; }

    /// <summary>
    /// Tidspunkt arsavslutning ble fullfort.
    /// </summary>
    public DateTime? FullfortTidspunkt { get; set; }
    public string? FullfortAv { get; set; }
}
