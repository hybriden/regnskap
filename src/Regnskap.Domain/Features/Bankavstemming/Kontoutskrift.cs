namespace Regnskap.Domain.Features.Bankavstemming;

using Regnskap.Domain.Common;

/// <summary>
/// En importert kontoutskrift fra banken (CAMT.053).
/// Representerer en Stmt-node i CAMT.053-filen.
/// Bokforingsloven 13: kontoutskrifter skal oppbevares i 5 aar.
/// </summary>
public class Kontoutskrift : AuditableEntity
{
    /// <summary>
    /// FK til bankkonto.
    /// </summary>
    public Guid BankkontoId { get; set; }
    public Bankkonto Bankkonto { get; set; } = default!;

    /// <summary>
    /// Meldings-ID fra CAMT.053 (GrpHdr/MsgId).
    /// Brukes til duplikat-sjekk ved reimport.
    /// </summary>
    public string MeldingsId { get; set; } = default!;

    /// <summary>
    /// Utskrift-ID fra CAMT.053 (Stmt/Id).
    /// </summary>
    public string UtskriftId { get; set; } = default!;

    /// <summary>
    /// Sekvensnummer (Stmt/ElctrncSeqNb).
    /// </summary>
    public string? Sekvensnummer { get; set; }

    /// <summary>
    /// Dato kontoutskriften ble opprettet av banken.
    /// </summary>
    public DateTime OpprettetAvBank { get; set; }

    /// <summary>
    /// Periode-start for kontoutskriften.
    /// </summary>
    public DateOnly PeriodeFra { get; set; }

    /// <summary>
    /// Periode-slutt for kontoutskriften.
    /// </summary>
    public DateOnly PeriodeTil { get; set; }

    /// <summary>
    /// Inngaaende saldo fra banken (OPBD).
    /// </summary>
    public Belop InngaendeSaldo { get; set; }

    /// <summary>
    /// Utgaaende saldo fra banken (CLBD).
    /// </summary>
    public Belop UtgaendeSaldo { get; set; }

    /// <summary>
    /// Antall bevegelser i utskriften.
    /// </summary>
    public int AntallBevegelser { get; set; }

    /// <summary>
    /// Sum innbetalinger.
    /// </summary>
    public Belop SumInn { get; set; } = Belop.Null;

    /// <summary>
    /// Sum utbetalinger.
    /// </summary>
    public Belop SumUt { get; set; } = Belop.Null;

    /// <summary>
    /// Status for behandling.
    /// </summary>
    public KontoutskriftStatus Status { get; set; } = KontoutskriftStatus.Importert;

    /// <summary>
    /// Filsti til original CAMT.053-fil (oppbevares ihht bokforingsloven).
    /// </summary>
    public string? OriginalFilsti { get; set; }

    /// <summary>
    /// SHA-256 hash av original fil (for integritetskontroll).
    /// </summary>
    public string? FilHash { get; set; }

    // --- Navigasjon ---

    public ICollection<Bankbevegelse> Bevegelser { get; set; } = new List<Bankbevegelse>();
}
