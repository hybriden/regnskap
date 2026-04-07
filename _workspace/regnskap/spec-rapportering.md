# Spesifikasjon: Rapportering (Financial Reporting)

**Modul:** Rapportering
**Status:** Komplett spesifikasjon
**Avhengigheter:** Kontoplan (implementert), Hovedbok (implementert), Bilagsregistrering (implementert), MVA (implementert), Leverandorreskontro (implementert), Kundereskontro (implementert), Bank (implementert)
**SAF-T-seksjon:** Komplett AuditFile (Header, MasterFiles, GeneralLedgerEntries)
**Lovgrunnlag:** Regnskapsloven 3-1, 3-2, 3-2a, 3-3; Bokforingsloven 4, 5; Bokforingsforskriften 3-1, 7-8; SAF-T v1.30

---

## Oversikt

Rapporteringsmodulen genererer alle lovpakrevde og operasjonelle finansielle rapporter fra data i hovedbok, reskontro og kontoplan. Modulen LESER data -- den skriver aldri til hovedbok. Alle rapporter er sporbare tilbake til individuelle bilag (NBS 2 kontrollspor).

Ni rapporttyper:

1. **Resultatregnskap** -- Regnskapsloven 3-2
2. **Balanse** -- Regnskapsloven 3-2a
3. **Kontantstromoppstilling** -- indirekte metode
4. **Saldobalanse** -- utvidet rapportformat (bygger pa HovedbokService.HentSaldobalanseAsync)
5. **Hovedboksutskrift** -- kontospesifikasjon per konto (Bokforingsforskriften 3-1)
6. **SAF-T eksport** -- komplett SAF-T Financial XML v1.30
7. **Dimensjonsrapporter** -- avdeling/prosjekt-oppsummering
8. **Sammenligning** -- periodevergelijking og budsjettsammenligning
9. **Nokkeltall** -- finansielle nøkkeltall

---

## Datamodell

### Nye entiteter

#### RapportKonfigurasjon

```csharp
namespace Regnskap.Domain.Features.Rapportering;

using Regnskap.Domain.Common;

/// <summary>
/// Konfigurasjon for firmaets rapporteringsoppsett.
/// Lagrer firmainfo som brukes i SAF-T header og rapportoverskrifter.
/// </summary>
public class RapportKonfigurasjon : AuditableEntity
{
    /// <summary>
    /// Firmanavn.
    /// </summary>
    public string Firmanavn { get; set; } = default!;

    /// <summary>
    /// Organisasjonsnummer (9 siffer).
    /// </summary>
    public string Organisasjonsnummer { get; set; } = default!;

    /// <summary>
    /// Adresselinje 1.
    /// </summary>
    public string Adresse { get; set; } = default!;

    /// <summary>
    /// Postnummer.
    /// </summary>
    public string Postnummer { get; set; } = default!;

    /// <summary>
    /// Poststed.
    /// </summary>
    public string Poststed { get; set; } = default!;

    /// <summary>
    /// Landskode (ISO 3166-1 alpha-2). Default "NO".
    /// </summary>
    public string Landskode { get; set; } = "NO";

    /// <summary>
    /// MVA-registrert (ja/nei). Styrer SAF-T TaxRegistration.
    /// </summary>
    public bool ErMvaRegistrert { get; set; }

    /// <summary>
    /// Kontaktperson for rapporter.
    /// </summary>
    public string? Kontaktperson { get; set; }

    /// <summary>
    /// Telefon.
    /// </summary>
    public string? Telefon { get; set; }

    /// <summary>
    /// E-post.
    /// </summary>
    public string? Epost { get; set; }

    /// <summary>
    /// Bankkontonummer (for SAF-T header).
    /// </summary>
    public string? Bankkontonummer { get; set; }

    /// <summary>
    /// IBAN (for SAF-T header).
    /// </summary>
    public string? Iban { get; set; }

    /// <summary>
    /// Om foretaket kvalifiserer som sma foretak (NRS 8).
    /// Pavirker hvilke rapporter som er obligatoriske.
    /// </summary>
    public bool ErSmaForetak { get; set; }

    /// <summary>
    /// Regnskapsvaluta. Default "NOK".
    /// </summary>
    public string Valuta { get; set; } = "NOK";
}
```

#### Budsjett

```csharp
namespace Regnskap.Domain.Features.Rapportering;

using Regnskap.Domain.Common;

/// <summary>
/// Budsjett for en konto i en periode. Brukes til budsjettsammenligning.
/// </summary>
public class Budsjett : AuditableEntity
{
    /// <summary>
    /// Kontonummer (NS 4102).
    /// </summary>
    public string Kontonummer { get; set; } = default!;

    /// <summary>
    /// Regnskapsaret.
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Periode (1-12). Periode 0 = arsbudsjett (hele aret).
    /// </summary>
    public int Periode { get; set; }

    /// <summary>
    /// Budsjettert belop. Positivt for debet-normalbalanse, negativt for kredit-normalbalanse.
    /// </summary>
    public decimal Belop { get; set; }

    /// <summary>
    /// Valgfritt budsjett-navn/versjon (f.eks. "Opprinnelig", "Revidert Q2").
    /// </summary>
    public string Versjon { get; set; } = "Opprinnelig";

    /// <summary>
    /// Fritekst merknad.
    /// </summary>
    public string? Merknad { get; set; }
}
```

#### RapportLogg

```csharp
namespace Regnskap.Domain.Features.Rapportering;

using Regnskap.Domain.Common;

/// <summary>
/// Logger alle genererte rapporter for sporbarhet og revisjon.
/// </summary>
public class RapportLogg : AuditableEntity
{
    /// <summary>
    /// Rapporttype (enum).
    /// </summary>
    public RapportType Type { get; set; }

    /// <summary>
    /// Regnskapsar rapporten dekker.
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Fra-periode (1-12, eller null for hele aret).
    /// </summary>
    public int? FraPeriode { get; set; }

    /// <summary>
    /// Til-periode (1-12, eller null for hele aret).
    /// </summary>
    public int? TilPeriode { get; set; }

    /// <summary>
    /// Tidspunkt for generering.
    /// </summary>
    public DateTime GenererTidspunkt { get; set; }

    /// <summary>
    /// Hvem som genererte rapporten.
    /// </summary>
    public string GenererAv { get; set; } = default!;

    /// <summary>
    /// Rapportparametre serialisert som JSON.
    /// </summary>
    public string? Parametre { get; set; }

    /// <summary>
    /// Kontrollsum (SHA-256 hash) av generert innhold.
    /// Sikrer etterprøvbarhet -- kan verifisere at rapport ikke er endret.
    /// </summary>
    public string? Kontrollsum { get; set; }
}
```

