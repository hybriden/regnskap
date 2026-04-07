# Spesifikasjon: Periodeavslutning (Period Closing)

**Modul:** Periodeavslutning
**Status:** Komplett spesifikasjon
**Avhengigheter:** Kontoplan (implementert), Hovedbok (implementert), Bilagsregistrering (implementert), MVA (implementert), Rapportering (modul 9)
**SAF-T-seksjon:** GeneralLedgerEntries (avslutnings- og avskrivningsbilag)
**Lovgrunnlag:** Regnskapsloven 3-1, 4-1, 5-3; Bokforingsloven 4, 5, 7; NBS 5 (balansedokumentasjon)

---

## Oversikt

Periodeavslutningsmodulen styrer alle prosesser knyttet til lukking av regnskapsperioder og arsavslutning. Den sikrer at bokforingen er komplett, avstemt og korrekt for perioden avsluttes.

Seks hovedfunksjoner:

1. **Manedlig avstemming** -- sjekkliste og automatiske kontroller for lukking
2. **Manedlig lukking** -- sperr perioden for videre bokforing
3. **Arsavslutning** -- disponering av resultat, apningsbalanse, lukking av resultatkontoer
4. **Avskrivninger** -- beregning og bokforing av lineaere avskrivninger
5. **Periodisering** -- forskuddsbetalte kostnader og opptjente inntekter
6. **Arsregnskapsklargjoring** -- forbered for innsending til Regnskapsregisteret

---

## Datamodell

### Nye entiteter

#### Anleggsmiddel

```csharp
namespace Regnskap.Domain.Features.Periodeavslutning;

using Regnskap.Domain.Common;

/// <summary>
/// Et anleggsmiddel som avskrives over tid (lineaer metode).
/// Regnskapsloven 5-3: anleggsmidler med begrenset levetid skal avskrives
/// etter en fornuftig avskrivningsplan.
/// </summary>
public class Anleggsmiddel : AuditableEntity
{
    /// <summary>
    /// Intern identifikator/navn.
    /// </summary>
    public string Navn { get; set; } = default!;

    /// <summary>
    /// Fritekst beskrivelse.
    /// </summary>
    public string? Beskrivelse { get; set; }

    /// <summary>
    /// Anskaffelsesdato.
    /// </summary>
    public DateOnly Anskaffelsesdato { get; set; }

    /// <summary>
    /// Anskaffelseskostnad (historisk kost).
    /// </summary>
    public decimal Anskaffelseskostnad { get; set; }

    /// <summary>
    /// Forventet utrangeringsverdi (restverdi).
    /// Avskrivningsgrunnlag = Anskaffelseskostnad - Restverdi.
    /// </summary>
    public decimal Restverdi { get; set; }

    /// <summary>
    /// Forventet levetid i maneder.
    /// </summary>
    public int LevetidManeder { get; set; }

    /// <summary>
    /// Balansepost-konto (f.eks. 1200 Maskiner). Debet-konto.
    /// </summary>
    public string BalanseKontonummer { get; set; } = default!;

    /// <summary>
    /// Avskrivningskonto (f.eks. 6000 Avskrivning). Debet-konto for kostnaden.
    /// </summary>
    public string AvskrivningsKontonummer { get; set; } = default!;

    /// <summary>
    /// Akkumulert avskrivning-konto (f.eks. 1209 Akkumulerte avskrivninger).
    /// Kredit-konto pa balansen (kontra-konto til anleggsmiddel).
    /// </summary>
    public string AkkumulertAvskrivningKontonummer { get; set; } = default!;

    /// <summary>
    /// Avdelingskode (valgfritt).
    /// </summary>
    public string? Avdelingskode { get; set; }

    /// <summary>
    /// Prosjektkode (valgfritt).
    /// </summary>
    public string? Prosjektkode { get; set; }

    /// <summary>
    /// Om anleggsmiddelet er aktivt (i bruk og avskrives).
    /// Settes til false ved utrangering/salg.
    /// </summary>
    public bool ErAktivt { get; set; } = true;

    /// <summary>
    /// Dato for utrangering/salg. Null hvis aktivt.
    /// </summary>
    public DateOnly? UtrangeringsDato { get; set; }

    /// <summary>
    /// Historikk over avskrivninger som er bokfort.
    /// </summary>
    public List<AvskrivningHistorikk> Avskrivninger { get; set; } = new();

    // --- Avledede egenskaper ---

    /// <summary>
    /// Avskrivningsgrunnlag = Anskaffelseskostnad - Restverdi.
    /// </summary>
    public decimal Avskrivningsgrunnlag => Anskaffelseskostnad - Restverdi;

    /// <summary>
    /// Manedlig avskrivningsbelop (lineaer metode).
    /// </summary>
    public decimal ManedligAvskrivning =>
        LevetidManeder > 0 ? Math.Round(Avskrivningsgrunnlag / LevetidManeder, 2) : 0;

    /// <summary>
    /// Arlig avskrivningsbelop.
    /// </summary>
    public decimal ArligAvskrivning => ManedligAvskrivning * 12;

    /// <summary>
    /// Sum av alle tidligere bokforte avskrivninger.
    /// </summary>
    public decimal AkkumulertAvskrivning => Avskrivninger.Sum(a => a.Belop);

    /// <summary>
    /// Bokfort verdi = Anskaffelseskostnad - AkkumulertAvskrivning.
    /// </summary>
    public decimal BokfortVerdi => Anskaffelseskostnad - AkkumulertAvskrivning;

    /// <summary>
    /// Gjenvaerende avskrivningsbelop = Avskrivningsgrunnlag - AkkumulertAvskrivning.
    /// Kan ikke bli negativt.
    /// </summary>
    public decimal GjenvaerendeAvskrivning =>
        Math.Max(0, Avskrivningsgrunnlag - AkkumulertAvskrivning);

    /// <summary>
    /// Om anleggsmiddelet er fullt avskrevet.
    /// </summary>
    public bool ErFulltAvskrevet => GjenvaerendeAvskrivning <= 0;
}
```

