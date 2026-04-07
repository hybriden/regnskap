namespace Regnskap.Domain.Features.Hovedbok;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Bilagsregistrering;

/// <summary>
/// Et bilag (voucher) -- den grunnleggende bokforingsenheten.
/// Alle posteringer i hovedboken tilhorer et bilag.
/// Et bilag MA vaere i balanse: sum debet = sum kredit.
///
/// Mapper til SAF-T: GeneralLedgerEntries > Journal > Transaction
/// </summary>
public class Bilag : AuditableEntity
{
    /// <summary>
    /// Fortlopende bilagsnummer innenfor regnskapsaret.
    /// Bokforingsloven krever fortlopende nummerering uten hull.
    /// </summary>
    public int Bilagsnummer { get; set; }

    /// <summary>
    /// Regnskapsaret bilaget tilhorer.
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Bilagstype. Mapper til SAF-T Journal/Type.
    /// </summary>
    public BilagType Type { get; set; }

    /// <summary>
    /// Bilagsdato -- den faktiske transaksjonsdatoen.
    /// Mapper til SAF-T TransactionDate.
    /// </summary>
    public DateOnly Bilagsdato { get; set; }

    /// <summary>
    /// Dato bilaget ble registrert i systemet.
    /// Mapper til SAF-T SystemEntryDate.
    /// </summary>
    public DateTime Registreringsdato { get; set; }

    /// <summary>
    /// Beskrivelse av bilaget.
    /// Mapper til SAF-T Transaction/Description.
    /// </summary>
    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// FK til regnskapsperioden bilaget er bokfort i.
    /// </summary>
    public Guid RegnskapsperiodeId { get; set; }
    public Regnskapsperiode Regnskapsperiode { get; set; } = default!;

    /// <summary>
    /// Ekstern referanse (fakturanummer, betalingsreferanse, etc.).
    /// </summary>
    public string? EksternReferanse { get; set; }

    /// <summary>
    /// Alle posteringer (linjer) i bilaget.
    /// </summary>
    public ICollection<Postering> Posteringer { get; set; } = new List<Postering>();

    // --- Bilagserie-felter ---

    /// <summary>
    /// FK til bilagserien dette bilaget tilhorer.
    /// </summary>
    public Guid? BilagSerieId { get; set; }
    public BilagSerie? BilagSerie { get; set; }

    /// <summary>
    /// Seriekode denormalisert (f.eks. "MAN").
    /// </summary>
    public string? SerieKode { get; set; }

    /// <summary>
    /// Serienummer innenfor bilagserien for dette aret.
    /// </summary>
    public int? SerieNummer { get; set; }

    /// <summary>
    /// Referanse til opprinnelig bilag ved tilbakeforing.
    /// </summary>
    public Guid? TilbakefortFraBilagId { get; set; }
    public Bilag? TilbakefortFraBilag { get; set; }

    /// <summary>
    /// Referanse til tilbakeforingsbilag (hvis dette bilaget er tilbakfort).
    /// </summary>
    public Guid? TilbakefortAvBilagId { get; set; }
    public Bilag? TilbakefortAvBilag { get; set; }

    /// <summary>
    /// Om bilaget er tilbakfort (reversert).
    /// </summary>
    public bool ErTilbakfort { get; set; }

    /// <summary>
    /// Om bilaget er bokfort mot hovedbok (KontoSaldo oppdatert).
    /// </summary>
    public bool ErBokfort { get; set; }

    /// <summary>
    /// Tidspunkt bilaget ble bokfort.
    /// </summary>
    public DateTime? BokfortTidspunkt { get; set; }

    /// <summary>
    /// Hvem som bokforte bilaget.
    /// </summary>
    public string? BokfortAv { get; set; }

    /// <summary>
    /// Vedlegg knyttet til bilaget.
    /// </summary>
    public ICollection<Vedlegg> Vedlegg { get; set; } = new List<Vedlegg>();

    // --- Avledede egenskaper ---

    /// <summary>
    /// Unik bilagsreferanse (f.eks. "2026-00042").
    /// Mapper til SAF-T TransactionID.
    /// </summary>
    public string BilagsId => $"{Ar}-{Bilagsnummer:D5}";

    /// <summary>
    /// Full bilagsreferanse med serie (f.eks. "MAN-2026-00042").
    /// </summary>
    public string? SerieBilagsId => SerieKode != null && SerieNummer.HasValue
        ? $"{SerieKode}-{Ar}-{SerieNummer.Value:D5}"
        : null;

    /// <summary>
    /// SAF-T-periode (1-12).
    /// </summary>
    public int SaftPeriode => Bilagsdato.Month;

    // --- Forretningslogikk ---

    /// <summary>
    /// Beregner sum debet for alle posteringer.
    /// </summary>
    public Belop SumDebet() => Posteringer
        .Where(p => p.Side == BokforingSide.Debet)
        .Aggregate(Belop.Null, (sum, p) => sum + p.Belop);

    /// <summary>
    /// Beregner sum kredit for alle posteringer.
    /// </summary>
    public Belop SumKredit() => Posteringer
        .Where(p => p.Side == BokforingSide.Kredit)
        .Aggregate(Belop.Null, (sum, p) => sum + p.Belop);

    /// <summary>
    /// Validerer at bilaget er i balanse og har minimum 2 linjer.
    /// Kaster AccountingBalanceException hvis ikke i balanse.
    /// </summary>
    public void ValiderBalanse()
    {
        if (Posteringer.Count < 2)
            throw new BilagValideringException(
                BilagsId, "Et bilag ma ha minimum 2 posteringer.");

        var debet = SumDebet();
        var kredit = SumKredit();

        if (debet.Verdi != kredit.Verdi)
            throw new AccountingBalanceException(debet.Verdi, kredit.Verdi);
    }
}
