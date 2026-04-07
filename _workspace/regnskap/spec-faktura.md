# Spesifikasjon: Fakturering (Invoicing)

**Modul:** Fakturering (Modul 7)
**Status:** Komplett spesifikasjon
**Avhengigheter:** Kundereskontro, Kontoplan, Hovedbok, Bilagsregistrering, MVA
**SAF-T-seksjon:** SourceDocuments > SalesInvoices, GeneralLedgerEntries
**Bokforingsloven:** 5 (spesifikasjoner), 6 (dokumentasjon), 7 (ajourhold), 10 (bilag)
**Bokforingsforskriften:** 5-1-1 (salgsdokument), 5-2-1 (nummerering og tidspunkt)

---

## Datamodell

### Enums

```csharp
namespace Regnskap.Domain.Features.Fakturering;

/// <summary>
/// Status for en faktura i faktureringsprosessen.
/// Skiller seg fra KundeFakturaStatus ved at denne dekker
/// hele flyten fra utkast til sendt/bokfort.
/// </summary>
public enum FakturaStatus
{
    /// <summary>Utkast -- kan fortsatt redigeres.</summary>
    Utkast,

    /// <summary>Godkjent -- klar for utsendelse.</summary>
    Godkjent,

    /// <summary>Utstedt -- faktura er sendt, bilag opprettet.</summary>
    Utstedt,

    /// <summary>Kreditert -- hel eller delvis kreditnota utstedt.</summary>
    Kreditert,

    /// <summary>Kansellert -- utkast som ble forkastet (aldri utstedt).</summary>
    Kansellert
}

/// <summary>
/// Type fakturadokument.
/// </summary>
public enum FakturaDokumenttype
{
    /// <summary>Ordinaer faktura. EHF InvoiceTypeCode = 380.</summary>
    Faktura,

    /// <summary>Kreditnota. EHF InvoiceTypeCode = 381.</summary>
    Kreditnota
}

/// <summary>
/// Leveringsformat for faktura.
/// </summary>
public enum FakturaLeveringsformat
{
    /// <summary>PDF sendt per e-post.</summary>
    Epost,

    /// <summary>EHF/PEPPOL elektronisk faktura.</summary>
    Ehf,

    /// <summary>Papir (utskrift).</summary>
    Papir,

    /// <summary>Kun lagret i systemet (f.eks. kontantsalg).</summary>
    Intern
}

/// <summary>
/// Rabattype per linje.
/// </summary>
public enum RabattType
{
    /// <summary>Prosent rabatt.</summary>
    Prosent,

    /// <summary>Fast belop rabatt.</summary>
    Belop
}

/// <summary>
/// Enhet for fakturalinje.
/// Mapper til UBL unitCode (UN/ECE Recommendation 20).
/// </summary>
public enum Enhet
{
    /// <summary>Stykk (EA).</summary>
    Stykk,

    /// <summary>Timer (HUR).</summary>
    Timer,

    /// <summary>Kilogram (KGM).</summary>
    Kilogram,

    /// <summary>Liter (LTR).</summary>
    Liter,

    /// <summary>Meter (MTR).</summary>
    Meter,

    /// <summary>Kvadratmeter (MTK).</summary>
    Kvadratmeter,

    /// <summary>Pakke (PK).</summary>
    Pakke,

    /// <summary>Maaned (MON).</summary>
    Maaned,

    /// <summary>Dag (DAY).</summary>
    Dag
}
```

### Entity: Faktura