#### AvskrivningHistorikk

```csharp
namespace Regnskap.Domain.Features.Periodeavslutning;

using Regnskap.Domain.Common;

/// <summary>
/// Historikkrad for en enkelt avskrivningspostering.
/// </summary>
public class AvskrivningHistorikk : AuditableEntity
{
    /// <summary>
    /// FK til anleggsmiddelet.
    /// </summary>
    public Guid AnleggsmiddelId { get; set; }
    public Anleggsmiddel Anleggsmiddel { get; set; } = default!;

    /// <summary>
    /// Regnskapsaret.
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Periode (1-12).
    /// </summary>
    public int Periode { get; set; }

    /// <summary>
    /// Avskrivningsbelopet.
    /// </summary>
    public decimal Belop { get; set; }

    /// <summary>
    /// Akkumulert avskrivning etter denne posteringen.
    /// </summary>
    public decimal AkkumulertEtter { get; set; }

    /// <summary>
    /// Bokfort verdi etter denne posteringen.
    /// </summary>
    public decimal BokfortVerdiEtter { get; set; }

    /// <summary>
    /// FK til bilaget som ble opprettet.
    /// </summary>
    public Guid BilagId { get; set; }
}
```

#### Periodisering

```csharp
namespace Regnskap.Domain.Features.Periodeavslutning;

using Regnskap.Domain.Common;

/// <summary>
/// En periodisering (accrual) som fordeler en kostnad/inntekt over flere perioder.
/// Folger sammenstillingsprinsippet (Regnskapsloven 4-1 nr. 3).
/// </summary>
public class Periodisering : AuditableEntity
{
    /// <summary>
    /// Beskrivelse av periodiseringen.
    /// </summary>
    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// Type periodisering.
    /// </summary>
    public PeriodiseringsType Type { get; set; }

    /// <summary>
    /// Totalbelop som periodiseres.
    /// </summary>
    public decimal TotalBelop { get; set; }

    /// <summary>
    /// Valutakode. Default "NOK".
    /// </summary>
    public string Valuta { get; set; } = "NOK";

    /// <summary>
    /// Forste periode periodiseringen gjelder (YYYY-MM format internt, som ar+periode).
    /// </summary>
    public int FraAr { get; set; }
    public int FraPeriode { get; set; }

    /// <summary>
    /// Siste periode periodiseringen gjelder.
    /// </summary>
    public int TilAr { get; set; }
    public int TilPeriode { get; set; }

    /// <summary>
    /// Konto som opprinnelig ble belastet/kreditert.
    /// F.eks. 1700 Forskuddsbetalt kostnad eller 2900 Opptjent inntekt.
    /// </summary>
    public string BalanseKontonummer { get; set; } = default!;

    /// <summary>
    /// Resultatkonto som belastes/krediteres manedlig.
    /// F.eks. 6300 Husleie eller 3000 Salgsinntekt.
    /// </summary>
    public string ResultatKontonummer { get; set; } = default!;

    /// <summary>
    /// Avdelingskode (valgfritt).
    /// </summary>
    public string? Avdelingskode { get; set; }

    /// <summary>
    /// Prosjektkode (valgfritt).
    /// </summary>
    public string? Prosjektkode { get; set; }

    /// <summary>
    /// Om periodiseringen er aktiv.
    /// </summary>
    public bool ErAktiv { get; set; } = true;

    /// <summary>
    /// Referanse til opprinnelig bilag.
    /// </summary>
    public Guid? OpprinneligBilagId { get; set; }

    /// <summary>
    /// Historikk over posteringer.
    /// </summary>
    public List<PeriodiseringsHistorikk> Posteringer { get; set; } = new();

    // --- Avledede egenskaper ---

    /// <summary>
    /// Antall perioder periodiseringen strekker seg over.
    /// </summary>
    public int AntallPerioder =>
        (TilAr - FraAr) * 12 + (TilPeriode - FraPeriode) + 1;

    /// <summary>
    /// Belop per periode (jevnt fordelt).
    /// </summary>
    public decimal BelopPerPeriode =>
        AntallPerioder > 0 ? Math.Round(TotalBelop / AntallPerioder, 2) : 0;

    /// <summary>
    /// Sum allerede periodisert.
    /// </summary>
    public decimal SumPeriodisert => Posteringer.Sum(p => p.Belop);

    /// <summary>
    /// Gjenstaende belop.
    /// </summary>
    public decimal GjenstaendeBelop => TotalBelop - SumPeriodisert;
}
```

#### PeriodiseringsHistorikk

```csharp
namespace Regnskap.Domain.Features.Periodeavslutning;

using Regnskap.Domain.Common;

public class PeriodiseringsHistorikk : AuditableEntity
{
    public Guid PeriodiseringId { get; set; }
    public Periodisering Periodisering { get; set; } = default!;
    public int Ar { get; set; }
    public int Periode { get; set; }
    public decimal Belop { get; set; }
    public decimal AkkumulertEtter { get; set; }
    public decimal GjenstaarEtter { get; set; }
    public Guid BilagId { get; set; }
}
```

#### PeriodeLukkingLogg

```csharp
namespace Regnskap.Domain.Features.Periodeavslutning;

using Regnskap.Domain.Common;

/// <summary>
/// Logger alle periodeavslutningssteg for etterprøvbarhet.
/// </summary>
public class PeriodeLukkingLogg : AuditableEntity
{
    public int Ar { get; set; }
    public int Periode { get; set; }
    public PeriodeLukkingSteg Steg { get; set; }
    public string Beskrivelse { get; set; } = default!;
    public string Status { get; set; } = default!; // "OK", "ADVARSEL", "FEIL"
    public string? Detaljer { get; set; }
    public DateTime Tidspunkt { get; set; }
    public string UtfortAv { get; set; } = default!;
}
```

#### ArsavslutningStatus

