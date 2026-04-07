namespace Regnskap.Domain.Features.Rapportering;

public enum RapportType
{
    Resultatregnskap,
    Balanse,
    Kontantstromoppstilling,
    Saldobalanse,
    Hovedboksutskrift,
    SaftEksport,
    Dimensjonsrapport,
    Sammenligning,
    Nokkeltall
}

/// <summary>
/// Format for resultatregnskapet ihht Regnskapsloven 3-2.
/// </summary>
public enum ResultatregnskapFormat
{
    /// <summary>Artsinndelt (standard for sma foretak).</summary>
    Artsinndelt,

    /// <summary>Funksjonsinndelt (valgfritt for store foretak).</summary>
    Funksjonsinndelt
}