```csharp
namespace Regnskap.Domain.Features.Fakturering;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kundereskontro;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Utgaaende faktura. Kjerneentiteten i faktureringsmodulen.
/// Representerer hele livssyklusen fra utkast til utstedt/kreditert.
///
/// Bokforingsforskriften 5-1-1: alle obligatoriske felter for salgsdokument.
/// Mapper til SAF-T: SourceDocuments > SalesInvoices > Invoice.
/// EHF: UBL 2.1 Invoice (typeCode 380) eller CreditNote (typeCode 381).
/// </summary>
public class Faktura : AuditableEntity
{
    // --- Identifikasjon ---

    /// <summary>
    /// Fakturanummer. Fortlopende per aar.
    /// Bokforingsforskriften 5-1-1 nr. 1: "fortlopende nummer".
    /// Tildeles forst ved overgang fra Utkast til Godkjent/Utstedt.
    /// </summary>
    public int? Fakturanummer { get; set; }

    /// <summary>
    /// Aaret fakturanummeret tilhorer. Nummerserie per aar.
    /// </summary>
    public int? FakturanummerAr { get; set; }

    /// <summary>
    /// Unik fakturaidentifikator for visning, f.eks. "2026-00042".
    /// </summary>
    public string? FakturaId => Fakturanummer.HasValue && FakturanummerAr.HasValue
        ? $"{FakturanummerAr}-{Fakturanummer:D5}"
        : null;

    /// <summary>
    /// Type dokument (Faktura eller Kreditnota).
    /// </summary>
    public FakturaDokumenttype Dokumenttype { get; set; } = FakturaDokumenttype.Faktura;

    /// <summary>
    /// Status i faktureringsflyten.
    /// </summary>
    public FakturaStatus Status { get; set; } = FakturaStatus.Utkast;

    // --- Kunde ---

    /// <summary>
    /// FK til kunden.
    /// Bokforingsforskriften 5-1-1 nr. 4: "kjoperens navn/adresse".
    /// </summary>
    public Guid KundeId { get; set; }
    public Kunde Kunde { get; set; } = default!;

    // --- Datoer ---

    /// <summary>
    /// Fakturadato. Bokforingsforskriften 5-1-1 nr. 8.
    /// Settes ved utstedelse.
    /// </summary>
    public DateOnly? Fakturadato { get; set; }

    /// <summary>
    /// Forfallsdato. Bokforingsforskriften 5-1-1 nr. 9.
    /// Beregnes fra kundens betalingsbetingelse.
    /// </summary>
    public DateOnly? Forfallsdato { get; set; }

    /// <summary>
    /// Leveringsdato / ytelsesperiode. Bokforingsforskriften 5-1-1 nr. 7.
    /// </summary>
    public DateOnly? Leveringsdato { get; set; }

    /// <summary>
    /// Periodens sluttdato (for tjenester over tid).
    /// EHF BT-73/BT-74: InvoicePeriod.
    /// </summary>
    public DateOnly? LeveringsperiodeSlutt { get; set; }

    // --- Belop (beregnet fra linjer) ---

    /// <summary>
    /// Sum ex. MVA. Bokforingsforskriften 5-1-1 nr. 10.
    /// Beregnes: sum av alle linjers nettobelop.
    /// </summary>
    public Belop BelopEksMva { get; set; } = Belop.Null;

    /// <summary>
    /// Sum MVA. Bokforingsforskriften 5-1-1 nr. 11.
    /// Beregnes: sum av alle linjers MVA-belop.
    /// </summary>
    public Belop MvaBelop { get; set; } = Belop.Null;

    /// <summary>
    /// Sum inkl. MVA. Bokforingsforskriften 5-1-1 nr. 12.
    /// </summary>
    public Belop BelopInklMva { get; set; } = Belop.Null;

    // --- Betaling og KID ---

    /// <summary>
    /// KID-nummer generert for denne fakturaen.
    /// Brukes til automatisk bankavstemming.
    /// </summary>
    public string? KidNummer { get; set; }

    /// <summary>
    /// Bankkontonummer for betaling. Hentes fra selskapsinnstillinger.
    /// EHF BT-84.
    /// </summary>
    public string? Bankkontonummer { get; set; }

    /// <summary>
    /// IBAN for betaling. EHF BT-84.
    /// </summary>
    public string? Iban { get; set; }

    /// <summary>
    /// BIC for betaling (internasjonal). EHF BT-86.
    /// </summary>
    public string? Bic { get; set; }

    // --- Valuta ---

    public string Valutakode { get; set; } = "NOK";
    public decimal? Valutakurs { get; set; }

    // --- Leveringsformat ---

    /// <summary>
    /// Hvordan fakturaen leveres til kunden.
    /// </summary>
    public FakturaLeveringsformat Leveringsformat { get; set; } = FakturaLeveringsformat.Epost;

    // --- Referanser ---

    /// <summary>
    /// Kundens bestillingsnummer / PO-nummer.
    /// EHF BT-13: OrderReference.
    /// </summary>
    public string? Bestillingsnummer { get; set; }

    /// <summary>
    /// Kjopers referanse (kontaktperson/prosjekt hos kunde).
    /// EHF BT-10: BuyerReference. Obligatorisk i EHF.
    /// </summary>
    public string? KjopersReferanse { get; set; }

    /// <summary>
    /// Vaars referanse (selgers kontaktperson).
    /// </summary>
    public string? VaarReferanse { get; set; }

    /// <summary>
    /// Ekstern referanse (fri tekst).
    /// </summary>
    public string? EksternReferanse { get; set; }

    /// <summary>
    /// Fritekst-merknad paa faktura.
    /// </summary>
    public string? Merknad { get; set; }

    // --- Kreditnota-kobling ---

    /// <summary>
    /// Hvis dette er en kreditnota: FK til opprinnelig faktura.
    /// EHF BT-25: InvoiceDocumentReference for kreditnota.
    /// </summary>
    public Guid? KreditertFakturaId { get; set; }
    public Faktura? KreditertFaktura { get; set; }

    /// <summary>
    /// Kreditnotaer utstedt mot denne fakturaen.
    /// </summary>
    public ICollection<Faktura> Kreditnotaer { get; set; } = new List<Faktura>();

    /// <summary>
    /// Aarsak til kreditering (obligatorisk ved kreditnota).
    /// </summary>
    public string? Krediteringsaarsak { get; set; }

    // --- Bokforing ---

    /// <summary>
    /// FK til bilag opprettet ved utstedelse.
    /// </summary>
    public Guid? BilagId { get; set; }
    public Bilag? Bilag { get; set; }

    /// <summary>
    /// FK til KundeFaktura i kundereskontro.
    /// Opprettes automatisk ved utstedelse.
    /// </summary>
    public Guid? KundeFakturaId { get; set; }
    public KundeFaktura? KundeFaktura { get; set; }

    // --- EHF ---

    /// <summary>
    /// Om EHF-XML er generert.
    /// </summary>
    public bool EhfGenerert { get; set; }

    /// <summary>
    /// Tidspunkt EHF ble sendt via PEPPOL.
    /// </summary>
    public DateTime? EhfSendtTidspunkt { get; set; }

    /// <summary>
    /// PEPPOL leveringskvittering-ID.
    /// </summary>
    public string? PeppolLeveringsId { get; set; }

    // --- PDF ---

    /// <summary>
    /// Filsti til generert PDF. Lagres som vedlegg.
    /// </summary>
    public string? PdfFilsti { get; set; }

    // --- Navigasjon ---

    /// <summary>
    /// Fakturalinjer.
    /// </summary>
    public ICollection<FakturaLinje> Linjer { get; set; } = new List<FakturaLinje>();

    /// <summary>
    /// MVA-spesifikasjon (oppsummering per sats).
    /// </summary>
    public ICollection<FakturaMvaLinje> MvaLinjer { get; set; } = new List<FakturaMvaLinje>();
}
```