### Enums

```csharp
namespace Regnskap.Domain.Features.Rapportering;

public enum RapportType
{
    Resultatregnskap,
    Balanse,
    Kontantstromoppstilling,
    Saldobalanse,
    Hovedboksutskrift,
    SaftEksport,
    Dimensjonsrapport,
    Sammenligning,
    Nokkeltall
}

/// <summary>
/// Format for resultatregnskapet ihht Regnskapsloven 3-2.
/// </summary>
public enum ResultatregnskapFormat
{
    /// <summary>Artsinndelт (standard for sma foretak).</summary>
    Artsinndelt,

    /// <summary>Funksjonsinndelt (valgfritt for store foretak).</summary>
    Funksjonsinndelt
}
```

### EF Core-konfigurasjon

```csharp
// RapportKonfigurasjonConfiguration
builder.ToTable("RapportKonfigurasjon");
builder.HasKey(e => e.Id);
builder.Property(e => e.Firmanavn).IsRequired().HasMaxLength(200);
builder.Property(e => e.Organisasjonsnummer).IsRequired().HasMaxLength(9);
builder.Property(e => e.Adresse).IsRequired().HasMaxLength(200);
builder.Property(e => e.Postnummer).IsRequired().HasMaxLength(4);
builder.Property(e => e.Poststed).IsRequired().HasMaxLength(100);
builder.Property(e => e.Landskode).IsRequired().HasMaxLength(2).HasDefaultValue("NO");
builder.Property(e => e.Valuta).IsRequired().HasMaxLength(3).HasDefaultValue("NOK");

// BudsjettConfiguration
builder.ToTable("Budsjett");
builder.HasKey(e => e.Id);
builder.Property(e => e.Kontonummer).IsRequired().HasMaxLength(6);
builder.Property(e => e.Belop).HasPrecision(18, 2);
builder.Property(e => e.Versjon).IsRequired().HasMaxLength(50);
builder.HasIndex(e => new { e.Kontonummer, e.Ar, e.Periode, e.Versjon }).IsUnique();

// RapportLoggConfiguration
builder.ToTable("RapportLogg");
builder.HasKey(e => e.Id);
builder.Property(e => e.GenererAv).IsRequired().HasMaxLength(100);
builder.Property(e => e.Kontrollsum).HasMaxLength(64);
builder.HasIndex(e => new { e.Type, e.Ar });
```

---

## API-kontrakt

### 1. Resultatregnskap

#### `GET /api/rapporter/resultatregnskap`

Genererer resultatregnskap ihht Regnskapsloven 3-2.

**Query-parametre:**

| Parameter | Type | Pakreves | Beskrivelse |
|---|---|---|---|
| ar | int | Ja | Regnskapsar |
| fraPeriode | int | Nei | Startperiode (default: 1) |
| tilPeriode | int | Nei | Sluttperiode (default: 12) |
| format | string | Nei | "artsinndelt" (default) eller "funksjonsinndelt" |
| inkluderForrigeAr | bool | Nei | Vis forrige ar som sammenligning (default: true) |

**Response: `ResultatregnskapDto`**

```csharp
public record ResultatregnskapDto(
    int Ar,
    int FraPeriode,
    int TilPeriode,
    string Format,
    List<ResultatregnskapSeksjonDto> Seksjoner,
    decimal Driftsresultat,
    decimal FinansresultatNetto,
    decimal OrdnaertResultatForSkatt,
    decimal Skattekostnad,
    decimal Arsresultat,
    // Forrige ar (sammenligning)
    decimal? ForrigeArDriftsresultat,
    decimal? ForrigeArArsresultat
);

public record ResultatregnskapSeksjonDto(
    string Kode,
    string Navn,
    List<ResultatregnskapLinjeDto> Linjer,
    decimal Sum,
    decimal? ForrigeArSum
);

public record ResultatregnskapLinjeDto(
    string Kontonummer,
    string Kontonavn,
    decimal Belop,
    decimal? ForrigeArBelop,
    bool ErSummeringslinje
);
```

**Seksjonskoder for artsinndelt format (Regnskapsloven 3-2):**

| Kode | Seksjon | Kontoklasser |
|---|---|---|
| DRIFTSINNTEKTER | Driftsinntekter | 30-39 |
| VAREKOSTNAD | Varekostnad | 40-49 |
| LONNSKOSTNAD | Lonnskostnad | 50-59 |
| AVSKRIVNING | Av- og nedskrivning | 60 |
| ANNEN_DRIFT | Annen driftskostnad | 61-79 |
| DRIFTSRESULTAT | Driftsresultat | (beregnet) |
| FINANSINNTEKT | Finansinntekter | 80-83 |
| FINANSKOSTNAD | Finanskostnader | 84-87 |
| FINANSNETTO | Netto finans | (beregnet) |
| RESULTAT_FOR_SKATT | Ordinaert resultat for skatt | (beregnet) |
| SKATTEKOSTNAD | Skattekostnad | 89 |
| ARSRESULTAT | Arsresultat | (beregnet) |

---

### 2. Balanse

#### `GET /api/rapporter/balanse`

Genererer balanse ihht Regnskapsloven 3-2a.

**Query-parametre:**

| Parameter | Type | Pakreves | Beskrivelse |
|---|---|---|---|
| ar | int | Ja | Regnskapsar |
| periode | int | Nei | Periode (default: 12 = arsavslutning) |
| inkluderForrigeAr | bool | Nei | Sammenligning mot forrige ar (default: true) |

**Response: `BalanseDto`**

