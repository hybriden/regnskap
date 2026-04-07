# Spesifikasjon: Kontoplan (Chart of Accounts)

**Modul:** Kontoplan
**Status:** Komplett spesifikasjon
**Avhengigheter:** Ingen (grunnmodul)
**SAF-T-seksjon:** MasterFiles > GeneralLedgerAccounts

---

## Datamodell

### Enums

```csharp
namespace Regnskap.Domain.Kontoplan;

/// <summary>
/// NS 4102 kontoklasser (1-8).
/// </summary>
public enum Kontoklasse
{
    Eiendeler = 1,
    EgenkapitalOgGjeld = 2,
    Salgsinntekt = 3,
    Varekostnad = 4,
    Lonnskostnad = 5,
    AvskrivningOgAnnenDriftskostnad = 6,
    AnnenDriftskostnad = 7,
    FinansposterSkatt = 8
}

/// <summary>
/// Kontotype bestemmer debet/kredit-oppforsel og hvilken rapport kontoen tilhorer.
/// </summary>
public enum Kontotype
{
    Eiendel,        // Balanse, oker med debet
    Gjeld,          // Balanse, oker med kredit
    Egenkapital,    // Balanse, oker med kredit
    Inntekt,        // Resultat, oker med kredit
    Kostnad         // Resultat, oker med debet
}

/// <summary>
/// Normalbalanse for kontoen. Brukes til a bestemme fortegn i rapporter.
/// </summary>
public enum Normalbalanse
{
    Debet,
    Kredit
}

/// <summary>
/// SAF-T GroupingCategory for norsk skatterapportering.
/// </summary>
public enum GrupperingsKategori
{
    RF1167,     // Naringsoppgave 1
    RF1175,     // Naringsoppgave 2
    RF1323      // Naringsoppgave for sma foretak (NRS 8)
}
```

### Entity: Kontogruppe

Representerer en kontogruppe i NS 4102-hierarkiet (f.eks. gruppe 10: Immaterielle eiendeler).

```csharp
namespace Regnskap.Domain.Kontoplan;

/// <summary>
/// Kontogruppe i NS 4102. Hierarki: Kontoklasse (1-8) -> Kontogruppe (10-89) -> Konto (1000-8999).
/// </summary>
public class Kontogruppe : AuditableEntity
{
    /// <summary>
    /// Tosifret gruppekode (10-89). Forste siffer = kontoklasse.
    /// </summary>
    public int Gruppekode { get; set; }

    /// <summary>
    /// Norsk navn pa gruppen (f.eks. "Immaterielle eiendeler").
    /// </summary>
    public string Navn { get; set; } = default!;

    /// <summary>
    /// Engelsk navn for SAF-T eksport.
    /// </summary>
    public string? NavnEn { get; set; }

    /// <summary>
    /// Avledet fra forste siffer i Gruppekode.
    /// </summary>
    public Kontoklasse Kontoklasse => (Kontoklasse)(Gruppekode / 10);

    /// <summary>
    /// Kontotype som gjelder for kontoer i denne gruppen.
    /// </summary>
    public Kontotype Kontotype { get; set; }

    /// <summary>
    /// Normalbalanse for gruppen.
    /// </summary>
    public Normalbalanse Normalbalanse { get; set; }

    /// <summary>
    /// Kontoene som tilhorer denne gruppen.
    /// </summary>
    public ICollection<Konto> Kontoer { get; set; } = new List<Konto>();

    /// <summary>
    /// Systemgruppe kan ikke slettes eller fa endret gruppekode.
    /// </summary>
    public bool ErSystemgruppe { get; set; }
}
```

### Entity: Konto

Hovedentiteten -- en konto i kontoplanen.

```csharp
namespace Regnskap.Domain.Kontoplan;

/// <summary>
/// En konto i kontoplanen. Firesifret kontonummer ihht NS 4102.
/// Kan ha brukerdefinerte underkontoer (5+ siffer).
/// </summary>
public class Konto : AuditableEntity
{
    /// <summary>
    /// Firesifret kontonummer (1000-8999). For underkontoer: 5-6 siffer.
    /// </summary>
    public string Kontonummer { get; set; } = default!;

    /// <summary>
    /// Norsk kontonavn.
    /// </summary>
    public string Navn { get; set; } = default!;

    /// <summary>
    /// Engelsk kontonavn for SAF-T eksport.
    /// </summary>
    public string? NavnEn { get; set; }

    /// <summary>
    /// Kontotype (Eiendel, Gjeld, Egenkapital, Inntekt, Kostnad).
    /// Arves normalt fra kontogruppen, men kan overstyres for spesialkontoer.
    /// </summary>
    public Kontotype Kontotype { get; set; }

    /// <summary>
    /// Normalbalanse (Debet eller Kredit).
    /// </summary>
    public Normalbalanse Normalbalanse { get; set; }

    /// <summary>
    /// FK til kontogruppen.
    /// </summary>
    public Guid KontogruppeId { get; set; }
    public Kontogruppe Kontogruppe { get; set; } = default!;

    /// <summary>
    /// SAF-T StandardAccountID. Obligatorisk mapping til Skatteetatens standardkonto.
    /// Firesifret kode fra Skatteetatens offisielle liste.
    /// </summary>
    public string StandardAccountId { get; set; } = default!;

    /// <summary>
    /// SAF-T GroupingCategory (RF-1167, RF-1175, RF-1323).
    /// Obligatorisk fra SAF-T v1.30.
    /// </summary>
    public GrupperingsKategori? GrupperingsKategori { get; set; }

    /// <summary>
    /// SAF-T GroupingCode innenfor valgt kategori.
    /// </summary>
    public string? GrupperingsKode { get; set; }

    /// <summary>
    /// Om kontoen er aktiv og kan brukes til bokforing.
    /// Inaktive kontoer vises ikke i oppslag, men beholder historikk.
    /// </summary>
    public bool ErAktiv { get; set; } = true;

    /// <summary>
    /// Systemkonto kan ikke slettes. F.eks. MVA-kontoer, resultat, balanse.
    /// </summary>
    public bool ErSystemkonto { get; set; }

    /// <summary>
    /// Om kontoen kan bokfores direkte (false = kun summekonto/overskrift).
    /// </summary>
    public bool ErBokforbar { get; set; } = true;

    /// <summary>
    /// Standard MVA-kode for denne kontoen. Brukes som default ved bokforing.
    /// Null = ingen MVA-behandling.
    /// </summary>
    public string? StandardMvaKode { get; set; }

    /// <summary>
    /// Fritekst beskrivelse / notat for kontoen.
    /// </summary>
    public string? Beskrivelse { get; set; }

    /// <summary>
    /// FK til overordnet konto (for underkontoer).
    /// Null = dette er en hoveddkonto (4 siffer).
    /// </summary>
    public Guid? OverordnetKontoId { get; set; }
    public Konto? OverordnetKonto { get; set; }

    /// <summary>
    /// Eventuelle underkontoer.
    /// </summary>
    public ICollection<Konto> Underkontoer { get; set; } = new List<Konto>();

    /// <summary>
    /// Om kontoen krever at en avdeling/kostnadssted angis ved bokforing.
    /// </summary>
    public bool KreverAvdeling { get; set; }

    /// <summary>
    /// Om kontoen krever at et prosjekt angis ved bokforing.
    /// </summary>
    public bool KreverProsjekt { get; set; }

    // --- Avledede egenskaper ---

    /// <summary>
    /// Kontoklasse avledet fra forste siffer i kontonummer.
    /// </summary>
    public Kontoklasse Kontoklasse => (Kontoklasse)int.Parse(Kontonummer[..1]);

    /// <summary>
    /// Om dette er en balansepost (klasse 1-2) eller resultatpost (klasse 3-8).
    /// </summary>
    public bool ErBalansekonto => Kontoklasse is Kontoklasse.Eiendeler
                                    or Kontoklasse.EgenkapitalOgGjeld;

    /// <summary>
    /// Om dette er en underkonto (5+ siffer).
    /// </summary>
    public bool ErUnderkonto => Kontonummer.Length > 4;
}
```

