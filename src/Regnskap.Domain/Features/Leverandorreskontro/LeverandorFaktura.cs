namespace Regnskap.Domain.Features.Leverandorreskontro;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Inngaende faktura fra leverandor.
/// Representerer en apne post i leverandorreskontro.
/// Hver faktura genererer et Bilag i hovedboken med posteringer:
///   Debet: kostnadskonto(er) + inngaende MVA
///   Kredit: 2400 Leverandorgjeld
/// </summary>
public class LeverandorFaktura : AuditableEntity
{
    public Guid LeverandorId { get; set; }
    public Leverandor Leverandor { get; set; } = default!;

    /// <summary>
    /// Leverandorens fakturanummer (ekstern referanse).
    /// </summary>
    public string EksternFakturanummer { get; set; } = default!;

    /// <summary>
    /// Internt fakturanummer (fortlopende i systemet).
    /// </summary>
    public int InternNummer { get; set; }

    public LeverandorTransaksjonstype Type { get; set; } = LeverandorTransaksjonstype.Faktura;

    public DateOnly Fakturadato { get; set; }
    public DateOnly Mottaksdato { get; set; }
    public DateOnly Forfallsdato { get; set; }

    public string Beskrivelse { get; set; } = default!;

    public Belop BelopEksMva { get; set; }
    public Belop MvaBelop { get; set; }
    public Belop BelopInklMva { get; set; }
    public Belop GjenstaendeBelop { get; set; }

    public FakturaStatus Status { get; set; } = FakturaStatus.Registrert;

    public string? KidNummer { get; set; }
    public string Valutakode { get; set; } = "NOK";
    public decimal? Valutakurs { get; set; }

    public Guid? BilagId { get; set; }
    public Bilag? Bilag { get; set; }

    public Guid? KreditnotaForFakturaId { get; set; }
    public LeverandorFaktura? KreditnotaForFaktura { get; set; }

    public bool ErSperret { get; set; }
    public string? SperreArsak { get; set; }

    // --- Navigasjon ---
    public ICollection<LeverandorFakturaLinje> Linjer { get; set; } = new List<LeverandorFakturaLinje>();
    public ICollection<LeverandorBetaling> Betalinger { get; set; } = new List<LeverandorBetaling>();

    // --- Avledede egenskaper ---

    /// <summary>
    /// Om fakturaen er forfalt.
    /// </summary>
    public bool ErForfalt(DateOnly iDag) => Forfallsdato < iDag && GjenstaendeBelop.Verdi > 0;

    /// <summary>
    /// Antall dager forfalt.
    /// </summary>
    public int DagerForfalt(DateOnly iDag) =>
        ErForfalt(iDag) ? iDag.DayNumber - Forfallsdato.DayNumber : 0;

    /// <summary>
    /// Alderskategori for aldersfordeling.
    /// </summary>
    public Alderskategori HentAlderskategori(DateOnly iDag)
    {
        var dager = DagerForfalt(iDag);
        return dager switch
        {
            0 => Alderskategori.IkkeForfalt,
            <= 30 => Alderskategori.Dager0Til30,
            <= 60 => Alderskategori.Dager31Til60,
            <= 90 => Alderskategori.Dager61Til90,
            _ => Alderskategori.Over90Dager
        };
    }
}
