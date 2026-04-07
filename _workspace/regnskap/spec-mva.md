# Spesifikasjon: MVA-handtering (VAT Management)

**Modul:** MVA-handtering
**Status:** Komplett spesifikasjon
**Avhengigheter:** Kontoplan (implementert), Hovedbok (implementert), Bilagsregistrering (implementert — MVA auto-postering)
**SAF-T-seksjon:** MasterFiles > TaxTable, GeneralLedgerEntries (TaxInformation per linje)
**Lovgrunnlag:** Merverdiavgiftsloven (Lov 2009-06-19 nr. 58), Skatteforvaltningsloven §8-3, Bokforingsloven §5 nr. 5 (MVA-spesifikasjon)

---

## Oversikt

MVA-modulen bygger PA TOPPEN av eksisterende MVA-infrastruktur:

- **MvaKode** (Kontoplan): Definerer MVA-koder med sats, retning, SAF-T StandardTaxCode, og kontotilknytning
- **Postering** (Hovedbok): Har MvaKode, MvaBelop, MvaGrunnlag, MvaSats, ErAutoGenerertMva
- **BilagRegistreringService** (Bilag): Automatisk MVA-postering ved bilagsregistrering (GenererMvaPosteringerAsync)

MVA-modulen dupliserer INGEN av denne logikken. Den tilforer:

1. **MVA-perioder** — Norske MVA-terminer (6 tomandersperioder eller arstermin)
2. **MVA-oppgjor** — Beregning av MVA til betaling/tilgode per termin
3. **MVA-melding** — Generere MVA-meldingsdata i RF-0002-struktur
4. **MVA-avstemming** — Avstem MVA-kontoer mot beregnede verdier
5. **SAF-T TaxTable** — Generere SAF-T skattetabell-seksjon
6. **MVA-konto sammenstilling** — Oversikt over alle MVA-posteringer per kode og periode

---

## Datamodell

### Nye entiteter

#### MvaTermin

```csharp
namespace Regnskap.Domain.Features.Mva;

using Regnskap.Domain.Common;

/// <summary>
/// En MVA-termin representerer en rapporteringsperiode for merverdiavgift.
/// Standard: 6 tomandersperioder per ar (Skatteforvaltningsloven §8-3, mval §11-1).
/// Arstermin for sma foretak med omsetning under NOK 1.000.000 (mval §11-4).
/// </summary>
public class MvaTermin : AuditableEntity
{
    /// <summary>
    /// Regnskapsaret.
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Terminnummer: 1-6 for tomandersperioder, 1 for arstermin.
    /// </summary>
    public int Termin { get; set; }

    /// <summary>
    /// Type termin.
    /// </summary>
    public MvaTerminType Type { get; set; }

    /// <summary>
    /// Periodens forste dato (inklusiv).
    /// </summary>
    public DateOnly FraDato { get; set; }

    /// <summary>
    /// Periodens siste dato (inklusiv).
    /// </summary>
    public DateOnly TilDato { get; set; }

    /// <summary>
    /// Frist for innlevering og betaling.
    /// Termin 1: 10. april, Termin 2: 10. juni, Termin 3: 31. august,
    /// Termin 4: 10. oktober, Termin 5: 10. desember, Termin 6: 10. februar (neste ar).
    /// Arstermin: 10. mars (neste ar).
    /// </summary>
    public DateOnly Frist { get; set; }

    /// <summary>
    /// Status for terminen.
    /// </summary>
    public MvaTerminStatus Status { get; set; } = MvaTerminStatus.Apen;

    /// <summary>
    /// Tidspunkt da terminen ble avsluttet/innsendt.
    /// </summary>
    public DateTime? AvsluttetTidspunkt { get; set; }

    /// <summary>
    /// Hvem som avsluttet terminen.
    /// </summary>
    public string? AvsluttetAv { get; set; }

    /// <summary>
    /// Referanse til MVA-oppgjorsbilag (bokfort oppgjorsbilag).
    /// </summary>
    public Guid? OppgjorsBilagId { get; set; }

    /// <summary>
    /// Menneskelig lesbart terminnavn.
    /// </summary>
    public string Terminnavn => Type switch
    {
        MvaTerminType.Tomaneders => $"{Ar} Termin {Termin} ({FraDato:MMM}-{TilDato:MMM})",
        MvaTerminType.Arlig => $"{Ar} Arstermin",
        _ => $"{Ar} Termin {Termin}"
    };
}
```

#### MvaOppgjor

```csharp
namespace Regnskap.Domain.Features.Mva;

using Regnskap.Domain.Common;

/// <summary>
/// Beregnet MVA-oppgjor for en termin. Inneholder alle beregnede poster
/// og endelig MVA til betaling eller tilgode.
///
/// Opprettes ved beregning, oppdateres ved ny beregning, lases ved innsending.
/// </summary>
public class MvaOppgjor : AuditableEntity
{
    /// <summary>
    /// FK til MVA-terminen dette oppgjoret gjelder.
    /// </summary>
    public Guid MvaTerminId { get; set; }
    public MvaTermin MvaTermin { get; set; } = default!;

    /// <summary>
    /// Tidspunkt for siste beregning.
    /// </summary>
    public DateTime BeregnetTidspunkt { get; set; }

    /// <summary>
    /// Hvem som kjorte beregningen.
    /// </summary>
    public string BeregnetAv { get; set; } = default!;

    /// <summary>
    /// Sum utgaende MVA (skyldige belop, positiv = skyldig).
    /// </summary>
    public decimal SumUtgaendeMva { get; set; }

    /// <summary>
    /// Sum inngaende MVA (fradragsbelop, positiv = til fradrag).
    /// </summary>
    public decimal SumInngaendeMva { get; set; }

    /// <summary>
    /// Sum snudd avregning utgaende (reverse charge output).
    /// </summary>
    public decimal SumSnuddAvregningUtgaende { get; set; }

    /// <summary>
    /// Sum snudd avregning inngaende (reverse charge input = fradrag).
    /// </summary>
    public decimal SumSnuddAvregningInngaende { get; set; }

    /// <summary>
    /// MVA til betaling. Positivt = skyldig Skatteetaten. Negativt = tilgode.
    /// Beregning: SumUtgaendeMva + SumSnuddAvregningUtgaende - SumInngaendeMva - SumSnuddAvregningInngaende
    /// </summary>
    public decimal MvaTilBetaling { get; set; }

    /// <summary>
    /// Om oppgjoret er last og ikke kan endres.
    /// Settes til true ved innsending av MVA-melding.
    /// </summary>
    public bool ErLast { get; set; }

    /// <summary>
    /// Detaljlinjer per MVA-kode.
    /// </summary>
    public List<MvaOppgjorLinje> Linjer { get; set; } = new();
}
```

#### MvaOppgjorLinje