### Entity: MvaKode

MVA-kodene som kan knyttes til kontoer. Denne entiteten er del av Kontoplan-modulen fordi den er nodvendig for konto-oppsett, men den brukes ogsa av Hovedbok og MVA-modulen.

```csharp
namespace Regnskap.Domain.Kontoplan;

/// <summary>
/// MVA-kode for bruk i bokforing. Mapper til SAF-T StandardTaxCode.
/// </summary>
public class MvaKode : AuditableEntity
{
    /// <summary>
    /// Intern MVA-kode (kan vaere hva som helst, f.eks. "1", "3", "25I").
    /// </summary>
    public string Kode { get; set; } = default!;

    /// <summary>
    /// Norsk beskrivelse.
    /// </summary>
    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// Engelsk beskrivelse for SAF-T.
    /// </summary>
    public string? BeskrivelseEn { get; set; }

    /// <summary>
    /// SAF-T StandardTaxCode som denne koden mapper til.
    /// </summary>
    public string StandardTaxCode { get; set; } = default!;

    /// <summary>
    /// MVA-sats i prosent (f.eks. 25.00, 15.00, 12.00, 0.00).
    /// </summary>
    public decimal Sats { get; set; }

    /// <summary>
    /// Om dette er utgaende (salg) eller inngaende (kjop) MVA.
    /// </summary>
    public MvaRetning Retning { get; set; }

    /// <summary>
    /// Konto for utgaende MVA (kredit). FK til Konto.
    /// Typisk 2700-serien.
    /// </summary>
    public Guid? UtgaendeKontoId { get; set; }
    public Konto? UtgaendeKonto { get; set; }

    /// <summary>
    /// Konto for inngaende MVA (debet). FK til Konto.
    /// Typisk 2710-serien eller 1600-serien.
    /// </summary>
    public Guid? InngaendeKontoId { get; set; }
    public Konto? InngaendeKonto { get; set; }

    /// <summary>
    /// Om koden er aktiv og kan brukes.
    /// </summary>
    public bool ErAktiv { get; set; } = true;

    /// <summary>
    /// Systemkode kan ikke slettes.
    /// </summary>
    public bool ErSystemkode { get; set; }
}

public enum MvaRetning
{
    Ingen,
    Inngaende,   // Kjop - gir fradrag
    Utgaende,    // Salg - skyldig MVA
    SnuddAvregning  // Reverse charge - bade inn og ut
}
```

### EF Core-konfigurasjon