```csharp
namespace Regnskap.Domain.Features.Periodeavslutning;

using Regnskap.Domain.Common;

/// <summary>
/// Sporer statusen for en arsavslutning.
/// </summary>
public class ArsavslutningStatus : AuditableEntity
{
    public int Ar { get; set; }
    public ArsavslutningFase Fase { get; set; } = ArsavslutningFase.IkkeStartet;

    /// <summary>
    /// Arsresultat som skal disponeres.
    /// Beregnes som netto resultat for kontoklasse 3-8.
    /// </summary>
    public decimal? Arsresultat { get; set; }

    /// <summary>
    /// Konto for disponering av overskudd/underskudd.
    /// Standard: 2050 (Annen innskutt EK) eller 2100 (Annen opptjent EK).
    /// </summary>
    public string? DisponeringKontonummer { get; set; }

    /// <summary>
    /// Bilag-ID for arsavslutningsbilaget.
    /// </summary>
    public Guid? ArsavslutningBilagId { get; set; }

    /// <summary>
    /// Bilag-ID for apningsbalanse neste ar.
    /// </summary>
    public Guid? ApningsbalanseBilagId { get; set; }

    /// <summary>
    /// Tidspunkt arsavslutning ble fullfort.
    /// </summary>
    public DateTime? FullfortTidspunkt { get; set; }
    public string? FullfortAv { get; set; }
}
```

### Enums

```csharp
namespace Regnskap.Domain.Features.Periodeavslutning;

public enum PeriodiseringsType
{
    /// <summary>Forskuddsbetalt kostnad (prepaid expense). Balanse -> Resultat.</summary>
    ForskuddsbetaltKostnad,

    /// <summary>Palopte kostnader (accrued expense). Resultat -> Balanse (gjeld).</summary>
    PaloptKostnad,

    /// <summary>Forskuddsbetalt inntekt (deferred revenue). Balanse (gjeld) -> Resultat.</summary>
    ForskuddsbetaltInntekt,

    /// <summary>Opptjent, ikke fakturert inntekt (accrued revenue). Resultat -> Balanse (fordring).</summary>
    OpptjentInntekt
}

public enum PeriodeLukkingSteg
{
    AvskrivningerBeregnet,
    PeriodiseringerBokfort,
    AvstemmingKjort,
    SaldokontrollBestatt,
    BilagsnummerKontrollert,
    MvaAvstemt,
    PeriodeLukket
}

public enum ArsavslutningFase
{
    IkkeStartet,
    AllePerioderLukket,
    ArsoppgjorBilagOpprettet,
    ResultatDisponert,
    ResultatkontoerNullstilt,
    ApningsbalanseOpprettet,
    Fullfort
}
```

### EF Core-konfigurasjon

```csharp
// AnleggsmiddelConfiguration
builder.ToTable("Anleggsmiddel");
builder.HasKey(e => e.Id);
builder.Property(e => e.Navn).IsRequired().HasMaxLength(200);
builder.Property(e => e.Anskaffelseskostnad).HasPrecision(18, 2);
builder.Property(e => e.Restverdi).HasPrecision(18, 2);
builder.Property(e => e.BalanseKontonummer).IsRequired().HasMaxLength(6);
builder.Property(e => e.AvskrivningsKontonummer).IsRequired().HasMaxLength(6);
builder.Property(e => e.AkkumulertAvskrivningKontonummer).IsRequired().HasMaxLength(6);
builder.HasMany(e => e.Avskrivninger)
    .WithOne(a => a.Anleggsmiddel)
    .HasForeignKey(a => a.AnleggsmiddelId)
    .OnDelete(DeleteBehavior.Restrict);
builder.HasIndex(e => e.BalanseKontonummer);

// AvskrivningHistorikkConfiguration
builder.ToTable("AvskrivningHistorikk");
builder.HasKey(e => e.Id);
builder.Property(e => e.Belop).HasPrecision(18, 2);
builder.Property(e => e.AkkumulertEtter).HasPrecision(18, 2);
builder.Property(e => e.BokfortVerdiEtter).HasPrecision(18, 2);
builder.HasIndex(e => new { e.AnleggsmiddelId, e.Ar, e.Periode }).IsUnique();

// PeriodiseringConfiguration
builder.ToTable("Periodisering");
builder.HasKey(e => e.Id);
builder.Property(e => e.Beskrivelse).IsRequired().HasMaxLength(500);
builder.Property(e => e.TotalBelop).HasPrecision(18, 2);
builder.Property(e => e.BalanseKontonummer).IsRequired().HasMaxLength(6);
builder.Property(e => e.ResultatKontonummer).IsRequired().HasMaxLength(6);
builder.HasMany(e => e.Posteringer)
    .WithOne(p => p.Periodisering)
    .HasForeignKey(p => p.PeriodiseringId)
    .OnDelete(DeleteBehavior.Restrict);

// PeriodiseringsHistorikkConfiguration
builder.ToTable("PeriodiseringsHistorikk");
builder.HasKey(e => e.Id);
builder.Property(e => e.Belop).HasPrecision(18, 2);
builder.HasIndex(e => new { e.PeriodiseringId, e.Ar, e.Periode }).IsUnique();

// PeriodeLukkingLoggConfiguration
builder.ToTable("PeriodeLukkingLogg");
builder.HasKey(e => e.Id);
builder.Property(e => e.Beskrivelse).IsRequired().HasMaxLength(500);
builder.Property(e => e.Status).IsRequired().HasMaxLength(20);
builder.Property(e => e.UtfortAv).IsRequired().HasMaxLength(100);
builder.HasIndex(e => new { e.Ar, e.Periode, e.Steg });

// ArsavslutningStatusConfiguration
builder.ToTable("ArsavslutningStatus");
builder.HasKey(e => e.Id);
builder.Property(e => e.Arsresultat).HasPrecision(18, 2);
builder.Property(e => e.DisponeringKontonummer).HasMaxLength(6);
builder.HasIndex(e => e.Ar).IsUnique();
```

---

## API-kontrakt

### 1. Manedlig avstemming

#### `POST /api/periodeavslutning/{ar}/{periode}/avstemming`