```csharp
namespace Regnskap.Domain.Features.Mva;

using Regnskap.Domain.Common;

/// <summary>
/// En linje i MVA-oppgjoret, aggregert per MVA-kode.
/// Representerer en post i MVA-meldingen (RF-0002).
/// </summary>
public class MvaOppgjorLinje : AuditableEntity
{
    /// <summary>
    /// FK til oppgjoret.
    /// </summary>
    public Guid MvaOppgjorId { get; set; }
    public MvaOppgjor MvaOppgjor { get; set; } = default!;

    /// <summary>
    /// MVA-kode (intern kode, f.eks. "1", "3", "81").
    /// </summary>
    public string MvaKode { get; set; } = default!;

    /// <summary>
    /// SAF-T StandardTaxCode (f.eks. "1", "3", "81").
    /// Denormalisert fra MvaKode-entiteten for sporbarhet.
    /// </summary>
    public string StandardTaxCode { get; set; } = default!;

    /// <summary>
    /// MVA-sats (snapshot).
    /// </summary>
    public decimal Sats { get; set; }

    /// <summary>
    /// Retning: Inngaende, Utgaende, eller SnuddAvregning.
    /// </summary>
    public MvaRetning Retning { get; set; }

    /// <summary>
    /// RF-0002 postnummer (1-12) som denne linjen tilhorer.
    /// </summary>
    public int RfPostnummer { get; set; }

    /// <summary>
    /// Sum grunnlag (basis for MVA-beregning) for denne koden i terminen.
    /// </summary>
    public decimal SumGrunnlag { get; set; }

    /// <summary>
    /// Sum MVA-belop for denne koden i terminen.
    /// </summary>
    public decimal SumMvaBelop { get; set; }

    /// <summary>
    /// Antall posteringer som inngaar i denne linjen.
    /// </summary>
    public int AntallPosteringer { get; set; }
}
```

#### MvaAvstemming

```csharp
namespace Regnskap.Domain.Features.Mva;

using Regnskap.Domain.Common;

/// <summary>
/// Resultat av MVA-avstemming for en termin.
/// Sammenligner saldo pa MVA-kontoer med beregnede MVA-verdier fra posteringer.
/// </summary>
public class MvaAvstemming : AuditableEntity
{
    /// <summary>
    /// FK til MVA-terminen.
    /// </summary>
    public Guid MvaTerminId { get; set; }
    public MvaTermin MvaTermin { get; set; } = default!;

    /// <summary>
    /// Tidspunkt for avstemming.
    /// </summary>
    public DateTime AvstemmingTidspunkt { get; set; }

    /// <summary>
    /// Hvem som utforte avstemmingen.
    /// </summary>
    public string AvstemmingAv { get; set; } = default!;

    /// <summary>
    /// Om avstemmingen er godkjent (ingen avvik, eller avvik akseptert).
    /// </summary>
    public bool ErGodkjent { get; set; }

    /// <summary>
    /// Eventuell merknad/begrunnelse.
    /// </summary>
    public string? Merknad { get; set; }

    /// <summary>
    /// Detaljlinjer per MVA-konto.
    /// </summary>
    public List<MvaAvstemmingLinje> Linjer { get; set; } = new();
}
```

#### MvaAvstemmingLinje

```csharp
namespace Regnskap.Domain.Features.Mva;

using Regnskap.Domain.Common;

/// <summary>
/// En linje i MVA-avstemmingen, en per MVA-relatert konto.
/// </summary>
public class MvaAvstemmingLinje : AuditableEntity
{
    /// <summary>
    /// FK til avstemmingen.
    /// </summary>
    public Guid MvaAvstemmingId { get; set; }
    public MvaAvstemming MvaAvstemming { get; set; } = default!;

    /// <summary>
    /// Kontonummer (f.eks. "2700", "2710", "1600").
    /// </summary>
    public string Kontonummer { get; set; } = default!;

    /// <summary>
    /// Kontonavn.
    /// </summary>
    public string Kontonavn { get; set; } = default!;

    /// <summary>
    /// Saldo pa kontoen ifg. KontoSaldo (for de aktuelle periodene i terminen).
    /// </summary>
    public decimal SaldoIflgHovedbok { get; set; }

    /// <summary>
    /// Beregnet MVA-belop fra posteringer med MVA-kode for denne kontoen.
    /// </summary>
    public decimal BeregnetFraPosteringer { get; set; }

    /// <summary>
    /// Avvik mellom saldo og beregnet belop.
    /// </summary>
    public decimal Avvik { get; set; }

    /// <summary>
    /// Om linjen har avvik over terskel (0.01 NOK).
    /// </summary>
    public bool HarAvvik => Math.Abs(Avvik) >= 0.01m;
}
```

### Enums

```csharp
namespace Regnskap.Domain.Features.Mva;

/// <summary>
/// Type MVA-termin.
/// </summary>
public enum MvaTerminType
{
    /// <summary>Standard tomandersperiode (6 per ar). Mval §11-1.</summary>
    Tomaaneders,

    /// <summary>Arlig termin for sma foretak under NOK 1.000.000. Mval §11-4.</summary>
    Arlig
}

/// <summary>
/// Status for en MVA-termin.
/// </summary>
public enum MvaTerminStatus
{
    /// <summary>Terminen er apen, transaksjoner kan bokfores.</summary>
    Apen,

    /// <summary>Oppgjor er beregnet, klar for avstemming.</summary>
    Beregnet,

    /// <summary>Avstemming er godkjent, klar for innsending.</summary>
    Avstemt,

    /// <summary>MVA-melding er innsendt.</summary>
    Innsendt,

    /// <summary>Betaling er registrert.</summary>
    Betalt
}
```

### EF Core-konfigurasjon