```csharp
public record BalanseDto(
    int Ar,
    int Periode,
    BalanseSideDto Eiendeler,
    BalanseSideDto EgenkapitalOgGjeld,
    decimal SumEiendeler,
    decimal SumEgenkapitalOgGjeld,
    bool ErIBalanse,
    // Forrige ar
    decimal? ForrigeArSumEiendeler,
    decimal? ForrigeArSumEgenkapitalOgGjeld
);

public record BalanseSideDto(
    List<BalanseSeksjonDto> Seksjoner,
    decimal Sum,
    decimal? ForrigeArSum
);

public record BalanseSeksjonDto(
    string Kode,
    string Navn,
    List<BalanseLinjeDto> Linjer,
    decimal Sum,
    decimal? ForrigeArSum
);

public record BalanseLinjeDto(
    string Kontonummer,
    string Kontonavn,
    decimal Belop,
    decimal? ForrigeArBelop,
    bool ErSummeringslinje
);
```

**Seksjonskoder for balanse (Regnskapsloven 3-2a):**

| Side | Kode | Seksjon | Kontogrupper |
|---|---|---|---|
| Eiendeler | IMMATR_ANLEGG | Immaterielle eiendeler | 10 |
| Eiendeler | VARIGE_ANLEGG | Varige driftsmidler | 11-12 |
| Eiendeler | FIN_ANLEGG | Finansielle anleggsmidler | 13 |
| Eiendeler | SUM_ANLEGG | Sum anleggsmidler | (beregnet) |
| Eiendeler | VARER | Varer | 14 |
| Eiendeler | FORDRINGER | Fordringer | 15-17 |
| Eiendeler | INVESTERING | Investeringer | 18 |
| Eiendeler | BANK_KONTANT | Bankinnskudd, kontanter | 19 |
| Eiendeler | SUM_OMLOPSMIDLER | Sum omlopsmidler | (beregnet) |
| Eiendeler | SUM_EIENDELER | Sum eiendeler | (beregnet) |
| EK+Gjeld | INNSKUTT_EK | Innskutt egenkapital | 20 |
| EK+Gjeld | OPPTJENT_EK | Opptjent egenkapital | 21 |
| EK+Gjeld | SUM_EK | Sum egenkapital | (beregnet) |
| EK+Gjeld | LANGSIKTIG_GJELD | Langsiktig gjeld | 22-23 |
| EK+Gjeld | LEVERANDOR_GJELD | Leverandorgjeld | 24 |
| EK+Gjeld | OFFENTLIG_GJELD | Skattetrekk, offentlige avgifter | 25-27 |
| EK+Gjeld | ANNEN_KORT_GJELD | Annen kortsiktig gjeld | 28-29 |
| EK+Gjeld | SUM_KORT_GJELD | Sum kortsiktig gjeld | (beregnet) |
| EK+Gjeld | SUM_GJELD | Sum gjeld | (beregnet) |
| EK+Gjeld | SUM_EK_GJELD | Sum egenkapital og gjeld | (beregnet) |

---

### 3. Kontantstromoppstilling

#### `GET /api/rapporter/kontantstrom`

Kontantstromoppstilling med indirekte metode.

**Query-parametre:**

| Parameter | Type | Pakreves | Beskrivelse |
|---|---|---|---|
| ar | int | Ja | Regnskapsar |
| inkluderForrigeAr | bool | Nei | Sammenligning (default: true) |

**Response: `KontantstromDto`**

```csharp
public record KontantstromDto(
    int Ar,
    KontantstromSeksjonDto Drift,
    KontantstromSeksjonDto Investering,
    KontantstromSeksjonDto Finansiering,
    decimal NettoEndringLikvider,
    decimal LikviderIB,
    decimal LikviderUB,
    // Forrige ar
    decimal? ForrigeArNettoEndring
);

public record KontantstromSeksjonDto(
    string Navn,
    List<KontantstromLinjeDto> Linjer,
    decimal Sum,
    decimal? ForrigeArSum
);

public record KontantstromLinjeDto(
    string Beskrivelse,
    decimal Belop,
    decimal? ForrigeArBelop
);
```

**Indirekte metode -- beregningslogikk:**

```
DRIFT:
  Arsresultat (fra resultatregnskap)
  + Avskrivninger og nedskrivninger (konto 60xx)
  + Tap pa fordringer (konto 78xx)
  +/- Endring kundefordringer (konto 1500-1599)
  +/- Endring varelager (konto 14xx)
  +/- Endring leverandorgjeld (konto 24xx)
  +/- Endring offentlig gjeld (konto 25-27xx)
  +/- Endring andre driftsrelaterte poster
  = Netto kontantstrom fra drift

INVESTERING:
  - Kjop av varige driftsmidler (okning konto 10-12xx)
  + Salg av varige driftsmidler (reduksjon konto 10-12xx)
  - Kjop av finansielle anleggsmidler (okning konto 13xx)
  + Salg av finansielle anleggsmidler (reduksjon konto 13xx)
  = Netto kontantstrom fra investering

FINANSIERING:
  + Opptak av ny langsiktig gjeld (okning konto 22-23xx)
  - Nedbetaling av langsiktig gjeld (reduksjon konto 22-23xx)
  + Kapitalinnskudd (okning konto 20xx)
  - Utbytte betalt (konto 8800-serien / 2800)
  = Netto kontantstrom fra finansiering

KONTROLL:
  Netto endring = Drift + Investering + Finansiering
  Likvider UB = Likvider IB + Netto endring
  Likvider = Sum konto 19xx (bank + kontanter)
```

---

### 4. Saldobalanse (utvidet)

#### `GET /api/rapporter/saldobalanse`

Bygger videre pa eksisterende `HovedbokService.HentSaldobalanseAsync` med rapporttilpasninger.

**Query-parametre:**

| Parameter | Type | Pakreves | Beskrivelse |
|---|---|---|---|
| ar | int | Ja | Regnskapsar |
| fraPeriode | int | Nei | Startperiode (default: 1) |
| tilPeriode | int | Nei | Sluttperiode (default: 12) |
| inkluderNullsaldo | bool | Nei | Inkluder kontoer uten bevegelse (default: false) |
| kontoklasse | int | Nei | Filtrer pa kontoklasse (1-8) |
| gruppert | bool | Nei | Grupper per kontogruppe med subtotaler (default: false) |

