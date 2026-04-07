namespace Regnskap.Domain.Features.Kundereskontro;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Utgaaende faktura til kunde.
/// Representerer en apne post i kundereskontro.
/// </summary>
public class KundeFaktura : AuditableEntity
{
    public Guid KundeId { get; set; }
    public Kunde Kunde { get; set; } = default!;

    /// <summary>
    /// Fakturanummer (fortlopende, ihht bokforingsforskriften 5-1-1).
    /// </summary>
    public int Fakturanummer { get; set; }

    public KundeTransaksjonstype Type { get; set; } = KundeTransaksjonstype.Faktura;
    public DateOnly Fakturadato { get; set; }
    public DateOnly Forfallsdato { get; set; }
    public DateOnly? Leveringsdato { get; set; }
    public string Beskrivelse { get; set; } = default!;

    public Belop BelopEksMva { get; set; }
    public Belop MvaBelop { get; set; }
    public Belop BelopInklMva { get; set; }
    public Belop GjenstaendeBelop { get; set; }

    public KundeFakturaStatus Status { get; set; } = KundeFakturaStatus.Utstedt;
    public string? KidNummer { get; set; }
    public string Valutakode { get; set; } = "NOK";
    public decimal? Valutakurs { get; set; }

    public Guid? BilagId { get; set; }
    public Bilag? Bilag { get; set; }

    public Guid? KreditnotaForFakturaId { get; set; }
    public KundeFaktura? KreditnotaForFaktura { get; set; }

    public string? EksternReferanse { get; set; }
    public string? Bestillingsnummer { get; set; }

    public int AntallPurringer { get; set; }
    public DateOnly? SistePurringDato { get; set; }
    public Belop PurregebyrTotalt { get; set; } = Belop.Null;

    // --- Navigasjon ---

    public ICollection<KundeFakturaLinje> Linjer { get; set; } = new List<KundeFakturaLinje>();
    public ICollection<KundeInnbetaling> Innbetalinger { get; set; } = new List<KundeInnbetaling>();
    public ICollection<Purring> Purringer { get; set; } = new List<Purring>();

    // --- Avledede egenskaper ---

    public bool ErForfalt(DateOnly iDag) => Forfallsdato < iDag && GjenstaendeBelop.Verdi > 0;

    public int DagerForfalt(DateOnly iDag) =>
        ErForfalt(iDag) ? iDag.DayNumber - Forfallsdato.DayNumber : 0;

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
