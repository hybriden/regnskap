# Spesifikasjon: Bankavstemming (Bank Reconciliation)

**Modul:** Bankavstemming (Modul 8)
**Status:** Komplett spesifikasjon
**Avhengigheter:** Kontoplan, Hovedbok, Kundereskontro, Leverandorreskontro, Fakturering
**SAF-T-seksjon:** MasterFiles > GeneralLedgerAccounts (1920-serien), GeneralLedgerEntries
**Bokforingsloven:** 4 nr. 7 (sporbarhet), 5 (spesifikasjoner), 11 (oppbevaring)
**NBS 5:** Dokumentasjon av balansen -- bankkontoer skal avstemmes mot kontoutskrifter
**NBS 7:** Dokumentasjon av betalingstransaksjoner

---

## Datamodell

### Enums

```csharp
namespace Regnskap.Domain.Features.Bankavstemming;

/// <summary>
/// Status for en bankbevegelse (importert transaksjon).
/// </summary>
public enum BankbevegelseStatus
{
    /// <summary>Importert, ikke matchet.</summary>
    IkkeMatchet,

    /// <summary>Automatisk matchet mot aapen post.</summary>
    AutoMatchet,

    /// <summary>Manuelt matchet av bruker.</summary>
    ManueltMatchet,

    /// <summary>Manuelt splittet og matchet.</summary>
    Splittet,

    /// <summary>Bokfort som ny transaksjon (ingen eksisterende match).</summary>
    Bokfort,

    /// <summary>Ignorert / markert som ikke relevant.</summary>
    Ignorert
}

/// <summary>
/// Retning paa bankbevegelse.
/// </summary>
public enum BankbevegelseRetning
{
    /// <summary>Innbetaling (CRDT i CAMT.053).</summary>
    Inn,

    /// <summary>Utbetaling (DBIT i CAMT.053).</summary>
    Ut
}

/// <summary>
/// Matchetype brukt for avstemming.
/// </summary>
public enum MatcheType
{
    /// <summary>Automatisk match paa KID-nummer.</summary>
    Kid,

    /// <summary>Automatisk match paa eksakt belop.</summary>
    Belop,

    /// <summary>Automatisk match paa referanse/tekst.</summary>
    Referanse,

    /// <summary>Manuell match utfort av bruker.</summary>
    Manuell,

    /// <summary>Splitt-match (en bevegelse fordelt paa flere poster).</summary>
    Splitt
}

/// <summary>
/// Status for en kontoutskrift-import.
/// </summary>
public enum KontoutskriftStatus
{
    /// <summary>Importert, behandling paagar.</summary>
    Importert,

    /// <summary>Alle bevegelser er matchet/bokfort.</summary>
    Ferdig,

    /// <summary>Delvis behandlet.</summary>
    DelvisBehandlet
}

/// <summary>
/// Status for en bankavstemming (per periode/konto).
/// </summary>
public enum AvstemmingStatus
{
    /// <summary>Paastartet, ikke ferdig.</summary>
    UnderArbeid,

    /// <summary>Avstemt, differanse = 0.</summary>
    Avstemt,

    /// <summary>Avstemt med forklart differanse.</summary>
    AvstemtMedDifferanse
}
```

### Entity: Bankkonto

```csharp
namespace Regnskap.Domain.Features.Bankavstemming;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kontoplan;

/// <summary>
/// En bankkonto registrert i systemet, koblet til en hovedbokkonto.
/// NBS 5: Bankkontoer skal avstemmes mot kontoutskrifter.
/// Mapper til SAF-T: Header > Company > BankAccount.
/// </summary>
public class Bankkonto : AuditableEntity
{
    /// <summary>
    /// Norsk bankkontonummer (11 siffer, format: XXXX.XX.XXXXX).
    /// </summary>
    public string Kontonummer { get; set; } = default!;

    /// <summary>
    /// IBAN (brukes i CAMT.053 for identifisering).
    /// </summary>
    public string? Iban { get; set; }

    /// <summary>
    /// BIC/SWIFT-kode for banken.
    /// </summary>
    public string? Bic { get; set; }

    /// <summary>
    /// Banknavn.
    /// </summary>
    public string Banknavn { get; set; } = default!;

    /// <summary>
    /// Beskrivelse / kallenavn (f.eks. "Driftskonto", "Skattetrekk").
    /// </summary>
    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// Valutakode for kontoen.
    /// </summary>
    public string Valutakode { get; set; } = "NOK";

    /// <summary>
    /// FK til hovedbokkonto (typisk 1920, 1930 etc.).
    /// NS 4102: 1920 = Bankinnskudd, 1930 = Skattetrekk.
    /// </summary>
    public Guid HovedbokkkontoId { get; set; }
    public Konto Hovedbokkonto { get; set; } = default!;

    /// <summary>
    /// Kontonummer fra hovedbok (denormalisert).
    /// </summary>
    public string Hovedbokkontonummer { get; set; } = default!;

    /// <summary>
    /// Om denne bankontoen er aktiv.
    /// </summary>
    public bool ErAktiv { get; set; } = true;

    /// <summary>
    /// Om dette er standardkonto for utbetalinger.
    /// </summary>
    public bool ErStandardUtbetaling { get; set; }

    /// <summary>
    /// Om dette er standardkonto for innbetalinger.
    /// </summary>
    public bool ErStandardInnbetaling { get; set; }

    // --- Navigasjon ---

    public ICollection<Kontoutskrift> Kontoutskrifter { get; set; } = new List<Kontoutskrift>();
    public ICollection<Bankavstemming> Avstemminger { get; set; } = new List<Bankavstemming>();
}
```