```csharp
namespace Regnskap.Infrastructure.Persistence.Configurations;

public class KontogruppeConfiguration : IEntityTypeConfiguration<Kontogruppe>
{
    public void Configure(EntityTypeBuilder<Kontogruppe> builder)
    {
        builder.ToTable("Kontogrupper");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Gruppekode)
            .IsRequired();

        builder.HasIndex(k => k.Gruppekode)
            .IsUnique()
            .HasFilter("IsDeleted = 0");

        builder.Property(k => k.Navn)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(k => k.NavnEn)
            .HasMaxLength(200);

        builder.Ignore(k => k.Kontoklasse); // Beregnet property

        builder.HasMany(k => k.Kontoer)
            .WithOne(k => k.Kontogruppe)
            .HasForeignKey(k => k.KontogruppeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class KontoConfiguration : IEntityTypeConfiguration<Konto>
{
    public void Configure(EntityTypeBuilder<Konto> builder)
    {
        builder.ToTable("Kontoer");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Kontonummer)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(k => k.Kontonummer)
            .IsUnique()
            .HasFilter("IsDeleted = 0");

        builder.Property(k => k.Navn)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(k => k.NavnEn)
            .HasMaxLength(200);

        builder.Property(k => k.StandardAccountId)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(k => k.GrupperingsKode)
            .HasMaxLength(50);

        builder.Property(k => k.StandardMvaKode)
            .HasMaxLength(10);

        builder.Property(k => k.Beskrivelse)
            .HasMaxLength(1000);

        builder.HasOne(k => k.OverordnetKonto)
            .WithMany(k => k.Underkontoer)
            .HasForeignKey(k => k.OverordnetKontoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(k => k.Kontogruppe)
            .WithMany(k => k.Kontoer)
            .HasForeignKey(k => k.KontogruppeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(k => k.Kontoklasse);
        builder.Ignore(k => k.ErBalansekonto);
        builder.Ignore(k => k.ErUnderkonto);
    }
}

public class MvaKodeConfiguration : IEntityTypeConfiguration<MvaKode>
{
    public void Configure(EntityTypeBuilder<MvaKode> builder)
    {
        builder.ToTable("MvaKoder");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Kode)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(k => k.Kode)
            .IsUnique()
            .HasFilter("IsDeleted = 0");

        builder.Property(k => k.Beskrivelse)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(k => k.BeskrivelseEn)
            .HasMaxLength(300);

        builder.Property(k => k.StandardTaxCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(k => k.Sats)
            .HasPrecision(5, 2);

        builder.HasOne(k => k.UtgaendeKonto)
            .WithMany()
            .HasForeignKey(k => k.UtgaendeKontoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(k => k.InngaendeKonto)
            .WithMany()
            .HasForeignKey(k => k.InngaendeKontoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### DbContext-utvidelse

```csharp
// Legg til i RegnskapDbContext:
public DbSet<Kontogruppe> Kontogrupper => Set<Kontogruppe>();
public DbSet<Konto> Kontoer => Set<Konto>();
public DbSet<MvaKode> MvaKoder => Set<MvaKode>();
```

---

## API-kontrakt

Base URL: `/api/v1`

### Kontogrupper

#### GET /kontogrupper

Hent alle kontogrupper.

**Response:** `200 OK`

```json
[
  {
    "id": "guid",
    "gruppekode": 10,
    "navn": "Immaterielle eiendeler",
    "navnEn": "Intangible assets",
    "kontoklasse": 1,
    "kontoklasseNavn": "Eiendeler",
    "kontotype": "Eiendel",
    "normalbalanse": "Debet",
    "erSystemgruppe": true,
    "antallKontoer": 5
  }
]
```

#### GET /kontogrupper/{gruppekode}

Hent en kontogruppe med alle kontoer.

**Response:** `200 OK`

```json
{
  "id": "guid",
  "gruppekode": 10,
  "navn": "Immaterielle eiendeler",
  "kontoer": [
    {
      "id": "guid",
      "kontonummer": "1000",
      "navn": "Forskning og utvikling",
      "kontotype": "Eiendel",
      "erAktiv": true,
      "standardMvaKode": null
    }
  ]
}
```

### Kontoer

#### GET /kontoer

Hent alle kontoer. Stotter filtrering og paginering.

**Query-parametere:**

| Parameter | Type | Beskrivelse |
|-----------|------|-------------|
| `kontoklasse` | int? | Filtrer pa kontoklasse (1-8) |
| `kontotype` | string? | Filtrer pa kontotype (Eiendel, Gjeld, etc.) |
| `gruppekode` | int? | Filtrer pa kontogruppe |
| `erAktiv` | bool? | Filtrer pa aktiv/inaktiv (default: true) |
| `erBokforbar` | bool? | Filtrer pa bokforbare kontoer |
| `sok` | string? | Fritekstsok i kontonummer og navn |
| `side` | int | Sidenummer (default: 1) |
| `antall` | int | Antall per side (default: 50, max: 500) |

**Response:** `200 OK`

```json
{
  "data": [
    {
      "id": "guid",
      "kontonummer": "1920",
      "navn": "Bankinnskudd",
      "navnEn": "Bank deposits",
      "kontotype": "Eiendel",
      "normalbalanse": "Debet",
      "kontoklasse": 1,
      "gruppekode": 19,
      "gruppeNavn": "Bankinnskudd, kontanter og lignende",
      "standardAccountId": "1920",
      "grupperingsKategori": "RF1167",
      "grupperingsKode": "0370",
      "erAktiv": true,
      "erSystemkonto": true,
      "erBokforbar": true,
      "standardMvaKode": null,
      "kreverAvdeling": false,
      "kreverProsjekt": false,
      "harUnderkontoer": false,
      "overordnetKontonummer": null
    }
  ],
  "side": 1,
  "antall": 50,
  "totaltAntall": 194
}
```

#### GET /kontoer/{kontonummer}

Hent en konto med detaljer og eventuelle underkontoer.

**Response:** `200 OK`

```json
{
  "id": "guid",
  "kontonummer": "1920",
  "navn": "Bankinnskudd",
  "navnEn": "Bank deposits",
  "kontotype": "Eiendel",
  "normalbalanse": "Debet",
  "kontoklasse": 1,
  "gruppekode": 19,
  "gruppeNavn": "Bankinnskudd, kontanter og lignende",
  "standardAccountId": "1920",
  "grupperingsKategori": "RF1167",
  "grupperingsKode": "0370",
  "erAktiv": true,
  "erSystemkonto": true,
  "erBokforbar": true,
  "standardMvaKode": null,
  "beskrivelse": null,
  "kreverAvdeling": false,
  "kreverProsjekt": false,
  "underkontoer": [
    {
      "kontonummer": "19201",
      "navn": "Driftskonto DNB",
      "erAktiv": true
    },
    {
      "kontonummer": "19202",
      "navn": "Sparekonto DNB",
      "erAktiv": true
    }
  ]
}
```

**Feilkoder:**
- `404 Not Found` -- Kontonummer finnes ikke

#### POST /kontoer

Opprett ny konto (brukerdefinert).

**Request:**

```json
{
  "kontonummer": "19201",
  "navn": "Driftskonto DNB",
  "navnEn": "Operating account DNB",
  "kontotype": "Eiendel",
  "gruppekode": 19,
  "standardAccountId": "1920",
  "grupperingsKategori": "RF1167",
  "grupperingsKode": "0370",
  "erBokforbar": true,
  "standardMvaKode": null,
  "beskrivelse": "Hovedkonto for daglig drift",
  "overordnetKontonummer": "1920",
  "kreverAvdeling": false,
  "kreverProsjekt": false
}
```

**Validering:**

| Felt | Regel |
|------|-------|
| `kontonummer` | Pakreves. 4-6 siffer. Ma starte med 1-8. Ma vaere unikt. |
| `navn` | Pakreves. Maks 200 tegn. |
| `kontotype` | Pakreves. Ma vaere gyldig enum-verdi. |
| `gruppekode` | Pakreves. Ma eksistere. Forste siffer ma matche kontonummerets forste siffer. |
| `standardAccountId` | Pakreves. Ma vaere gyldig SAF-T standard-konto (4 siffer). |
| `overordnetKontonummer` | Valgfritt. Hvis satt, ma eksistere og kontonummeret ma starte med overordnet konto. |
| `standardMvaKode` | Valgfritt. Hvis satt, ma vaere gyldig MVA-kode. |

**Response:** `201 Created`

```json
{
  "id": "guid",
  "kontonummer": "19201",
  "navn": "Driftskonto DNB"
}
```

**Feilkoder:**

| Kode | Melding |
|------|---------|
| `400 KONTO_NUMMER_UGYLDIG` | Kontonummer ma vaere 4-6 siffer og starte med 1-8 |
| `400 KONTO_NUMMER_OPPTATT` | Kontonummer {nummer} er allerede i bruk |
| `400 KONTO_GRUPPE_MISMATCH` | Kontonummer {nummer} tilhorer ikke gruppe {gruppe} |
| `400 KONTO_SAF_T_UGYLDIG` | StandardAccountId {id} er ikke en gyldig SAF-T standardkonto |
| `400 KONTO_OVERORDNET_UGYLDIG` | Overordnet konto {nummer} finnes ikke |
| `400 KONTO_UNDERKONTO_PREFIX` | Underkonto ma starte med overordnet kontonummer |

#### PUT /kontoer/{kontonummer}

Oppdater en eksisterende konto.

**Request:**

```json
{
  "navn": "Driftskonto DNB (hoved)",
  "navnEn": "Operating account DNB (main)",
  "erAktiv": true,
  "erBokforbar": true,
  "standardMvaKode": null,
  "beskrivelse": "Oppdatert beskrivelse",
  "grupperingsKategori": "RF1167",
  "grupperingsKode": "0370",
  "kreverAvdeling": false,
  "kreverProsjekt": true
}
```

**Begrensninger for systemkontoer:**
- `kontonummer` kan aldri endres
- `kontotype` og `normalbalanse` kan ikke endres pa systemkontoer
- `erSystemkonto` kan ikke endres
- `gruppekode` kan ikke endres pa systemkontoer

**Response:** `200 OK`

**Feilkoder:**
- `400 KONTO_SYSTEM_FELT_ENDRING` -- Forsok pa a endre beskyttet felt pa systemkonto
- `404 KONTO_IKKE_FUNNET` -- Konto finnes ikke

#### DELETE /kontoer/{kontonummer}

Soft-delete en konto.

**Response:** `204 No Content`

**Feilkoder:**

| Kode | Melding |
|------|---------|
| `400 KONTO_ER_SYSTEMKONTO` | Systemkonto {nummer} kan ikke slettes |
| `400 KONTO_HAR_POSTERINGER` | Konto {nummer} har posteringer og kan ikke slettes |
| `400 KONTO_HAR_UNDERKONTOER` | Konto {nummer} har aktive underkontoer |

#### POST /kontoer/{kontonummer}/deaktiver

Deaktiver en konto (kan ikke brukes til nye posteringer, men beholder historikk).

**Response:** `200 OK`

#### POST /kontoer/{kontonummer}/aktiver

Reaktiver en deaktivert konto.

**Response:** `200 OK`

### MVA-koder

#### GET /mva-koder

Hent alle MVA-koder.

**Query-parametere:**

| Parameter | Type | Beskrivelse |
|-----------|------|-------------|
| `erAktiv` | bool? | Filtrer pa aktiv/inaktiv (default: true) |
| `retning` | string? | Filtrer pa retning (Inngaende, Utgaende, etc.) |

**Response:** `200 OK`

```json
[
  {
    "id": "guid",
    "kode": "3",
    "beskrivelse": "Utgaende mva, alminnelig sats 25%",
    "beskrivelseEn": "Output VAT, standard rate 25%",
    "standardTaxCode": "3",
    "sats": 25.00,
    "retning": "Utgaende",
    "utgaendeKontonummer": "2700",
    "inngaendeKontonummer": null,
    "erAktiv": true,
    "erSystemkode": true
  }
]
```

#### GET /mva-koder/{kode}

Hent en MVA-kode.

**Response:** `200 OK`

#### POST /mva-koder

Opprett ny MVA-kode (brukerdefinert).

**Request:**

```json
{
  "kode": "25I",
  "beskrivelse": "Inngaende MVA 25% - kun for prosjekt X",
  "standardTaxCode": "1",
  "sats": 25.00,
  "retning": "Inngaende",
  "inngaendeKontonummer": "2710"
}
```

**Response:** `201 Created`

#### PUT /mva-koder/{kode}

Oppdater MVA-kode.

**Response:** `200 OK`

### Import/Eksport

#### POST /kontoer/importer

Importer kontoplan fra CSV/JSON.

**Request:** `multipart/form-data`

| Felt | Type | Beskrivelse |
|------|------|-------------|
| `fil` | File | CSV- eller JSON-fil med kontoplan |
| `format` | string | "csv" eller "json" |
| `modus` | string | "opprett" (bare nye), "oppdater" (oppdater eksisterende), "erstatt" (erstatt hele planen) |

**CSV-format (forventet):**

```csv
Kontonummer;Navn;NavnEn;StandardAccountId;Kontotype;StandardMvaKode;Aktiv
1920;Bankinnskudd;Bank deposits;1920;Eiendel;;true
3000;Salgsinntekt;Sales revenue;3000;Inntekt;3;true
```

**Response:** `200 OK`

```json
{
  "opprettet": 45,
  "oppdatert": 12,
  "hoppetOver": 3,
  "feil": [
    {
      "linje": 15,
      "kontonummer": "9999",
      "melding": "Ugyldig kontonummer -- ma vaere 1000-8999"
    }
  ]
}
```

#### GET /kontoer/eksporter

Eksporter kontoplanen.

**Query-parametere:**

| Parameter | Type | Beskrivelse |
|-----------|------|-------------|
| `format` | string | "csv", "json", eller "saft" (default: "json") |
| `inkluderInaktive` | bool | Inkluder inaktive kontoer (default: false) |

**Response for format=saft:** Returnerer XML-fragment for SAF-T `GeneralLedgerAccounts`-seksjonen.

```xml
<GeneralLedgerAccounts>
  <Account>
    <AccountID>1920</AccountID>
    <AccountDescription>Bankinnskudd</AccountDescription>
    <StandardAccountID>1920</StandardAccountID>
    <AccountType>GL</AccountType>
    <GroupingCategory>RF-1167</GroupingCategory>
    <GroupingCode>0370</GroupingCode>
    <OpeningDebitBalance>0.00</OpeningDebitBalance>
    <ClosingDebitBalance>0.00</ClosingDebitBalance>
  </Account>
