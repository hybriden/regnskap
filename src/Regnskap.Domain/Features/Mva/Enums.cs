namespace Regnskap.Domain.Features.Mva;

/// <summary>
/// Type MVA-termin.
/// </summary>
public enum MvaTerminType
{
    /// <summary>Standard tomandersperiode (6 per ar). Mval §11-1.</summary>
    Tomaaneders,

    /// <summary>Arlig termin for sma foretak under NOK 1.000.000. Mval §11-4.</summary>
    Arlig
}

/// <summary>
/// Status for en MVA-termin.
/// </summary>
public enum MvaTerminStatus
{
    /// <summary>Terminen er apen, transaksjoner kan bokfores.</summary>
    Apen,

    /// <summary>Oppgjor er beregnet, klar for avstemming.</summary>
    Beregnet,

    /// <summary>Avstemming er godkjent, klar for innsending.</summary>
    Avstemt,

    /// <summary>MVA-melding er innsendt.</summary>
    Innsendt,

    /// <summary>Betaling er registrert.</summary>
    Betalt
}
