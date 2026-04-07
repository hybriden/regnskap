namespace Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Status for en regnskapsperiode.
/// </summary>
public enum PeriodeStatus
{
    /// <summary>Perioden er open for bokforing.</summary>
    Apen,

    /// <summary>Perioden er midlertidig sperret (f.eks. under avstemming).</summary>
    Sperret,

    /// <summary>Perioden er endelig lukket. Ingen posteringer kan legges til.</summary>
    Lukket
}

/// <summary>
/// Type bilag/journal. Mapper til SAF-T Journal/Type.
/// </summary>
public enum BilagType
{
    /// <summary>Manuelt bilag (generell journalforing).</summary>
    Manuelt,

    /// <summary>Inngaende faktura.</summary>
    InngaendeFaktura,

    /// <summary>Utgaende faktura.</summary>
    UtgaendeFaktura,

    /// <summary>Bankbilag (betaling).</summary>
    Bank,

    /// <summary>Lonsbilag.</summary>
    Lonn,

    /// <summary>Avskrivninger.</summary>
    Avskrivning,

    /// <summary>MVA-oppgjor.</summary>
    MvaOppgjor,

    /// <summary>Arsavslutning.</summary>
    Arsavslutning,

    /// <summary>Apningsbalanse.</summary>
    Apningsbalanse,

    /// <summary>Kreditnota.</summary>
    Kreditnota,

    /// <summary>Korrigeringsbilag.</summary>
    Korreksjon,

    /// <summary>Periodisering (accruals).</summary>
    Periodisering
}

/// <summary>
/// Side i et dobbelt bokholderi (debet eller kredit).
/// </summary>
public enum BokforingSide
{
    Debet,
    Kredit
}