**Response: `SaldobalanseRapportDto`**

```csharp
public record SaldobalanseRapportDto(
    int Ar,
    int FraPeriode,
    int TilPeriode,
    List<SaldobalanseGruppeDto> Grupper,
    SaldobalanseTotalerDto Totaler,
    bool DebetKredittStemmer
);

public record SaldobalanseGruppeDto(
    int Gruppekode,
    string Gruppenavn,
    List<SaldobalanseRapportLinjeDto> Linjer,
    decimal GruppeIB,
    decimal GruppeSumDebet,
    decimal GruppeSumKredit,
    decimal GruppeUB
);

public record SaldobalanseRapportLinjeDto(
    string Kontonummer,
    string Kontonavn,
    string Kontotype,
    decimal InngaendeBalanse,
    decimal SumDebet,
    decimal SumKredit,
    decimal Endring,
    decimal UtgaendeBalanse
);

public record SaldobalanseTotalerDto(
    decimal TotalIB,
    decimal TotalDebet,
    decimal TotalKredit,
    decimal TotalUB,
    decimal DebetSaldo,
    decimal KreditSaldo
);
```

---

### 5. Hovedboksutskrift

#### `GET /api/rapporter/hovedboksutskrift`

Kontospesifikasjon per konto -- oppfyller Bokforingsforskriften 3-1.

**Query-parametre:**

| Parameter | Type | Pakreves | Beskrivelse |
|---|---|---|---|
| ar | int | Ja | Regnskapsar |
| fraPeriode | int | Nei | Startperiode (default: 1) |
| tilPeriode | int | Nei | Sluttperiode (default: 12) |
| fraKonto | string | Nei | Fra kontonummer (inklusiv) |
| tilKonto | string | Nei | Til kontonummer (inklusiv) |

**Response: `HovedboksutskriftDto`**

```csharp
public record HovedboksutskriftDto(
    int Ar,
    int FraPeriode,
    int TilPeriode,
    List<KontoUtskriftDto> Kontoer
);

public record KontoUtskriftDto(
    string Kontonummer,
    string Kontonavn,
    string Kontotype,
    string Normalbalanse,
    decimal InngaendeBalanse,
    List<PosteringUtskriftDto> Posteringer,
    decimal SumDebet,
    decimal SumKredit,
    decimal UtgaendeBalanse,
    int AntallPosteringer
);

public record PosteringUtskriftDto(
    DateOnly Dato,
    string BilagsId,
    string Beskrivelse,
    int Linjenummer,
    string Side,
    decimal Belop,
    decimal LopendeSaldo,
    string? MvaKode,
    string? Avdelingskode,
    string? Prosjektkode,
    string? Motkontonummer
);
```

**Forretningsregel:** Rapporten skal vise motkontonummer for 2-linjers bilag. For bilag med 3+ linjer vises "Diverse" som motkonto.

---

### 6. SAF-T eksport

#### `POST /api/rapporter/saft`

Genererer komplett SAF-T Financial XML v1.30.

**Request body:**

```csharp
public record SaftEksportRequest(
    int Ar,
    int FraPeriode = 1,
    int TilPeriode = 12,
    string TaxAccountingBasis = "A"  // "A" = general, "S" = tax
);
```

**Response:** XML-fil (application/xml) med Content-Disposition for nedlasting.

**SAF-T struktur som genereres:**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<AuditFile xmlns="urn:StandardAuditFile-Taxation-Financial:NO">
  <Header>
    <AuditFileVersion>1.30</AuditFileVersion>
    <AuditFileCountry>NO</AuditFileCountry>
    <AuditFileDateCreated>{genereringsdato}</AuditFileDateCreated>
    <SoftwareCompanyName>Regnskap AS</SoftwareCompanyName>
    <SoftwareID>Regnskap</SoftwareID>
    <SoftwareVersion>{versjon}</SoftwareVersion>
    <Company>
      <RegistrationNumber>{orgnr}</RegistrationNumber>
      <Name>{firmanavn}</Name>
      <Address>
        <StreetName>{adresse}</StreetName>
        <PostalCode>{postnr}</PostalCode>
        <City>{poststed}</City>
        <Country>{landskode}</Country>
      </Address>
      <TaxRegistration>
        <TaxRegistrationNumber>{orgnr}MVA</TaxRegistrationNumber>
        <TaxAuthority>Skatteetaten</TaxAuthority>
      </TaxRegistration>
    </Company>
    <DefaultCurrencyCode>NOK</DefaultCurrencyCode>
    <SelectionCriteria>
      <SelectionStartDate>{fra-dato}</SelectionStartDate>
      <SelectionEndDate>{til-dato}</SelectionEndDate>
    </SelectionCriteria>
    <TaxAccountingBasis>{basis}</TaxAccountingBasis>
  </Header>
  <MasterFiles>
    <GeneralLedgerAccounts>...</GeneralLedgerAccounts>
    <Customers>...</Customers>
    <Suppliers>...</Suppliers>
    <TaxTable>...</TaxTable>
    <AnalysisTypeTable>...</AnalysisTypeTable>
  </MasterFiles>
  <GeneralLedgerEntries>
    <NumberOfEntries>{antall}</NumberOfEntries>
    <TotalDebit>{totalDebet}</TotalDebit>
    <TotalCredit>{totalKredit}</TotalCredit>
    <Journal>...</Journal>
  </GeneralLedgerEntries>