### Entity: Kontoutskrift

```csharp
namespace Regnskap.Domain.Features.Bankavstemming;

using Regnskap.Domain.Common;

/// <summary>
/// En importert kontoutskrift fra banken (CAMT.053).
/// Representerer en Stmt-node i CAMT.053-filen.
/// Bokforingsloven 13: kontoutskrifter skal oppbevares i 5 aar.
/// </summary>
public class Kontoutskrift : AuditableEntity
{
    /// <summary>
    /// FK til bankkonto.
    /// </summary>
    public Guid BankkontoId { get; set; }
    public Bankkonto Bankkonto { get; set; } = default!;

    /// <summary>
    /// Meldings-ID fra CAMT.053 (GrpHdr/MsgId).
    /// Brukes til duplikat-sjekk ved reimport.
    /// </summary>
    public string MeldingsId { get; set; } = default!;

    /// <summary>
    /// Utskrift-ID fra CAMT.053 (Stmt/Id).
    /// </summary>
    public string UtskriftId { get; set; } = default!;

    /// <summary>
    /// Sekvensnummer (Stmt/ElctrncSeqNb).
    /// </summary>
    public string? Sekvensnummer { get; set; }

    /// <summary>
    /// Dato kontoutskriften ble opprettet av banken.
    /// </summary>
    public DateTime OpprettetAvBank { get; set; }

    /// <summary>
    /// Periode-start for kontoutskriften.
    /// </summary>
    public DateOnly PeriodeFra { get; set; }

    /// <summary>
    /// Periode-slutt for kontoutskriften.
    /// </summary>
    public DateOnly PeriodeTil { get; set; }

    /// <summary>
    /// Inngaaende saldo fra banken (OPBD).
    /// </summary>
    public Belop InngaendeSaldo { get; set; }

    /// <summary>
    /// Utgaaende saldo fra banken (CLBD).
    /// </summary>
    public Belop UtgaendeSaldo { get; set; }

    /// <summary>
    /// Antall bevegelser i utskriften.
    /// </summary>
    public int AntallBevegelser { get; set; }

    /// <summary>
    /// Sum innbetalinger.
    /// </summary>
    public Belop SumInn { get; set; } = Belop.Null;

    /// <summary>
    /// Sum utbetalinger.
    /// </summary>
    public Belop SumUt { get; set; } = Belop.Null;

    /// <summary>
    /// Status for behandling.
    /// </summary>
    public KontoutskriftStatus Status { get; set; } = KontoutskriftStatus.Importert;

    /// <summary>
    /// Filsti til original CAMT.053-fil (oppbevares ihht bokforingsloven).
    /// </summary>
    public string? OriginalFilsti { get; set; }

    /// <summary>
    /// SHA-256 hash av original fil (for integritetskontroll).
    /// </summary>
    public string? FilHash { get; set; }

    // --- Navigasjon ---

    public ICollection<Bankbevegelse> Bevegelser { get; set; } = new List<Bankbevegelse>();
}
```

### Entity: Bankbevegelse

```csharp
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
```

### Entity: BankbevegelseMatch