### Entity: FakturaLinje

```csharp
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
    /// Bokforingsforskriften 5-1-1 nr. 5.
    /// </summary>
    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// Antall. EHF BT-129.
    /// Bokforingsforskriften 5-1-1 nr. 6.
    /// </summary>
    public decimal Antall { get; set; } = 1;

    /// <summary>
    /// Enhet. Mapper til UBL unitCode.
    /// </summary>
    public Enhet Enhet { get; set; } = Enhet.Stykk;

    /// <summary>
    /// Enhetspris ekskl. MVA. EHF BT-146.
    /// Bokforingsforskriften 5-1-1 nr. 6.
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
    /// EHF BT-151 (TaxCategory) + BT-152 (Percent).
    /// </summary>
    public string MvaKode { get; set; } = default!;

    /// <summary>
    /// MVA-sats (snapshot). Hentes fra MvaKode ved opprettelse.
    /// </summary>
    public decimal MvaSats { get; set; }

    /// <summary>
    /// Beregnet MVA-belop for linjen.
    /// = Nettobelop * MvaSats / 100, avrundet til 2 desimaler.
    /// </summary>
    public Belop MvaBelop { get; set; }

    /// <summary>
    /// Bruttobelop inkl. MVA = Nettobelop + MvaBelop.
    /// </summary>
    public Belop Bruttobelop { get; set; }

    // --- Bokforing ---

    /// <summary>
    /// Inntektskonto (klasse 3xxx). Brukes ved automatisk bokforing.
    /// </summary>
    public Guid KontoId { get; set; }
    public string Kontonummer { get; set; } = default!;

    /// <summary>
    /// Avdelingskode / kostnadssted.
    /// </summary>
    public string? Avdelingskode { get; set; }

    /// <summary>
    /// Prosjektkode.
    /// </summary>
    public string? Prosjektkode { get; set; }
}
```

### Entity: FakturaMvaLinje

```csharp
namespace Regnskap.Domain.Features.Fakturering;

using Regnskap.Domain.Common;

/// <summary>
/// MVA-oppsummering per sats paa en faktura.
/// Bokforingsforskriften 5-1-1 nr. 11: "MVA-belop spesifisert per sats".
/// Mapper til EHF TaxSubtotal (BT-116..BT-119).
/// </summary>
public class FakturaMvaLinje : AuditableEntity
{
    public Guid FakturaId { get; set; }
    public Faktura Faktura { get; set; } = default!;

    /// <summary>
    /// MVA-kode (f.eks. "3" for 25% utgaaende).
    /// </summary>
    public string MvaKode { get; set; } = default!;

    /// <summary>
    /// MVA-sats i prosent.
    /// </summary>
    public decimal MvaSats { get; set; }

    /// <summary>
    /// Sum grunnlag (netto) for denne satsen. EHF BT-116.
    /// </summary>
    public Belop Grunnlag { get; set; }

    /// <summary>
    /// Sum MVA for denne satsen. EHF BT-117.
    /// </summary>
    public Belop MvaBelop { get; set; }

    /// <summary>
    /// EHF TaxCategory ID (S=standard, Z=zero, E=exempt).
    /// </summary>
    public string EhfTaxCategoryId { get; set; } = "S";
}
```

### Entity: FakturaNummerserie

