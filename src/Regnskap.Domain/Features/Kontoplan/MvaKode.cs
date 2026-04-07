using Regnskap.Domain.Common;

namespace Regnskap.Domain.Features.Kontoplan;

/// <summary>
/// MVA-kode for bruk i bokforing. Mapper til SAF-T StandardTaxCode.
/// </summary>
public class MvaKode : AuditableEntity
{
    /// <summary>
    /// Intern MVA-kode (f.eks. "1", "3", "25I").
    /// </summary>
    public string Kode { get; set; } = default!;

    /// <summary>
    /// Norsk beskrivelse.
    /// </summary>
    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// Engelsk beskrivelse for SAF-T.
    /// </summary>
    public string? BeskrivelseEn { get; set; }

    /// <summary>
    /// SAF-T StandardTaxCode som denne koden mapper til.
    /// </summary>
    public string StandardTaxCode { get; set; } = default!;

    /// <summary>
    /// MVA-sats i prosent (f.eks. 25.00, 15.00, 12.00, 0.00).
    /// </summary>
    public decimal Sats { get; set; }

    /// <summary>
    /// Om dette er utgaende (salg) eller inngaende (kjop) MVA.
    /// </summary>
    public MvaRetning Retning { get; set; }

    /// <summary>
    /// Konto for utgaende MVA (kredit). FK til Konto. Typisk 2700-serien.
    /// </summary>
    public Guid? UtgaendeKontoId { get; set; }
    public Konto? UtgaendeKonto { get; set; }

    /// <summary>
    /// Konto for inngaende MVA (debet). FK til Konto. Typisk 2710-serien eller 1600-serien.
    /// </summary>
    public Guid? InngaendeKontoId { get; set; }
    public Konto? InngaendeKonto { get; set; }

    /// <summary>
    /// Om koden er aktiv og kan brukes.
    /// </summary>
    public bool ErAktiv { get; set; } = true;

    /// <summary>
    /// Systemkode kan ikke slettes.
    /// </summary>
    public bool ErSystemkode { get; set; }
}