```csharp
// MvaTermin
builder.HasKey(e => e.Id);
builder.HasIndex(e => new { e.Ar, e.Termin }).IsUnique();
builder.HasIndex(e => e.Status);
builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
builder.HasQueryFilter(e => !e.IsDeleted);

// MvaOppgjor
builder.HasKey(e => e.Id);
builder.HasIndex(e => e.MvaTerminId).IsUnique(); // 1:1 med termin
builder.HasOne(e => e.MvaTermin).WithOne().HasForeignKey<MvaOppgjor>(e => e.MvaTerminId);
builder.Property(e => e.SumUtgaendeMva).HasPrecision(18, 2);
builder.Property(e => e.SumInngaendeMva).HasPrecision(18, 2);
builder.Property(e => e.SumSnuddAvregningUtgaende).HasPrecision(18, 2);
builder.Property(e => e.SumSnuddAvregningInngaende).HasPrecision(18, 2);
builder.Property(e => e.MvaTilBetaling).HasPrecision(18, 2);
builder.HasQueryFilter(e => !e.IsDeleted);

// MvaOppgjorLinje
builder.HasKey(e => e.Id);
builder.HasIndex(e => new { e.MvaOppgjorId, e.MvaKode }).IsUnique();
builder.HasOne(e => e.MvaOppgjor).WithMany(o => o.Linjer).HasForeignKey(e => e.MvaOppgjorId);
builder.Property(e => e.MvaKode).HasMaxLength(10);
builder.Property(e => e.StandardTaxCode).HasMaxLength(10);
builder.Property(e => e.Retning).HasConversion<string>().HasMaxLength(20);
builder.Property(e => e.SumGrunnlag).HasPrecision(18, 2);
builder.Property(e => e.SumMvaBelop).HasPrecision(18, 2);
builder.HasQueryFilter(e => !e.IsDeleted);

// MvaAvstemming
builder.HasKey(e => e.Id);
builder.HasIndex(e => e.MvaTerminId); // kan ha flere avstemminger per termin (historikk)
builder.HasOne(e => e.MvaTermin).WithMany().HasForeignKey(e => e.MvaTerminId);
builder.HasQueryFilter(e => !e.IsDeleted);

// MvaAvstemmingLinje
builder.HasKey(e => e.Id);
builder.HasIndex(e => new { e.MvaAvstemmingId, e.Kontonummer }).IsUnique();
builder.HasOne(e => e.MvaAvstemming).WithMany(a => a.Linjer).HasForeignKey(e => e.MvaAvstemmingId);
builder.Property(e => e.Kontonummer).HasMaxLength(10);
builder.Property(e => e.Kontonavn).HasMaxLength(200);
builder.Property(e => e.SaldoIflgHovedbok).HasPrecision(18, 2);
builder.Property(e => e.BeregnetFraPosteringer).HasPrecision(18, 2);
builder.Property(e => e.Avvik).HasPrecision(18, 2);
builder.HasQueryFilter(e => !e.IsDeleted);
```

### Seed-data: Standard MVA-terminer

Systemet genererer MVA-terminer automatisk per regnskapsar. For standard tomandersperioder:

| Termin | FraDato | TilDato | Frist |
|--------|---------|---------|-------|
| 1 | 1. januar | 28/29. februar | 10. april |
| 2 | 1. mars | 30. april | 10. juni |
| 3 | 1. mai | 30. juni | 31. august |
| 4 | 1. juli | 31. august | 10. oktober |
| 5 | 1. september | 31. oktober | 10. desember |
| 6 | 1. november | 31. desember | 10. februar (neste ar) |

Termin 3 har forlenget frist (31. august) grunnet sommerferie.

For arstermin: FraDato = 1. januar, TilDato = 31. desember, Frist = 10. mars (neste ar).

---

## Repository-kontrakt

```csharp
namespace Regnskap.Domain.Features.Mva;

public interface IMvaRepository
{
    // --- Terminer ---
    Task<List<MvaTermin>> HentTerminerForArAsync(int ar, CancellationToken ct = default);
    Task<MvaTermin?> HentTerminAsync(Guid id, CancellationToken ct = default);
    Task<MvaTermin?> HentTerminForDatoAsync(DateOnly dato, CancellationToken ct = default);
    Task<MvaTermin?> HentTerminAsync(int ar, int termin, CancellationToken ct = default);
    Task<bool> TerminerFinnesForArAsync(int ar, CancellationToken ct = default);
    Task LeggTilTerminAsync(MvaTermin termin, CancellationToken ct = default);
    Task LeggTilTerminerAsync(IEnumerable<MvaTermin> terminer, CancellationToken ct = default);

    // --- Oppgjor ---
    Task<MvaOppgjor?> HentOppgjorForTerminAsync(Guid terminId, CancellationToken ct = default);
    Task<MvaOppgjor?> HentOppgjorMedLinjerAsync(Guid oppgjorId, CancellationToken ct = default);
    Task LeggTilOppgjorAsync(MvaOppgjor oppgjor, CancellationToken ct = default);

    // --- Avstemming ---
    Task<MvaAvstemming?> HentSisteAvstemmingForTerminAsync(Guid terminId, CancellationToken ct = default);
    Task<List<MvaAvstemming>> HentAvstemmingshistorikkAsync(Guid terminId, CancellationToken ct = default);
    Task LeggTilAvstemmingAsync(MvaAvstemming avstemming, CancellationToken ct = default);

    // --- Posteringsaggregering (leser fra Hovedbok) ---
    /// <summary>
    /// Henter aggregerte MVA-data fra posteringer i en gitt periode, gruppert per MvaKode.
    /// Kun bokforte posteringer med MvaKode != null inkluderes.
    /// </summary>
    Task<List<MvaAggregeringDto>> HentMvaAggregertForPeriodeAsync(
        DateOnly fraDato, DateOnly tilDato, CancellationToken ct = default);

    /// <summary>
    /// Henter alle MVA-posteringer (detaljert) for en periode, gruppert per MvaKode.
    /// </summary>
    Task<List<MvaPosteringDetalj>> HentMvaPosteringerForPeriodeAsync(
        DateOnly fraDato, DateOnly tilDato, string? mvaKode = null, CancellationToken ct = default);

    /// <summary>
    /// Henter saldo for MVA-relaterte kontoer (2600-2699 serien) for gitte perioder.
    /// </summary>
    Task<List<MvaKontoSaldoDto>> HentMvaKontoSaldoerAsync(
        int ar, int fraPeriode, int tilPeriode, CancellationToken ct = default);

    Task LagreEndringerAsync(CancellationToken ct = default);
}
```

### Interne DTO-er for aggregering

```csharp
namespace Regnskap.Domain.Features.Mva;

/// <summary>
/// Aggregert MVA per kode for en periode. Produseres av repository-query.
/// </summary>
public record MvaAggregeringDto(
    string MvaKode,
    string StandardTaxCode,
    decimal Sats,
    MvaRetning Retning,
    decimal SumGrunnlag,
    decimal SumMvaBelop,
    int AntallPosteringer
);

/// <summary>
/// Detaljert MVA-postering for sammenstilling.
/// </summary>
public record MvaPosteringDetalj(
    Guid PosteringId,
    Guid BilagId,
    int Bilagsnummer,
    DateOnly Bilagsdato,
    string Kontonummer,
    string Beskrivelse,
    BokforingSide Side,
    decimal Belop,
    string MvaKode,
    decimal MvaGrunnlag,
    decimal MvaBelop,
    decimal MvaSats,
    bool ErAutoGenerertMva
);

/// <summary>
/// Saldo for en MVA-konto, fra KontoSaldo.
/// </summary>
public record MvaKontoSaldoDto(
    string Kontonummer,
    string Kontonavn,
    decimal InngaendeBalanse,
    decimal DebetIPerioden,
    decimal KreditIPerioden,
    decimal UtgaendeBalanse
);
```

---

## API-kontrakt