```csharp
namespace Regnskap.Domain.Features.Fakturering;

using Regnskap.Domain.Common;

/// <summary>
/// Nummerserie for fakturering per aar.
/// Bokforingsforskriften 5-1-1 / 5-2-1: "kontrollbar ubrudt nummerserie".
/// Sikrer fortlopende nummerering uten hull.
/// </summary>
public class FakturaNummerserie : AuditableEntity
{
    /// <summary>
    /// Regnskapsaaret.
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Dokumenttype (Faktura eller Kreditnota).
    /// Separat serie per type er tillatt (bokforingsforskriften 5-2-1).
    /// </summary>
    public FakturaDokumenttype Dokumenttype { get; set; }

    /// <summary>
    /// Siste brukte nummer i serien.
    /// Neste nummer = SisteNummer + 1.
    /// </summary>
    public int SisteNummer { get; set; }

    /// <summary>
    /// Prefiks for visning (f.eks. "F" for faktura, "K" for kreditnota).
    /// </summary>
    public string? Prefiks { get; set; }
}
```

### Entity: Selskapsinfo (for fakturahode/PDF/EHF)

```csharp
namespace Regnskap.Domain.Features.Fakturering;

using Regnskap.Domain.Common;

/// <summary>
/// Selskapsinnstillinger for fakturering.
/// Inneholder alle felter som kraeves paa utgaaende faktura ihht
/// bokforingsforskriften 5-1-1 nr. 2-3 og EHF BT-27..BT-35.
/// </summary>
public class Selskapsinfo : AuditableEntity
{
    public string Navn { get; set; } = default!;
    public string Organisasjonsnummer { get; set; } = default!;
    public bool ErMvaRegistrert { get; set; }

    /// <summary>
    /// "Foretaksregisteret" -- obligatorisk for AS/ASA/NUF.
    /// EHF BT-35.
    /// </summary>
    public string? Foretaksregister { get; set; }

    // --- Adresse ---
    public string Adresse1 { get; set; } = default!;
    public string? Adresse2 { get; set; }
    public string Postnummer { get; set; } = default!;
    public string Poststed { get; set; } = default!;
    public string Landkode { get; set; } = "NO";

    // --- Kontakt ---
    public string? Telefon { get; set; }
    public string? Epost { get; set; }
    public string? Nettside { get; set; }

    // --- Bank ---
    public string Bankkontonummer { get; set; } = default!;
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public string? Banknavn { get; set; }

    // --- KID ---
    public KidAlgoritme StandardKidAlgoritme { get; set; } = KidAlgoritme.MOD10;

    // --- PDF ---
    /// <summary>
    /// Filsti til firmalogo (PNG/SVG). Brukes i PDF-generering.
    /// </summary>
    public string? LogoFilsti { get; set; }

    // --- PEPPOL ---
    /// <summary>
    /// PEPPOL-identifikator for selskapet (avsender).
    /// Format: "0192:{orgnummer}" for norske foretak.
    /// </summary>
    public string? PeppolId { get; set; }
}
```

### EF Core-konfigurasjon

```csharp
// FakturaConfiguration.cs
builder.HasKey(f => f.Id);
builder.HasIndex(f => new { f.FakturanummerAr, f.Fakturanummer })
    .IsUnique()
    .HasFilter("Fakturanummer IS NOT NULL");
builder.HasIndex(f => f.KundeId);
builder.HasIndex(f => f.Status);
builder.HasIndex(f => f.Fakturadato);
builder.HasIndex(f => f.KidNummer).HasFilter("KidNummer IS NOT NULL");
builder.HasIndex(f => f.KreditertFakturaId).HasFilter("KreditertFakturaId IS NOT NULL");

builder.HasOne(f => f.Kunde).WithMany().HasForeignKey(f => f.KundeId)
    .OnDelete(DeleteBehavior.Restrict);
builder.HasOne(f => f.Bilag).WithMany().HasForeignKey(f => f.BilagId)
    .OnDelete(DeleteBehavior.Restrict);
builder.HasOne(f => f.KundeFaktura).WithMany().HasForeignKey(f => f.KundeFakturaId)
    .OnDelete(DeleteBehavior.Restrict);
builder.HasOne(f => f.KreditertFaktura)
    .WithMany(f => f.Kreditnotaer)
    .HasForeignKey(f => f.KreditertFakturaId)
    .OnDelete(DeleteBehavior.Restrict);

builder.Property(f => f.BelopEksMva).HasConversion<decimal>();
builder.Property(f => f.MvaBelop).HasConversion<decimal>();
builder.Property(f => f.BelopInklMva).HasConversion<decimal>();

// FakturaLinjeConfiguration.cs
builder.HasKey(l => l.Id);
builder.HasIndex(l => new { l.FakturaId, l.Linjenummer }).IsUnique();
builder.HasOne(l => l.Faktura).WithMany(f => f.Linjer)
    .HasForeignKey(l => l.FakturaId).OnDelete(DeleteBehavior.Cascade);

// FakturaMvaLinjeConfiguration.cs
builder.HasKey(m => m.Id);
builder.HasIndex(m => new { m.FakturaId, m.MvaKode }).IsUnique();
builder.HasOne(m => m.Faktura).WithMany(f => f.MvaLinjer)
    .HasForeignKey(m => m.FakturaId).OnDelete(DeleteBehavior.Cascade);

// FakturaNummerserieConfiguration.cs
builder.HasKey(n => n.Id);
builder.HasIndex(n => new { n.Ar, n.Dokumenttype }).IsUnique();
```