</AuditFile>
```

**SAF-T mapping fra domenemodeller:**

| Domenemodell | SAF-T element |
|---|---|
| Konto.Kontonummer | GeneralLedgerAccounts/Account/AccountID |
| Konto.Navn | Account/AccountDescription |
| Konto.StandardAccountId | Account/StandardAccountID |
| Konto.GrupperingsKategori | Account/GroupingCategory |
| Konto.GrupperingsKode | Account/GroupingCode |
| KontoSaldo.InngaendeBalanse | Account/OpeningDebitBalance eller OpeningCreditBalance |
| KontoSaldo.UtgaendeBalanse | Account/ClosingDebitBalance eller ClosingCreditBalance |
| MvaKode.Kode | TaxCodeDetails/TaxCode |
| MvaKode.StandardTaxCode | TaxCodeDetails/StandardTaxCode |
| MvaKode.Sats | TaxCodeDetails/TaxPercentage |
| BilagSerie -> Journal | Journal/JournalID, Type |
| Bilag.BilagsId | Transaction/TransactionID |
| Bilag.Bilagsdato | Transaction/TransactionDate |
| Bilag.Registreringsdato | Transaction/SystemEntryDate |
| Bilag.BokfortTidspunkt | Transaction/GLPostingDate |
| Bilag.SaftPeriode | Transaction/Period |
| Postering.Linjenummer | Line/RecordID |
| Postering.Kontonummer | Line/AccountID |
| Postering.Belop (Debet) | Line/DebitAmount/Amount |
| Postering.Belop (Kredit) | Line/CreditAmount/Amount |
| Postering.MvaKode | Line/TaxInformation/TaxCode |
| Postering.MvaSats | Line/TaxInformation/TaxPercentage |
| Postering.MvaGrunnlag | Line/TaxInformation/TaxBase |
| Postering.MvaBelop | Line/TaxInformation/TaxAmount |
| Postering.KundeId | Line/CustomerID |
| Postering.LeverandorId | Line/SupplierID |

**Valideringsregler for SAF-T:**
1. Sum av alle Transaction/Line debet = TotalDebit i GeneralLedgerEntries
2. Sum av alle Transaction/Line kredit = TotalCredit i GeneralLedgerEntries
3. Alle kontoer med posteringer MA ha en Account-oppforing i MasterFiles
4. Alle kunder/leverandorer referert i posteringer MA ha en oppforing i MasterFiles
5. Alle MVA-koder brukt i posteringer MA ha en TaxCodeDetails i TaxTable
6. Alle kontoer MA ha StandardAccountID-mapping (v1.30 obligatorisk)
7. Alle kontoer MA ha GroupingCategory og GroupingCode (v1.30 obligatorisk)
8. Opening/Closing balance for kunder og leverandorer er obligatorisk i v1.30
9. XML MA validere mot Skatteetatens XSD-skjema

---

### 7. Dimensjonsrapporter

#### `GET /api/rapporter/dimensjon`

Rapport per avdeling eller prosjekt.

**Query-parametre:**

| Parameter | Type | Pakreves | Beskrivelse |
|---|---|---|---|
| ar | int | Ja | Regnskapsar |
| fraPeriode | int | Nei | Startperiode (default: 1) |
| tilPeriode | int | Nei | Sluttperiode (default: 12) |
| dimensjon | string | Ja | "avdeling" eller "prosjekt" |
| kode | string | Nei | Filtrer pa spesifikk avdelings-/prosjektkode |
| kontoklasse | int | Nei | Filtrer pa kontoklasse |

**Response: `DimensjonsrapportDto`**

```csharp
public record DimensjonsrapportDto(
    int Ar,
    int FraPeriode,
    int TilPeriode,
    string Dimensjon,
    List<DimensjonsGruppeDto> Grupper,
    DimensjonsTotalerDto Totaler
);

public record DimensjonsGruppeDto(
    string Kode,
    string Navn,
    List<DimensjonsLinjeDto> Linjer,
    decimal SumDebet,
    decimal SumKredit,
    decimal Netto
);

public record DimensjonsLinjeDto(
    string Kontonummer,
    string Kontonavn,
    decimal SumDebet,
    decimal SumKredit,
    decimal Netto
);

public record DimensjonsTotalerDto(
    decimal TotalDebet,
    decimal TotalKredit,
    decimal TotalNetto,
    int AntallPosteringer
);
```

**Mapper til SAF-T AnalysisTypeTable:**

| Dimensjonstype | SAF-T AnalysisType |
|---|---|
| Avdeling | "Avdeling" / AnalysisID = Avdelingskode |
| Prosjekt | "Prosjekt" / AnalysisID = Prosjektkode |

---

### 8. Sammenligning

#### `GET /api/rapporter/sammenligning`

Periodevergelijking og budsjettsammenligning.

**Query-parametre:**

| Parameter | Type | Pakreves | Beskrivelse |
|---|---|---|---|
| ar | int | Ja | Regnskapsar |
| fraPeriode | int | Nei | Startperiode (default: 1) |
| tilPeriode | int | Nei | Sluttperiode (default: 12) |
| type | string | Ja | "forrige_ar" eller "budsjett" |
| budsjettVersjon | string | Nei | Budsjettversjon (default: "Opprinnelig") |
| kontoklasse | int | Nei | Filtrer pa kontoklasse |

**Response: `SammenligningDto`**

```csharp
public record SammenligningDto(
    int Ar,
    int FraPeriode,
    int TilPeriode,
    string Type,
    List<SammenligningLinjeDto> Linjer,
    SammenligningTotalerDto Totaler
);

public record SammenligningLinjeDto(
    string Kontonummer,
    string Kontonavn,
    string Kontotype,
    decimal Faktisk,
    decimal Sammenligning,
    decimal AvvikBelop,
    decimal AvvikProsent
);

public record SammenligningTotalerDto(
    decimal TotalFaktisk,
    decimal TotalSammenligning,
    decimal TotalAvvik,
    decimal TotalAvvikProsent
);
```

---

### 9. Nokkeltall

#### `GET /api/rapporter/nokkeltall`

Beregner finansielle nokkeltall.

**Query-parametre:**

| Parameter | Type | Pakreves | Beskrivelse |
|---|---|---|---|
| ar | int | Ja | Regnskapsar |
| periode | int | Nei | Periode (default: 12) |
| inkluderForrigeAr | bool | Nei | Sammenligning (default: true) |

**Response: `NokkeltallDto`**

```csharp
public record NokkeltallDto(
    int Ar,
    int Periode,
    LikviditetDto Likviditet,
    SoliditetDto Soliditet,
    LonnsomhetDto Lonnsomhet,
    NokkeltallDto? ForrigeAr
);