</GeneralLedgerAccounts>
```

**Merk:** OpeningDebitBalance/ClosingDebitBalance fylles med 0.00 fra eksport-endepunktet. Faktiske saldoer beregnes av Hovedbok-modulen ved full SAF-T-eksport.

### Oppslag

#### GET /kontoer/oppslag

Raskt oppslag for autocomplet i brukergrensesnitt.

**Query-parametere:**

| Parameter | Type | Beskrivelse |
|-----------|------|-------------|
| `q` | string | Sok (minimum 1 tegn) |
| `antall` | int | Maks antall resultater (default: 10) |

**Response:** `200 OK`

```json
[
  {
    "kontonummer": "1920",
    "navn": "Bankinnskudd",
    "kontotype": "Eiendel",
    "standardMvaKode": null
  }
]
```

Soker i bade kontonummer (prefix-match) og navn (inneholder).

---

## Forretningsregler

### Kontonummer

**FR-1: Kontonummerformat**
Kontonummer ma vaere 4-6 siffer. Forste siffer (1-8) bestemmer kontoklasse. Kontonummer 0xxx og 9xxx er ikke tillatt.

> Eksempel: "1920" er gyldig (klasse 1, eiendeler). "0100" er ugyldig. "92001" er ugyldig.

**FR-2: Kontonummer er immutable**
Nar en konto er opprettet, kan kontonummeret aldri endres. Hvis brukeren onsker et annet nummer, ma en ny konto opprettes og den gamle deaktiveres.

> Begrunnelse: Kontrollspor (NBS 2) krever at kontonummer er stabilt gjennom hele oppbevaringsperioden.

**FR-3: Kontonummer-gruppe-konsistens**
De to forste sifrene i kontonummeret ma matche kontogruppes gruppekode.

> Eksempel: Konto "1920" ma tilhore gruppe 19. Konto "2400" ma tilhore gruppe 24.

**FR-4: Underkonto-prefix**
Underkontoer (5-6 siffer) ma starte med overordnet kontos kontonummer.

> Eksempel: Underkontoer til "1920" ma starte med "1920", f.eks. "19201", "19202".

### Systemkontoer

**FR-5: Systemkontoer kan ikke slettes**
Kontoer merket `ErSystemkonto = true` kan aldri slettes (hverken hard eller soft delete). De kan deaktiveres, men kun hvis ingen aktive posteringer refererer til dem.

Folgende kontoer er systemkontoer (minimum):
- 1500 Kundefordringer
- 1920 Bankinnskudd
- 2000 Aksjekapital
- 2400 Leverandorgjeld
- 2500 Skattetrekk
- 2600 Utgaende merverdiavgift
- 2610 Inngaende merverdiavgift
- 2700 Oppgjorskonto MVA
- 2710 Skyldig arbeidsgiveravgift
- 2780 Palopte feriepenger
- 2900 Annen kortsiktig gjeld (diverse)
- 3000 Salgsinntekt, avgiftspliktig
- 8800 Arsresultat (arets resultat)

**FR-6: Systemkontoer -- beskyttede felter**
For systemkontoer kan folgende felter ikke endres: `Kontonummer`, `Kontotype`, `Normalbalanse`, `ErSystemkonto`, `KontogruppeId`.

Felter som KAN endres pa systemkontoer: `Navn`, `NavnEn`, `Beskrivelse`, `StandardMvaKode`, `GrupperingsKategori`, `GrupperingsKode`, `KreverAvdeling`, `KreverProsjekt`.

### Sletting og deaktivering

**FR-7: Konto med posteringer kan ikke slettes**
Hvis en konto har bokforte transaksjoner (i noen periode), kan den ikke slettes. Den kan deaktiveres.

> Begrunnelse: Bokforingslovens krav til oppbevaring og sporbarhet.

**FR-8: Konto med aktive underkontoer kan ikke slettes**
En overordnet konto kan ikke slettes sa lenge den har aktive underkontoer. Underkontoeene ma slettes eller deaktiveres forst.

**FR-9: Deaktiverte kontoer kan ikke bokfores pa**
En deaktivert konto (`ErAktiv = false`) skal avvises ved forsok pa bokforing. Eksisterende posteringer forblir uendret.

### Kontotype og normalbalanse

**FR-10: Kontotype-til-normalbalanse-mapping**
Kontotypen bestemmer normalbalanse:

| Kontotype | Normalbalanse | Oker med |
|-----------|---------------|----------|
| Eiendel | Debet | Debet |
| Gjeld | Kredit | Kredit |
| Egenkapital | Kredit | Kredit |
| Inntekt | Kredit | Kredit |
| Kostnad | Debet | Debet |

**FR-11: Kontoklasse-til-kontotype-konsistens**

| Kontoklasse | Tillatte kontotyper |
|-------------|---------------------|
| 1 | Eiendel |
| 2 | Gjeld, Egenkapital |
| 3 | Inntekt |
| 4 | Kostnad |
| 5 | Kostnad |
| 6 | Kostnad |
| 7 | Kostnad |
| 8 | Inntekt, Kostnad (begge tillatt) |

> Eksempel: Konto "8000" (finansinntekt) er Inntekt. Konto "8400" (finanskostnad) er Kostnad.

### SAF-T

**FR-12: StandardAccountID er obligatorisk**
Alle kontoer ma ha en gyldig `StandardAccountId` som mapper til Skatteetatens offisielle liste over standardkontoer (4-sifret).

> Begrunnelse: SAF-T v1.30 krever dette feltet for a produsere gyldig eksport.

**FR-13: GroupingCategory er obligatorisk fra SAF-T v1.30**
Alle kontoer bor ha `GrupperingsKategori` og `GrupperingsKode` satt. Systemet skal varsle brukeren hvis disse mangler.

**FR-14: StandardAccountID ma matche kontoklasse**
StandardAccountId's forste siffer ma matche kontonummerets kontoklasse.

> Eksempel: Konto 1920 ma mappe til en StandardAccountId som starter med "1" (f.eks. "1920"). Konto 3000 ma mappe til "3xxx".

### MVA-koder pa kontoer

**FR-15: Standard MVA-kode er et forslag**
`StandardMvaKode` pa en konto er en default som brukes ved ny bokforing. Brukeren kan overstyre denne per postering.

**FR-16: MVA-kode-konsistens med kontotype**
- Inntektskontoer (klasse 3) skal normalt ha utgaende MVA-koder (kode 3, 31, 33, 5, 6)
- Kostnadskontoer (klasse 4-7) skal normalt ha inngaende MVA-koder (kode 1, 11, 13)
- Balansekontoer (klasse 1-2) skal normalt ikke ha MVA-kode (unntak: MVA-kontoene selv)
- Systemet skal varsle (ikke blokkere) ved uvanlige kombinasjoner

**FR-17: MVA-kontokoblinger**
Hver MVA-kode ma ha en koblet konto for a automatisere bokforing:
- Utgaende MVA-koder ma ha `UtgaendeKontoId` satt (typisk 2700)
- Inngaende MVA-koder ma ha `InngaendeKontoId` satt (typisk 2710 eller 1600-serien)
- Reverse charge-koder ma ha begge satt

### Import/Eksport

**FR-18: Import validerer alle regler**
Ved import av kontoplan gjelder alle forretningsregler. Systemkontoer kan ikke overskrives av import. Ugyldige rader hoppes over og rapporteres i resultatet.

**FR-19: Erstatt-modus beskytter systemkontoer**
Ved import med `modus=erstatt` slettes (soft delete) alle brukerdefinerte kontoer som ikke finnes i importfilen. Systemkontoer forblir uendret.

**FR-20: Eksport i SAF-T-format**
Eksport med `format=saft` produserer XML som validerer mot SAF-T XSD for `GeneralLedgerAccounts`-seksjonen. AccountType er alltid "GL".

---

## MVA-handling

### MVA-kontoer i kontoplanen

Folgende kontoer i NS 4102 er sentrale for MVA-behandling:

| Kontonummer | Navn | Bruk |
|-------------|------|------|
| 1600 | Merverdiavgift, inngaende (fordringsside) | Mellomkonto for inngaende MVA som ikke er forfalt |
| 2600 | Utgaende merverdiavgift | Skyldig utgaende MVA |
| 2610 | Inngaende merverdiavgift (motregning) | Fradrag for inngaende MVA |
| 2700 | Oppgjorskonto merverdiavgift | MVA-oppgjor med Skatteetaten |
| 2701 | Inngaende merverdiavgift, alminnelig sats | Detaljkonto inn-MVA 25% |
| 2702 | Inngaende merverdiavgift, middels sats | Detaljkonto inn-MVA 15% |
| 2703 | Inngaende merverdiavgift, lav sats | Detaljkonto inn-MVA 12% |
| 2710 | Utgaende merverdiavgift, alminnelig sats | Detaljkonto ut-MVA 25% |
| 2711 | Utgaende merverdiavgift, middels sats | Detaljkonto ut-MVA 15% |
| 2712 | Utgaende merverdiavgift, lav sats | Detaljkonto ut-MVA 12% |

### Standard MVA-koder (seed data)

| Intern kode | Beskrivelse | StandardTaxCode | Sats | Retning | Konto |
|-------------|-------------|-----------------|------|---------|-------|
| 0 | Ingen MVA-behandling | 0 | 0% | Ingen | - |
| 1 | Inngaende MVA 25% | 1 | 25% | Inngaende | 2710 |
| 11 | Inngaende MVA 15% | 11 | 15% | Inngaende | 2711 |
| 13 | Inngaende MVA 12% | 13 | 12% | Inngaende | 2712 |
| 3 | Utgaende MVA 25% | 3 | 25% | Utgaende | 2700 |
| 31 | Utgaende MVA 15% | 31 | 15% | Utgaende | 2700 |
| 33 | Utgaende MVA 12% | 33 | 12% | Utgaende | 2700 |
| 5 | Utgaende MVA 0% (utforsel) | 5 | 0% | Utgaende | - |
| 6 | Utenfor MVA-omradet | 6 | 0% | Ingen | - |
| 14 | Inngaende MVA innforsel 25% | 14 | 25% | Inngaende | 2710 |
| 15 | Inngaende MVA innforsel 15% | 15 | 15% | Inngaende | 2711 |

### MVA-beregning (for kontoplankontekst)

Selve MVA-beregningen skjer i Hovedbok/Bilagsregistrering-modulen. Kontoplan-modulen er ansvarlig for:

1. **Definere MVA-kodene** med riktig sats og SAF-T-mapping
2. **Koble MVA-koder til riktige balansekontoer** (2600-2712)
3. **Sette standard MVA-kode pa driftskontoer** sa brukeren slipper a velge manuelt
4. **Validere MVA-konsistens** (FR-16)

---

## SAF-T felt-mapping

### GeneralLedgerAccounts

| SAF-T-element | Kilde i datamodellen | Merknad |
|---------------|---------------------|---------|
| `AccountID` | `Konto.Kontonummer` | |
| `AccountDescription` | `Konto.NavnEn ?? Konto.Navn` | Engelsk preferert i SAF-T |
| `StandardAccountID` | `Konto.StandardAccountId` | Obligatorisk |
| `AccountType` | Hardkodet "GL" | Alle kontoer er General Ledger |
| `GroupingCategory` | `Konto.GrupperingsKategori` | RF-1167, RF-1175, eller RF-1323 |
| `GroupingCode` | `Konto.GrupperingsKode` | Kode innenfor valgt kategori |
| `OpeningDebitBalance` | Beregnes av Hovedbok | 0 nar bare kontoplan eksporteres |
| `OpeningCreditBalance` | Beregnes av Hovedbok | 0 nar bare kontoplan eksporteres |
| `ClosingDebitBalance` | Beregnes av Hovedbok | 0 nar bare kontoplan eksporteres |
| `ClosingCreditBalance` | Beregnes av Hovedbok | 0 nar bare kontoplan eksporteres |

### TaxTable (TaxCodeDetails)

| SAF-T-element | Kilde i datamodellen | Merknad |
|---------------|---------------------|---------|
| `TaxCodeDetails/TaxCode` | `MvaKode.Kode` | Intern kode |
| `TaxCodeDetails/Description` | `MvaKode.BeskrivelseEn ?? MvaKode.Beskrivelse` | |
| `StandardTaxCode` | `MvaKode.StandardTaxCode` | Obligatorisk mapping |
| `TaxPercentage` | `MvaKode.Sats` | |
| `Country` | Hardkodet "NO" | |

### Eksempel SAF-T XML-output

```xml
<MasterFiles>
  <GeneralLedgerAccounts>
    <Account>
      <AccountID>1920</AccountID>
      <AccountDescription>Bank deposits</AccountDescription>
      <StandardAccountID>1920</StandardAccountID>
      <AccountType>GL</AccountType>
      <GroupingCategory>RF-1167</GroupingCategory>
      <GroupingCode>0370</GroupingCode>
      <OpeningDebitBalance>150000.00</OpeningDebitBalance>
      <ClosingDebitBalance>175230.50</ClosingDebitBalance>
    </Account>
    <Account>
      <AccountID>2400</AccountID>
      <AccountDescription>Accounts payable</AccountDescription>
      <StandardAccountID>2400</StandardAccountID>
      <AccountType>GL</AccountType>
      <GroupingCategory>RF-1167</GroupingCategory>
      <GroupingCode>0820</GroupingCode>
      <OpeningCreditBalance>45000.00</OpeningCreditBalance>
      <ClosingCreditBalance>38500.00</ClosingCreditBalance>
    </Account>
  </GeneralLedgerAccounts>

  <TaxTable>
    <TaxTableEntry>
      <TaxCodeDetails>
        <TaxCode>3</TaxCode>
        <Description>Output VAT, standard rate 25%</Description>
      </TaxCodeDetails>
      <StandardTaxCode>3</StandardTaxCode>
      <TaxPercentage>25.00</TaxPercentage>
      <Country>NO</Country>
    </TaxTableEntry>
    <TaxTableEntry>
      <TaxCodeDetails>
        <TaxCode>1</TaxCode>
        <Description>Input VAT, standard rate 25%</Description>
      </TaxCodeDetails>
      <StandardTaxCode>1</StandardTaxCode>
      <TaxPercentage>25.00</TaxPercentage>
      <Country>NO</Country>
    </TaxTableEntry>
  </TaxTable>