Kjorer komplett avstemming for en maned. Returnerer sjekkliste med status per kontroll.

**Response: `AvstemmingResultatDto`**

```csharp
public record AvstemmingResultatDto(
    int Ar,
    int Periode,
    bool ErKlarForLukking,
    List<AvstemmingKontrollDto> Kontroller,
    List<AvstemmingAdvarselDto> Advarsler
);

public record AvstemmingKontrollDto(
    string Kode,
    string Beskrivelse,
    string Status,  // "OK", "ADVARSEL", "FEIL"
    string? Detaljer
);

public record AvstemmingAdvarselDto(
    string Kode,
    string Melding,
    string Alvorlighet  // "INFO", "ADVARSEL", "KRITISK"
);
```

**Kontroller som kjores:**

| Kode | Beskrivelse | Blokkerer lukking |
|---|---|---|
| FORRIGE_LUKKET | Forrige periode er lukket (sekvensiell lukking) | Ja |
| DEBET_KREDIT | Sum debet = sum kredit for alle bilag i perioden | Ja |
| SALDO_KONTROLL | Materialiserte saldoer stemmer med posteringer | Ja |
| FORTLOPENDE_NR | Bilagsnumre er fortlopende uten hull | Ja |
| MVA_AVSTEMT | MVA-kontoer (26xx) stemmer med MVA-oppgjor for terminen | Nei (advarsel) |
| BANK_AVSTEMT | Bankkonto (19xx) stemmer med siste bankavstemmings UB | Nei (advarsel) |
| KUNDESALDO | Kundereskontro stemmer med sammendraget pa konto 1500 | Nei (advarsel) |
| LEVERANDORSALDO | Leverandorreskontro stemmer med konto 2400 | Nei (advarsel) |
| UBOKFORTE_BILAG | Alle bilag i perioden er bokfort (ErBokfort = true) | Ja |
| ALLE_PERIODER_HAR_SALDO | Alle kontoer med posteringer har KontoSaldo-rad | Ja |

---

### 2. Manedlig lukking

#### `POST /api/periodeavslutning/{ar}/{periode}/lukk`

Lukker en maned slik at ingen flere posteringer kan legges til.

**Request:**

```csharp
public record LukkPeriodeRequest(
    string? Merknad = null,
    bool TvingLukking = false  // Overstyrer advarsler (ikke feil)
);
```

**Response: `PeriodeLukkingDto`**

```csharp
public record PeriodeLukkingDto(
    int Ar,
    int Periode,
    string NyStatus,
    DateTime LukketTidspunkt,
    string LukketAv,
    AvstemmingResultatDto Avstemming,
    List<PeriodeLukkingLoggDto> Logg
);

public record PeriodeLukkingLoggDto(
    string Steg,
    string Beskrivelse,
    string Status,
    string? Detaljer,
    DateTime Tidspunkt
);
```

**Prosessflyt:**

```
1. Valider at perioden er Apen eller Sperret (ikke allerede Lukket)
2. Sperr perioden midlertidig (Status = Sperret)
3. Kjor komplett avstemming
4. Hvis avstemming har FEIL:
   a. Hvis TvingLukking = false: returner feil, hold perioden Sperret
   b. TvingLukking kan ALDRI overstyre FEIL, kun ADVARSEL
5. Hvis OK (eller bare advarsler + TvingLukking):
   a. Sett Status = Lukket
   b. Sett LukketTidspunkt og LukketAv
   c. Logg alle steg til PeriodeLukkingLogg
6. Returner resultat
```

#### `POST /api/periodeavslutning/{ar}/{periode}/gjenapne`

Gjenapner en lukket periode (krever spesiell tilgang).

**Request:**

```csharp
public record GjenapnePeriodeRequest(
    string Begrunnelse
);
```

**Forretningsregel:** Gjenaping er kun tillatt hvis INGEN etterfølgende periode er lukket. Begrunnelsen logges.

---

### 3. Arsavslutning

#### `POST /api/periodeavslutning/{ar}/arsavslutning`

Starter og gjennomforer arsavslutningsprosessen.

**Request:**

```csharp
public record ArsavslutningRequest(
    /// <summary>
    /// Konto for disponering av overskudd/underskudd.
    /// Standard: "2050" (Annen innskutt EK) eller "2100" (Udisponert resultat).
    /// </summary>
    string DisponeringKontonummer = "2050",

    /// <summary>
    /// Eventuelt utbyttebelop (trekkes fra disponeringsbelop).
    /// Krever at arsresultat >= utbyttebelop.
    /// </summary>
    decimal? Utbytte = null,

    /// <summary>
    /// Konto for utbytte. Standard: "2800" (Avsatt utbytte).
    /// </summary>
    string UtbytteKontonummer = "2800"
);
```

**Response: `ArsavslutningDto`**

```csharp
public record ArsavslutningDto(
    int Ar,
    ArsavslutningFase Fase,
    decimal Arsresultat,
    decimal? Utbytte,
    decimal DisponertTilEgenkapital,
    Guid ArsavslutningBilagId,
    Guid ApningsbalanseBilagId,
    List<ArsavslutningStegDto> Steg,
    DateTime FullfortTidspunkt,
    string FullfortAv
);

public record ArsavslutningStegDto(
    string Steg,
    string Beskrivelse,
    string Status,
    string? Detaljer
);
```

**Arsavslutningsprosessen (sekvensiell):**

