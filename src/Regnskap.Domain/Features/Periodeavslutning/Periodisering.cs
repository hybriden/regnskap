namespace Regnskap.Domain.Features.Periodeavslutning;

using Regnskap.Domain.Common;

/// <summary>
/// En periodisering (accrual) som fordeler en kostnad/inntekt over flere perioder.
/// Folger sammenstillingsprinsippet (Regnskapsloven 4-1 nr. 3).
/// </summary>
public class Periodisering : AuditableEntity
{
    public string Beskrivelse { get; set; } = default!;
    public PeriodiseringsType Type { get; set; }
    public decimal TotalBelop { get; set; }
    public string Valuta { get; set; } = "NOK";
    public int FraAr { get; set; }
    public int FraPeriode { get; set; }
    public int TilAr { get; set; }
    public int TilPeriode { get; set; }
    public string BalanseKontonummer { get; set; } = default!;
    public string ResultatKontonummer { get; set; } = default!;
    public string? Avdelingskode { get; set; }
    public string? Prosjektkode { get; set; }
    public bool ErAktiv { get; set; } = true;
    public Guid? OpprinneligBilagId { get; set; }

    public List<PeriodiseringsHistorikk> Posteringer { get; set; } = new();

    // --- Avledede egenskaper ---

    public int AntallPerioder =>
        (TilAr - FraAr) * 12 + (TilPeriode - FraPeriode) + 1;

    public decimal BelopPerPeriode =>
        AntallPerioder > 0 ? Math.Round(TotalBelop / AntallPerioder, 2) : 0;

    public decimal SumPeriodisert => Posteringer.Sum(p => p.Belop);

    public decimal GjenstaendeBelop => TotalBelop - SumPeriodisert;
}