### Endepunkter

#### MVA-terminer

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/mva/terminer?ar={ar}` | Hent alle terminer for et ar |
| GET | `/api/mva/terminer/{id}` | Hent en termin med status og oppgjorsinfo |
| POST | `/api/mva/terminer/generer` | Generer terminer for et ar |

#### MVA-oppgjor

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| POST | `/api/mva/terminer/{terminId}/oppgjor/beregn` | Beregn MVA-oppgjor for termin |
| GET | `/api/mva/terminer/{terminId}/oppgjor` | Hent beregnet oppgjor med linjer |
| POST | `/api/mva/terminer/{terminId}/oppgjor/bokfor` | Bokfor oppgjorsbilag (nulstill MVA-kontoer) |

#### MVA-melding

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/mva/terminer/{terminId}/melding` | Generer MVA-meldingsdata (RF-0002 format) |
| POST | `/api/mva/terminer/{terminId}/melding/marker-innsendt` | Marker termin som innsendt |

#### MVA-avstemming

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| POST | `/api/mva/terminer/{terminId}/avstemming/kjor` | Kjor avstemming |
| GET | `/api/mva/terminer/{terminId}/avstemming` | Hent siste avstemming |
| GET | `/api/mva/terminer/{terminId}/avstemming/historikk` | Hent alle avstemminger |
| POST | `/api/mva/terminer/{terminId}/avstemming/{id}/godkjenn` | Godkjenn avstemming |

#### MVA-konto sammenstilling

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/mva/sammenstilling?ar={ar}&termin={termin}` | MVA-posteringer gruppert per kode og periode |
| GET | `/api/mva/sammenstilling/detalj?ar={ar}&termin={termin}&mvaKode={kode}` | Detaljerte posteringer for en MVA-kode |

#### SAF-T TaxTable

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/saft/taxtable` | Generer SAF-T TaxTable-seksjon (JSON) |

### Request/Response DTO-er

#### GenererTerminerRequest

```csharp
public record GenererTerminerRequest(
    /// <summary>Regnskapsaret (f.eks. 2026).</summary>
    int Ar,

    /// <summary>Termintype: "Tomaaneders" (standard) eller "Arlig" (sma foretak).</summary>
    MvaTerminType Type = MvaTerminType.Tomaaneders
);
```

**Validering:**
- `Ar`: Pakrevd, 2000-2099
- `Type`: Pakrevd, gyldig enum-verdi
- Terminer for aret ma ikke allerede eksistere

#### MvaTerminDto

```csharp
public record MvaTerminDto(
    Guid Id,
    int Ar,
    int Termin,
    string Type,
    DateOnly FraDato,
    DateOnly TilDato,
    DateOnly Frist,
    string Status,
    string Terminnavn,
    DateTime? AvsluttetTidspunkt,
    string? AvsluttetAv,
    Guid? OppgjorsBilagId,
    bool HarOppgjor,
    bool ErForfalt       // Frist < dagens dato og Status != Innsendt/Betalt
);
```

#### MvaOppgjorDto

```csharp
public record MvaOppgjorDto(
    Guid Id,
    Guid MvaTerminId,
    string Terminnavn,
    DateTime BeregnetTidspunkt,
    string BeregnetAv,
    decimal SumUtgaendeMva,
    decimal SumInngaendeMva,
    decimal SumSnuddAvregningUtgaende,
    decimal SumSnuddAvregningInngaende,
    decimal MvaTilBetaling,
    bool ErLast,
    List<MvaOppgjorLinjeDto> Linjer
);

public record MvaOppgjorLinjeDto(
    string MvaKode,
    string StandardTaxCode,
    decimal Sats,
    string Retning,
    int RfPostnummer,
    decimal SumGrunnlag,
    decimal SumMvaBelop,
    int AntallPosteringer
);
```

#### MvaMeldingDto (RF-0002 struktur)

```csharp
public record MvaMeldingDto(
    Guid MvaTerminId,
    string Terminnavn,
    int Ar,
    int Termin,
    DateOnly FraDato,
    DateOnly TilDato,

    // --- RF-0002 poster ---
    List<MvaMeldingPostDto> Poster,

    // --- Oppsummering ---
    decimal SumUtgaendeMva,         // Post 1-6 summert
    decimal SumInngaendeMva,         // Post 7-10 summert
    decimal MvaGrunnlagHoySats,      // Sum grunnlag 25%
    decimal MvaGrunnlagMiddelsSats,  // Sum grunnlag 15%
    decimal MvaGrunnlagLavSats,      // Sum grunnlag 12%
    decimal MvaTilBetaling           // Sum utgaende - Sum inngaende
);

public record MvaMeldingPostDto(
    /// <summary>RF-0002 postnummer (1-12).</summary>
    int Postnummer,

    /// <summary>Postbeskrivelse (norsk).</summary>
    string Beskrivelse,

    /// <summary>MVA-grunnlag for denne posten.</summary>
    decimal Grunnlag,

    /// <summary>Beregnet MVA-belop.</summary>
    decimal MvaBelop,

    /// <summary>SAF-T StandardTaxCodes som inngaar i denne posten.</summary>
    List<string> StandardTaxCodes
);
```

#### MvaAvstemmingDto

```csharp
public record MvaAvstemmingDto(
    Guid Id,
    Guid MvaTerminId,
    string Terminnavn,
    DateTime AvstemmingTidspunkt,
    string AvstemmingAv,
    bool ErGodkjent,
    string? Merknad,
    bool HarAvvik,
    decimal TotaltAvvik,
    List<MvaAvstemmingLinjeDto> Linjer
);

public record MvaAvstemmingLinjeDto(
    string Kontonummer,
    string Kontonavn,
    decimal SaldoIflgHovedbok,
    decimal BeregnetFraPosteringer,
    decimal Avvik,
    bool HarAvvik
);
```

#### MvaSammenstillingDto

```csharp
public record MvaSammenstillingDto(
    int Ar,
    int Termin,
    DateOnly FraDato,
    DateOnly TilDato,
    List<MvaSammenstillingGruppeDto> Grupper,
    decimal TotaltMvaGrunnlag,
    decimal TotaltMvaBelop
);

public record MvaSammenstillingGruppeDto(
    string MvaKode,
    string Beskrivelse,
    string StandardTaxCode,
    decimal Sats,
    string Retning,
    decimal SumGrunnlag,
    decimal SumMvaBelop,
    int AntallPosteringer
);

public record MvaSammenstillingDetaljDto(
    string MvaKode,
    string Beskrivelse,
    List<MvaPosteringDetaljDto> Posteringer,
    decimal SumGrunnlag,
    decimal SumMvaBelop,
    int TotaltAntall
);

public record MvaPosteringDetaljDto(
    Guid PosteringId,
    Guid BilagId,
    int Bilagsnummer,
    DateOnly Bilagsdato,
    string Kontonummer,
    string Beskrivelse,
    string Side,
    decimal Belop,
    decimal MvaGrunnlag,
    decimal MvaBelop,
    decimal MvaSats,
    bool ErAutoGenerertMva
);
```