---

## API-kontrakt

### Fakturaer

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/fakturaer` | List fakturaer med filtrering/paginering |
| GET | `/api/fakturaer/{id}` | Hent enkelt faktura med linjer |
| POST | `/api/fakturaer` | Opprett ny faktura (utkast) |
| PUT | `/api/fakturaer/{id}` | Oppdater utkast |
| POST | `/api/fakturaer/{id}/utstede` | Utstede faktura (tildel nummer, bokfor, generer KID) |
| POST | `/api/fakturaer/{id}/kreditnota` | Opprett kreditnota for faktura |
| POST | `/api/fakturaer/{id}/ehf` | Generer EHF-XML |
| GET | `/api/fakturaer/{id}/ehf` | Last ned EHF-XML |
| POST | `/api/fakturaer/{id}/pdf` | Generer PDF |
| GET | `/api/fakturaer/{id}/pdf` | Last ned PDF |
| DELETE | `/api/fakturaer/{id}` | Kanseller utkast (soft delete) |

### Request/Response DTO-er

```csharp
// --- Opprett faktura ---
public record OpprettFakturaRequest(
    Guid KundeId,
    DateOnly? Leveringsdato,
    DateOnly? LeveringsperiodeSlutt,
    string? Bestillingsnummer,          // EHF BT-13
    string? KjopersReferanse,           // EHF BT-10 (obligatorisk for EHF)
    string? VaarReferanse,
    string? EksternReferanse,
    string? Merknad,
    string Valutakode = "NOK",
    FakturaLeveringsformat Leveringsformat = FakturaLeveringsformat.Epost,
    List<FakturaLinjeRequest> Linjer = default!
);

public record FakturaLinjeRequest(
    string Beskrivelse,                 // Obligatorisk
    decimal Antall,                     // > 0
    Enhet Enhet,
    decimal Enhetspris,                 // >= 0
    string MvaKode,                     // Obligatorisk
    Guid KontoId,                       // Inntektskonto (3xxx)
    RabattType? RabattType = null,
    decimal? RabattProsent = null,       // 0-100
    decimal? RabattBelop = null,
    string? Avdelingskode = null,
    string? Prosjektkode = null
);

// --- Kreditnota ---
public record OpprettKreditnotaRequest(
    string Krediteringsaarsak,           // Obligatorisk
    string? KjopersReferanse,
    List<KreditnotaLinjeRequest>? Linjer // Null = full kreditering
);

public record KreditnotaLinjeRequest(
    int OpprinneligLinjenummer,          // Referanse til original linje
    decimal Antall                       // Antall som krediteres (<= orig.)
);

// --- Response ---
public record FakturaResponse(
    Guid Id,
    string? FakturaId,
    FakturaDokumenttype Dokumenttype,
    FakturaStatus Status,
    Guid KundeId,
    string KundeNavn,
    string? Kundenummer,
    DateOnly? Fakturadato,
    DateOnly? Forfallsdato,
    DateOnly? Leveringsdato,
    decimal BelopEksMva,
    decimal MvaBelop,
    decimal BelopInklMva,
    string? KidNummer,
    string Valutakode,
    string? Bestillingsnummer,
    string? KjopersReferanse,
    FakturaLeveringsformat Leveringsformat,
    Guid? KreditertFakturaId,
    string? Krediteringsaarsak,
    bool EhfGenerert,
    List<FakturaLinjeResponse> Linjer,
    List<FakturaMvaLinjeResponse> MvaLinjer
);

public record FakturaLinjeResponse(
    Guid Id,
    int Linjenummer,
    string Beskrivelse,
    decimal Antall,
    Enhet Enhet,
    decimal Enhetspris,
    RabattType? RabattType,
    decimal? RabattProsent,
    decimal? RabattBelop,
    decimal Nettobelop,
    string MvaKode,
    decimal MvaSats,
    decimal MvaBelop,
    decimal Bruttobelop,
    string Kontonummer
);

