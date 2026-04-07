namespace Regnskap.Domain.Features.Leverandorreskontro;

using Regnskap.Domain.Common;

/// <summary>
/// En linje i et betalingsforslag. Representerer betaling av en faktura.
/// Mapper til en CdtTrfTxInf i pain.001.
/// </summary>
public class BetalingsforslagLinje : AuditableEntity
{
    public Guid BetalingsforslagId { get; set; }
    public Betalingsforslag Betalingsforslag { get; set; } = default!;

    public Guid LeverandorFakturaId { get; set; }
    public LeverandorFaktura LeverandorFaktura { get; set; } = default!;

    public Guid LeverandorId { get; set; }
    public Leverandor Leverandor { get; set; } = default!;

    public Belop Belop { get; set; }

    public string? MottakerKontonummer { get; set; }
    public string? MottakerIban { get; set; }
    public string? MottakerBic { get; set; }
    public string? KidNummer { get; set; }
    public string? Melding { get; set; }
    public string? EndToEndId { get; set; }

    public bool ErInkludert { get; set; } = true;
    public bool? ErUtfort { get; set; }
    public string? Feilmelding { get; set; }
}