```csharp
namespace Regnskap.Domain.Features.Bankavstemming;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kundereskontro;
using Regnskap.Domain.Features.Leverandorreskontro;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Kobling mellom en bankbevegelse og en aapen post (faktura, betaling, bilag).
/// En bevegelse kan matche flere poster (splitt), og en post kan matche flere bevegelser.
/// </summary>
public class BankbevegelseMatch : AuditableEntity
{
    /// <summary>
    /// FK til bankbevegelsen.
    /// </summary>
    public Guid BankbevegelseId { get; set; }
    public Bankbevegelse Bankbevegelse { get; set; } = default!;

    /// <summary>
    /// Belop som er matchet fra denne bevegelsen.
    /// Ved splitt: delbelop. Ellers: hele belopet.
    /// </summary>
    public Belop Belop { get; set; }

    // --- Matching mot ulike entiteter ---

    /// <summary>
    /// FK til kundefaktura (innbetaling fra kunde).
    /// </summary>
    public Guid? KundeFakturaId { get; set; }
    public KundeFaktura? KundeFaktura { get; set; }

    /// <summary>
    /// FK til leverandorfaktura (utbetaling til leverandor).
    /// </summary>
    public Guid? LeverandorFakturaId { get; set; }
    public LeverandorFaktura? LeverandorFaktura { get; set; }

    /// <summary>
    /// FK til eksisterende bilag (f.eks. lonnskjoring, manuelt bilag).
    /// </summary>
    public Guid? BilagId { get; set; }
    public Bilag? Bilag { get; set; }

    /// <summary>
    /// Beskrivelse av matching (automatisk generert eller brukerkommentar).
    /// </summary>
    public string? Beskrivelse { get; set; }

    /// <summary>
    /// Type match.
    /// </summary>
    public MatcheType MatcheType { get; set; }
}
```

### Entity: Bankavstemming

```csharp
namespace Regnskap.Domain.Features.Bankavstemming;

using Regnskap.Domain.Common;

/// <summary>
/// Avstemming av en bankkonto for en gitt periode.
/// NBS 5: Bankkontoer maa dokumenteres/avstemmes ved aarsavslutning.
/// </summary>
public class Bankavstemming : AuditableEntity
{
    /// <summary>
    /// FK til bankkonto.
    /// </summary>
    public Guid BankkontoId { get; set; }
    public Bankkonto Bankkonto { get; set; } = default!;

    /// <summary>
    /// Avstemmingsdato (typisk periodeslutt eller maanedsslutt).
    /// </summary>
    public DateOnly Avstemmingsdato { get; set; }

    /// <summary>
    /// Regnskapsaar.
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Periodenummer (1-12, eller 0 for helaar).
    /// </summary>
    public int Periode { get; set; }

    // --- Saldoer ---

    /// <summary>
    /// Saldo i hovedbok (fra KontoSaldo) per avstemmingsdato.
    /// </summary>
    public Belop SaldoHovedbok { get; set; }

    /// <summary>
    /// Saldo ihht kontoutskrift fra banken.
    /// </summary>
    public Belop SaldoBank { get; set; }

    /// <summary>
    /// Differanse = SaldoBank - SaldoHovedbok.
    /// Maa vaere 0 for godkjent avstemming (eller forklart).
    /// </summary>
    public Belop Differanse { get; set; }

    // --- Tidsavgrensninger (forklarer differanse) ---

    /// <summary>
    /// Sum utestaaende sjekker / betalinger ikke registrert i bank.
    /// </summary>
    public Belop UtestaaendeBetalinger { get; set; } = Belop.Null;

    /// <summary>
    /// Sum innbetalinger i transitt (registrert i bank, ikke i regnskap).
    /// </summary>
    public Belop InnbetalingerITransitt { get; set; } = Belop.Null;

    /// <summary>
    /// Andre forklarte differanser.
    /// </summary>
    public Belop AndreDifferanser { get; set; } = Belop.Null;

    /// <summary>
    /// Forklaring til eventuell gjenstaende differanse.
    /// </summary>
    public string? DifferanseForklaring { get; set; }

    /// <summary>
    /// Status for avstemmingen.
    /// </summary>
    public AvstemmingStatus Status { get; set; } = AvstemmingStatus.UnderArbeid;

    /// <summary>
    /// Hvem som utforte/godkjente avstemmingen.
    /// </summary>
    public string? GodkjentAv { get; set; }

    /// <summary>
    /// Tidspunkt for godkjenning.
    /// </summary>
    public DateTime? GodkjentTidspunkt { get; set; }

    // --- Avstemt differanse-sjekk ---

    /// <summary>
    /// Beregnet forklart differanse.
    /// Hvis Differanse = UtestaendeBetalinger + InnbetalingerITransitt + AndreDifferanser
    /// => avstemmingen kan godkjennes.
    /// </summary>
    public Belop ForklartDifferanse =>
        UtestaaendeBetalinger + InnbetalingerITransitt + AndreDifferanser;

    /// <summary>
    /// Uforklart differanse = Differanse - ForklartDifferanse.
    /// Maa vaere 0 for status Avstemt.
    /// </summary>
    public Belop UforklartDifferanse => Differanse - ForklartDifferanse;
}
```

### EF Core-konfigurasjon

