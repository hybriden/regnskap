namespace Regnskap.Domain.Features.Kundereskontro;

/// <summary>
/// Betalingsstatus for en kundefaktura.
/// </summary>
public enum KundeFakturaStatus
{
    /// <summary>Faktura er utstedt.</summary>
    Utstedt,

    /// <summary>Betalt i sin helhet.</summary>
    Betalt,

    /// <summary>Delvis betalt.</summary>
    DelvisBetalt,

    /// <summary>Kreditert (kreditnota utstedt).</summary>
    Kreditert,

    /// <summary>Forste purring sendt.</summary>
    Purring1,

    /// <summary>Andre purring sendt.</summary>
    Purring2,

    /// <summary>Tredje purring sendt (siste varsel).</summary>
    Purring3,

    /// <summary>Sendt til inkasso.</summary>
    Inkasso,

    /// <summary>Konstatert tap / avskrevet.</summary>
    Tap
}

/// <summary>
/// Type kundetransaksjon.
/// </summary>
public enum KundeTransaksjonstype
{
    Faktura,
    Kreditnota,
    Innbetaling,
    Purregebyr,
    Tap
}

/// <summary>
/// Betalingsbetingelser for kunder.
/// </summary>
public enum KundeBetalingsbetingelse
{
    Netto10,
    Netto14,
    Netto20,
    Netto30,
    Netto45,
    Netto60,
    Kontant,
    Forskudd,
    Egendefinert
}

/// <summary>
/// KID-algoritme.
/// </summary>
public enum KidAlgoritme
{
    MOD10,
    MOD11
}

/// <summary>
/// Purringsstatus / -type.
/// </summary>
public enum PurringType
{
    /// <summary>Forste purring (betalingspaaminnelse).</summary>
    Purring1,

    /// <summary>Andre purring (med gebyr).</summary>
    Purring2,

    /// <summary>Tredje og siste purring / inkassovarsel.</summary>
    Purring3Inkassovarsel
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