public record LikviditetDto(
    /// <summary>Likviditetsgrad 1 = Omlopsmidler / Kortsiktig gjeld</summary>
    decimal Likviditetsgrad1,
    /// <summary>Likviditetsgrad 2 = (Omlopsmidler - Varelager) / Kortsiktig gjeld</summary>
    decimal Likviditetsgrad2,
    /// <summary>Arbeidskapital = Omlopsmidler - Kortsiktig gjeld</summary>
    decimal Arbeidskapital
);

public record SoliditetDto(
    /// <summary>Egenkapitalandel = Egenkapital / Totalkapital * 100</summary>
    decimal Egenkapitalandel,
    /// <summary>Gjeldsgrad = Total gjeld / Egenkapital</summary>
    decimal Gjeldsgrad,
    /// <summary>Rentedekningsgrad = (Resultat for skatt + Rentekostnader) / Rentekostnader</summary>
    decimal Rentedekningsgrad
);

public record LonnsomhetDto(
    /// <summary>Totalkapitalrentabilitet = (Resultat for skatt + Rentekostnader) / Gjennomsnittlig totalkapital * 100</summary>
    decimal Totalkapitalrentabilitet,
    /// <summary>Egenkapitalrentabilitet = Arsresultat / Gjennomsnittlig egenkapital * 100</summary>
    decimal Egenkapitalrentabilitet,
    /// <summary>Resultatmargin = Driftsresultat / Driftsinntekter * 100</summary>
    decimal Resultatmargin,
    /// <summary>Driftsmargin = Driftsresultat / Driftsinntekter * 100</summary>
    decimal Driftsmargin
);
```

**Beregningslogikk for nokkeltall:**

```
LIKVIDITET:
  Omlopsmidler = Sum UB konto 14xx-19xx
  Varelager = Sum UB konto 14xx
  Kortsiktig gjeld = Sum UB konto 24xx-29xx

  Likviditetsgrad 1 = Omlopsmidler / Kortsiktig gjeld
  Likviditetsgrad 2 = (Omlopsmidler - Varelager) / Kortsiktig gjeld
  Arbeidskapital = Omlopsmidler - Kortsiktig gjeld

SOLIDITET:
  Egenkapital = Sum UB konto 20xx-21xx (absoluttverdi, kredit-normert)
  Total gjeld = Sum UB konto 22xx-29xx (absoluttverdi)
  Totalkapital = Sum UB konto 1xxx (eiendeler)
  Rentekostnader = Sum konto 84xx (absolutt)
  Resultat for skatt = Sum konto 30xx-87xx netto

  Egenkapitalandel = Egenkapital / Totalkapital * 100
  Gjeldsgrad = Total gjeld / Egenkapital
  Rentedekningsgrad = (Resultat for skatt + Rentekostnader) / Rentekostnader

LONNSOMHET:
  Driftsinntekter = Sum konto 30xx-39xx (absolutt)
  Driftsresultat = Driftsinntekter - Sum konto 40xx-79xx
  Arsresultat = Sum konto 30xx-89xx netto
  Gj.snitt totalkapital = (Totalkapital IB + Totalkapital UB) / 2
  Gj.snitt egenkapital = (Egenkapital IB + Egenkapital UB) / 2

  Totalkapitalrentabilitet = (Resultat for skatt + Rentekostnader) / Gj.snitt totalkapital * 100
  Egenkapitalrentabilitet = Arsresultat / Gj.snitt egenkapital * 100
  Resultatmargin = Driftsresultat / Driftsinntekter * 100
```

---

### Budsjett-endepunkter

#### `POST /api/budsjett`

Opprett/oppdater budsjettlinjer.

**Request:**

```csharp
public record OpprettBudsjettRequest(
    string Kontonummer,
    int Ar,
    int Periode,
    decimal Belop,
    string Versjon = "Opprinnelig",
    string? Merknad = null
);
```

#### `POST /api/budsjett/bulk`

Masseimport av budsjettlinjer.

**Request:**

```csharp
public record BudsjettBulkRequest(
    int Ar,
    string Versjon,
    List<BudsjettLinjeRequest> Linjer
);