</MasterFiles>
```

---

## Seed Data

### Kontogrupper (NS 4102)

Systemet ma seedes med alle 27 kontogrupper:

| Gruppekode | Navn | Kontotype | Normalbalanse |
|------------|------|-----------|---------------|
| 10 | Immaterielle eiendeler | Eiendel | Debet |
| 11 | Tomter, bygninger og annen fast eiendom | Eiendel | Debet |
| 12 | Transportmidler, maskiner, inventar | Eiendel | Debet |
| 13 | Finansielle anleggsmidler | Eiendel | Debet |
| 14 | Varelager og forskudd til leverandorer | Eiendel | Debet |
| 15 | Kortsiktige fordringer | Eiendel | Debet |
| 16 | Merverdiavgift, opptjente inntekter | Eiendel | Debet |
| 17 | Forskuddsbetalt kostnad, pabegynt arbeid | Eiendel | Debet |
| 18 | Kortsiktige finansinvesteringer | Eiendel | Debet |
| 19 | Bankinnskudd, kontanter og lignende | Eiendel | Debet |
| 20 | Innskutt egenkapital | Egenkapital | Kredit |
| 21 | Opptjent egenkapital | Egenkapital | Kredit |
| 22 | Langsiktig gjeld | Gjeld | Kredit |
| 23 | Annen langsiktig gjeld | Gjeld | Kredit |
| 24 | Leverandorgjeld | Gjeld | Kredit |
| 25 | Skattetrekk og offentlige avgifter | Gjeld | Kredit |
| 26 | Skyldig merverdiavgift | Gjeld | Kredit |
| 27 | Skyldig arbeidsgiveravgift, lonnsrelatert gjeld | Gjeld | Kredit |
| 28 | Annen kortsiktig gjeld | Gjeld | Kredit |
| 29 | Annen gjeld og egenkapitalposter | Gjeld | Kredit |
| 30 | Salgsinntekt, avgiftspliktig | Inntekt | Kredit |
| 31 | Salgsinntekt, avgiftsfri | Inntekt | Kredit |
| 32-39 | (Oppsummert: ovrige inntektsgrupper) | Inntekt | Kredit |
| 40-49 | (Oppsummert: varekostnadsgrupper) | Kostnad | Debet |
| 50-59 | (Oppsummert: lonnskostnadsgrupper) | Kostnad | Debet |
| 60-69 | (Oppsummert: avskrivning/driftskostnader) | Kostnad | Debet |
| 70-79 | (Oppsummert: andre driftskostnader) | Kostnad | Debet |
| 80-89 | (Oppsummert: finansposter/skatt) | Inntekt/Kostnad | Varierer |

Alle kontogrupper seedes som `ErSystemgruppe = true`.

### Standard NS 4102-kontoer (utvalg -- minimum seed)

Folgende kontoer seedes som `ErSystemkonto = true`:

| Kontonummer | Navn | StandardAccountId | MVA-kode | Kontotype |
|-------------|------|-------------------|----------|-----------|
| 1000 | Forskning og utvikling | 1000 | - | Eiendel |
| 1070 | Utsatt skattefordel | 1070 | - | Eiendel |
| 1100 | Bygninger | 1100 | - | Eiendel |
| 1200 | Maskiner og anlegg | 1200 | - | Eiendel |
| 1250 | Inventar | 1250 | - | Eiendel |
| 1280 | Kontormaskiner | 1280 | - | Eiendel |
| 1300 | Investeringer i datterselskap | 1300 | - | Eiendel |
| 1350 | Investeringer i aksjer og andeler | 1350 | - | Eiendel |
| 1400 | Varelager | 1400 | - | Eiendel |
| 1500 | Kundefordringer | 1500 | - | Eiendel |
| 1570 | Andre kortsiktige fordringer | 1570 | - | Eiendel |
| 1600 | Inngaende merverdiavgift (mellomkonto) | 1600 | - | Eiendel |
| 1700 | Forskuddsbetalte kostnader | 1700 | - | Eiendel |
| 1900 | Kasse | 1900 | - | Eiendel |
| 1920 | Bankinnskudd | 1920 | - | Eiendel |
| 1930 | Bankinnskudd skattetrekk | 1930 | - | Eiendel |
| 2000 | Aksjekapital | 2000 | - | Egenkapital |
| 2020 | Overkurs | 2020 | - | Egenkapital |
| 2050 | Annen innskutt egenkapital | 2050 | - | Egenkapital |
| 2080 | Udekket tap | 2080 | - | Egenkapital |
| 2100 | Fond | 2100 | - | Egenkapital |
| 2120 | Annen egenkapital | 2120 | - | Egenkapital |
| 2200 | Langsiktige lan | 2200 | - | Gjeld |
| 2400 | Leverandorgjeld | 2400 | - | Gjeld |
| 2500 | Skattetrekk | 2500 | - | Gjeld |
| 2600 | Utgaende merverdiavgift | 2600 | - | Gjeld |
| 2610 | Inngaende merverdiavgift | 2610 | - | Gjeld |
| 2700 | Oppgjorskonto merverdiavgift | 2700 | - | Gjeld |
| 2710 | Skyldig arbeidsgiveravgift | 2710 | - | Gjeld |
| 2770 | Skyldig arbeidsgiveravgift av feriepenger | 2770 | - | Gjeld |
| 2780 | Palopte feriepenger | 2780 | - | Gjeld |
| 2800 | Avsatt utbytte | 2800 | - | Gjeld |
| 2900 | Annen kortsiktig gjeld | 2900 | - | Gjeld |
| 3000 | Salgsinntekt, avgiftspliktig | 3000 | 3 | Inntekt |
| 3100 | Salgsinntekt, avgiftsfri | 3100 | 5 | Inntekt |
| 3200 | Salgsinntekt, utenfor avgiftsomradet | 3200 | 6 | Inntekt |
| 3600 | Leieinntekt | 3600 | 3 | Inntekt |
| 3900 | Annen driftsinntekt | 3900 | - | Inntekt |
| 4000 | Varekjop | 4000 | 1 | Kostnad |
| 4300 | Innkjop for viderefakturering | 4300 | 1 | Kostnad |
| 5000 | Lonn til ansatte | 5000 | - | Kostnad |
| 5100 | Feriepenger | 5100 | - | Kostnad |
| 5400 | Arbeidsgiveravgift | 5400 | - | Kostnad |
| 5420 | Arbeidsgiveravgift av feriepenger | 5420 | - | Kostnad |
| 5900 | Annen personalkostnad | 5900 | - | Kostnad |
| 6000 | Avskrivning | 6000 | - | Kostnad |
| 6100 | Frakt og transport | 6100 | 1 | Kostnad |
| 6300 | Leie lokaler | 6300 | 1 | Kostnad |
| 6400 | Leie maskiner og inventar | 6400 | 1 | Kostnad |
| 6500 | Verktoy, inventar uten aktiveringsplikt | 6500 | 1 | Kostnad |
| 6600 | Reparasjon og vedlikehold | 6600 | 1 | Kostnad |
| 6700 | Fremmedtjenester (revisor, advokat) | 6700 | 1 | Kostnad |
| 6800 | Kontorrekvisita | 6800 | 1 | Kostnad |
| 6900 | Telefon, porto, data | 6900 | 1 | Kostnad |
| 7000 | Reisekostnad | 7000 | - | Kostnad |
| 7100 | Bilkostnad | 7100 | 1 | Kostnad |
| 7300 | Salgskostnad, reklame | 7300 | 1 | Kostnad |
| 7400 | Kontingenter | 7400 | - | Kostnad |
| 7500 | Forsikringspremie | 7500 | - | Kostnad |
| 7700 | Annen kostnad | 7700 | 1 | Kostnad |
| 7800 | Tap pa fordringer | 7800 | - | Kostnad |
| 8000 | Finansinntekt | 8000 | - | Inntekt |
| 8050 | Renteinntekt | 8050 | - | Inntekt |
| 8100 | Annen finansinntekt | 8100 | - | Inntekt |
| 8150 | Gevinst ved salg av aksjer | 8150 | - | Inntekt |
| 8400 | Finanskostnad | 8400 | - | Kostnad |
| 8450 | Rentekostnad | 8450 | - | Kostnad |
| 8500 | Annen finanskostnad | 8500 | - | Kostnad |
| 8800 | Arsresultat | 8800 | - | Kostnad |
| 8900 | Skattekostnad | 8900 | - | Kostnad |
| 8960 | Endring utsatt skatt | 8960 | - | Kostnad |

Alle 531 kontoer fra NS 4102:2023 bor seedes ved forste gangs oppstart. Listen ovenfor er minimumssettet. Komplett seed-data hentes fra Skatteetatens offisielle CSV:
https://github.com/Skatteetaten/saf-t/blob/master/General%20Ledger%20Standard%20Accounts/CSV/General_Ledger_Standard_Accounts_4_character.csv

---

## Avhengigheter

### Denne modulen avhenger av

**Ingen.** Kontoplan er grunnmodulen uten avhengigheter.

Bruker kun:
- `Regnskap.Domain.Common.AuditableEntity` -- basisklasse for alle entiteter
- `Regnskap.Infrastructure.Persistence.RegnskapDbContext` -- databasekontekst

### Andre moduler som avhenger av denne

| Modul | Avhengighet |
|-------|-------------|
| **Hovedbok** | Bruker `Konto` for bokforing. Leser kontotype og normalbalanse for saldoberegning. |
| **Bilagsregistrering** | Bruker `Konto` og `MvaKode` for a validere og opprette posteringer. |
| **MVA-handing** | Bruker `MvaKode` og tilhorende kontoer for MVA-beregning og -rapportering. |
| **Leverandorreskontro** | Bruker konto 2400 (leverandorgjeld) og inngaende MVA-koder. |
| **Kundereskontro** | Bruker konto 1500 (kundefordringer) og utgaende MVA-koder. |
| **Rapportering** | Bruker kontohierarki (klasse/gruppe/konto) for resultat og balanse. |
| **SAF-T Eksport** | Bruker `StandardAccountId`, `GrupperingsKategori`, `GrupperingsKode` og TaxTable-data. |

### Interfaces som denne modulen eksponerer

```csharp
namespace Regnskap.Domain.Kontoplan;