```
STEG 1: VALIDER FORUTSETNINGER
  - Alle perioder 1-12 MA vaere Lukket
  - Periode 13 (arsavslutning) MA vaere Apen
  - Ingen tidligere arsavslutning for dette aret

STEG 2: BEREGN ARSRESULTAT
  - Arsresultat = Sum netto for alle resultatkontoer (klasse 3-8)
  - Inntekter (kredit-normert, klasse 3): Sum kredit - Sum debet
  - Kostnader (debet-normert, klasse 4-8): Sum debet - Sum kredit
  - Arsresultat = Inntekter - Kostnader
  - Positivt = overskudd, negativt = underskudd

STEG 3: OPPRETT ARSAVSLUTNINGSBILAG (Periode 13)
  Bilag med BilagType.Arsavslutning:

  a) Nullstill alle resultatkontoer:
     For hver resultatkonto med saldo != 0:
       Hvis UB er debet-saldo: Kredit konto, Debet 8900/8800 (arsoppgjor)
       Hvis UB er kredit-saldo: Debet konto, Kredit 8900/8800 (arsoppgjor)

  b) Disponer resultatet:
     Hvis overskudd (positiv arsresultat):
       Debet 8800 (Arsresultat)       arsresultat
       Kredit {disponeringskonto}      arsresultat - utbytte
       Kredit 2800 (Avsatt utbytte)    utbytte (hvis angitt)

     Hvis underskudd (negativ arsresultat):
       Debet {disponeringskonto}        |arsresultat|
       Kredit 8800 (Arsresultat)        |arsresultat|

  c) Valider at bilaget er i balanse (debet = kredit)

STEG 4: BOKFOR ARSAVSLUTNINGSBILAG
  - Bokfor bilaget mot hovedbok (oppdater KontoSaldo for periode 13)
  - Verifiser at alle resultatkontoer na har UB = 0

STEG 5: OPPRETT APNINGSBALANSE FOR NESTE AR
  a) Opprett perioder for neste ar (ar + 1) hvis de ikke finnes
  b) Opprett bilag med BilagType.Apningsbalanse i periode 0 for ar + 1
  c) For hver balansepost (kontoklasse 1-2) med UB != 0 i periode 13:
     - Opprett postering med IB = UB fra forrige ar
     - Eiendeler (klasse 1): Debet med positiv saldo
     - EK/Gjeld (klasse 2): Kredit med positiv saldo (kredit-normert)
  d) Valider at apningsbalanse-bilaget er i balanse
  e) Bokfor apningsbalansebilaget
  f) Sett InngaendeBalanse pa KontoSaldo for periode 1 i neste ar

STEG 6: LUKK PERIODE 13
  - Sett periode 13 status = Lukket

STEG 7: OPPDATER ARSAVSLUTNING-STATUS
  - Fase = Fullfort
  - FullfortTidspunkt, FullfortAv
```

#### `GET /api/periodeavslutning/{ar}/arsavslutning/status`

Hent status for arsavslutning.

#### `POST /api/periodeavslutning/{ar}/arsavslutning/reverser`

Reverser en arsavslutning (gjenapner alle perioder og sletter avslutningsbilag). Kun mulig hvis neste ars perioder ikke har posteringer (utover apningsbalanse).

---

### 4. Avskrivninger

#### `POST /api/anleggsmidler`

Registrer nytt anleggsmiddel.

**Request:**

```csharp
public record OpprettAnleggsmiddelRequest(
    string Navn,
    string? Beskrivelse,
    DateOnly Anskaffelsesdato,
    decimal Anskaffelseskostnad,
    decimal Restverdi,
    int LevetidManeder,
    string BalanseKontonummer,
    string AvskrivningsKontonummer,
    string AkkumulertAvskrivningKontonummer,
    string? Avdelingskode,
    string? Prosjektkode
);
```

**Valideringsregler:**

| Felt | Regel |
|---|---|
| Anskaffelseskostnad | Ma vaere > 0 |
| Restverdi | Ma vaere >= 0 og < Anskaffelseskostnad |
| LevetidManeder | Ma vaere >= 1 |
| BalanseKontonummer | Ma vaere gyldig konto i klasse 1 (10xx-12xx) |
| AvskrivningsKontonummer | Ma vaere gyldig konto i klasse 6 (typisk 60xx) |
| AkkumulertAvskrivningKontonummer | Ma vaere gyldig konto i klasse 1 (kontra-konto) |

#### `GET /api/anleggsmidler`

List anleggsmidler med status.

**Query-parametre:**

| Parameter | Type | Pakreves | Beskrivelse |
|---|---|---|---|
| aktive | bool | Nei | Filtrer pa aktive (default: true) |
| kontonummer | string | Nei | Filtrer pa balansekonto |

#### `GET /api/anleggsmidler/{id}`

Hent ett anleggsmiddel med avskrivningshistorikk.

#### `POST /api/avskrivninger/beregn`

Beregner avskrivninger for en periode, returnerer forhåndsvisning uten å bokfore.

**Request:**

```csharp
public record BeregnAvskrivningerRequest(
    int Ar,
    int Periode
);
```

**Response: `AvskrivningBeregningDto`**

```csharp
public record AvskrivningBeregningDto(
    int Ar,
    int Periode,
    List<AvskrivningLinjeDto> Linjer,
    decimal TotalAvskrivning,
    int AntallAnleggsmidler
);

public record AvskrivningLinjeDto(
    Guid AnleggsmiddelId,
    string Navn,
    string BalanseKontonummer,
    string AvskrivningsKontonummer,
    string AkkumulertAvskrivningKontonummer,
    decimal Belop,
    decimal AkkumulertFor,
    decimal AkkumulertEtter,
    decimal BokfortVerdiEtter,
    bool ErSisteAvskrivning
);
```

#### `POST /api/avskrivninger/bokfor`

Bokforer beregnede avskrivninger.

**Request:**

```csharp
public record BokforAvskrivningerRequest(
    int Ar,
    int Periode
);
```

**Bilagsstruktur for avskrivninger:**

```
BilagType: Avskrivning
Beskrivelse: "Avskrivninger periode {ar}-{periode:D2}"
Bilagserie: AVS (automatisk)

For hvert anleggsmiddel:
  Linje N:   Debet {avskrivningskonto}              {belop}
  Linje N+1: Kredit {akkumulert avskrivning-konto}   {belop}
```

---

### 5. Periodisering

#### `POST /api/periodiseringer`

Opprett ny periodisering.

**Request:**

