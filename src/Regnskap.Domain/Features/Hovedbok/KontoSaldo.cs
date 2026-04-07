namespace Regnskap.Domain.Features.Hovedbok;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kontoplan;

/// <summary>
/// Saldo for en konto i en spesifikk regnskapsperiode.
/// Inneholder inngaende balanse, sum debet/kredit i perioden, og utgaende balanse.
///
/// Oppdateres inkrementelt ved hver bokforing -- ALDRI beregnet pa nytt fra posteringer
/// med mindre det er en eksplisitt reberegning/avstemming.
/// </summary>
public class KontoSaldo : AuditableEntity
{
    /// <summary>
    /// FK til kontoen.
    /// </summary>
    public Guid KontoId { get; set; }
    public Konto Konto { get; set; } = default!;

    /// <summary>
    /// Kontonummer denormalisert for ytelse.
    /// </summary>
    public string Kontonummer { get; set; } = default!;

    /// <summary>
    /// FK til regnskapsperioden.
    /// </summary>
    public Guid RegnskapsperiodeId { get; set; }
    public Regnskapsperiode Regnskapsperiode { get; set; } = default!;

    /// <summary>
    /// Regnskapsaret.
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Periodenummer (0-13).
    /// </summary>
    public int Periode { get; set; }

    /// <summary>
    /// Inngaende balanse (IB) ved periodens start.
    /// For periode 1: kommer fra forrige ars utgaende balanse (balansekontoer)
    /// eller er 0 (resultatkontoer, som nullstilles ved arsavslutning).
    /// </summary>
    public Belop InngaendeBalanse { get; set; } = Belop.Null;

    /// <summary>
    /// Sum av alle debetposteringer i perioden.
    /// </summary>
    public Belop SumDebet { get; set; } = Belop.Null;

    /// <summary>
    /// Sum av alle kreditposteringer i perioden.
    /// </summary>
    public Belop SumKredit { get; set; } = Belop.Null;

    /// <summary>
    /// Antall posteringer i perioden.
    /// </summary>
    public int AntallPosteringer { get; set; }

    // --- Avledede egenskaper ---

    /// <summary>
    /// Endring i perioden = SumDebet - SumKredit.
    /// Positivt = netto debet, negativt = netto kredit.
    /// </summary>
    public Belop Endring => SumDebet - SumKredit;

    /// <summary>
    /// Utgaende balanse (UB) = IB + SumDebet - SumKredit.
    /// </summary>
    public Belop UtgaendeBalanse => InngaendeBalanse + SumDebet - SumKredit;

    // --- Forretningslogikk ---

    /// <summary>
    /// Oppdater saldoen med en ny postering.
    /// </summary>
    public void LeggTilPostering(BokforingSide side, Belop belop)
    {
        if (side == BokforingSide.Debet)
            SumDebet += belop;
        else
            SumKredit += belop;

        AntallPosteringer++;
    }
}