```csharp
// BankkontoConfiguration.cs
builder.HasKey(b => b.Id);
builder.HasIndex(b => b.Kontonummer).IsUnique();
builder.HasIndex(b => b.Iban).IsUnique().HasFilter("Iban IS NOT NULL");
builder.HasOne(b => b.Hovedbokkonto).WithMany()
    .HasForeignKey(b => b.HovedbokkkontoId).OnDelete(DeleteBehavior.Restrict);

// KontoutskriftConfiguration.cs
builder.HasKey(k => k.Id);
builder.HasIndex(k => new { k.BankkontoId, k.MeldingsId }).IsUnique(); // Duplikat-vern
builder.HasIndex(k => new { k.BankkontoId, k.PeriodeFra, k.PeriodeTil });
builder.HasOne(k => k.Bankkonto).WithMany(b => b.Kontoutskrifter)
    .HasForeignKey(k => k.BankkontoId).OnDelete(DeleteBehavior.Restrict);

// BankbevegelseConfiguration.cs
builder.HasKey(b => b.Id);
builder.HasIndex(b => b.KidNummer).HasFilter("KidNummer IS NOT NULL");
builder.HasIndex(b => b.Status);
builder.HasIndex(b => new { b.BankkontoId, b.Bokforingsdato });
builder.HasOne(b => b.Kontoutskrift).WithMany(k => k.Bevegelser)
    .HasForeignKey(b => b.KontoutskriftId).OnDelete(DeleteBehavior.Cascade);
builder.HasOne(b => b.Bankkonto).WithMany()
    .HasForeignKey(b => b.BankkontoId).OnDelete(DeleteBehavior.Restrict);
builder.HasOne(b => b.Bilag).WithMany()
    .HasForeignKey(b => b.BilagId).OnDelete(DeleteBehavior.Restrict);

// BankbevegelseMatchConfiguration.cs
builder.HasKey(m => m.Id);
builder.HasIndex(m => m.BankbevegelseId);
builder.HasOne(m => m.Bankbevegelse).WithMany(b => b.Matchinger)
    .HasForeignKey(m => m.BankbevegelseId).OnDelete(DeleteBehavior.Cascade);
builder.HasOne(m => m.KundeFaktura).WithMany()
    .HasForeignKey(m => m.KundeFakturaId).OnDelete(DeleteBehavior.Restrict);
builder.HasOne(m => m.LeverandorFaktura).WithMany()
    .HasForeignKey(m => m.LeverandorFakturaId).OnDelete(DeleteBehavior.Restrict);
builder.HasOne(m => m.Bilag).WithMany()
    .HasForeignKey(m => m.BilagId).OnDelete(DeleteBehavior.Restrict);

// BankavstemmingConfiguration.cs
builder.HasKey(a => a.Id);
builder.HasIndex(a => new { a.BankkontoId, a.Ar, a.Periode }).IsUnique();
builder.HasOne(a => a.Bankkonto).WithMany(b => b.Avstemminger)
    .HasForeignKey(a => a.BankkontoId).OnDelete(DeleteBehavior.Restrict);
```

---

## API-kontrakt

### Bankkontoer

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/bankkontoer` | List alle bankkontoer |
| GET | `/api/bankkontoer/{id}` | Hent bankkonto med detaljer |
| POST | `/api/bankkontoer` | Registrer ny bankkonto |
| PUT | `/api/bankkontoer/{id}` | Oppdater bankkonto |
| DELETE | `/api/bankkontoer/{id}` | Deaktiver bankkonto (soft delete) |

### Kontoutskrift-import

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| POST | `/api/bankkontoer/{id}/import` | Importer CAMT.053-fil |
| GET | `/api/bankkontoer/{id}/kontoutskrifter` | List importerte kontoutskrifter |
| GET | `/api/kontoutskrifter/{id}` | Hent kontoutskrift med bevegelser |

### Bankbevegelser

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/bankkontoer/{id}/bevegelser` | List bevegelser med filtrering |
| GET | `/api/bankbevegelser/{id}` | Hent enkelt bevegelse med matchinger |
| POST | `/api/bankbevegelser/{id}/match` | Manuell matching mot aapen post |
| POST | `/api/bankbevegelser/{id}/splitt` | Splitt bevegelse og match deler |
| POST | `/api/bankbevegelser/{id}/bokfor` | Bokfor bevegelse som ny transaksjon |
| POST | `/api/bankbevegelser/{id}/ignorer` | Marker bevegelse som ignorert |
| DELETE | `/api/bankbevegelser/{id}/match` | Fjern matching (tilbakestill) |

### Automatisk matching

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| POST | `/api/bankkontoer/{id}/auto-match` | Kjoer automatisk matching paa alle umatchede |
| GET | `/api/bankkontoer/{id}/match-forslag` | Hent forslag til matching |