```csharp
public record OpprettPeriodiseringRequest(
    string Beskrivelse,
    PeriodiseringsType Type,
    decimal TotalBelop,
    int FraAr,
    int FraPeriode,
    int TilAr,
    int TilPeriode,
    string BalanseKontonummer,
    string ResultatKontonummer,
    string? Avdelingskode,
    string? Prosjektkode,
    Guid? OpprinneligBilagId
);
```

**Valideringsregler:**

| Felt | Regel |
|---|---|
| TotalBelop | Ma vaere > 0 |
| FraPeriode/TilPeriode | 1-12 |
| Fra ma vaere for eller lik Til | FraAr*12+FraPeriode <= TilAr*12+TilPeriode |
| BalanseKontonummer | Ma vaere gyldig balansepost |
| ResultatKontonummer | Ma vaere gyldig resultatpost |

#### `GET /api/periodiseringer`

List periodiseringer.

#### `POST /api/periodiseringer/bokfor`

Bokforer periodiseringer for en gitt periode.

**Request:**

```csharp
public record BokforPeriodiseringerRequest(
    int Ar,
    int Periode
);
```

**Response: `PeriodiseringBokforingDto`**

```csharp
public record PeriodiseringBokforingDto(
    int Ar,
    int Periode,
    List<PeriodiseringLinjeDto> Linjer,
    decimal TotalBelop,
    Guid BilagId
);

public record PeriodiseringLinjeDto(
    Guid PeriodiseringId,
    string Beskrivelse,
    PeriodiseringsType Type,
    decimal Belop,
    decimal GjenstaarEtter
);
```

**Bilagsstruktur per periodiseringstype:**

```
FORSKUDDSBETALT KOSTNAD (f.eks. arsforsikring betalt i januar, periodiseres over 12 mnd):
  Ved opprinnelig betaling (manuelt bilag):
    Debet 1700 Forskuddsbetalt kostnad    12 000
    Kredit 1920 Bank                       12 000

  Manedlig periodisering (automatisk):
    Debet 6300 Forsikring                   1 000
    Kredit 1700 Forskuddsbetalt kostnad     1 000

PALOPT KOSTNAD (f.eks. palapt feriepenger):
    Debet 5000 Lonn (feriepengekostnad)     belop
    Kredit 2780 Palapt feriepenger          belop

FORSKUDDSBETALT INNTEKT (f.eks. kundebetaling for 12 mnd service):
    Debet 2900 Forskuddsbetalt inntekt      belop
    Kredit 3000 Salgsinntekt                belop

OPPTJENT INNTEKT (f.eks. utfort arbeid, ikke fakturert):
    Debet 1500 Opptjent inntekt             belop
    Kredit 3000 Salgsinntekt                belop
```

---

### 6. Arsregnskapsklargjoring

#### `GET /api/periodeavslutning/{ar}/klargjoring`

Sjekker om arsregnskapet er klart for innsending.

**Response: `ArsregnskapsklarDto`**

```csharp
public record ArsregnskapsklarDto(
    int Ar,
    bool ErKlar,
    List<KlargjoringKontrollDto> Kontroller,
    FilingDeadlineDto Frister
);

public record KlargjoringKontrollDto(
    string Kode,
    string Beskrivelse,
    string Status,   // "OK", "FEIL", "ADVARSEL"
    string? Detaljer
);

public record FilingDeadlineDto(
    DateOnly Godkjenningsfrist,    // 6 mnd etter regnskapsarsslutt
    DateOnly Innsendingsfrist,     // 31. juli (for 31.12-ar)
    bool ErFristUtlopt
);
```

**Kontroller:**

| Kode | Beskrivelse |
|---|---|
| ARSAVSLUTNING | Arsavslutning er gjennomfort |
| BALANSEKONTROLL | Eiendeler = EK + Gjeld |
| RESULTAT_DISPONERT | Arsresultat er disponert |
| APNINGSBALANSE | Apningsbalanse for neste ar er opprettet |
| MVA_AVSLUTTET | Alle MVA-terminer for aret er avsluttet/innsendt |
| SAFT_GYLDIG | SAF-T kan genereres uten feil |
| ALLE_PERIODER_LUKKET | Perioder 1-13 er lukket |
| BALANSEPOST_DOKUMENTERT | Balansepostene er dokumentert (NBS 5) -- manuell bekreftelse |

---

## Forretningsregler

### Manedlig lukking

1. **FR-P01:** Perioder MA lukkes sekvensielt. Periode N kan ikke lukkes for periode N-1 er lukket. Unntaket er periode 1 (ingen forrige) og periode 0 (apningsbalanse, kan lukkes uavhengig).

2. **FR-P02:** En lukket periode kan ALDRI fa nye posteringer. Korreksjon av feil i lukket periode skjer med korreksjonsbilag i neste apne periode.

3. **FR-P03:** Gjenaping av en lukket periode er mulig, men KUN hvis ingen etterfølgende periode er lukket. Gjenaping logges med begrunnelse.

4. **FR-P04:** Avstemming er obligatorisk for lukking. Avstemming kjores automatisk ved lukkingsforsok. Alle kontroller med "FEIL"-status blokkerer lukking -- TvingLukking overstyrer kun "ADVARSEL".

5. **FR-P05:** Manedlig lukking folger PeriodeService.EndrePeriodeStatusAsync med overgang Apen -> Sperret -> Lukket. Periodeavslutningsmodulen orkestrerer dette.

### Arsavslutning

6. **FR-P06:** Arsavslutning krever at ALLE perioder 1-12 er lukket. Periode 13 brukes kun for arsavslutningsbilag.

7. **FR-P07:** Arsresultat beregnes som netto for kontoklasse 3-8.
   ```
   Arsresultat = Sum(kredit - debet) for kontogruppe 30-89
   ```
   Positivt = overskudd, negativt = underskudd.

8. **FR-P08:** Ved disponering av resultat:
   - Overskudd: Debet 8800 -> Kredit disponeringskonto (og evt. Kredit 2800 for utbytte)
   - Underskudd: Debet disponeringskonto -> Kredit 8800
   - Standard disponeringskonto: 2050 (Annen innskutt EK) eller 2100 (Udisponert resultat)

