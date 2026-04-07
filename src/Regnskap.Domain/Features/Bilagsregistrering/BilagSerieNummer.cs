namespace Regnskap.Domain.Features.Bilagsregistrering;

using Regnskap.Domain.Common;

/// <summary>
/// Holder styr pa neste bilagsnummer per serie per ar.
/// Brukes for a sikre fortlopende nummerering uten hull (Bokforingsloven 5).
/// Concurrency-safe via optimistic concurrency (RowVersion).
/// </summary>
public class BilagSerieNummer : AuditableEntity
{
    /// <summary>
    /// FK til bilagserien.
    /// </summary>
    public Guid BilagSerieId { get; set; }
    public BilagSerie BilagSerie { get; set; } = default!;

    /// <summary>
    /// Seriekode denormalisert for ytelse.
    /// </summary>
    public string SerieKode { get; set; } = default!;

    /// <summary>
    /// Regnskapsaret.
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Neste tilgjengelige bilagsnummer i denne serien for dette aret.
    /// Starter pa 1 for hvert nytt ar.
    /// </summary>
    public int NesteNummer { get; set; } = 1;

    /// <summary>
    /// Concurrency token for a hindre doble numre ved samtidige transaksjoner.
    /// </summary>
    public byte[] RowVersion { get; set; } = default!;

    // --- Forretningslogikk ---

    /// <summary>
    /// Tildel neste nummer og inkrementer.
    /// </summary>
    public int TildelNummer()
    {
        var nummer = NesteNummer;
        NesteNummer++;
        return nummer;
    }
}