### Avstemming

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/bankkontoer/{id}/avstemming` | Hent avstemmingsstatus |
| POST | `/api/bankkontoer/{id}/avstemming` | Opprett/oppdater avstemming |
| POST | `/api/bankkontoer/{id}/avstemming/godkjenn` | Godkjenn avstemming |
| GET | `/api/bankkontoer/{id}/avstemming/rapport` | Generer avstemmingsrapport |

### Request/Response DTO-er

```csharp
// --- Bankkonto ---
public record OpprettBankkontoRequest(
    string Kontonummer,              // 11 siffer
    string? Iban,
    string? Bic,
    string Banknavn,
    string Beskrivelse,
    string Valutakode = "NOK",
    Guid HovedbokkkontoId = default, // Maa vaere konto i 1900-serien
    bool ErStandardUtbetaling = false,
    bool ErStandardInnbetaling = false
);

// --- Import ---
public record ImportKontoutskriftResponse(
    Guid KontoutskriftId,
    string MeldingsId,
    DateOnly PeriodeFra,
    DateOnly PeriodeTil,
    decimal InngaendeSaldo,
    decimal UtgaendeSaldo,
    int AntallBevegelser,
    int AntallAutoMatchet,
    int AntallIkkeMatchet
);

// --- Bevegelser ---
public record BankbevegelseResponse(
    Guid Id,
    DateOnly Bokforingsdato,
    DateOnly? Valuteringsdato,
    BankbevegelseRetning Retning,
    decimal Belop,
    string? KidNummer,
    string? Motpart,
    string? Beskrivelse,
    BankbevegelseStatus Status,
    MatcheType? MatcheType,
    decimal? MatcheKonfidens,
    List<BankbevegelseMatchResponse> Matchinger
);

public record BankbevegelseMatchResponse(
    Guid Id,
    decimal Belop,
    MatcheType MatcheType,
    string? Beskrivelse,
    // Matchet entitet (en av disse er utfylt)
    Guid? KundeFakturaId,
    string? KundeFakturaNummer,
    Guid? LeverandorFakturaId,
    string? LeverandorFakturaNummer,
    Guid? BilagId,
    string? BilagNummer
);

// --- Manuell matching ---
public record ManuellMatchRequest(
    Guid? KundeFakturaId,
    Guid? LeverandorFakturaId,
    Guid? BilagId,
    string? Beskrivelse
);

// --- Splitt ---
public record SplittMatchRequest(
    List<SplittLinjeRequest> Linjer
);

public record SplittLinjeRequest(
    decimal Belop,
    Guid? KundeFakturaId,
    Guid? LeverandorFakturaId,
    Guid? BilagId,
    string? Beskrivelse
);

// --- Bokfor ny transaksjon ---
public record BokforBankbevegelseRequest(
    Guid MotkontoId,                    // Motkonto for bokforing
    string? MvaKode,                    // Eventuell MVA
    string Beskrivelse,
    string? Avdelingskode,
    string? Prosjektkode
);

// --- Avstemming ---
public record AvstemmingResponse(
    Guid Id,
    Guid BankkontoId,
    string Bankkontonummer,
    DateOnly Avstemmingsdato,
    decimal SaldoHovedbok,
    decimal SaldoBank,
    decimal Differanse,
    decimal UtestaaendeBetalinger,
    decimal InnbetalingerITransitt,
    decimal AndreDifferanser,
    decimal UforklartDifferanse,
    AvstemmingStatus Status,
    string? GodkjentAv,
    DateTime? GodkjentTidspunkt,
    // Detaljer
    int AntallMatchedeBevegelser,
    int AntallUmatchedeBevegelser,
    List<BankbevegelseResponse> UmatchedeBevegelser
);

// --- Avstemmingsrapport ---
public record AvstemmingsrapportResponse(
    string Bankkontonummer,
    string Banknavn,
    string Hovedbokkontonummer,
    DateOnly Avstemmingsdato,
    // Saldoer
    decimal SaldoHovedbok,
    decimal SaldoBank,
    decimal Differanse,
    // Tidsavgrensninger
    List<AvstemmingspostResponse> UtestaaendeBetalinger,
    List<AvstemmingspostResponse> InnbetalingerITransitt,
    List<AvstemmingspostResponse> AndrePoster,
    // Kontroll
    decimal SumTidsavgrensninger,
    decimal UforklartDifferanse,
    AvstemmingStatus Status
);

