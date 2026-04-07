namespace Regnskap.Domain.Features.Leverandorreskontro;

/// <summary>
/// Betalingsstatus for en leverandorfaktura.
/// </summary>
public enum FakturaStatus
{
    /// <summary>Registrert men ikke godkjent for betaling.</summary>
    Registrert,

    /// <summary>Godkjent for betaling (attestert).</summary>
    Godkjent,

    /// <summary>Inkludert i et betalingsforslag.</summary>
    IBetalingsforslag,

    /// <summary>Betalingsfil er generert og sendt til bank.</summary>
    SendtTilBank,

    /// <summary>Betalt i sin helhet.</summary>
    Betalt,

    /// <summary>Delvis betalt.</summary>
    DelvisBetalt,

    /// <summary>Kreditert (kreditnota mottatt).</summary>
    Kreditert,

    /// <summary>Omstridt/sperret for betaling.</summary>
    Sperret
}

/// <summary>
/// Status for et betalingsforslag.
/// </summary>
public enum BetalingsforslagStatus
{
    /// <summary>Forslaget er opprettet, kan redigeres.</summary>
    Utkast,

    /// <summary>Godkjent, klar for filoppretting.</summary>
    Godkjent,

    /// <summary>pain.001 fil generert.</summary>
    FilGenerert,

    /// <summary>Sendt til bank.</summary>
    SendtTilBank,

    /// <summary>Bekreftet utfort av bank.</summary>
    Utfort,

    /// <summary>Avvist av bank (helt eller delvis).</summary>
    Avvist,

    /// <summary>Kansellert for sending.</summary>
    Kansellert
}

/// <summary>
/// Type leverandortransaksjon.
/// </summary>
public enum LeverandorTransaksjonstype
{
    Faktura,
    Kreditnota,
    Betaling,
    Forskudd
}

/// <summary>
/// Betalingsbetingelser.
/// </summary>
public enum Betalingsbetingelse
{
    Netto10,
    Netto14,
    Netto20,
    Netto30,
    Netto45,
    Netto60,
    Netto90,
    Kontant,
    Egendefinert
}

/// <summary>
/// Alderskategorier for aldersfordeling av apne poster.
/// </summary>
public enum Alderskategori
{
    IkkeForfalt,
    Dager0Til30,
    Dager31Til60,
    Dager61Til90,
    Over90Dager
}