#### SAF-T TaxTable DTO

```csharp
public record SaftTaxTableDto(
    List<SaftTaxCodeDetailDto> TaxCodeDetails
);

public record SaftTaxCodeDetailDto(
    /// <summary>Intern MVA-kode (TaxCode).</summary>
    string TaxCode,

    /// <summary>Beskrivelse (Description).</summary>
    string Description,

    /// <summary>SAF-T StandardTaxCode.</summary>
    string StandardTaxCode,

    /// <summary>MVA-sats i prosent (TaxPercentage).</summary>
    decimal TaxPercentage,

    /// <summary>Land (Country).</summary>
    string Country,

    /// <summary>Base rate.</summary>
    decimal? BaseRate
);
```

### Feilkoder

| HTTP | Kode | Melding | Nar |
|------|------|---------|-----|
| 404 | MVA_TERMIN_IKKE_FUNNET | MVA-termin {id} finnes ikke | Ugyldig termin-ID |
| 409 | MVA_TERMINER_FINNES | Terminer for {ar} finnes allerede | Forsoker generere terminer som allerede finnes |
| 409 | MVA_OPPGJOR_ALLEREDE_LAST | Oppgjoret for termin {id} er last | Forsoker beregne pa nytt etter innsending |
| 422 | MVA_TERMIN_IKKE_APEN | Termin {id} er ikke apen for beregning | Forsoker beregne pa avstemt/innsendt termin |
| 422 | MVA_OPPGJOR_MANGLER | Oppgjor for termin {id} er ikke beregnet | Forsoker bokfore uten beregning |
| 422 | MVA_AVSTEMMING_IKKE_GODKJENT | Avstemming for termin {id} er ikke godkjent | Forsoker sende inn uten godkjent avstemming |
| 422 | MVA_PERIODER_IKKE_LUKKET | Regnskapsperiodene for terminen er ikke lukket | Perioder ma vaere lukket for innsending |
| 422 | MVA_AVVIK_FUNNET | Avstemming viser avvik pa {belop} NOK | Avvik uten godkjenning |

---

## Forretningsregler

### FR-MVA-01: Generering av MVA-terminer

Systemet genererer MVA-terminer for et regnskapsar basert pa valgt termintype.

**Tomaandersperioder (standard):**
- Termin 1: jan-feb, frist 10. april
- Termin 2: mar-apr, frist 10. juni
- Termin 3: mai-jun, frist 31. august (forlenget frist, sommerferie)
- Termin 4: jul-aug, frist 10. oktober
- Termin 5: sep-okt, frist 10. desember
- Termin 6: nov-des, frist 10. februar (neste ar)

**Arstermin:**
- Termin 1: jan-des, frist 10. mars (neste ar)

**Regler:**
1. Kan kun generere terminer en gang per ar (409 hvis finnes)
2. Alle terminer opprettes med status `Apen`
3. Terminene knyttes til korrekte datoer automatisk
4. Skuddarslogikk i februar handteres (28 vs 29 dager)

### FR-MVA-02: Beregning av MVA-oppgjor

Beregningen aggregerer alle bokforte posteringer med MVA-kode innenfor terminens datoperiode.

**Algoritme:**
1. Hent alle posteringer hvor `Bilagsdato` er innenfor terminens `FraDato`-`TilDato` OG `ErAutoGenerertMva = true` (dvs. de automatisk genererte MVA-posteringene)
2. Grupper per `MvaKode`
3. For hver gruppe:
   - Summer `MvaBelop` (hent fra kildelinjen, ikke MVA-posteringen selv)
   - Summer `MvaGrunnlag` (fra kildelinjen)
   - Tell antall posteringer
   - Tilordne RF-0002 postnummer basert pa StandardTaxCode
4. Beregn totaler:
   - `SumUtgaendeMva` = sum av alle linjer med Retning = Utgaende
   - `SumInngaendeMva` = sum av alle linjer med Retning = Inngaende
   - `SumSnuddAvregningUtgaende` = sum utgaende-del av SnuddAvregning-linjer
   - `SumSnuddAvregningInngaende` = sum inngaende-del av SnuddAvregning-linjer
   - `MvaTilBetaling` = SumUtgaendeMva + SumSnuddAvregningUtgaende - SumInngaendeMva - SumSnuddAvregningInngaende

**VIKTIG: Snudd avregning (reverse charge)**
For snudd avregning (MvaRetning.SnuddAvregning) har BilagRegistreringService allerede opprettet BADE inngaende og utgaende MVA-posteringer. I oppgjoret summeres disse separat:
- Den utgaende posteringen (kredit pa MVA-konto) teller som skyldig utgaende
- Den inngaende posteringen (debet pa MVA-konto) teller som fradrag
- Nettoresultatet er 0 (korrekt — reverse charge er resultatnoytralt)

**Regler:**
1. Termin ma ha status `Apen` eller `Beregnet` (kan reberegne)
2. Eksisterende oppgjor overskriver ved reberegning (soft delete gammelt)
3. Oppgjor lastes nar termin settes til `Innsendt`
4. Terminstatus endres til `Beregnet`

**Eksempel: Termin 1 (jan-feb 2026):**

Forutsatt posteringer:
- Salg NOK 100.000 ekskl. MVA med kode 3 (25%): Utgaende MVA = NOK 25.000
- Salg NOK 50.000 ekskl. MVA med kode 31 (15%): Utgaende MVA = NOK 7.500
- Kjop NOK 40.000 ekskl. MVA med kode 1 (25%): Inngaende MVA = NOK 10.000
- Kjop av tjenester fra utlandet NOK 20.000 med kode 81/82 (snudd avregning 25%): Utg. MVA = 5.000, Inng. MVA = 5.000

Resultat:
- SumUtgaendeMva = 25.000 + 7.500 = 32.500
- SumInngaendeMva = 10.000
- SumSnuddAvregningUtgaende = 5.000
- SumSnuddAvregningInngaende = 5.000
- MvaTilBetaling = 32.500 + 5.000 - 10.000 - 5.000 = **22.500** (skyldig Skatteetaten)

### FR-MVA-03: RF-0002 poststruktur (MVA-melding)

MVA-meldingen struktureres i poster basert pa SAF-T StandardTaxCodes:

| Post | Beskrivelse | StandardTaxCodes | Type |
|------|-------------|-----------------|------|
| 1 | Utgaende MVA, alminnelig sats (25%) | 3 | Grunnlag + MVA |
| 2 | Utgaende MVA, middels sats (15%) | 31 | Grunnlag + MVA |
| 3 | Utgaende MVA, lav sats (12%) | 33 | Grunnlag + MVA |
| 4 | Innforsel av varer, alminnelig sats (25%) | 51 | Grunnlag + MVA |
| 5 | Innforsel av varer, middels sats (15%) | 52 | Grunnlag + MVA |
| 6 | Tjenester kjopt fra utlandet (snudd avregning, 25%) | 82 | Grunnlag + MVA |
| 7 | Inngaende MVA, alminnelig sats (25%) | 1 | Fradrag |
| 8 | Inngaende MVA, middels sats (15%) | 11 | Fradrag |
| 9 | Inngaende MVA, lav sats (12%) | 13 | Fradrag |
| 10 | Innforsel av varer, inngaende MVA (25%) | 14 | Fradrag |
| 11 | Innforsel av varer, inngaende MVA (15%) | 15 | Fradrag |
| 12 | Tjenester fra utlandet, inngaende MVA (snudd avregning, 25%) | 81 | Fradrag |

**Oppsummeringslinjer:**
- **Sum utgaende MVA** = Post 1-6 sum MVA-belop
- **Sum inngaende MVA (fradrag)** = Post 7-12 sum MVA-belop
- **MVA til betaling/tilgode** = Sum utgaende - Sum inngaende

**Regler:**
1. Poster med 0 i bade grunnlag og MVA inkluderes ikke i meldingen
2. Grunnlag vises alltid (for poster 1-6)
3. Fradragsposter (7-12) viser kun MVA-belop, ikke grunnlag
4. Alle belop rundes til hele kroner for innlevering (Math.Round, MidpointRounding.ToEven)
5. Null-melding: hvis ingen aktivitet, sendes melding med alle poster = 0

### FR-MVA-04: MVA-avstemming

Avstemmingen verifiserer at saldo pa MVA-kontoer stemmer overens med beregnede MVA-verdier fra posteringer.

**Algoritme:**
1. Identifiser alle MVA-relaterte kontoer (kontoer som er referert fra MvaKode.UtgaendeKontoId eller MvaKode.InngaendeKontoId). Typisk kontogruppe 26xx (2600, 2610, etc.) og 1600-serien
2. For hver MVA-konto:
   - Hent saldo fra `KontoSaldo` for regnskapsperiodene som inngaar i terminen
   - Beregn forventet belop ved a summere alle MVA-posteringer (ErAutoGenerertMva = true) mot denne kontoen i perioden
   - Beregn avvik = SaldoIflgHovedbok - BeregnetFraPosteringer
3. Flagg linjer med avvik >= 0.01 NOK
4. Samlet avvik beregnes

**Typiske avviksarsaker:**
- Manuelle korreksjoner pa MVA-kontoer uten MVA-kode
- Avrundingsdifferanser (normalt < 1 NOK)
- Feil i MVA-kode-oppsett

**Regler:**
1. Avstemming kan kjores flere ganger (ny oppforingn lagres)
2. Godkjenning av avstemming krever eksplisitt handling
3. Godkjent avstemming er pakrevd for a sende inn MVA-melding
4. Avvik under 1.00 NOK kan aksepteres som avrundingsdifferanse

### FR-MVA-05: Bokforing av MVA-oppgjorsbilag

Nar MVA-oppgjor bokfores, opprettes et bilag som nulstiller MVA-kontoene mot en oppgjorskonto.

**Konterings mal:**
- Debet konto 2700 (Utgaende MVA) — nulstill utgaende MVA
- Kredit konto 2710 (Inngaende MVA) — nulstill inngaende MVA (motpostering, da 2710 normalt har debetsaldo)
- Differansen posteres mot konto 2740 (Oppgjorskonto for MVA) eller tilsvarende

**Eksempel med FR-MVA-02 tallene:**
```
Debet  2600  Utgaende MVA 25%      25.000   (nulstill)
Debet  2601  Utgaende MVA 15%       7.500   (nulstill)
Debet  2603  Utg. MVA snudd avregn  5.000   (nulstill)
Kredit 2610  Inngaende MVA 25%     10.000   (nulstill)
Kredit 2613  Inng. MVA snudd avregn 5.000   (nulstill)
Kredit 2740  Oppgjorskonto MVA     22.500   (skyldig Skatteetaten)
```

Sum debet = 37.500, Sum kredit = 37.500. Balanse OK.

**Regler:**
1. Oppgjorsbilag opprettes i bilagsserie "AUTO"
2. BilagType = Manuelt (systemgenerert via AUTO-serien)
3. Bilagsdato = siste dag i terminen
4. Alle posteringer markeres med `ErAutoGenerertMva = false` (dette er oppgjorsposteringer, ikke MVA-beregning)
5. Oppgjorsbilag-ID lagres pa MvaTermin
6. Oppgjorsbilag bokfores umiddelbart
7. Kan kun bokfores en gang per termin

### FR-MVA-06: MVA-konto sammenstilling

Produserer oversikt over alle MVA-relaterte posteringer, gruppert per MVA-kode og eventuelt periode.

**Regler:**
1. Inkluderer ALLE posteringer med MvaKode != null (bade bruker-posteringer og auto-genererte)
2. Viser grunnlag, MVA-belop, sats, og antall per kode
3. Drill-down til enkelttransaksjoner
4. Sporbarhet: fra sammenstilling -> bilag -> postering (Bokforingsloven §4 kontrollspor)
5. Tilfredsstiller Bokforingsloven §5 nr. 5 (MVA-spesifikasjon)

### FR-MVA-07: SAF-T TaxTable-generering

Genererer SAF-T TaxTable-seksjon fra systemets MvaKode-entiteter.

**XML-struktur som skal produseres:**
```xml
<TaxTable>
  <TaxTableEntry>
    <TaxType>MVA</TaxType>
    <Description>Inngaende mva, alminnelig sats</Description>
    <TaxCodeDetails>
      <TaxCode>1</TaxCode>
      <Description>Inngaende mva, alminnelig sats</Description>
      <TaxPercentage>25.00</TaxPercentage>
      <Country>NO</Country>
      <StandardTaxCode>1</StandardTaxCode>
      <BaseRate>0</BaseRate>
    </TaxCodeDetails>
  </TaxTableEntry>
  <!-- ... en per aktiv MvaKode ... -->
</TaxTable>
```

**Regler:**
1. Kun aktive MVA-koder (ErAktiv = true) inkluderes
2. TaxType er alltid "MVA" for norske koder
3. Country er alltid "NO"
4. StandardTaxCode hentes fra MvaKode.StandardTaxCode
5. TaxPercentage formateres med to desimaler
6. Validering: alle aktive koder MA ha StandardTaxCode utfylt

### FR-MVA-08: Terminstatusoverganger