public record AvstemmingspostResponse(
    DateOnly Dato,
    string Beskrivelse,
    decimal Belop,
    string? Referanse
);
```

### Validering

| Felt | Regel |
|------|-------|
| Bankkonto.Kontonummer | 11 siffer, gyldig MOD11-kontroll |
| Bankkonto.HovedbokkkontoId | Maa vaere konto i klasse 1 (19xx-serien) |
| CAMT.053-fil | Gyldig XML, maa matche bankkontoens IBAN/kontonummer |
| ManuellMatch | Noyaktig en av KundeFakturaId, LeverandorFakturaId, BilagId maa vaere satt |
| Splitt.Linjer | Sum av delbelop maa vaere lik bevegelsens totalbelop |
| BokforBankbevegelse.MotkontoId | Maa vaere aktiv, bokforbar konto |
| Avstemming.Godkjenning | UforklartDifferanse maa vaere 0, eller DifferanseForklaring maa vaere utfylt |

### Feilkoder

| Kode | Melding |
|------|---------|
| BANK_KONTO_FINNES | Bankkonto med dette kontonummeret er allerede registrert |
| BANK_UGYLDIG_KONTONUMMER | Bankkontonummer er ugyldig (MOD11-kontroll feilet) |
| BANK_UGYLDIG_HOVEDBOK | Hovedbokkonto maa vaere i 19xx-serien (bank/kasse) |
| IMPORT_DUPLIKAT | Kontoutskrift med MeldingsId '{id}' er allerede importert |
| IMPORT_FEIL_KONTO | IBAN/kontonummer i filen matcher ikke valgt bankkonto |
| IMPORT_UGYLDIG_XML | Filen er ikke gyldig CAMT.053 XML |
| MATCH_ALLEREDE_MATCHET | Bevegelsen er allerede matchet |
| MATCH_BELOP_MISMATCH | Belopet matcher ikke den aapne posten |
| SPLITT_SUM_FEIL | Sum av delbelop ({sum}) stemmer ikke med bevegelsebelop ({belop}) |
| AVSTEMMING_UFORKLART | Kan ikke godkjenne avstemming med uforklart differanse |

---

## Forretningsregler

### FR-B01: CAMT.053 Import

Ved import av CAMT.053 XML-fil:

1. Parse XML og valider mot CAMT.053-skjema
2. Sjekk duplikat: MeldingsId (GrpHdr/MsgId) maa vaere unik per bankkonto
3. Identifiser bankkonto via IBAN (Stmt/Acct/Id/IBAN) -- maa matche registrert konto
4. For hver Stmt-node: opprett Kontoutskrift med saldoer
5. For hver Ntry-node: opprett Bankbevegelse med:
   - Retning: CdtDbtInd = "CRDT" => Inn, "DBIT" => Ut
   - Belop: Amt
   - Bokforingsdato: BookgDt/Dt
   - KidNummer: NtryDtls/TxDtls/RmtInf/Strd/CdtrRefInf/Ref
   - Motpart: NtryDtls/TxDtls/RltdPties/Dbtr/Nm eller Cdtr/Nm
   - EndToEndId: NtryDtls/TxDtls/Refs/EndToEndId
6. Valider: UtgaendeSaldo = InngaendeSaldo + SumInn - SumUt
7. Lagre original fil med SHA-256 hash
8. Kjoer automatisk matching (FR-B02) paa importerte bevegelser

### FR-B02: Automatisk matching

Matching kjores i prioritert rekkefolge. Stopper ved forste treff:

**Prioritet 1: KID-matching (innbetalinger)**
```
Hvis bevegelse.Retning = Inn OG bevegelse.KidNummer er satt:
  1. Valider KID (MOD10/MOD11)
  2. Sok etter KundeFaktura med KidNummer = bevegelse.KidNummer
     OG GjenstaendeBelop > 0
  3. Hvis funnet OG bevegelse.Belop == faktura.GjenstaendeBelop:
     => Match med konfidens 1.0
  4. Hvis funnet OG bevegelse.Belop < faktura.GjenstaendeBelop:
     => Match som delbetaling med konfidens 0.95
```

**Prioritet 2: EndToEndId-matching (utbetalinger)**
```
Hvis bevegelse.EndToEndId er satt:
  Sok etter LeverandorBetaling med Bankreferanse = bevegelse.EndToEndId
  Hvis funnet: match med konfidens 0.9
```

**Prioritet 3: Belop + dato-matching**
```
Sok etter aapne poster (KundeFaktura/LeverandorFaktura) der:
  - GjenstaendeBelop == bevegelse.Belop
  - Forfallsdato er innenfor +/- 5 dager fra bokforingsdato
Hvis noyaktig 1 treff: match med konfidens 0.7
Hvis flere treff: legg til som forslag (ingen automatisk match)
```

**Prioritet 4: Referanse/tekst-matching**
```
Sok i bevegelse.Beskrivelse etter:
  - Fakturanummer-moenster (f.eks. "Faktura 2026-00042")
  - Leverandornavn