/// <summary>
/// Service for a hente og validere kontoer. Brukes av andre moduler.
/// </summary>
public interface IKontoService
{
    Task<Konto?> HentKontoAsync(string kontonummer, CancellationToken ct = default);
    Task<bool> KontoFinnesOgErAktivAsync(string kontonummer, CancellationToken ct = default);
    Task<Konto> HentKontoEllerKastAsync(string kontonummer, CancellationToken ct = default);
    Task<IReadOnlyList<Konto>> HentKontoerForGruppeAsync(int gruppekode, CancellationToken ct = default);
    Task<Kontotype> HentKontotypeAsync(string kontonummer, CancellationToken ct = default);
    Task<Normalbalanse> HentNormalbalanseAsync(string kontonummer, CancellationToken ct = default);
}

/// <summary>
/// Service for a hente og validere MVA-koder. Brukes av Bilag og MVA-modulen.
/// </summary>
public interface IMvaKodeService
{
    Task<MvaKode?> HentMvaKodeAsync(string kode, CancellationToken ct = default);
    Task<MvaKode> HentMvaKodeEllerKastAsync(string kode, CancellationToken ct = default);
    Task<IReadOnlyList<MvaKode>> HentAlleMvaKoderAsync(bool kunAktive = true, CancellationToken ct = default);
    Task<string?> HentStandardMvaKodeForKontoAsync(string kontonummer, CancellationToken ct = default);
}
```

### Exceptions

```csharp
namespace Regnskap.Domain.Kontoplan;