9. **FR-P09:** Etter arsavslutning MA alle resultatkontoer (klasse 3-8) ha UB = 0. Balansekontoer (klasse 1-2) beholder sine saldoer.

10. **FR-P10:** Apningsbalanse for neste ar kopierer UB fra balansekontoer som IB. Resultatkontoer far IB = 0 (de er nullstilt).

11. **FR-P11:** Arsavslutningsbilaget og apningsbalansebilaget MA begge vaere i balanse (debet = kredit).

### Avskrivninger

12. **FR-P12:** Lineaer avskrivning:
    ```
    Manedlig belop = (Anskaffelseskostnad - Restverdi) / LevetidManeder
    ```
    Avrunding til 2 desimaler. Siste periode tar differansen for a unnga avrundingsfeil.

13. **FR-P13:** Avskrivning starter fra maneden etter anskaffelse. Anleggsmiddel kjopt 15. mars far forste avskrivning i april.

14. **FR-P14:** Avskrivning stopper nar BokfortVerdi = Restverdi (fullt avskrevet) eller ved utrangering/salg.

15. **FR-P15:** Avskrivningsbilag bokfores i hovedbok med BilagType.Avskrivning. Hvert anleggsmiddel far egne posteringslinjer for sporbarhet.

16. **FR-P16:** Dobbel avskrivning for samme periode er ikke tillatt. Sjekk mot AvskrivningHistorikk for eksisterende rad for (anleggsmiddelId, ar, periode).

### Periodisering

17. **FR-P17:** Periodisering fordeler belop jevnt over perioder. Siste periode tar gjenstaende belop (for a handtere avrunding).
    ```
    Belop per periode = Round(TotalBelop / AntallPerioder, 2)
    Siste periode = TotalBelop - Sum(alle tidligere perioder)
    ```

18. **FR-P18:** Periodisering kan ikke bokfores for en lukket periode. Systemet hopper over lukkede perioder og akkumulerer til neste apne.

19. **FR-P19:** En periodisering avsluttes automatisk nar siste periode er bokfort (GjenstaendeBelop = 0).

20. **FR-P20:** Dobbel periodisering for samme (periodisering, ar, periode) er ikke tillatt.

### Arsregnskapsklargjoring

21. **FR-P21:** Arsregnskapet er klart for innsending nar alle kontroller i klargjoring gir "OK". Noen kontroller (BALANSEPOST_DOKUMENTERT) krever manuell bekreftelse.

22. **FR-P22:** Frister beregnes fra regnskapsarets slutt:
    - Godkjenningsfrist: 6 maneder etter arsavslutning (30. juni for 31.12-ar)
    - Innsendingsfrist til Regnskapsregisteret: 31. juli (for 31.12-ar)

---

## MVA-handtering

Periodeavslutningsmodulen har ingen direkte MVA-logikk, men:

- **Manedlig avstemming** sjekker at MVA-kontoer (26xx) stemmer med MVA-oppgjor
- **Arsregnskapsklargjoring** sjekker at alle MVA-terminer er avsluttet
- Avskrivninger og periodiseringer bruker ikke MVA (de er MVA-frie interne posteringer)

---

## Avhengigheter

### Brukte interfaces fra andre moduler

| Interface/Service | Modul | Bruk |
|---|---|---|
| IHovedbokRepository | Hovedbok | Hent/oppdater KontoSaldo, Posteringer, Perioder |
| IPeriodeService | Hovedbok | EndrePeriodeStatus, HentPerioder |
| IKontoService | Kontoplan | Valider kontoer, hent kontotype |
| IBilagRegistreringService | Bilagsregistrering | Opprett avskrivnings-/periodiserings-/arsavslutningsbilag |
| IRapporteringService | Rapportering | Generer resultatregnskap/balanse for validering |
| ISaftEksportService | Rapportering | Valider SAF-T ved arsklargjoring |

### Nye interfaces

