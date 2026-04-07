namespace Regnskap.Domain.Features.Bankavstemming;

/// <summary>
/// Status for en bankbevegelse (importert transaksjon).
/// </summary>
public enum BankbevegelseStatus
{
    /// <summary>Importert, ikke matchet.</summary>
    IkkeMatchet,

    /// <summary>Automatisk matchet mot aapen post.</summary>
    AutoMatchet,

    /// <summary>Manuelt matchet av bruker.</summary>
    ManueltMatchet,

    /// <summary>Manuelt splittet og matchet.</summary>
    Splittet,

    /// <summary>Bokfort som ny transaksjon (ingen eksisterende match).</summary>
    Bokfort,

    /// <summary>Ignorert / markert som ikke relevant.</summary>
    Ignorert
}

/// <summary>
/// Retning paa bankbevegelse.
/// </summary>
public enum BankbevegelseRetning
{
    /// <summary>Innbetaling (CRDT i CAMT.053).</summary>
    Inn,

    /// <summary>Utbetaling (DBIT i CAMT.053).</summary>
    Ut
}

/// <summary>
/// Matchetype brukt for avstemming.
/// </summary>
public enum MatcheType
{
    /// <summary>Automatisk match paa KID-nummer.</summary>
    Kid,

    /// <summary>Automatisk match paa eksakt belop.</summary>
    Belop,

    /// <summary>Automatisk match paa referanse/tekst.</summary>
    Referanse,

    /// <summary>Manuell match utfort av bruker.</summary>
    Manuell,

    /// <summary>Splitt-match (en bevegelse fordelt paa flere poster).</summary>
    Splitt
}

/// <summary>
/// Status for en kontoutskrift-import.
/// </summary>
public enum KontoutskriftStatus
{
    /// <summary>Importert, behandling paagar.</summary>
    Importert,

    /// <summary>Alle bevegelser er matchet/bokfort.</summary>
    Ferdig,

    /// <summary>Delvis behandlet.</summary>
    DelvisBehandlet
}

/// <summary>
/// Status for en bankavstemming (per periode/konto).
/// </summary>
public enum AvstemmingStatus
{
    /// <summary>Paastartet, ikke ferdig.</summary>
    UnderArbeid,

    /// <summary>Avstemt, differanse = 0.</summary>
    Avstemt,

    /// <summary>Avstemt med forklart differanse.</summary>
    AvstemtMedDifferanse
}