```
Apen -> Beregnet    (ved beregning av oppgjor)
Beregnet -> Apen    (ved reapning, sletter oppgjor)
Beregnet -> Avstemt (ved godkjent avstemming)
Avstemt -> Beregnet (ved reberegning etter endringer)
Avstemt -> Innsendt (ved innsending av MVA-melding)
Innsendt -> Betalt  (ved registrering av betaling)
```

Ugyldige overganger:
- `Apen -> Innsendt` (ma beregne og avstemme forst)
- `Innsendt -> Apen` (kan ikke gjenapne innsendt termin)
- `Betalt -> *` (endelig status)

### FR-MVA-09: Avrunding

Alle MVA-belop beregnes med 2 desimaler og `MidpointRounding.ToEven` (bankers rounding).

For MVA-meldingen: belop rundes til hele kroner for innlevering. Avrundingsdifferanse akkumuleres og vises pa meldingen.

### FR-MVA-10: Null-melding

Dersom det ikke er MVA-pliktig omsetning i en termin, skal det fortsatt vaere mulig a generere en null-melding (Skatteforvaltningsloven). Alle poster = 0, MvaTilBetaling = 0.

---

## MVA-handtering (eksisterende infrastruktur)

MVA-modulen BRUKER men dupliserer IKKE folgende:

### Fra Kontoplan-modulen

| Entitet | Brukes til |
|---------|------------|
| `MvaKode` | Henter alle MVA-koder med sats, retning, StandardTaxCode, kontotilknytning |
| `MvaRetning` (enum) | Ingen/Inngaende/Utgaende/SnuddAvregning — brukes for gruppering |
| `Konto` | MVA-kontoer (2600-serien) for avstemming |
| `IKontoplanRepository.HentAlleMvaKoderAsync()` | Henter alle koder for TaxTable og oppgjorsberegning |

### Fra Hovedbok-modulen

| Entitet | Brukes til |
|---------|------------|
| `Postering.MvaKode` | Filtrering av MVA-posteringer |
| `Postering.MvaBelop` | Summering av MVA-belop |
| `Postering.MvaGrunnlag` | Summering av MVA-grunnlag |
| `Postering.MvaSats` | Verifisering av sats |
| `Postering.ErAutoGenerertMva` | Skille auto-posteringer fra bruker-posteringer |
| `KontoSaldo` | Saldoer for MVA-kontoer ved avstemming |
| `Regnskapsperiode` | Periodeavgrensning |

### Fra Bilag-modulen

| Funksjon | Brukes til |
|----------|------------|
| `GenererMvaPosteringerAsync()` | MVA-posteringer opprettes HER, ikke i MVA-modulen |
| `IBilagService.OpprettBilagAsync()` | Oppgjorsbilag opprettes via denne |

---

## Service-kontrakt

```csharp
namespace Regnskap.Application.Features.Mva;

public interface IMvaService
{
    // --- Terminer ---
    Task<List<MvaTerminDto>> HentTerminerAsync(int ar, CancellationToken ct = default);
    Task<MvaTerminDto> HentTerminAsync(Guid id, CancellationToken ct = default);
    Task<List<MvaTerminDto>> GenererTerminerAsync(GenererTerminerRequest request, CancellationToken ct = default);

    // --- Oppgjor ---
    Task<MvaOppgjorDto> BeregnOppgjorAsync(Guid terminId, CancellationToken ct = default);
    Task<MvaOppgjorDto> HentOppgjorAsync(Guid terminId, CancellationToken ct = default);
    Task<MvaOppgjorDto> BokforOppgjorAsync(Guid terminId, CancellationToken ct = default);

    // --- Melding ---
    Task<MvaMeldingDto> GenererMvaMeldingAsync(Guid terminId, CancellationToken ct = default);
    Task MarkerInnsendtAsync(Guid terminId, CancellationToken ct = default);

    // --- Avstemming ---
    Task<MvaAvstemmingDto> KjorAvstemmingAsync(Guid terminId, CancellationToken ct = default);
    Task<MvaAvstemmingDto> HentAvstemmingAsync(Guid terminId, CancellationToken ct = default);
    Task<List<MvaAvstemmingDto>> HentAvstemmingshistorikkAsync(Guid terminId, CancellationToken ct = default);
    Task<MvaAvstemmingDto> GodkjennAvstemmingAsync(Guid terminId, Guid avstemmingId, string? merknad, CancellationToken ct = default);

    // --- Sammenstilling ---
    Task<MvaSammenstillingDto> HentSammenstillingAsync(int ar, int termin, CancellationToken ct = default);
    Task<MvaSammenstillingDetaljDto> HentSammenstillingDetaljAsync(int ar, int termin, string mvaKode, CancellationToken ct = default);

    // --- SAF-T ---
    Task<SaftTaxTableDto> GenererSaftTaxTableAsync(CancellationToken ct = default);
}
```

---

## Avhengigheter

### Moduler som ma eksistere

| Modul | Interface/Service | Brukes til |
|-------|-------------------|------------|
| Kontoplan | `IKontoplanRepository` | Hent MVA-koder, MVA-kontoer |
| Kontoplan | `IMvaKodeService` | Hent og valider MVA-koder |
| Hovedbok | `IHovedbokRepository` | Hent posteringer, saldoer, perioder |
| Bilag | `IBilagService` | Opprett oppgjorsbilag |

### Nye interfaces for MVA-modulen

| Interface | Beskrivelse |
|-----------|-------------|
| `IMvaRepository` | Data-tilgang for MVA-terminer, oppgjor, avstemming |
| `IMvaService` | Forretningslogikk for alle MVA-operasjoner |

---

## SAF-T Mapping

### TaxTable (MasterFiles)

Genereres fra alle aktive `MvaKode`-entiteter. Se FR-MVA-07.

### TaxInformation (per TransactionLine)

Allerede ivaretatt av eksisterende `Postering`-entitet:

| SAF-T Element | Kilde |
|---------------|-------|
| TaxType | Hardkodet "MVA" |
| TaxCode | `Postering.MvaKode` |
| TaxPercentage | `Postering.MvaSats` |
| TaxBase | `Postering.MvaGrunnlag` |
| TaxAmount | `Postering.MvaBelop` |

### Komplett SAF-T TaxCode-mapping (seed-data)

Disse MvaKode-oppforinger ma finnes i systemet:

| Intern kode | StandardTaxCode | Sats | Retning | Beskrivelse |
|-------------|----------------|------|---------|-------------|
| 0 | 0 | 0% | Ingen | Ingen MVA-behandling |
| 1 | 1 | 25% | Inngaende | Inngaende MVA, alminnelig sats |
| 11 | 11 | 15% | Inngaende | Inngaende MVA, middels sats |
| 13 | 13 | 12% | Inngaende | Inngaende MVA, lav sats |
| 14 | 14 | 25% | Inngaende | Inngaende MVA, innforsel av varer |
| 15 | 15 | 15% | Inngaende | Inngaende MVA, innforsel av varer, middels sats |
| 3 | 3 | 25% | Utgaende | Utgaende MVA, alminnelig sats |
| 31 | 31 | 15% | Utgaende | Utgaende MVA, middels sats |
| 33 | 33 | 12% | Utgaende | Utgaende MVA, lav sats |
| 5 | 5 | 0% | Utgaende | Utforsel av varer og tjenester (fritatt) |
| 6 | 6 | 0% | Ingen | Omsetning utenfor MVA-loven |
| 51 | 51 | 25% | Utgaende | Innforsel av varer, alminnelig sats (utgaende) |
| 52 | 52 | 15% | Utgaende | Innforsel av varer, middels sats (utgaende) |
| 81 | 81 | 25% | SnuddAvregning | Kjop tjenester utlandet, alminnelig sats (inng. del) |
| 82 | 82 | 25% | SnuddAvregning | Kjop tjenester utlandet, alminnelig sats (utg. del) |
| 86 | 86 | 25% | SnuddAvregning | Kjop klimakvoter/gull (inng. del) |
| 87 | 87 | 25% | SnuddAvregning | Kjop klimakvoter/gull (utg. del) |
| 91 | 91 | 25% | SnuddAvregning | Kjop varer utlandet (inng. del) |
| 92 | 92 | 25% | SnuddAvregning | Kjop varer utlandet (utg. del) |
| 20 | 20 | 0% | Ingen | Ingen MVA-behandling (kjop) |

**Merk om snudd avregning-koder:** Kodene 81/82, 86/87, og 91/92 fungerer i par. Kode 81 er inngaende-delen, kode 82 er utgaende-delen. I praksis bruker BilagRegistreringService en ENKELT MvaKode med `Retning = SnuddAvregning` som automatisk genererer bade inngaende og utgaende posteringer. Intern mapping:

| Brukbar kode | Retning | Genererer inng. StandardTaxCode | Genererer utg. StandardTaxCode |
|-------------|---------|------|------|
| 81 | SnuddAvregning | 81 | 82 |
| 86 | SnuddAvregning | 86 | 87 |
| 91 | SnuddAvregning | 91 | 92 |

---

## Kanttilfeller

### K-1: Termin uten posteringer
Null-melding genereres. Alle poster = 0. Oppgjor kan beregnes (resultat = 0). Ingen oppgjorsbilag opprettes.

### K-2: Tilbakeforte bilag i terminen
Tilbakeforte bilag inkluderes i beregningen med motsatt fortegn (de har speilvendte posteringer). Netto-effekten er korrekt.

### K-3: Posteringer pa grensen mellom terminer
En postering tilhorer terminen basert pa `Bilagsdato`, IKKE `Registreringsdato`. Bilag datert 28. februar tilhorer termin 1, bilag datert 1. mars tilhorer termin 2.

### K-4: Korreksjoner etter beregning
Hvis nye posteringer bokfores etter at oppgjor er beregnet (termin status = Beregnet), ma oppgjor reberegnes. Status ga tilbake til `Apen` automatisk dersom nye posteringer bokfores i terminperioden.

### K-5: Avrundingsdifferanser
Individuelle posteringer rundes til 2 desimaler. Summering kan gi mikroskopiske avvik vs. manuell beregning. Akseptabelt avvik i avstemming: < 1.00 NOK.

### K-6: Regnskapsperioder vs. MVA-terminer
En MVA-termin dekker 2 regnskapsperioder (f.eks. termin 1 = periode 1 + periode 2). Posteringer hentes basert pa dato, men saldoer hentes fra begge underliggende regnskapsperioder.

### K-7: Arsskifte for termin 6
Termin 6 (nov-des) har frist 10. februar neste ar. Oppgjorsbilag dateres 31. desember (siste dag i terminen), IKKE pa fristen.

### K-8: Bytte fra tomaanders til arstermin (eller omvendt)
Stotte for a bytte termintype krever at alle eksisterende terminer for aret er i status `Apen` og uten oppgjor. Slett eksisterende terminer (soft delete) og generer nye.

---

## Arbeidsflyt (typisk bruk)

### Normal MVA-oppgjorsflyt

```
1. Terminer genereres ved arsstart
   POST /api/mva/terminer/generer { ar: 2026, type: "Tomaaneders" }

2. Gjennom terminen: bilag bokfores som normalt
   (MVA-posteringer opprettes automatisk av BilagRegistreringService)

3. Ved terminens slutt: beregn oppgjor
   POST /api/mva/terminer/{id}/oppgjor/beregn
   -> Returnerer MvaOppgjorDto med alle poster

4. Kjor avstemming
   POST /api/mva/terminer/{id}/avstemming/kjor
   -> Returnerer MvaAvstemmingDto med eventuelle avvik

5. Verifiser sammenstilling (valgfritt)
   GET /api/mva/sammenstilling?ar=2026&termin=1
   -> Drill-down i posteringer per MVA-kode

6. Godkjenn avstemming
   POST /api/mva/terminer/{id}/avstemming/{avstemmingId}/godkjenn

7. Generer MVA-melding for innlevering
   GET /api/mva/terminer/{id}/melding
   -> RF-0002-data for manuell innlevering via Altinn

8. Bokfor oppgjorsbilag (nulstill MVA-kontoer)
   POST /api/mva/terminer/{id}/oppgjor/bokfor
   -> Oppretter bilag i serie AUTO

9. Marker som innsendt
   POST /api/mva/terminer/{id}/melding/marker-innsendt
   -> Termin lases, oppgjor lases
```

---

## Filstruktur

```
src/Regnskap.Domain/Features/Mva/
    MvaTermin.cs
    MvaOppgjor.cs
    MvaOppgjorLinje.cs
    MvaAvstemming.cs
    MvaAvstemmingLinje.cs
    Enums.cs                    (MvaTerminType, MvaTerminStatus)
    IMvaRepository.cs
    Exceptions.cs
    MvaAggregeringDto.cs        (interne query-DTOer)

src/Regnskap.Application/Features/Mva/
    IMvaService.cs
    MvaService.cs
    Dtos/
        MvaTerminDto.cs
        MvaOppgjorDto.cs
        MvaOppgjorLinjeDto.cs
        MvaMeldingDto.cs
        MvaMeldingPostDto.cs
        MvaAvstemmingDto.cs
        MvaAvstemmingLinjeDto.cs
        MvaSammenstillingDto.cs
        SaftTaxTableDto.cs
        GenererTerminerRequest.cs

src/Regnskap.Infrastructure/Features/Mva/
    MvaRepository.cs
    MvaTerminConfiguration.cs   (EF Core)
    MvaOppgjorConfiguration.cs
    MvaAvstemmingConfiguration.cs

src/Regnskap.Api/Features/Mva/
    MvaController.cs
```