public record FakturaMvaLinjeResponse(
    string MvaKode,
    decimal MvaSats,
    decimal Grunnlag,
    decimal MvaBelop,
    string EhfTaxCategoryId
);
```

### Validering

| Felt | Regel |
|------|-------|
| KundeId | Obligatorisk, maa eksistere og vaere aktiv |
| Linjer | Minimum 1 linje |
| Linje.Beskrivelse | Obligatorisk, 1-500 tegn |
| Linje.Antall | > 0 |
| Linje.Enhetspris | >= 0 |
| Linje.MvaKode | Obligatorisk, maa eksistere i MvaKode-tabell |
| Linje.KontoId | Obligatorisk, maa vaere aktiv konto i klasse 3 (inntekt) |
| Linje.RabattProsent | 0-100 (hvis angitt) |
| KjopersReferanse | Obligatorisk hvis Leveringsformat = Ehf |
| Bestillingsnummer | Obligatorisk hvis KjopersReferanse er null og Leveringsformat = Ehf |
| Kreditnota.KreditertFakturaId | Obligatorisk, original maa ha status Utstedt |
| Kreditnota.Krediteringsaarsak | Obligatorisk, 1-500 tegn |

### Feilkoder

| Kode | Melding |
|------|---------|
| FAKTURA_KUNDE_IKKE_FUNNET | Kunde med angitt ID finnes ikke |
| FAKTURA_KUNDE_SPERRET | Kunden er sperret for fakturering |
| FAKTURA_INGEN_LINJER | Faktura maa ha minimum en linje |
| FAKTURA_UGYLDIG_MVA_KODE | MVA-kode '{kode}' finnes ikke |
| FAKTURA_UGYLDIG_KONTO | Konto '{konto}' er ikke en gyldig inntektskonto |
| FAKTURA_IKKE_UTKAST | Kun utkast kan redigeres/kanselleres |
| FAKTURA_ALLEREDE_UTSTEDT | Faktura er allerede utstedt |
| FAKTURA_PERIODE_LUKKET | Regnskapsperioden er lukket |
| KREDITNOTA_ORIGINAL_IKKE_FUNNET | Opprinnelig faktura finnes ikke |
| KREDITNOTA_ALLEREDE_KREDITERT | Fakturaen er allerede fullt kreditert |
| KREDITNOTA_BELOP_OVERSTIGER | Kreditert belop overstiger gjenstaende |
| EHF_MANGLER_KJOPERS_REF | EHF krever BuyerReference eller OrderReference |
| EHF_KUNDE_UTEN_PEPPOL | Kunden har ikke PEPPOL-ID |

---

## Forretningsregler

### FR-F01: Fakturanummerering

Fakturanummer tildeles ved utstedelse, ALDRI ved opprettelse av utkast.

1. Hent FakturaNummerserie for gjeldende aar og dokumenttype
2. Inkrementer SisteNummer med 1 (med pessimistisk laas / `UPDLOCK`)
3. Tildel nytt nummer til fakturaen
4. Hvis nummerserie ikke finnes for aaret: opprett med SisteNummer = 0

**Bokforingsforskriften 5-2-1:** Nummerserien maa vaere kontrollbar og ubrudt. Kansellerte utkast faar aldri nummer, saa det oppstaar ingen hull.

### FR-F02: Belopberegning per linje

```
For hver linje:
  Bruttolinjebelop = Antall * Enhetspris
  Rabatt:
    Hvis RabattType = Prosent: RabattBelop = Bruttolinjebelop * RabattProsent / 100
    Hvis RabattType = Belop: RabattBelop = oppgitt belop
  Nettobelop = Bruttolinjebelop - RabattBelop
  MvaBelop = Math.Round(Nettobelop * MvaSats / 100, 2, MidpointRounding.AwayFromZero)
  Bruttobelop = Nettobelop + MvaBelop
```

### FR-F03: Belopberegning for faktura (totaler)

```
Faktura.BelopEksMva = Sum(alle linjer.Nettobelop)
Faktura.MvaBelop    = Sum(alle linjer.MvaBelop)
Faktura.BelopInklMva = Faktura.BelopEksMva + Faktura.MvaBelop
```

MVA-linjer (FakturaMvaLinje) beregnes ved aa gruppere fakturalinjer per MvaKode:
```
For hver unik MvaKode blant linjene:
  MvaLinje.Grunnlag = Sum(linjer med denne kode.Nettobelop)
  MvaLinje.MvaBelop = Sum(linjer med denne kode.MvaBelop)
  MvaLinje.MvaSats  = satsen fra MvaKode
```

**Oresavrunding:** MVA beregnes per linje, IKKE paa totalnivaa. Dette forhindrer avrundingsfeil og er konsistent med EHF-standarden.

### FR-F04: Forfallsdato-beregning

```
Forfallsdato = Fakturadato + KundeBetalingsbetingelse.AntallDager
Hvis Forfallsdato faller paa lordag/sondag: flytt til neste mandag
Kontant: Forfallsdato = Fakturadato
Forskudd: Forfallsdato = Fakturadato (betaling forventes for levering)
```

### FR-F05: KID-generering

Ved utstedelse av faktura:
1. Hent kundens KidAlgoritme (fallback til Selskapsinfo.StandardKidAlgoritme)
2. Generer KID: `KidGenerator.Generer(kundenummer, fakturanummer, algoritme)`
3. Hvis MOD11 returnerer -1 (ugyldig kontrollsiffer): bruk MOD10 som fallback
4. Lagre generert KID paa fakturaen

### FR-F06: Automatisk bokforing ved utstedelse

Naar en faktura utstedes, opprettes automatisk et bilag:

**Faktura (salg med 25% MVA, belop 10.000 eks. MVA):**
```
Bilag: Type = UtgaendeFaktura
  Linje 1: Debet  1500 Kundefordringer    12.500,00  (inkl. MVA)
  Linje 2: Kredit 3000 Salgsinntekt       10.000,00  (eks. MVA)
  Linje 3: Kredit 2710 Utg. MVA 25%        2.500,00  (MVA-belop)
