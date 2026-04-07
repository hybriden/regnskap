namespace Regnskap.Domain.Features.Hovedbok;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kontoplan;

/// <summary>
/// En postering i hovedboken. Representerer en enkelt debet- eller kredit-linje
/// i et bilag, bokfort mot en spesifikk konto.
///
/// Mapper til SAF-T: GeneralLedgerEntries > Journal > Transaction > Line
/// </summary>
public class Postering : AuditableEntity
{
    /// <summary>
    /// FK til bilaget denne posteringen tilhorer.
    /// </summary>
    public Guid BilagId { get; set; }
    public Bilag Bilag { get; set; } = default!;

    /// <summary>
    /// Linjenummer innenfor bilaget (1, 2, 3...).
    /// Mapper til SAF-T Line/RecordID.
    /// </summary>
    public int Linjenummer { get; set; }

    /// <summary>
    /// FK til kontoen det posteres mot.
    /// Mapper til SAF-T Line/AccountID.
    /// </summary>
    public Guid KontoId { get; set; }
    public Konto Konto { get; set; } = default!;

    /// <summary>
    /// Kontonummer denormalisert for ytelse og sporbarhet.
    /// </summary>
    public string Kontonummer { get; set; } = default!;

    /// <summary>
    /// Debet eller kredit.
    /// </summary>
    public BokforingSide Side { get; set; }

    /// <summary>
    /// Belopet som posteres (alltid positivt).
    /// Mapper til SAF-T DebitAmount/Amount eller CreditAmount/Amount.
    /// </summary>
    public Belop Belop { get; set; }

    /// <summary>
    /// Beskrivelse av posteringen.
    /// Mapper til SAF-T Line/Description.
    /// </summary>
    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// MVA-kode brukt for denne posteringen. Null hvis MVA-fri.
    /// </summary>
    public string? MvaKode { get; set; }

    /// <summary>
    /// MVA-belop beregnet for denne posteringen.
    /// </summary>
    public Belop? MvaBelop { get; set; }

    /// <summary>
    /// MVA-grunnlag (basbelop for MVA-beregning).
    /// </summary>
    public Belop? MvaGrunnlag { get; set; }

    /// <summary>
    /// MVA-sats brukt (snapshot ved bokforingstidspunkt).
    /// </summary>
    public decimal? MvaSats { get; set; }

    /// <summary>
    /// Avdelingskode/kostnadssted. Pakreves for kontoer med KreverAvdeling = true.
    /// </summary>
    public string? Avdelingskode { get; set; }

    /// <summary>
    /// Prosjektkode. Pakreves for kontoer med KreverProsjekt = true.
    /// </summary>
    public string? Prosjektkode { get; set; }

    /// <summary>
    /// Kunde-ID for kunde-relaterte posteringer.
    /// Mapper til SAF-T Line/CustomerID.
    /// </summary>
    public Guid? KundeId { get; set; }

    /// <summary>
    /// Leverandor-ID for leverandor-relaterte posteringer.
    /// Mapper til SAF-T Line/SupplierID.
    /// </summary>
    public Guid? LeverandorId { get; set; }

    /// <summary>
    /// Bilagsdato kopieres hit for enklere sporing og indeksering.
    /// </summary>
    public DateOnly Bilagsdato { get; set; }

    /// <summary>
    /// Om denne posteringen er automatisk generert for MVA.
    /// </summary>
    public bool ErAutoGenerertMva { get; set; }

    // --- Avledede egenskaper ---

    /// <summary>
    /// Fortegnsberegnet belop: positivt for debet, negativt for kredit.
    /// Nyttig for saldoberegninger.
    /// </summary>
    public Belop FortegnsBelop => Side == BokforingSide.Debet ? Belop : -Belop;
}