```csharp
namespace Regnskap.Application.Features.Periodeavslutning;

public interface IPeriodeavslutningService
{
    // Avstemming
    Task<AvstemmingResultatDto> KjorAvstemmingAsync(
        int ar, int periode, CancellationToken ct = default);

    // Lukking
    Task<PeriodeLukkingDto> LukkPeriodeAsync(
        int ar, int periode, LukkPeriodeRequest request, CancellationToken ct = default);
    Task<PeriodeLukkingDto> GjenapnePeriodeAsync(
        int ar, int periode, GjenapnePeriodeRequest request, CancellationToken ct = default);

    // Arsavslutning
    Task<ArsavslutningDto> KjorArsavslutningAsync(
        int ar, ArsavslutningRequest request, CancellationToken ct = default);
    Task<ArsavslutningStatus> HentArsavslutningStatusAsync(
        int ar, CancellationToken ct = default);
    Task ReverserArsavslutningAsync(
        int ar, string begrunnelse, CancellationToken ct = default);

    // Arsregnskapsklargjoring
    Task<ArsregnskapsklarDto> SjekkKlargjoringAsync(
        int ar, CancellationToken ct = default);
}

public interface IAvskrivningService
{
    // Anleggsmidler
    Task<AnleggsmiddelDto> OpprettAnleggsmiddelAsync(
        OpprettAnleggsmiddelRequest request, CancellationToken ct = default);
    Task<List<AnleggsmiddelDto>> HentAnleggsmidlerAsync(
        bool? aktive = true, string? kontonummer = null, CancellationToken ct = default);
    Task<AnleggsmiddelDto> HentAnleggsmiddelAsync(
        Guid id, CancellationToken ct = default);
    Task UtrangerAnleggsmiddelAsync(
        Guid id, DateOnly utrangeringsDato, CancellationToken ct = default);

    // Avskrivninger
    Task<AvskrivningBeregningDto> BeregnAvskrivningerAsync(
        int ar, int periode, CancellationToken ct = default);
    Task<AvskrivningBokforingDto> BokforAvskrivningerAsync(
        int ar, int periode, CancellationToken ct = default);
}

public interface IPeriodiseringsService
{
    Task<PeriodiseringDto> OpprettPeriodiseringAsync(
        OpprettPeriodiseringRequest request, CancellationToken ct = default);
    Task<List<PeriodiseringDto>> HentPeriodiseringerAsync(
        bool? aktive = true, CancellationToken ct = default);
    Task<PeriodiseringBokforingDto> BokforPeriodiseringerAsync(
        int ar, int periode, CancellationToken ct = default);
    Task DeaktiverPeriodiseringAsync(
        Guid id, CancellationToken ct = default);
}

public interface IPeriodeavslutningRepository
{
    // Anleggsmidler
    Task<Anleggsmiddel?> HentAnleggsmiddelAsync(Guid id, CancellationToken ct = default);
    Task<List<Anleggsmiddel>> HentAnleggsmidlerAsync(
        bool? aktive = null, string? kontonummer = null, CancellationToken ct = default);
    Task LeggTilAnleggsmiddelAsync(Anleggsmiddel anleggsmiddel, CancellationToken ct = default);
    Task<bool> AvskrivningFinnesAsync(Guid anleggsmiddelId, int ar, int periode, CancellationToken ct = default);
    Task LeggTilAvskrivningHistorikkAsync(AvskrivningHistorikk historikk, CancellationToken ct = default);

    // Periodiseringer
    Task<Periodisering?> HentPeriodiseringAsync(Guid id, CancellationToken ct = default);
    Task<List<Periodisering>> HentAktivePeriodiseringerForPeriodeAsync(
        int ar, int periode, CancellationToken ct = default);
    Task LeggTilPeriodiseringAsync(Periodisering periodisering, CancellationToken ct = default);
    Task<bool> PeriodiseringsHistorikkFinnesAsync(
        Guid periodiseringId, int ar, int periode, CancellationToken ct = default);
    Task LeggTilPeriodiseringsHistorikkAsync(
        PeriodiseringsHistorikk historikk, CancellationToken ct = default);

    // Logg
    Task LeggTilPeriodeLukkingLoggAsync(PeriodeLukkingLogg logg, CancellationToken ct = default);
    Task<List<PeriodeLukkingLogg>> HentPeriodeLukkingLoggerAsync(
        int ar, int periode, CancellationToken ct = default);

    // Arsavslutning
    Task<ArsavslutningStatus?> HentArsavslutningStatusAsync(int ar, CancellationToken ct = default);
    Task LagreArsavslutningStatusAsync(ArsavslutningStatus status, CancellationToken ct = default);

    Task LagreEndringerAsync(CancellationToken ct = default);
}
```

---

## Eksempler

### Eksempel: Avskrivning

Maskin kjopt 1. januar 2026:
- Anskaffelseskostnad: 120 000 kr
- Restverdi: 0 kr
- Levetid: 60 maneder (5 ar)

Manedlig avskrivning = (120 000 - 0) / 60 = 2 000 kr

Bilag for januar 2026:
```
Debet  6000 Avskrivning                2 000
Kredit 1209 Akkumulerte avskrivninger  2 000
```

Etter 12 maneder (desember 2026):
- Akkumulert avskrivning: 24 000 kr
- Bokfort verdi: 96 000 kr

### Eksempel: Periodisering (forskuddsbetalt kostnad)

Arsforsikring betalt 1. januar 2026: 24 000 kr, dekker jan-des 2026.

Opprinnelig bilag (manuelt):
```
Debet  1700 Forskuddsbetalt kostnad   24 000
Kredit 1920 Bank                       24 000
```

Manedlig periodisering (12 maneder):
```
Debet  7500 Forsikring                 2 000
Kredit 1700 Forskuddsbetalt kostnad    2 000
```

Etter 12 maneder: Konto 1700 har saldo 0, konto 7500 har 24 000 i kostnad.

### Eksempel: Arsavslutning

Gitt at arsresultat for 2026 er 97 500 kr (overskudd).

Arsavslutningsbilag (periode 13):

Steg 1 -- Nullstill resultatkontoer:
```
Debet  3000 Salgsinntekt           1 000 000   (nullstill kredit-saldo)
Kredit 4000 Varekjop                 400 000   (nullstill debet-saldo)
Kredit 5000 Lonn                     300 000
Kredit 6000 Avskrivning               50 000
Kredit 6300 Husleie                   100 000
Kredit 8000 Renteinntekt               5 000   (nullstill kredit-saldo, men vi debeterer)
Debet  8400 Rentekostnad              20 000   (nullstill debet-saldo, men vi krediterer)
Kredit 8900 Skattekostnad             37 500
-- Midlertidig ubalanse fanges opp via motpostering til 8800

Netto: 1 000 000 + 5 000 - 400 000 - 300 000 - 50 000 - 100 000 - 20 000 - 37 500 = 97 500
```

Forenklet som to-trinns:
```
Trinn 1: Overfør netto resultat til 8800
  Alle resultatkontoer nullstilles mot 8800 Arsresultat

Trinn 2: Disponer arsresultat
  Debet  8800 Arsresultat              97 500
  Kredit 2050 Annen innskutt EK        97 500
```

Apningsbalanse 2027 (periode 0):
```
Debet  1200 Maskiner                  250 000
Debet  1500 Kundefordringer           150 000
Debet  1920 Bank                      300 000
Kredit 2000 Aksjekapital              100 000
Kredit 2050 Annen innskutt EK         347 500  (250 000 + 97 500)
Kredit 2400 Leverandorgjeld           200 000
Kredit 2600 Skyldig MVA                62 500
Kredit 2900 Annen gjeld                37 500
-- Kontroll: Debet 700 000 = Kredit 747 500... 

NB: Tallene er illustrative. I praksis vises alle balansekontoer med UB fra periode 12/13.
Sum eiendeler = Sum EK+gjeld er allerede garantert av balansekontrollen.
```
