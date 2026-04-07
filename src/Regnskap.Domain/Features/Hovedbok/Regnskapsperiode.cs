namespace Regnskap.Domain.Features.Hovedbok;

using Regnskap.Domain.Common;

/// <summary>
/// En regnskapsperiode (maned i et regnskapsar).
/// Perioder opprettes per regnskapsar og styrer tilgang til bokforing.
/// </summary>
public class Regnskapsperiode : AuditableEntity
{
    /// <summary>
    /// Regnskapsaret (f.eks. 2026).
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Maned (1-12). Periode 0 brukes for apningsbalanse, periode 13 for arsavslutning.
    /// </summary>
    public int Periode { get; set; }

    /// <summary>
    /// Periodens forste dato (inklusiv).
    /// </summary>
    public DateOnly FraDato { get; set; }

    /// <summary>
    /// Periodens siste dato (inklusiv).
    /// </summary>
    public DateOnly TilDato { get; set; }

    /// <summary>
    /// Periodens status: Apen, Sperret, eller Lukket.
    /// </summary>
    public PeriodeStatus Status { get; set; } = PeriodeStatus.Apen;

    /// <summary>
    /// Tidspunkt da perioden ble lukket. Null hvis apen.
    /// </summary>
    public DateTime? LukketTidspunkt { get; set; }

    /// <summary>
    /// Hvem som lukket perioden.
    /// </summary>
    public string? LukketAv { get; set; }

    /// <summary>
    /// Begrunnelse for lukking eller sperring.
    /// </summary>
    public string? Merknad { get; set; }

    // --- Navigation ---

    /// <summary>
    /// Alle kontosaldoer for denne perioden.
    /// </summary>
    public ICollection<KontoSaldo> KontoSaldoer { get; set; } = new List<KontoSaldo>();

    // --- Avledede egenskaper ---

    /// <summary>
    /// Menneskelig lesbart periodenavn (f.eks. "2026-01", "2026-00 Apningsbalanse").
    /// </summary>
    public string Periodenavn => Periode switch
    {
        0 => $"{Ar}-00 Apningsbalanse",
        13 => $"{Ar}-13 Arsavslutning",
        _ => $"{Ar}-{Periode:D2}"
    };

    /// <summary>
    /// Om perioden aksepterer nye posteringer.
    /// </summary>
    public bool ErApen => Status == PeriodeStatus.Apen;

    /// <summary>
    /// Om perioden er endelig lukket.
    /// </summary>
    public bool ErLukket => Status == PeriodeStatus.Lukket;

    // --- Forretningslogikk ---

    /// <summary>
    /// Valider at en dato faller innenfor perioden.
    /// </summary>
    public bool DatoErInnenforPeriode(DateOnly dato) =>
        dato >= FraDato && dato <= TilDato;

    /// <summary>
    /// Kast exception hvis perioden ikke er apen for bokforing.
    /// </summary>
    public void ValiderApen()
    {
        if (Status != PeriodeStatus.Apen)
            throw new PeriodeLukketException(Ar, Periode);
    }
}