```

**Kreditnota (kreditering av ovenstaende):**
```
Bilag: Type = Kreditnota
  Linje 1: Kredit 1500 Kundefordringer    12.500,00
  Linje 2: Debet  3000 Salgsinntekt       10.000,00
  Linje 3: Debet  2710 Utg. MVA 25%        2.500,00
```

Generell bokforingslogikk:
1. Opprett bilag med type UtgaendeFaktura (eller Kreditnota)
2. For hver unik inntektskonto blant linjene: opprett kredit-postering med sum netto
3. For hver unik MVA-kode: opprett kredit-postering paa MVA-konto (2710/2711/2712)
4. Opprett debet-postering paa 1500 Kundefordringer med totalt inkl. MVA
5. Valider balanse (debet = kredit)
6. Bokfor bilaget
7. Opprett KundeFaktura-post i kundereskontro

### FR-F07: Kreditnota-regler

1. Kreditnota MÅ referere til en utstedt faktura (KreditertFakturaId)
2. Kreditnota arver kundeinfo fra originalfakturaen
3. Delvis kreditering: bruker angir hvilke linjer og antall som krediteres
4. Full kreditering: alle linjer krediteres med originalt antall
5. Sum kreditert belop kan aldri overstige originalfakturaens gjenstaende belop
6. Kreditnota faar eget fakturanummer fra kreditnota-nummerserien
7. Kreditnota oppdaterer originalfakturaens status til Kreditert (hvis fullt kreditert)
8. Originalfakturaens KundeFaktura.GjenstaendeBelop reduseres

### FR-F08: EHF/PEPPOL-generering

EHF-XML genereres som UBL 2.1 dokument ihht PEPPOL BIS Billing 3.0:

1. Valider at alle obligatoriske EHF-felter er tilstede (se API-kontrakt validering)
2. Generer UBL Invoice (typeCode 380) eller CreditNote (typeCode 381)
3. Inkluder alle obligatoriske elementer fra seksjon 15 i legal reference:
   - CustomizationID og ProfileID (faste verdier)
   - Seller info fra Selskapsinfo
   - Buyer info fra Kunde
   - Betalingsinfo (bankkonto, KID)
   - Alle linjer med MVA per linje
   - MVA-oppsummering (TaxSubtotal per sats)
   - Totaler
4. Valider generert XML mot UBL 2.1 skjema (XSD-validering)
5. For kreditnota: inkluder BillingReference med original fakturanummer

**EHF-spesifikke krav for Norge:**
- "Foretaksregisteret" i CompanyLegalForm (for AS/ASA/NUF)
- BuyerReference (BT-10) ELLER OrderReference (BT-13) er paakrevd
- Landkode obligatorisk for baade kjoper og selger
- Hver linje maa ha MVA-sats, ogsaa hvis alle har samme sats

### FR-F09: PDF-generering

PDF-faktura inneholder (ihht bokforingsforskriften 5-1-1):

**Toppseksjon:**
- Firmalogo (fra Selskapsinfo.LogoFilsti)
- Firmanavn, adresse, org.nr + "MVA"
- Telefon, e-post, nettside

**Fakturainfo:**
- Fakturanummer, fakturadato, forfallsdato, leveringsdato
- Kundenummer, kundens navn og adresse
- Bestillingsnr, kjopers referanse, vaar referanse

**Linjetabell:**
| Linje | Beskrivelse | Antall | Enhet | Pris | Rabatt | Netto | MVA% | MVA |
|-------|-------------|--------|-------|------|--------|-------|------|-----|

**Bunn:**
- MVA-spesifikasjon per sats (grunnlag + MVA-belop)
- Sum ekskl. MVA, sum MVA, sum inkl. MVA
- Betalingsinfo: bankkontonr, KID, forfallsdato
- Eventuell merknad

### FR-F10: Utstedelse-flyt (komplett)

Naar `POST /api/fakturaer/{id}/utstede` kalles:

1. Valider at faktura har status = Utkast
2. Valider at regnskapsperioden for fakturadato er aapen
3. Sett Fakturadato = idag (hvis ikke allerede satt)
4. Beregn Forfallsdato (FR-F04)
5. Tildel Fakturanummer (FR-F01)
6. Beregn alle belop (FR-F02, FR-F03)
7. Generer KID (FR-F05)
8. Sett betalingsinfo (bankkontonr, IBAN fra Selskapsinfo)
9. Opprett bilag + posteringer (FR-F06)
10. Opprett KundeFaktura i kundereskontro
11. Sett Status = Utstedt
12. Generer PDF (FR-F09)
13. Generer EHF (FR-F08) hvis Leveringsformat = Ehf
14. Alt i en transaksjon (unit of work)

---

## MVA-haandtering

### Relevante MVA-koder for fakturering (utgaaende)

| Intern kode | SAF-T StandardTaxCode | Sats | Beskrivelse |
|-------------|----------------------|------|-------------|
| 3 | 3 | 25% | Utgaaende MVA, alminnelig sats |
| 31 | 31 | 15% | Utgaaende MVA, naeringsmiddel |
| 33 | 33 | 12% | Utgaaende MVA, lav sats |
| 5 | 5 | 0% | Utforsel (eksport) |
| 6 | 6 | 0% | Utenfor MVA-omraadet |

### Beregningslogikk

MVA beregnes per fakturalinje:
```
MvaBelop = Math.Round(Nettobelop.Verdi * MvaSats / 100m, 2, MidpointRounding.AwayFromZero)
```

### EHF TaxCategory-mapping

| MVA-kode | EHF TaxCategory | Beskrivelse |
|----------|-----------------|-------------|
| 3, 31, 33 | S | Standard rate |
| 5 | Z | Zero rated (export) |
| 6 | E | Exempt |

### SAF-T SourceDocuments mapping

Hver utstedt faktura mapper til:
```xml
<SalesInvoices>
  <Invoice>
    <InvoiceNo>{Fakturanummer}</InvoiceNo>
    <CustomerInfo><CustomerID>{Kundenummer}</CustomerID></CustomerInfo>
    <InvoiceDate>{Fakturadato}</InvoiceDate>
    <GLPostingDate>{Bilag.Bilagsdato}</GLPostingDate>
    <TransactionID>{Bilag.BilagsId}</TransactionID>
    <Line>
      <AccountID>{Kontonummer}</AccountID>
      <Description>{Beskrivelse}</Description>
      <DebitAmount/CreditAmount>
        <Amount>{Nettobelop}</Amount>
      </DebitAmount/CreditAmount>
      <TaxInformation>
        <TaxType>MVA</TaxType>
        <TaxCode>{MvaKode}</TaxCode>
        <TaxPercentage>{MvaSats}</TaxPercentage>
        <TaxBase>{Nettobelop}</TaxBase>
        <TaxAmount>{MvaBelop}</TaxAmount>
      </TaxInformation>
    </Line>
  </Invoice>