public record BudsjettLinjeRequest(
    string Kontonummer,
    int Periode,
    decimal Belop
);
```

#### `GET /api/budsjett?ar={ar}&versjon={versjon}`

Hent budsjett for et ar.

---

## Forretningsregler

### Generelle regler

1. **FR-R01:** Alle rapporter er skrivebeskyttet -- de leser kun fra eksisterende data i hovedbok, reskontro og kontoplan. Ingen rapport endrer data.

2. **FR-R02:** Alle belop i resultatregnskap vises med korrekt fortegn: inntekter positivt, kostnader positivt (men trekkes fra). Arsresultat kan vaere positivt (overskudd) eller negativt (underskudd).

3. **FR-R03:** Alle belop i balansen vises som absoluttverdi. Eiendeler (kontoklasse 1) viser debet-saldo. EK og gjeld (kontoklasse 2) viser kredit-saldo. Negativ EK er mulig og vises eksplisitt.

4. **FR-R04:** Resultatregnskap summerer netto endring i perioden (ikke saldo). Balanse viser utgaende saldo per valgt periode.

5. **FR-R05:** Sammenligning med forrige ar henter KontoSaldo for (ar-1, tilPeriode). Konto-mapping gjores pa kontonummer.

6. **FR-R06:** Alle rapporter logger til RapportLogg med SHA-256 hash av innholdet for etterprøvbarhet.

### Resultatregnskap-regler

7. **FR-R07:** Beregning av poster per seksjon:
   - Driftsinntekter = Sum netto kredit konto 30xx-39xx (vises positivt)
   - Varekostnad = Sum netto debet konto 40xx-49xx (vises positivt)
   - Lonnskostnad = Sum netto debet konto 50xx-59xx (vises positivt)
   - Avskrivninger = Sum netto debet konto 60xx (vises positivt)
   - Annen driftskostnad = Sum netto debet konto 61xx-79xx (vises positivt)
   - Driftsresultat = Driftsinntekter - Varekostnad - Lonnskostnad - Avskrivninger - Annen driftskostnad
   - Finansinntekter = Sum netto kredit konto 80xx-83xx
   - Finanskostnader = Sum netto debet konto 84xx-87xx
   - Resultat for skatt = Driftsresultat + Finansinntekter - Finanskostnader
   - Skattekostnad = Sum netto debet konto 89xx
   - Arsresultat = Resultat for skatt - Skattekostnad

8. **FR-R08:** Artsinndelt format er standard for sma foretak. Funksjonsinndelt krever manuell mapping av kontoer til funksjoner.

### Balanse-regler

9. **FR-R09:** Eiendeler = Sum UB for kontoklasse 1, konvertert til absoluttverdi (debet = positiv).
   EK og gjeld = Sum UB for kontoklasse 2, konvertert til absoluttverdi (kredit = positiv).

10. **FR-R10:** Balansekontroll: SumEiendeler SKAL vaere lik SumEgenkapitalOgGjeld. Hvis ikke, logg advarsel men generer likevel rapporten med ErIBalanse = false.

### Kontantstrom-regler

11. **FR-R11:** Kontantstrom beregnes fra endringer i balansekontoer mellom perioder (indirekte metode). Utgangspunkt er arsresultat, som justeres for ikke-kontante poster.

12. **FR-R12:** Likvider = Sum UB for konto 19xx (bankinnskudd, kontanter). LikviderIB = Sum IB for konto 19xx i periode 1.

13. **FR-R13:** Kontrollregel: LikviderUB = LikviderIB + Drift + Investering + Finansiering. Avvik logges som advarsel.

### SAF-T regler

14. **FR-R14:** SAF-T-filen MA validere mot Skatteetatens XSD-skjema for v1.30. Produksjonen gjores i to trinn: (1) bygg intern XML-modell, (2) valider mot XSD, (3) returner bare ved gyldig validering.

15. **FR-R15:** Alle MasterFiles-data samles fra forste posteringsdato i perioden til siste. Kontoer, kunder, leverandorer og MVA-koder som aldri er brukt i perioden trenger IKKE inkluderes, men KAN inkluderes.

16. **FR-R16:** Transaksjon-ID (TransactionID) = Bilag.BilagsId. Linje-ID (RecordID) = Postering.Linjenummer (cast til string).

17. **FR-R17:** For split-filer (>2 GB): Header + MasterFiles i fil 1. GeneralLedgerEntries kan splittes over filer. Hver fil ma validere selvstendig.

### Nokkeltall-regler

18. **FR-R18:** Ved divisjon med null (f.eks. ingen kortsiktig gjeld for likviditetsgrad): returner null/0 og merk med "Ikke beregnet -- nevner er 0".

19. **FR-R19:** Gjennomsnittlig kapital beregnes som (IB + UB) / 2 for valgt ar. IB hentes fra periode 0 (apningsbalanse).

### Dimensjonsrapport-regler

20. **FR-R20:** Posteringer uten avdelings-/prosjektkode grupperes under "Uspesifisert" i dimensjonsrapporten.

21. **FR-R21:** Kun kontoer med KreverAvdeling/KreverProsjekt produserer dimensjonsdata. Andre kontoer vises kun i "Uspesifisert".

---

## MVA-handtering

Rapporteringsmodulen har ingen egen MVA-logikk. MVA-data leses fra:
- Postering.MvaKode, MvaBelop, MvaGrunnlag, MvaSats
- MVA-modulens MvaOppgjor og MvaOppgjorLinje

SAF-T TaxTable genereres fra MvaKode-entiteter i Kontoplan-modulen. TaxInformation per linje i GeneralLedgerEntries genereres fra Postering-data.

---

## Avhengigheter

### Brukte interfaces fra andre moduler

| Interface/Service | Modul | Bruk |
|---|---|---|
| IHovedbokRepository | Hovedbok | Hent KontoSaldo, Posteringer, Bilag, Perioder |
| IKontoService | Kontoplan | Hent Konto, Kontogruppe |
| IKontoRepository | Kontoplan | Hent alle kontoer, kontogrupper, MVA-koder |
| IKundeRepository | Kundereskontro | Hent kunder for SAF-T MasterFiles |
| ILeverandorRepository | Leverandorreskontro | Hent leverandorer for SAF-T MasterFiles |
| IBilagRepository | Bilagsregistrering | Hent bilagserier for SAF-T Journal-mapping |

### Nye interfaces

```csharp
namespace Regnskap.Application.Features.Rapportering;

public interface IRapporteringService
{
    // Resultatregnskap
    Task<ResultatregnskapDto> GenererResultatregnskapAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        ResultatregnskapFormat format = ResultatregnskapFormat.Artsinndelt,
        bool inkluderForrigeAr = true,
        CancellationToken ct = default);

    // Balanse
    Task<BalanseDto> GenererBalanseAsync(
        int ar, int periode = 12,
        bool inkluderForrigeAr = true,
        CancellationToken ct = default);

    // Kontantstrom
    Task<KontantstromDto> GenererKontantstromAsync(
        int ar,
        bool inkluderForrigeAr = true,
        CancellationToken ct = default);

    // Saldobalanse (utvidet)
    Task<SaldobalanseRapportDto> GenererSaldobalanseRapportAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        bool inkluderNullsaldo = false,
        int? kontoklasse = null,
        bool gruppert = false,
        CancellationToken ct = default);

    // Hovedboksutskrift
    Task<HovedboksutskriftDto> GenererHovedboksutskriftAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        string? fraKonto = null, string? tilKonto = null,
        CancellationToken ct = default);

    // Dimensjonsrapport
    Task<DimensjonsrapportDto> GenererDimensjonsrapportAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        string dimensjon = "avdeling",
        string? kode = null,
        int? kontoklasse = null,
        CancellationToken ct = default);

    // Sammenligning
    Task<SammenligningDto> GenererSammenligningAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        string type = "forrige_ar",
        string budsjettVersjon = "Opprinnelig",
        int? kontoklasse = null,
        CancellationToken ct = default);

    // Nokkeltall
    Task<NokkeltallDto> GenererNokkeltallAsync(
        int ar, int periode = 12,
        bool inkluderForrigeAr = true,
        CancellationToken ct = default);
}

