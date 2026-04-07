namespace Regnskap.Domain.Features.Mva;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kontoplan;

/// <summary>
/// En linje i MVA-oppgjoret, aggregert per MVA-kode.
/// Representerer en post i MVA-meldingen (RF-0002).
/// </summary>
public class MvaOppgjorLinje : AuditableEntity
{
    /// <summary>
    /// FK til oppgjoret.
    /// </summary>
    public Guid MvaOppgjorId { get; set; }
    public MvaOppgjor MvaOppgjor { get; set; } = default!;

    /// <summary>
    /// MVA-kode (intern kode, f.eks. "1", "3", "81").
    /// </summary>
    public string MvaKode { get; set; } = default!;

    /// <summary>
    /// SAF-T StandardTaxCode (f.eks. "1", "3", "81").
    /// Denormalisert fra MvaKode-entiteten for sporbarhet.
    /// </summary>
    public string StandardTaxCode { get; set; } = default!;

    /// <summary>
    /// MVA-sats (snapshot).
    /// </summary>
    public decimal Sats { get; set; }

    /// <summary>
    /// Retning: Inngaende, Utgaende, eller SnuddAvregning.
    /// </summary>
    public MvaRetning Retning { get; set; }

    /// <summary>
    /// RF-0002 postnummer (1-12) som denne linjen tilhorer.
    /// </summary>
    public int RfPostnummer { get; set; }

    /// <summary>
    /// Sum grunnlag (basis for MVA-beregning) for denne koden i terminen.
    /// </summary>
    public decimal SumGrunnlag { get; set; }

    /// <summary>
    /// Sum MVA-belop for denne koden i terminen.
    /// </summary>
    public decimal SumMvaBelop { get; set; }

    /// <summary>
    /// Antall posteringer som inngaar i denne linjen.
    /// </summary>
    public int AntallPosteringer { get; set; }
}