public class KontoIkkeFunnetException : Exception
{
    public string Kontonummer { get; }
    public KontoIkkeFunnetException(string kontonummer)
        : base($"Konto {kontonummer} finnes ikke.") => Kontonummer = kontonummer;
}

public class KontoInaktivException : Exception
{
    public string Kontonummer { get; }
    public KontoInaktivException(string kontonummer)
        : base($"Konto {kontonummer} er deaktivert og kan ikke brukes til bokforing.") => Kontonummer = kontonummer;
}

public class SystemkontoSlettingException : Exception
{
    public string Kontonummer { get; }
    public SystemkontoSlettingException(string kontonummer)
        : base($"Systemkonto {kontonummer} kan ikke slettes.") => Kontonummer = kontonummer;
}

public class KontoHarPosteringerException : Exception
{
    public string Kontonummer { get; }
    public KontoHarPosteringerException(string kontonummer)
        : base($"Konto {kontonummer} har posteringer og kan ikke slettes.") => Kontonummer = kontonummer;
}

public class MvaKodeIkkeFunnetException : Exception
{
    public string Kode { get; }
    public MvaKodeIkkeFunnetException(string kode)
        : base($"MVA-kode {kode} finnes ikke.") => Kode = kode;
}
```

---

## Implementasjonsrekkefølge

1. **Enums og value objects** -- `Kontoklasse`, `Kontotype`, `Normalbalanse`, `GrupperingsKategori`, `MvaRetning`
2. **Entiteter** -- `Kontogruppe`, `Konto`, `MvaKode`
3. **EF Core-konfigurasjon** -- Indekser, constraints, cascade-regler
4. **Seed data** -- Kontogrupper, standardkontoer, MVA-koder
5. **Interfaces og services** -- `IKontoService`, `IMvaKodeService`
6. **API-endepunkter** -- Controllers med validering og DTOs
7. **Import/eksport** -- CSV/JSON/SAF-T