public interface ISaftEksportService
{
    /// <summary>
    /// Genererer komplett SAF-T Financial XML v1.30.
    /// </summary>
    Task<Stream> GenererSaftXmlAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        string taxAccountingBasis = "A",
        CancellationToken ct = default);

    /// <summary>
    /// Validerer generert SAF-T mot XSD-skjema.
    /// Returnerer liste med valideringsfeil (tom = gyldig).
    /// </summary>
    Task<List<string>> ValiderSaftXmlAsync(
        Stream xmlStream,
        CancellationToken ct = default);
}

public interface IBudsjettService
{
    Task<BudsjettDto> OpprettBudsjettLinjeAsync(OpprettBudsjettRequest request, CancellationToken ct = default);
    Task<List<BudsjettDto>> BulkImportAsync(BudsjettBulkRequest request, CancellationToken ct = default);
    Task<List<BudsjettDto>> HentBudsjettAsync(int ar, string versjon = "Opprinnelig", CancellationToken ct = default);
    Task SlettBudsjettAsync(int ar, string versjon, CancellationToken ct = default);
}

public interface IRapporteringRepository
{
    // Budsjett
    Task<Budsjett?> HentBudsjettLinjeAsync(string kontonummer, int ar, int periode, string versjon, CancellationToken ct = default);
    Task<List<Budsjett>> HentBudsjettForArAsync(int ar, string versjon, CancellationToken ct = default);
    Task LeggTilBudsjettAsync(Budsjett budsjett, CancellationToken ct = default);
    Task SlettBudsjettForArAsync(int ar, string versjon, CancellationToken ct = default);

    // Konfigurasjon
    Task<RapportKonfigurasjon?> HentKonfigurasjonAsync(CancellationToken ct = default);
    Task LagreKonfigurasjonAsync(RapportKonfigurasjon konfigurasjon, CancellationToken ct = default);

    // Rapportlogg
    Task LeggTilRapportLoggAsync(RapportLogg logg, CancellationToken ct = default);
    Task<List<RapportLogg>> HentRapportLoggerAsync(int ar, RapportType? type = null, CancellationToken ct = default);

    // Aggregeringsspørringer (ytelsesoptimalisert)
    Task<List<KontoSaldoAggregat>> HentAggregerteSaldoerAsync(
        int ar, int fraPeriode, int tilPeriode,
        int? kontoklasse = null,
        CancellationToken ct = default);

    Task<List<DimensjonsSaldoAggregat>> HentDimensjonsSaldoerAsync(
        int ar, int fraPeriode, int tilPeriode,
        string dimensjon,
        string? kode = null,
        int? kontoklasse = null,
        CancellationToken ct = default);

    Task LagreEndringerAsync(CancellationToken ct = default);
}
```

**Hjelpetyper for aggregering:**

```csharp
public record KontoSaldoAggregat(
    string Kontonummer,
    string Kontonavn,
    string Kontotype,
    string Normalbalanse,
    int Gruppekode,
    string Gruppenavn,
    decimal InngaendeBalanse,
    decimal SumDebet,
    decimal SumKredit,
    decimal UtgaendeBalanse
);

public record DimensjonsSaldoAggregat(
    string DimensjonsKode,
    string Kontonummer,
    string Kontonavn,
    decimal SumDebet,
    decimal SumKredit,
    decimal Netto
);
```

---

## Eksempler

### Eksempel: Resultatregnskap

Gitt et selskap med disse posteringer i 2026:

| Konto | Beskrivelse | Debet | Kredit |
|---|---|---|---|
| 3000 | Salgsinntekt | | 1 000 000 |
| 4000 | Varekjop | 400 000 | |
| 5000 | Lonn | 300 000 | |
| 6000 | Avskrivning | 50 000 | |
| 6300 | Husleie | 100 000 | |
| 8000 | Renteinntekt | | 5 000 |
| 8400 | Rentekostnad | 20 000 | |
| 8900 | Skattekostnad | 37 500 | |

Resultatregnskap:
```
Driftsinntekter:
  3000 Salgsinntekt                    1 000 000
  Sum driftsinntekter                  1 000 000

Varekostnad:
  4000 Varekjop                          400 000

Lonnskostnad:
  5000 Lonn                              300 000

Av- og nedskrivning:
  6000 Avskrivning                        50 000

Annen driftskostnad:
  6300 Husleie                           100 000

Driftsresultat:                          150 000

Finansinntekter:
  8000 Renteinntekt                        5 000

Finanskostnader:
  8400 Rentekostnad                       20 000

Netto finans:                            -15 000

Resultat for skatt:                      135 000

Skattekostnad:
  8900 Skattekostnad                      37 500

Arsresultat:                              97 500
```

### Eksempel: Balanse

| Konto | Beskrivelse | UB |
|---|---|---|
| 1200 | Maskiner | 250 000 (debet) |
| 1500 | Kundefordringer | 150 000 (debet) |
| 1920 | Bank | 300 000 (debet) |
| 2000 | Aksjekapital | 100 000 (kredit) |
| 2050 | Overkurs | 50 000 (kredit) |
| 2100 | Annen EK / opptjent | 250 000 (kredit) |
| 2400 | Leverandorgjeld | 200 000 (kredit) |
| 2600 | Skyldig MVA | 62 500 (kredit) |
| 2900 | Annen gjeld | 37 500 (kredit) |

Balanse:
```
EIENDELER
  Anleggsmidler:
    Varige driftsmidler:
      1200 Maskiner                      250 000
    Sum anleggsmidler:                   250 000

  Omlopsmidler:
    Fordringer:
      1500 Kundefordringer               150 000
    Bankinnskudd:
      1920 Bank                          300 000
    Sum omlopsmidler:                    450 000

  SUM EIENDELER:                         700 000

EGENKAPITAL OG GJELD
  Egenkapital:
    Innskutt EK:
      2000 Aksjekapital                  100 000
      2050 Overkurs                       50 000
    Opptjent EK:
      2100 Annen EK                      250 000
    Sum egenkapital:                     400 000

  Gjeld:
    Leverandorgjeld:
      2400 Leverandorgjeld               200 000
    Offentlig gjeld:
      2600 Skyldig MVA                    62 500
    Annen kortsiktig gjeld:
      2900 Annen gjeld                    37 500
    Sum kortsiktig gjeld:                300 000

  SUM EGENKAPITAL OG GJELD:             700 000
  Balansekontroll: OK (700 000 = 700 000)
```
