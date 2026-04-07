namespace Regnskap.Domain.Features.Leverandorreskontro;

using Regnskap.Domain.Common;

/// <summary>
/// Betalingsforslag for leverandorfakturaer.
/// Genereres basert pa forfallsdato og brukervalg.
/// Kan resultere i en pain.001 betalingsfil.
/// </summary>
public class Betalingsforslag : AuditableEntity
{
    public int Forslagsnummer { get; set; }
    public string Beskrivelse { get; set; } = default!;
    public DateOnly Opprettdato { get; set; }
    public DateOnly Betalingsdato { get; set; }
    public DateOnly ForfallTilOgMed { get; set; }

    public BetalingsforslagStatus Status { get; set; } = BetalingsforslagStatus.Utkast;

    public Belop TotalBelop { get; set; }
    public int AntallBetalinger { get; set; }

    public Guid? FraBankkontoId { get; set; }
    public string? FraKontonummer { get; set; }
    public string? FraBic { get; set; }

    public string? BetalingsfilReferanse { get; set; }
    public DateTime? FilGenererTidspunkt { get; set; }
    public DateTime? SendtTilBankTidspunkt { get; set; }

    public string? GodkjentAv { get; set; }
    public DateTime? GodkjentTidspunkt { get; set; }

    // --- Navigasjon ---
    public ICollection<BetalingsforslagLinje> Linjer { get; set; } = new List<BetalingsforslagLinje>();

    /// <summary>
    /// Oppdater totalbelop og antall basert pa inkluderte linjer.
    /// </summary>
    public void OppdaterTotaler()
    {
        var inkluderte = Linjer.Where(l => l.ErInkludert).ToList();
        TotalBelop = inkluderte.Aggregate(Belop.Null, (sum, l) => sum + l.Belop);
        AntallBetalinger = inkluderte.Count;
    }
}