Hvis funnet: foreslaa match med konfidens 0.5 (krever manuell bekreftelse)
```

### FR-B03: Manuell matching

Bruker kan manuelt matche en bevegelse mot:
1. En KundeFaktura (innbetaling)
2. En LeverandorFaktura (utbetaling)
3. Et eksisterende Bilag (annen transaksjon)

Ved manuell match:
- Opprett BankbevegelseMatch med MatcheType = Manuell
- Sett bevegelse.Status = ManueltMatchet
- Oppdater aapen post (KundeFaktura.GjenstaendeBelop etc.)
- Opprett/oppdater innbetalings-/betalingspost i reskontro

### FR-B04: Splitt-matching

En bankbevegelse kan splittes naar den dekker flere poster:

**Eksempel:** Innbetaling 25.000 kr dekker to fakturaer: 15.000 + 10.000

1. Valider: sum delbelop == bevegelse.Belop
2. Opprett BankbevegelseMatch for hver del
3. Sett bevegelse.Status = Splittet
4. Oppdater gjenstaende belop paa hver matchet post

### FR-B05: Bokforing av umatchede bevegelser

Naar en bevegelse ikke matcher noen eksisterende post, kan bruker bokfore den direkte:

**Innbetaling til bankkonto (eks. renteinntekt 150 kr):**
```
Bilag: Type = Bank
  Linje 1: Debet  1920 Bank           150,00
  Linje 2: Kredit 8050 Renteinntekt   150,00
```

**Utbetaling fra bankkonto (eks. bankgebyr 50 kr):**
```
Bilag: Type = Bank
  Linje 1: Kredit 1920 Bank           50,00
  Linje 2: Debet  6800 Bankgebyr      50,00
```

1. Bruker angir motkonto (og eventuelt MVA-kode)
2. System oppretter bilag med bankkonto og motkonto
3. Bilag bokfores
4. Bevegelse.Status = Bokfort, BilagId settes

### FR-B06: Avstemmingsrapport

Avstemmingsrapporten sammenligner saldo i hovedbok med saldo fra banken:

```
Saldo ihht hovedbok (konto 1920):           XXX.XXX,XX
Saldo ihht kontoutskrift fra bank:           YYY.YYY,YY
                                             ----------
Differanse:                                    ZZZ,ZZ

Tidsavgrensninger:
  + Utestaaende betalinger (i regnskap, ikke i bank):
    - Faktura 2026-00123 betalt 28.03.2026          1.500,00
    - ...
  - Innbetalinger i transitt (i bank, ikke i regnskap):
    - Innbetaling 29.03.2026 KID 00001200042         3.000,00
    - ...
  = Forklart differanse:                               ZZZ,ZZ
  = Uforklart differanse:                                0,00
```

**Beregning:**
```
SaldoHovedbok = KontoSaldo.UtgaendeBalanse for bankkontoens GL-konto per dato
SaldoBank = Kontoutskrift.UtgaendeSaldo (siste kontoutskrift for perioden)
Differanse = SaldoBank.Verdi - SaldoHovedbok.Verdi
```

NB: Fortegn paa differanse: bankens saldo er normalt positiv (kredit fra bankens perspektiv), mens hovedbok-saldo er debet (eiendel). Differanse beregnes slik at positiv differanse = banken har mer enn hovedbok.

### FR-B07: Validering av bankkontonummer

Norske bankkontonummer valideres med MOD11:
```
Kontonummer: 11 siffer, format XXXX.XX.XXXXX
Registernummer (4 siffer) + kontotype (2 siffer) + kontonummer (5 siffer)
Siste siffer = MOD11 kontrollsiffer
Vekter (fra hoyre): 2, 3, 4, 5, 6, 7, 2, 3, 4, 5
```

### FR-B08: Innbetaling-registrering ved match

Naar en innbetaling matches mot en kundefaktura:

1. Opprett KundeInnbetaling i kundereskontro
2. Reduser KundeFaktura.GjenstaendeBelop
3. Oppdater KundeFaktura.Status (Betalt hvis GjenstaendeBelop = 0, DelvisBetalt ellers)
4. Opprett bilag (hvis ikke allerede bokfort):
   ```
   Debet  1920 Bank               12.500,00
   Kredit 1500 Kundefordringer    12.500,00
   ```
5. Koble bilag til bankbevegelsen

### FR-B09: Utbetaling-registrering ved match

Naar en utbetaling matches mot en leverandorfaktura:

1. Opprett/oppdater LeverandorBetaling i leverandorreskontro
2. Reduser LeverandorFaktura.GjenstaendeBelop
3. Bilag er normalt allerede opprettet ved betalingsforslag (pain.001)
4. Koble bilag til bankbevegelsen

---

## MVA-haandtering

Bankavstemming involverer normalt ikke MVA direkte. MVA er allerede bokfort paa faktura-/bilagsnivaa.

Unntak ved direkte bokforing av bankbevegelser (FR-B05):
- Hvis bruker angir MVA-kode ved bokforing, genereres MVA-postering
- Eksempel: Kjop med debetkort, 1.000 kr inkl. 25% MVA:
  ```
  Kredit 1920 Bank               1.000,00
  Debet  6500 Inventar             800,00
  Debet  2720 Inng. MVA 25%        200,00
  ```

---

## Avhengigheter

### Moduler dette avhenger av

| Modul | Interface/Service | Bruk |
|-------|------------------|------|
| Kontoplan | `Konto` entity | Hovedbokkonto for bankkonto (19xx) |
| Hovedbok | `Bilag`, `Postering` | Bokforing av bankbevegelser |
| Hovedbok | `KontoSaldo` | Saldo for avstemming |
| Kundereskontro | `KundeFaktura` | Matching innbetalinger |
| Kundereskontro | `KundeInnbetaling` | Registrere innbetalinger |
| Kundereskontro | `KidGenerator` | Validering av KID |
| Leverandorreskontro | `LeverandorFaktura` | Matching utbetalinger |
| Leverandorreskontro | `LeverandorBetaling` | Registrere betalinger |
| Bilagsregistrering | `IBilagRepository` | Bilagsopprettelse |

### Interfaces dette eksponerer

```csharp
/// <summary>
/// Service for import og parsing av CAMT.053.
/// </summary>
public interface ICamt053ImportService
{
    /// <summary>
    /// Importer en CAMT.053-fil for en bankkonto.
    /// Returnerer importresultat med antall bevegelser og auto-matchinger.
    /// </summary>
    Task<ImportKontoutskriftResponse> Importer(Guid bankkontoId, Stream fil, string filnavn);
}

