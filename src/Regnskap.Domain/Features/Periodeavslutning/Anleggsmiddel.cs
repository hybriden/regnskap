namespace Regnskap.Domain.Features.Periodeavslutning;

using Regnskap.Domain.Common;

/// <summary>
/// Et anleggsmiddel som avskrives over tid (lineaer metode).
/// Regnskapsloven 5-3: anleggsmidler med begrenset levetid skal avskrives
/// etter en fornuftig avskrivningsplan.
/// </summary>
public class Anleggsmiddel : AuditableEntity
{
    public string Navn { get; set; } = default!;
    public string? Beskrivelse { get; set; }
    public DateOnly Anskaffelsesdato { get; set; }
    public decimal Anskaffelseskostnad { get; set; }
    public decimal Restverdi { get; set; }
    public int LevetidManeder { get; set; }
    public string BalanseKontonummer { get; set; } = default!;
    public string AvskrivningsKontonummer { get; set; } = default!;
    public string AkkumulertAvskrivningKontonummer { get; set; } = default!;
    public string? Avdelingskode { get; set; }
    public string? Prosjektkode { get; set; }
    public bool ErAktivt { get; set; } = true;
    public DateOnly? UtrangeringsDato { get; set; }

    public List<AvskrivningHistorikk> Avskrivninger { get; set; } = new();

    // --- Avledede egenskaper ---

    public decimal Avskrivningsgrunnlag => Anskaffelseskostnad - Restverdi;

    public decimal ManedligAvskrivning =>
        LevetidManeder > 0 ? Math.Round(Avskrivningsgrunnlag / LevetidManeder, 2) : 0;

    public decimal ArligAvskrivning => ManedligAvskrivning * 12;

    public decimal AkkumulertAvskrivning => Avskrivninger.Sum(a => a.Belop);

    public decimal BokfortVerdi => Anskaffelseskostnad - AkkumulertAvskrivning;

    public decimal GjenvaerendeAvskrivning =>
        Math.Max(0, Avskrivningsgrunnlag - AkkumulertAvskrivning);

    public bool ErFulltAvskrevet => GjenvaerendeAvskrivning <= 0;
}
