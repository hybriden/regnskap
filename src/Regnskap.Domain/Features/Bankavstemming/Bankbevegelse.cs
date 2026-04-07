namespace Regnskap.Domain.Features.Bankavstemming;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// En enkelt banktransaksjon importert fra CAMT.053.
/// Representerer en Ntry-node i CAMT.053-filen.
/// NBS 7: Alle betalinger maa vaere dokumentert.
/// </summary>
public class Bankbevegelse : AuditableEntity
{
    /// <summary>
    /// FK til kontoutskrift denne bevegelsen tilhorer.
    /// </summary>
    public Guid KontoutskriftId { get; set; }
    public Kontoutskrift Kontoutskrift { get; set; } = default!;

    /// <summary>
    /// FK til bankkonto (denormalisert for enklere sporing).
    /// </summary>
    public Guid BankkontoId { get; set; }
    public Bankkonto Bankkonto { get; set; } = default!;

    /// <summary>
    /// Retning: inn (CRDT) eller ut (DBIT).
    /// </summary>
    public BankbevegelseRetning Retning { get; set; }

    /// <summary>
    /// Belop (alltid positivt, retning angis av Retning).
    /// </summary>
    public Belop Belop { get; set; }

    /// <summary>
    /// Valutakode.
    /// </summary>
    public string Valutakode { get; set; } = "NOK";

    /// <summary>
    /// Bokforingsdato fra banken (Ntry/BookgDt).
    /// </summary>
    public DateOnly Bokforingsdato { get; set; }

    /// <summary>
    /// Valuteringsdato fra banken (Ntry/ValDt).
    /// </summary>
    public DateOnly? Valuteringsdato { get; set; }

    /// <summary>
    /// KID-nummer fra betalingen (Ntry/NtryDtls/TxDtls/RmtInf/Strd/CdtrRefInf/Ref).
    /// Brukes til automatisk matching mot kundefakturaer.
    /// </summary>
    public string? KidNummer { get; set; }

    /// <summary>
    /// Ende-til-ende-referanse (EndToEndId fra CAMT.053).
    /// Kan matche mot utgaaende betalingsreferanser (pain.001).
    /// </summary>
    public string? EndToEndId { get; set; }

    /// <summary>
    /// Betalers/mottakers navn fra banken.
    /// </summary>
    public string? Motpart { get; set; }

    /// <summary>
    /// Betalers/mottakers kontonummer.
    /// </summary>
    public string? MotpartKonto { get; set; }

    /// <summary>
    /// Banktransaksjonskode (BkTxCd).
    /// </summary>
    public string? Transaksjonskode { get; set; }

    /// <summary>
    /// Fritekst fra banken (Ntry/AddtlNtryInf eller NtryDtls/TxDtls/AddtlTxInf).
    /// </summary>
    public string? Beskrivelse { get; set; }

    /// <summary>
    /// Intern meldings-referanse fra CAMT.053 (TxDtls/Refs/MsgId).
    /// </summary>
    public string? BankReferanse { get; set; }

    // --- Matching ---

    /// <summary>
    /// Status for matching/avstemming.
    /// </summary>
    public BankbevegelseStatus Status { get; set; } = BankbevegelseStatus.IkkeMatchet;

    /// <summary>
    /// Type matching som ble brukt.
    /// </summary>
    public MatcheType? MatcheType { get; set; }

    /// <summary>
    /// Konfidens-score for automatisk matching (0.0-1.0).
    /// </summary>
    public decimal? MatcheKonfidens { get; set; }

    /// <summary>
    /// FK til bilag opprettet ved bokforing av denne bevegelsen.
    /// Null hvis bevegelsen matcher et eksisterende bilag.
    /// </summary>
    public Guid? BilagId { get; set; }
    public Bilag? Bilag { get; set; }

    // --- Navigasjon ---

    /// <summary>
    /// Matchinger mot aapne poster/bilag.
    /// </summary>
    public ICollection<BankbevegelseMatch> Matchinger { get; set; } = new List<BankbevegelseMatch>();
}