/// <summary>
/// Service for automatisk og manuell matching av bankbevegelser.
/// </summary>
public interface IBankMatchingService
{
    /// <summary>
    /// Kjoer automatisk matching paa alle umatchede bevegelser for en bankkonto.
    /// </summary>
    Task<int> AutoMatch(Guid bankkontoId);

    /// <summary>
    /// Hent matchforslag for en enkelt bevegelse.
    /// </summary>
    Task<IReadOnlyList<MatcheForslagResponse>> HentForslag(Guid bankbevegelseId);

    /// <summary>
    /// Manuell matching av en bevegelse.
    /// </summary>
    Task Match(Guid bankbevegelseId, ManuellMatchRequest request);

    /// <summary>
    /// Splitt-matching.
    /// </summary>
    Task Splitt(Guid bankbevegelseId, SplittMatchRequest request);

    /// <summary>
    /// Fjern matching og tilbakestill bevegelse til IkkeMatchet.
    /// </summary>
    Task FjernMatch(Guid bankbevegelseId);
}

/// <summary>
/// Service for bankavstemming.
/// </summary>
public interface IBankavstemmingService
{
    /// <summary>
    /// Hent eller opprett avstemming for en bankkonto og periode.
    /// </summary>
    Task<Bankavstemming> HentEllerOpprett(Guid bankkontoId, int aar, int periode);

    /// <summary>
    /// Oppdater tidsavgrensninger og forklaringer.
    /// </summary>
    Task<Bankavstemming> Oppdater(Guid avstemmingId, OppdaterAvstemmingRequest request);

    /// <summary>
    /// Godkjenn avstemming (validerer at differanse er forklart).
    /// </summary>
    Task<Bankavstemming> Godkjenn(Guid avstemmingId, string godkjentAv);

    /// <summary>
    /// Generer avstemmingsrapport.
    /// </summary>
    Task<AvstemmingsrapportResponse> GenererRapport(Guid bankkontoId, DateOnly dato);
}

/// <summary>
/// Repository for bankavstemming.
/// </summary>
public interface IBankRepository
{
    Task<Bankkonto?> HentBankkonto(Guid id);
    Task<Bankkonto?> HentBankkontoMedIban(string iban);
    Task<IReadOnlyList<Bankkonto>> HentAlleBankkontoer(bool kunAktive = true);
    Task<Bankbevegelse?> HentBevegelse(Guid id);
    Task<IReadOnlyList<Bankbevegelse>> HentUmatchedeBevegelser(Guid bankkontoId);
    Task<Kontoutskrift?> HentKontoutskrift(Guid id);
    Task<bool> KontoutskriftFinnes(Guid bankkontoId, string meldingsId);
}

public record MatcheForslagResponse(
    MatcheType MatcheType,
    decimal Konfidens,
    string Beskrivelse,
    Guid? KundeFakturaId,
    string? KundeFakturaNummer,
    decimal? KundeFakturaGjenstaende,
    Guid? LeverandorFakturaId,
    string? LeverandorFakturaNummer,
    decimal? LeverandorFakturaGjenstaende,
    Guid? BilagId,
    string? BilagNummer
);

public record OppdaterAvstemmingRequest(
    decimal UtestaaendeBetalinger,
    decimal InnbetalingerITransitt,
    decimal AndreDifferanser,
    string? DifferanseForklaring
);
```

### Moduler som avhenger av denne

| Modul | Bruk |
|-------|------|
| (Ingen direkte avhengigheter foreloping) | Avstemmingsrapport brukes ved periodeslutt/aarsavslutning |
