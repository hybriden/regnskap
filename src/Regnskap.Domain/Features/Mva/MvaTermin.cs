namespace Regnskap.Domain.Features.Mva;

using Regnskap.Domain.Common;

/// <summary>
/// En MVA-termin representerer en rapporteringsperiode for merverdiavgift.
/// Standard: 6 tomandersperioder per ar (Skatteforvaltningsloven §8-3, mval §11-1).
/// Arstermin for sma foretak med omsetning under NOK 1.000.000 (mval §11-4).
/// </summary>
public class MvaTermin : AuditableEntity
{
    /// <summary>
    /// Regnskapsaret.
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Terminnummer: 1-6 for tomandersperioder, 1 for arstermin.
    /// </summary>
    public int Termin { get; set; }

    /// <summary>
    /// Type termin.
    /// </summary>
    public MvaTerminType Type { get; set; }

    /// <summary>
    /// Periodens forste dato (inklusiv).
    /// </summary>
    public DateOnly FraDato { get; set; }

    /// <summary>
    /// Periodens siste dato (inklusiv).
    /// </summary>
    public DateOnly TilDato { get; set; }

    /// <summary>
    /// Frist for innlevering og betaling.
    /// Termin 1: 10. april, Termin 2: 10. juni, Termin 3: 31. august,
    /// Termin 4: 10. oktober, Termin 5: 10. desember, Termin 6: 10. februar (neste ar).
    /// Arstermin: 10. mars (neste ar).
    /// </summary>
    public DateOnly Frist { get; set; }

    /// <summary>
    /// Status for terminen.
    /// </summary>
    public MvaTerminStatus Status { get; set; } = MvaTerminStatus.Apen;

    /// <summary>
    /// Tidspunkt da terminen ble avsluttet/innsendt.
    /// </summary>
    public DateTime? AvsluttetTidspunkt { get; set; }

    /// <summary>
    /// Hvem som avsluttet terminen.
    /// </summary>
    public string? AvsluttetAv { get; set; }

    /// <summary>
    /// Referanse til MVA-oppgjorsbilag (bokfort oppgjorsbilag).
    /// </summary>
    public Guid? OppgjorsBilagId { get; set; }

    /// <summary>
    /// Menneskelig lesbart terminnavn.
    /// </summary>
    public string Terminnavn => Type switch
    {
        MvaTerminType.Tomaaneders => $"{Ar} Termin {Termin} ({FraDato:MMM}-{TilDato:MMM})",
        MvaTerminType.Arlig => $"{Ar} Arstermin",
        _ => $"{Ar} Termin {Termin}"
    };
}
