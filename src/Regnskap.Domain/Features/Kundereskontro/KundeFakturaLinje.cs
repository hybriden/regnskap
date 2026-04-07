namespace Regnskap.Domain.Features.Kundereskontro;

using Regnskap.Domain.Common;

/// <summary>
/// Fakturalinje for en kundefaktura.
/// Bokforingsforskriften 5-1-1: beskrivelse av varer/tjenester, antall, enhetspris.
/// </summary>
public class KundeFakturaLinje : AuditableEntity
{
    public Guid KundeFakturaId { get; set; }
    public KundeFaktura KundeFaktura { get; set; } = default!;

    public int Linjenummer { get; set; }
    public Guid KontoId { get; set; }
    public string Kontonummer { get; set; } = default!;
    public string Beskrivelse { get; set; } = default!;
    public decimal Antall { get; set; } = 1;
    public Belop Enhetspris { get; set; }
    public Belop Belop { get; set; }
    public string? MvaKode { get; set; }
    public decimal? MvaSats { get; set; }
    public Belop? MvaBelop { get; set; }
    public decimal Rabatt { get; set; }
    public string? Avdelingskode { get; set; }
    public string? Prosjektkode { get; set; }
}
