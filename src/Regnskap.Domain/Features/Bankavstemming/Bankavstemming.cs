namespace Regnskap.Domain.Features.Bankavstemming;

using Regnskap.Domain.Common;

/// <summary>
/// Avstemming av en bankkonto for en gitt periode.
/// NBS 5: Bankkontoer maa dokumenteres/avstemmes ved aarsavslutning.
/// </summary>
public class Bankavstemming : AuditableEntity
{
    /// <summary>
    /// FK til bankkonto.
    /// </summary>
    public Guid BankkontoId { get; set; }
    public Bankkonto Bankkonto { get; set; } = default!;

    /// <summary>
    /// Avstemmingsdato (typisk periodeslutt eller maanedsslutt).
    /// </summary>
    public DateOnly Avstemmingsdato { get; set; }

    /// <summary>
    /// Regnskapsaar.
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Periodenummer (1-12, eller 0 for helaar).
    /// </summary>
    public int Periode { get; set; }

    // --- Saldoer ---

    /// <summary>
    /// Saldo i hovedbok (fra KontoSaldo) per avstemmingsdato.
    /// </summary>
    public Belop SaldoHovedbok { get; set; }

    /// <summary>
    /// Saldo ihht kontoutskrift fra banken.
    /// </summary>
    public Belop SaldoBank { get; set; }

    /// <summary>
    /// Differanse = SaldoBank - SaldoHovedbok.
    /// Maa vaere 0 for godkjent avstemming (eller forklart).
    /// </summary>
    public Belop Differanse { get; set; }

    // --- Tidsavgrensninger (forklarer differanse) ---

    /// <summary>
    /// Sum utestaaende sjekker / betalinger ikke registrert i bank.
    /// </summary>
    public Belop UtestaaendeBetalinger { get; set; } = Belop.Null;

    /// <summary>
    /// Sum innbetalinger i transitt (registrert i bank, ikke i regnskap).
    /// </summary>
    public Belop InnbetalingerITransitt { get; set; } = Belop.Null;

    /// <summary>
    /// Andre forklarte differanser.
    /// </summary>
    public Belop AndreDifferanser { get; set; } = Belop.Null;

    /// <summary>
    /// Forklaring til eventuell gjenstaende differanse.
    /// </summary>
    public string? DifferanseForklaring { get; set; }

    /// <summary>
    /// Status for avstemmingen.
    /// </summary>
    public AvstemmingStatus Status { get; set; } = AvstemmingStatus.UnderArbeid;

    /// <summary>
    /// Hvem som utforte/godkjente avstemmingen.
    /// </summary>
    public string? GodkjentAv { get; set; }

    /// <summary>
    /// Tidspunkt for godkjenning.
    /// </summary>
    public DateTime? GodkjentTidspunkt { get; set; }

    // --- Avstemt differanse-sjekk ---

    /// <summary>
    /// Beregnet forklart differanse.
    /// </summary>
    public Belop ForklartDifferanse =>
        UtestaaendeBetalinger + InnbetalingerITransitt + AndreDifferanser;

    /// <summary>
    /// Uforklart differanse = Differanse - ForklartDifferanse.
    /// Maa vaere 0 for status Avstemt.
    /// </summary>
    public Belop UforklartDifferanse => Differanse - ForklartDifferanse;
}
