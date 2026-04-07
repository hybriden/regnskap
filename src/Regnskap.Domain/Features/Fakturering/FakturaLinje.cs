namespace Regnskap.Domain.Features.Fakturering;

using Regnskap.Domain.Common;

/// <summary>
/// En fakturalinje med vare/tjeneste, antall, pris, rabatt og MVA.
/// Bokforingsforskriften 5-1-1 nr. 5-6: beskrivelse, antall, enhetspris.
/// Mapper til EHF BT-126..BT-152.
/// </summary>
public class FakturaLinje : AuditableEntity
{
    public Guid FakturaId { get; set; }
    public Faktura Faktura { get; set; } = default!;

    /// <summary>
    /// Linjenummer (1, 2, 3...). EHF BT-126.
    /// </summary>
    public int Linjenummer { get; set; }

    /// <summary>
    /// Varenavn / tjenestebeskrivelse. EHF BT-131.
    /// </summary>
    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// Antall. EHF BT-129.
    /// </summary>
    public decimal Antall { get; set; } = 1;

    /// <summary>
    /// Enhet. Mapper til UBL unitCode.
    /// </summary>
    public Enhet Enhet { get; set; } = Enhet.Stykk;

    /// <summary>
    /// Enhetspris ekskl. MVA. EHF BT-146.
    /// </summary>
    public Belop Enhetspris { get; set; }

    /// <summary>
    /// Rabattype (prosent eller belop).
    /// </summary>
    public RabattType? RabattType { get; set; }

    /// <summary>
    /// Rabattprosent (0-100). EHF BT-138.
    /// </summary>
    public decimal? RabattProsent { get; set; }

    /// <summary>
    /// Rabattbelop. EHF BT-136.
    /// </summary>
    public Belop? RabattBelop { get; set; }

    /// <summary>
    /// Nettobelop = (Antall * Enhetspris) - Rabatt. EHF BT-130.
    /// </summary>
    public Belop Nettobelop { get; set; }

    // --- MVA ---

    /// <summary>
    /// MVA-kode for denne linjen.
    /// </summary>
    public string MvaKode { get; set; } = default!;

    /// <summary>
    /// MVA-sats (snapshot). Hentes fra MvaKode ved opprettelse.
    /// </summary>
    public decimal MvaSats { get; set; }

    /// <summary>
    /// Beregnet MVA-belop for linjen.
    /// </summary>
    public Belop MvaBelop { get; set; }

    /// <summary>
    /// Bruttobelop inkl. MVA = Nettobelop + MvaBelop.
    /// </summary>
    public Belop Bruttobelop { get; set; }

    // --- Bokforing ---

    public Guid KontoId { get; set; }
    public string Kontonummer { get; set; } = default!;
    public string? Avdelingskode { get; set; }
    public string? Prosjektkode { get; set; }
}
