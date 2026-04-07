namespace Regnskap.Domain.Features.Kontoplan;

/// <summary>
/// NS 4102 kontoklasser (1-8).
/// </summary>
public enum Kontoklasse
{
    Eiendeler = 1,
    EgenkapitalOgGjeld = 2,
    Salgsinntekt = 3,
    Varekostnad = 4,
    Lonnskostnad = 5,
    AvskrivningOgAnnenDriftskostnad = 6,
    AnnenDriftskostnad = 7,
    FinansposterSkatt = 8
}

/// <summary>
/// Kontotype bestemmer debet/kredit-oppforsel og hvilken rapport kontoen tilhorer.
/// </summary>
public enum Kontotype
{
    Eiendel,
    Gjeld,
    Egenkapital,
    Inntekt,
    Kostnad
}

/// <summary>
/// Normalbalanse for kontoen. Brukes til a bestemme fortegn i rapporter.
/// </summary>
public enum Normalbalanse
{
    Debet,
    Kredit
}

/// <summary>
/// SAF-T GroupingCategory for norsk skatterapportering.
/// </summary>
public enum GrupperingsKategori
{
    RF1167,
    RF1175,
    RF1323
}

/// <summary>
/// MVA-retning for MVA-koder.
/// </summary>
public enum MvaRetning
{
    Ingen,
    Inngaende,
    Utgaende,
    SnuddAvregning
}
