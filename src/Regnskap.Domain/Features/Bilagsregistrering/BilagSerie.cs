namespace Regnskap.Domain.Features.Bilagsregistrering;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// En bilagsserie for gruppering og nummerering av bilag.
/// Hver serie har sin egen fortlopende nummerserie per ar.
/// Bokforingsloven 5: flere serier tillatt nar hver danner en kontrollerbar, sammenhengende sekvens.
/// </summary>
public class BilagSerie : AuditableEntity
{
    /// <summary>
    /// Unik seriekode (f.eks. "IB", "MAN", "AUTO", "BANK", "LON").
    /// Maks 10 tegn, kun store bokstaver og tall.
    /// </summary>
    public string Kode { get; set; } = default!;

    /// <summary>
    /// Beskrivelse av serien.
    /// </summary>
    public string Navn { get; set; } = default!;

    /// <summary>
    /// Beskrivelse pa engelsk for SAF-T.
    /// </summary>
    public string? NavnEn { get; set; }

    /// <summary>
    /// Standard BilagType for bilag opprettet i denne serien.
    /// </summary>
    public BilagType StandardType { get; set; }

    /// <summary>
    /// Om serien er aktiv og kan motta nye bilag.
    /// </summary>
    public bool ErAktiv { get; set; } = true;

    /// <summary>
    /// Systemserie kan ikke slettes eller deaktiveres.
    /// </summary>
    public bool ErSystemserie { get; set; }

    /// <summary>
    /// SAF-T JournalID. Mapper til GeneralLedgerEntries/Journal/JournalID.
    /// </summary>
    public string SaftJournalId { get; set; } = default!;
}