</SalesInvoices>
```

---

## Avhengigheter

### Moduler dette avhenger av

| Modul | Interface/Service | Bruk |
|-------|------------------|------|
| Kundereskontro | `Kunde` entity | Kundedata for faktura |
| Kundereskontro | `KundeFaktura` entity | Opprettes ved utstedelse |
| Kundereskontro | `KidGenerator` | KID-generering |
| Kontoplan | `Konto` entity | Inntektskontoer (3xxx) |
| Kontoplan | `MvaKode` entity | MVA-sats og konto-mapping |
| Hovedbok | `Bilag` entity | Automatisk bokforing |
| Hovedbok | `Postering` entity | Posteringslinjer |
| Bilagsregistrering | `IBilagRepository` | Bilagsopprettelse |
| MVA | `IMvaRepository` | MVA-kode-oppslag |

### Interfaces dette eksponerer

```csharp
/// <summary>
/// Service for fakturagenerering og -haandtering.
/// </summary>
public interface IFaktureringService
{
    Task<Faktura> OpprettFaktura(OpprettFakturaRequest request);
    Task<Faktura> OppdaterFaktura(Guid fakturaId, OpprettFakturaRequest request);
    Task<Faktura> UtstedeFaktura(Guid fakturaId);
    Task<Faktura> OpprettKreditnota(Guid originalFakturaId, OpprettKreditnotaRequest request);
    Task KansellerFaktura(Guid fakturaId);
}

/// <summary>
/// Service for EHF/PEPPOL XML-generering.
/// </summary>
public interface IEhfService
{
    Task<byte[]> GenererEhfXml(Guid fakturaId);
    Task<bool> ValiderEhfXml(byte[] xml);
    Task SendViaPeppol(Guid fakturaId, byte[] xml);
}

/// <summary>
/// Service for PDF-generering.
/// </summary>
public interface IFakturaPdfService
{
    Task<byte[]> GenererPdf(Guid fakturaId);
}

/// <summary>
/// Repository for fakturering.
/// </summary>
public interface IFakturaRepository
{
    Task<Faktura?> HentMedLinjer(Guid id);
    Task<int> NesteNummer(int aar, FakturaDokumenttype type);
    Task<IReadOnlyList<Faktura>> Sok(FakturaSokFilter filter);
}
```

### Moduler som avhenger av denne

| Modul | Bruk |
|-------|------|
| Bankavstemming (Modul 8) | Matcher bankbevegelser mot fakturaer via KID |
| Purring (eksisterende i Kundereskontro) | Purring paa ubetalte fakturaer |
