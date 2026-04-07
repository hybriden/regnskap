# Spesifikasjon: Bilagsregistrering (Journal Entry Registration)

**Modul:** Bilagsregistrering
**Status:** Komplett spesifikasjon
**Avhengigheter:** Kontoplan (implementert), Hovedbok (implementert)
**SAF-T-seksjon:** GeneralLedgerEntries > Journal > Transaction
**Bokforingsloven:** §4 (grunnleggende prinsipper), §5 (spesifikasjoner), §7 (ajourhold), §10 (bilag og dokumentasjon)

---

## Oversikt

Bilagsregistrering implementerer forretningslogikken for opprettelse, validering, nummerering og forvaltning av bilag (vouchers). Modulen bygger pa de eksisterende entitetene `Bilag` og `Postering` i Hovedbok-modulen, og implementerer kontrakten definert i `IBilagService`.

Modulen dekker:
- Opprettelse av bilag med automatisk balansekontroll
- Fortlopende nummerering per ar og bilagsserie (Bokforingsloven §5)
- MVA-beregning og automatisk MVA-postering per linje
- Bilagsserier (IB, MAN, AUTO, etc.)
- Vedlegg (filmetadata for kvitteringer og fakturakopier)
- Tilbakeforing (reversering av bilag)
- Bilagssok (dato, belop, konto, beskrivelse, bilagsnummer)
- Validering for bokforing (balanse, periode, kontoer, MVA)
- Bokforing mot hovedbok (oppdatering av KontoSaldo)

---

## Datamodell

### Nye entiteter

#### BilagSerie

```csharp
namespace Regnskap.Domain.Features.Bilag;

using Regnskap.Domain.Common;

/// <summary>
/// En bilagsserie for gruppering og nummerering av bilag.
/// Hver serie har sin egen fortlopende nummerserie per ar.
/// Bokforingsloven §5: flere serier tillatt nar hver danner en kontrollerbar, sammenhengende sekvens.
/// </summary>
public class BilagSerie : AuditableEntity
{
    /// <summary>
    /// Unik seriekode (f.eks. "IB", "MAN", "AUTO", "BANK", "LON").
    /// Maks 10 tegn, kun store bokstaver og tall.
    /// </summary>
    public string Kode { get; set; } = default!;

    /// <summary>
    /// Beskrivelse av serien.
    /// </summary>
    public string Navn { get; set; } = default!;

    /// <summary>
    /// Beskrivelse pa engelsk for SAF-T.
    /// </summary>
    public string? NavnEn { get; set; }

    /// <summary>
    /// Standard BilagType for bilag opprettet i denne serien.
    /// </summary>
    public BilagType StandardType { get; set; }

    /// <summary>
    /// Om serien er aktiv og kan motta nye bilag.
    /// </summary>
    public bool ErAktiv { get; set; } = true;

    /// <summary>
    /// Systemserie kan ikke slettes eller deaktiveres.
    /// </summary>
    public bool ErSystemserie { get; set; }

    /// <summary>
    /// SAF-T JournalID. Mapper til GeneralLedgerEntries/Journal/JournalID.
    /// </summary>
    public string SaftJournalId { get; set; } = default!;
}
```

**Standardserier (seed-data):**

| Kode | Navn | StandardType | SaftJournalId | Beskrivelse |
|------|------|-------------|---------------|-------------|
| IB | Apningsbalanse | Apningsbalanse | IB | Apningsbalanse ved arsstart |
| MAN | Manuelt bilag | Manuelt | MAN | Manuelle bilag / generell journalforing |
| AUTO | Automatisk bilag | Manuelt | AUTO | Systemgenererte bilag (MVA-oppgjor, avskrivning etc.) |
| IF | Inngaende faktura | InngaendeFaktura | IF | Leverandorfakturaer |
| UF | Utgaende faktura | UtgaendeFaktura | UF | Kundefakturaer |
| BANK | Bankbilag | Bank | BANK | Betalinger og bankbevegelser |
| LON | Lonsbilag | Lonn | LON | Lonnsbehandling |
| KOR | Korreksjon | Korreksjon | KOR | Korreksjons- og tilbakeforingsbilag |

#### BilagSerieNummer

```csharp
namespace Regnskap.Domain.Features.Bilag;

using Regnskap.Domain.Common;

/// <summary>
/// Holder styr pa neste bilagsnummer per serie per ar.
/// Brukes for a sikre fortlopende nummerering uten hull (Bokforingsloven §5).
/// Concurrency-safe via optimistic concurrency (RowVersion).
/// </summary>
public class BilagSerieNummer : AuditableEntity
{
    /// <summary>
    /// FK til bilagserien.
    /// </summary>
    public Guid BilagSerieId { get; set; }
    public BilagSerie BilagSerie { get; set; } = default!;

    /// <summary>
    /// Seriekode denormalisert for ytelse.
    /// </summary>
    public string SerieKode { get; set; } = default!;

    /// <summary>
    /// Regnskapsaret.
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Neste tilgjengelige bilagsnummer i denne serien for dette aret.
    /// Starter pa 1 for hvert nytt ar.
    /// </summary>
    public int NesteNummer { get; set; } = 1;

    /// <summary>
    /// Concurrency token for a hindre doble numre ved samtidige transaksjoner.
    /// </summary>
    public byte[] RowVersion { get; set; } = default!;

    // --- Forretningslogikk ---

    /// <summary>
    /// Tildel neste nummer og inkrementer.
    /// </summary>
    public int TildelNummer()
    {
        var nummer = NesteNummer;
        NesteNummer++;
        return nummer;
    }
}
```

#### Vedlegg

```csharp
namespace Regnskap.Domain.Features.Bilag;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Vedlegg knyttet til et bilag (kvittering, fakturakopi, kontrakt etc.).
/// Lagrer kun metadata og filsti -- selve filen lagres i filsystem eller blob storage.
/// Bokforingsloven §6 og §10: dokumentasjonskrav for bokforte transaksjoner.
/// </summary>
public class Vedlegg : AuditableEntity
{
    /// <summary>
    /// FK til bilaget vedlegget tilhorer.
    /// </summary>
    public Guid BilagId { get; set; }
    public Bilag Bilag { get; set; } = default!;

    /// <summary>
    /// Opprinnelig filnavn ved opplasting.
    /// </summary>
    public string Filnavn { get; set; } = default!;

    /// <summary>
    /// MIME-type (f.eks. "application/pdf", "image/jpeg").
    /// </summary>
    public string MimeType { get; set; } = default!;

    /// <summary>
    /// Filstorrelse i bytes.
    /// </summary>
    public long Storrelse { get; set; }

    /// <summary>
    /// Sti til filen i lagring (relativ sti, blob-referanse, eller full sti).
    /// </summary>
    public string LagringSti { get; set; } = default!;

    /// <summary>
    /// SHA-256 hash av filinnholdet for integritetskontroll.
    /// </summary>
    public string HashSha256 { get; set; } = default!;

    /// <summary>
    /// Valgfri beskrivelse av vedlegget.
    /// </summary>
    public string? Beskrivelse { get; set; }

    /// <summary>
    /// Sorteringsrekkefolge nar et bilag har flere vedlegg.
    /// </summary>
    public int Rekkefolge { get; set; }
}
```

### Utvidelse av eksisterende Bilag-entitet

Folgende felter legges til `Bilag`-entiteten i Hovedbok:

```csharp
// I Bilag.cs -- nye properties

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
/// Komplementerer Bilagsnummer som er globalt per ar.
/// </summary>
public int? SerieNummer { get; set; }

/// <summary>
/// Full bilagsreferanse med serie (f.eks. "MAN-2026-00042").
/// </summary>
public string? SerieBilagsId => SerieKode != null && SerieNummer.HasValue
    ? $"{SerieKode}-{Ar}-{SerieNummer.Value:D5}"
    : null;

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
/// Om bilaget er tilbakfort (reversert). Soft-flag for rask filtrering.
/// </summary>
public bool ErTilbakfort { get; set; }

/// <summary>
/// Om bilaget er bokfort mot hovedbok (KontoSaldo oppdatert).
/// Bilag kan opprettes som kladd for validering for bokforing.
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
```

### Nye Enums

```csharp
namespace Regnskap.Domain.Features.Bilag;

/// <summary>
/// Status for et bilag i registreringsflyten.
/// </summary>
public enum BilagStatus
{
    /// <summary>Bilaget er under arbeid, ikke validert.</summary>
    Kladd,

    /// <summary>Bilaget er validert og klart for bokforing.</summary>
    Validert,

    /// <summary>Bilaget er bokfort mot hovedbok.</summary>
    Bokfort,

    /// <summary>Bilaget er tilbakfort (reversert).</summary>
    Tilbakfort
}
```

### EF Core-konfigurasjon

```csharp
// BilagSerieConfiguration.cs
builder.HasKey(b => b.Id);
builder.HasIndex(b => b.Kode).IsUnique();
builder.Property(b => b.Kode).HasMaxLength(10).IsRequired();
builder.Property(b => b.Navn).HasMaxLength(200).IsRequired();
builder.Property(b => b.NavnEn).HasMaxLength(200);
builder.Property(b => b.SaftJournalId).HasMaxLength(20).IsRequired();

// BilagSerieNummerConfiguration.cs
builder.HasKey(b => b.Id);
builder.HasIndex(b => new { b.SerieKode, b.Ar }).IsUnique();
builder.Property(b => b.RowVersion).IsRowVersion();
builder.HasOne(b => b.BilagSerie)
    .WithMany()
    .HasForeignKey(b => b.BilagSerieId)
    .OnDelete(DeleteBehavior.Restrict);

// VedleggConfiguration.cs
builder.HasKey(v => v.Id);
builder.Property(v => v.Filnavn).HasMaxLength(500).IsRequired();
builder.Property(v => v.MimeType).HasMaxLength(100).IsRequired();
builder.Property(v => v.LagringSti).HasMaxLength(1000).IsRequired();
builder.Property(v => v.HashSha256).HasMaxLength(64).IsRequired();
builder.Property(v => v.Beskrivelse).HasMaxLength(500);
builder.HasIndex(v => v.BilagId);
builder.HasOne(v => v.Bilag)
    .WithMany(b => b.Vedlegg)
    .HasForeignKey(v => v.BilagId)
    .OnDelete(DeleteBehavior.Restrict);

// Bilag-utvidelser (oppdater BilagConfiguration.cs)
builder.HasOne(b => b.BilagSerie)
    .WithMany()
    .HasForeignKey(b => b.BilagSerieId)
    .OnDelete(DeleteBehavior.Restrict);
builder.HasIndex(b => new { b.SerieKode, b.Ar, b.SerieNummer })
    .IsUnique()
    .HasFilter("[SerieKode] IS NOT NULL");
builder.HasOne(b => b.TilbakefortFraBilag)
    .WithOne(b => b.TilbakefortAvBilag)  // Nesten 1:1, men konfigurert via to FK-er
    .HasForeignKey<Bilag>(b => b.TilbakefortFraBilagId)
    .OnDelete(DeleteBehavior.Restrict);
builder.Property(b => b.SerieKode).HasMaxLength(10);
builder.Property(b => b.BokfortAv).HasMaxLength(200);
```

---

## API-kontrakt

### DTOer (nye og utvidede)

```csharp
namespace Regnskap.Application.Features.Bilag;

using Regnskap.Domain.Features.Hovedbok;

// --- Request DTOer ---

public record OpprettBilagRequest(
    BilagType Type,
    DateOnly Bilagsdato,
    string Beskrivelse,
    string? EksternReferanse,
    string? SerieKode,
    List<OpprettPosteringRequest> Posteringer,
    bool BokforDirekte = true);

public record OpprettPosteringRequest(
    string Kontonummer,
    BokforingSide Side,
    decimal Belop,
    string Beskrivelse,
    string? MvaKode,
    string? Avdelingskode,
    string? Prosjektkode,
    Guid? KundeId,
    Guid? LeverandorId);

public record TilbakeforBilagRequest(
    Guid OriginalBilagId,
    DateOnly Tilbakeforingsdato,
    string Beskrivelse);

public record LeggTilVedleggRequest(
    Guid BilagId,
    string Filnavn,
    string MimeType,
    long Storrelse,
    string LagringSti,
    string HashSha256,
    string? Beskrivelse);

public record BilagSokRequest(
    int? Ar,
    int? Periode,
    BilagType? Type,
    string? SerieKode,
    DateOnly? FraDato,
    DateOnly? TilDato,
    string? Kontonummer,
    decimal? MinBelop,
    decimal? MaxBelop,
    string? Beskrivelse,
    string? EksternReferanse,
    int? Bilagsnummer,
    bool? ErBokfort,
    bool? ErTilbakfort,
    int Side = 1,
    int Antall = 50);

public record ValiderBilagRequest(
    BilagType Type,
    DateOnly Bilagsdato,
    string Beskrivelse,
    string? SerieKode,
    List<OpprettPosteringRequest> Posteringer);

// --- Response DTOer ---

public record BilagDto(
    Guid Id,
    string BilagsId,
    string? SerieBilagsId,
    int Bilagsnummer,
    int? SerieNummer,
    string? SerieKode,
    int Ar,
    string Type,
    DateOnly Bilagsdato,
    DateTime Registreringsdato,
    string Beskrivelse,
    string? EksternReferanse,
    RegnskapsperiodeDto Periode,
    List<PosteringDto> Posteringer,
    List<VedleggDto> Vedlegg,
    decimal SumDebet,
    decimal SumKredit,
    bool ErBokfort,
    DateTime? BokfortTidspunkt,
    bool ErTilbakfort,
    Guid? TilbakefortFraBilagId,
    Guid? TilbakefortAvBilagId);

public record PosteringDto(
    Guid Id,
    int Linjenummer,
    string Kontonummer,
    string Kontonavn,
    string Side,
    decimal Belop,
    string Beskrivelse,
    string? MvaKode,
    decimal? MvaBelop,
    decimal? MvaGrunnlag,
    decimal? MvaSats,
    string? Avdelingskode,
    string? Prosjektkode,
    Guid? KundeId,
    Guid? LeverandorId,
    bool ErAutoGenerertMva);

public record VedleggDto(
    Guid Id,
    string Filnavn,
    string MimeType,
    long Storrelse,
    string LagringSti,
    string? Beskrivelse,
    int Rekkefolge,
    DateTime OpplastetTidspunkt);

public record BilagSerieDto(
    Guid Id,
    string Kode,
    string Navn,
    string? NavnEn,
    string StandardType,
    bool ErAktiv,
    bool ErSystemserie,
    string SaftJournalId);

public record BilagValideringResultatDto(
    bool ErGyldig,
    List<BilagValideringFeilDto> Feil,
    List<BilagValideringAdvarselDto> Advarsler,
    List<PosteringDto>? GenererteeMvaPosteringer);

public record BilagValideringFeilDto(
    string Kode,
    string Melding,
    int? Linjenummer);

public record BilagValideringAdvarselDto(
    string Kode,
    string Melding,
    int? Linjenummer);

public record BilagSokResultatDto(
    List<BilagDto> Data,
    int TotaltAntall,
    int Side,
    int Antall);
```

### Endepunkter

#### Bilag CRUD og bokforing

| Metode | URL | Beskrivelse | Request | Response |
|--------|-----|-------------|---------|----------|
| POST | `/api/bilag` | Opprett og (valgfritt) bokfor bilag | `OpprettBilagRequest` | `BilagDto` (201) |
| GET | `/api/bilag/{id}` | Hent bilag med posteringer og vedlegg | - | `BilagDto` (200) |
| GET | `/api/bilag/nummer/{ar}/{bilagsnummer}` | Hent bilag pa bilagsnummer | - | `BilagDto` (200) |
| GET | `/api/bilag/serie/{serieKode}/{ar}/{serieNummer}` | Hent bilag pa seriereferanse | - | `BilagDto` (200) |
| POST | `/api/bilag/sok` | Sok i bilag | `BilagSokRequest` | `BilagSokResultatDto` (200) |
| POST | `/api/bilag/valider` | Valider bilag uten a opprette | `ValiderBilagRequest` | `BilagValideringResultatDto` (200) |
| POST | `/api/bilag/{id}/bokfor` | Bokfor et kladd-bilag | - | `BilagDto` (200) |
| POST | `/api/bilag/{id}/tilbakefor` | Tilbakefor (reverser) et bilag | `TilbakeforBilagRequest` | `BilagDto` (201) |

#### Vedlegg

| Metode | URL | Beskrivelse | Request | Response |
|--------|-----|-------------|---------|----------|
| POST | `/api/bilag/{id}/vedlegg` | Legg til vedlegg | `LeggTilVedleggRequest` | `VedleggDto` (201) |
| GET | `/api/bilag/{id}/vedlegg` | Hent alle vedlegg for bilag | - | `List<VedleggDto>` (200) |
| DELETE | `/api/bilag/{id}/vedlegg/{vedleggId}` | Slett vedlegg (soft delete) | - | 204 |

#### Bilagserier

| Metode | URL | Beskrivelse | Request | Response |
|--------|-----|-------------|---------|----------|
| GET | `/api/bilagserier` | Hent alle bilagserier | - | `List<BilagSerieDto>` (200) |
| GET | `/api/bilagserier/{kode}` | Hent bilagserie | - | `BilagSerieDto` (200) |
| POST | `/api/bilagserier` | Opprett ny bilagserie | `OpprettBilagSerieRequest` | `BilagSerieDto` (201) |
| PUT | `/api/bilagserier/{kode}` | Oppdater bilagserie | `OppdaterBilagSerieRequest` | `BilagSerieDto` (200) |

### Feilkoder

| HTTP | Feilkode | Melding | Kontekst |
|------|----------|---------|----------|
| 400 | `BILAG_IKKE_I_BALANSE` | Bilaget er ikke i balanse. Debet: {d}, Kredit: {k}, Differanse: {diff} | Opprettelse |
| 400 | `BILAG_FOR_FA_LINJER` | Et bilag ma ha minimum 2 posteringer | Opprettelse |
| 400 | `KONTO_IKKE_FUNNET` | Konto {kontonummer} finnes ikke | Postering |
| 400 | `KONTO_INAKTIV` | Konto {kontonummer} er deaktivert | Postering |
| 400 | `KONTO_IKKE_BOKFORBAR` | Konto {kontonummer} er ikke bokforbar | Postering |
| 400 | `MVA_KODE_IKKE_FUNNET` | MVA-kode {kode} finnes ikke | Postering |
| 400 | `MVA_KODE_INAKTIV` | MVA-kode {kode} er deaktivert | Postering |
| 400 | `PERIODE_LUKKET` | Perioden {ar}-{periode} er lukket for bokforing | Opprettelse |
| 400 | `PERIODE_SPERRET` | Perioden {ar}-{periode} er sperret | Opprettelse |
| 400 | `PERIODE_IKKE_FUNNET` | Regnskapsperiode for dato {dato} finnes ikke | Opprettelse |
| 400 | `SERIE_IKKE_FUNNET` | Bilagserie {kode} finnes ikke | Opprettelse |
| 400 | `SERIE_INAKTIV` | Bilagserie {kode} er deaktivert | Opprettelse |
| 400 | `AVDELING_PAKREVD` | Konto {kontonummer} krever avdelingskode | Postering |
| 400 | `PROSJEKT_PAKREVD` | Konto {kontonummer} krever prosjektkode | Postering |
| 400 | `BELOP_MA_VAERE_POSITIVT` | Belop ma vaere storre enn 0 | Postering |
| 400 | `BILAG_ALLEREDE_BOKFORT` | Bilaget er allerede bokfort | Bokforing |
| 400 | `BILAG_ALLEREDE_TILBAKFORT` | Bilaget er allerede tilbakfort | Tilbakeforing |
| 400 | `BILAG_IKKE_BOKFORT_FOR_TILBAKEFORING` | Kun bokforte bilag kan tilbakefores | Tilbakeforing |
| 404 | `BILAG_IKKE_FUNNET` | Bilag {id} finnes ikke | Oppslag |
| 409 | `NUMMERERING_KONFLIKT` | Samtidig nummerering — prosv igjen | Opprettelse |

---

## Forretningsregler

### FR-1: Dobbelt bokholderi — balansekrav

Hvert bilag MA ha sum debet = sum kredit. Denne invarianten sjekkes ALLTID for lagring.

**Validering:**
- Sum alle posteringer der `Side = Debet` => `SumDebet`
- Sum alle posteringer der `Side = Kredit` => `SumKredit`
- `SumDebet.Verdi == SumKredit.Verdi` (eksakt desimalsammenligning)

**Merk:** MVA-posteringer som auto-genereres INNGÅR i balansesjekken. De legges til for balansekontroll.

**Eksempel:**
```
Kjop av kontorrekvisita kr 1 000 + MVA 25%:
  Linje 1: Konto 6800 (Kontorkostnad), Debet, 1 000,00, MvaKode "1"
  Linje 2: Konto 1920 (Bank), Kredit, 1 250,00
  [Auto-MVA] Linje 3: Konto 2710 (Inngaende MVA), Debet, 250,00

  Sum debet: 1 000 + 250 = 1 250
  Sum kredit: 1 250
  Balanse: OK
```

### FR-2: Minimum antall posteringer

Et bilag MA ha minimum 2 posteringer (inkludert auto-genererte MVA-posteringer).

### FR-3: Positive belop

Alle belop pa posteringer MA vaere storre enn 0. Debet/kredit styres via `BokforingSide`, ikke via fortegn.

### FR-4: Fortlopende bilagsnummerering

**Bokforingsloven §5:** Bilag skal nummereres fortlopende uten hull.

**To niva av nummerering:**
1. **Globalt bilagsnummer** (`Bilag.Bilagsnummer`): Fortlopende per ar, pa tvers av alle serier. Brukes som primaer referanse og SAF-T TransactionID.
2. **Serienummer** (`Bilag.SerieNummer`): Fortlopende per serie per ar. Gir lesbar referanse innenfor serien.

**Algoritme for nummertildeling:**
```
1. Finn/opprett BilagSerieNummer for (serieKode, ar)
2. Lâs raden med optimistic concurrency (RowVersion)
3. Tildel SerieNummer = BilagSerieNummer.TildelNummer()
4. Tildel Bilagsnummer = IHovedbokRepository.NestebilagsnummerAsync(ar)
5. Lagre med concurrency check
6. Ved DbUpdateConcurrencyException: retry opp til 3 ganger
```

**Merk:** Globalt bilagsnummer er det som tilfredsstiller Bokforingslovens krav. Serienummer er en praktisk tilleggsreferanse.

### FR-5: Bilagsserier

- Hvert bilag tilhorer en (valgfri) serie
- Hvis `SerieKode` ikke angis, brukes "MAN" som standard
- Nye serier kan opprettes, men systemserier kan ikke slettes
- BilagType pa bilaget arves fra seriens `StandardType` med mindre eksplisitt overstyrt

### FR-6: Periodevalidering

- Bilagsdato bestemmer hvilken regnskapsperiode bilaget tilhorer
- Perioden MA vaere `Apen` for at bilaget kan bokfores
- Perioder med status `Sperret` eller `Lukket` avviser nye bilag
- Perioden ma finnes (opprettes via Hovedbok-modulen)

### FR-7: Kontovalidering per linje

For hver posteringslinje valideres:
1. Kontoen MA finnes (`IKontoplanRepository.KontoFinnesAsync`)
2. Kontoen MA vaere aktiv (`Konto.ErAktiv = true`)
3. Kontoen MA vaere bokforbar (`Konto.ErBokforbar = true`)
4. Hvis `Konto.KreverAvdeling = true`, MA `Avdelingskode` vaere satt
5. Hvis `Konto.KreverProsjekt = true`, MA `Prosjektkode` vaere satt

### FR-8: MVA-validering per linje

For posteringslinjer med `MvaKode != null`:
1. MVA-koden MA finnes og vaere aktiv
2. MVA-koden MA ha definert relevant konto (`InngaendeKontoId` eller `UtgaendeKontoId`)
3. MVA-sats og MVA-belop beregnes automatisk (se FR-9)

### FR-9: Automatisk MVA-postering

Nar en posteringslinje har `MvaKode`, genererer systemet automatisk en ekstra posteringslinje for MVA-belopet.

**Algoritme:**
```
1. Hent MvaKode-entitet fra Kontoplan
2. Beregn MVA-belop:
   MvaBelop = Belop * (MvaKode.Sats / 100)
   MvaGrunnlag = Belop (opprinnelig linjebelop)
3. Snapshot MVA-sats pa posteringen (MvaSats = MvaKode.Sats)
4. Bestem MVA-konto og side basert pa MvaKode.Retning:
```

| MvaRetning | MVA-konto | MVA-postering Side | Forklaring |
|------------|-----------|-------------------|------------|
| Inngaende | MvaKode.InngaendeKontoId | Debet | Inngaende MVA er en eiendel/fordring |
| Utgaende | MvaKode.UtgaendeKontoId | Kredit | Utgaende MVA er gjeld |
| SnuddAvregning | Bade InngaendeKontoId og UtgaendeKontoId | Debet + Kredit | To auto-linjer: en inngaende (debet) og en utgaende (kredit) |
| Ingen | Ingen MVA-postering | - | MVA-fri, ingen auto-linje |

**Eksempel — Salg med utgaende MVA 25%:**
```
Bruker registrerer:
  Linje 1: Konto 3000 (Salgsinntekt), Kredit, 10 000,00, MvaKode "3"
  Linje 2: Konto 1500 (Kundefordringer), Debet, 12 500,00

System auto-genererer:
  Linje 3: Konto 2700 (Utgaende MVA), Kredit, 2 500,00
  (MvaGrunnlag=10000, MvaBelop=2500, MvaSats=25)

Balanse: Debet 12 500 = Kredit 10 000 + 2 500. OK.
```

**Eksempel — Kjop med inngaende MVA 25%:**
```
Bruker registrerer:
  Linje 1: Konto 6300 (Husleie), Debet, 8 000,00, MvaKode "1"
  Linje 2: Konto 2400 (Leverandorgjeld), Kredit, 10 000,00

System auto-genererer:
  Linje 3: Konto 2710 (Inngaende MVA), Debet, 2 000,00
  (MvaGrunnlag=8000, MvaBelop=2000, MvaSats=25)

Balanse: Debet 8 000 + 2 000 = Kredit 10 000. OK.
```

**Eksempel — Snudd avregning (kjop av tjenester fra utlandet) 25%:**
```
Bruker registrerer:
  Linje 1: Konto 6700 (Fremmedtjenester), Debet, 5 000,00, MvaKode "81" (snudd avregning, inngaende)
  Linje 2: Konto 2400 (Leverandorgjeld), Kredit, 5 000,00

System auto-genererer (MvaKode "81" + tilhorende "82"):
  Linje 3: Konto 2710 (Inngaende MVA), Debet, 1 250,00  (fra kode 81)
  Linje 4: Konto 2700 (Utgaende MVA), Kredit, 1 250,00   (fra kode 82)

Balanse: Debet 5 000 + 1 250 = Kredit 5 000 + 1 250. OK.
Merk: MVA-linjene gar i null mot hverandre, men ma rapporteres pa MVA-meldingen.
```

### FR-10: Tilbakeforing (reversering)

Et bokfort bilag kan tilbakefores ved a opprette et nytt bilag med speilvendte posteringer.

**Regler:**
1. Kun bokforte bilag kan tilbakefores
2. Et bilag kan kun tilbakefores en gang
3. Tilbakeforingsbilag far sine egne bilagsnummer (i KOR-serien)
4. Alle posteringer speilvendes: Debet blir Kredit og omvendt, med identiske belop
5. Tilbakeforingsbilag linkes toveis via `TilbakefortFraBilagId` / `TilbakefortAvBilagId`
6. Originalbilaget markeres med `ErTilbakfort = true`
7. Tilbakeforingsbilag bokfores umiddelbart (atomisk)
8. Tilbakeforingsdato bestemmer perioden — MA vaere i en apen periode
9. MVA-posteringer speilvendes ogsa (korrigerer MVA-grunnlag)

**Algoritme:**
```
1. Valider at originalbilag er bokfort og ikke allerede tilbakfort
2. Finn apen periode for tilbakeforingsdato
3. Opprett nytt bilag i KOR-serien med:
   - Type = Korreksjon
   - Beskrivelse = "Tilbakeforing av {original.BilagsId}: {request.Beskrivelse}"
   - TilbakefortFraBilagId = original.Id
4. For hver postering i originalbilaget:
   - Opprett ny postering med motsatt Side og identisk Belop
   - Kopier MVA-felter uendret (men pa motsatt side)
5. Sett original.TilbakefortAvBilagId = nytt bilag Id
6. Sett original.ErTilbakfort = true
7. Bokfor tilbakeforingsbilag (oppdater KontoSaldo)
```

### FR-11: Bokforing mot hovedbok

Bokforing betyr a oppdatere `KontoSaldo` for alle berorte kontoer og perioder.

**Algoritme:**
```
1. Valider bilaget (FR-1 til FR-8)
2. Finn regnskapsperiode for bilagsdato
3. Valider at perioden er apen
4. For hver postering i bilaget (inkl. auto-MVA):
   a. Finn/opprett KontoSaldo for (kontonummer, ar, periode)
   b. Kall KontoSaldo.LeggTilPostering(side, belop)
5. Sett Bilag.ErBokfort = true
6. Sett Bilag.BokfortTidspunkt = DateTime.UtcNow
7. Sett Bilag.BokfortAv = brukerident
8. Lagre alt i en transaksjon (Unit of Work)
```

**Viktig:** Hele operasjonen (bilagopprettelse + nummerering + saldooppdatering) MA skje i EN databasetransaksjon for a sikre konsistens.

### FR-12: Bilagssok

Sok stotter filtrering pa:
- `Ar` — regnskapsar
- `Periode` — regnskapsperiode (maned)
- `Type` — BilagType
- `SerieKode` — bilagserie
- `FraDato` / `TilDato` — datoperiode
- `Kontonummer` — bilag som har posteringer mot gitt konto
- `MinBelop` / `MaxBelop` — belopsomfang (pa bilagsniva: sum debet)
- `Beskrivelse` — fritekst (LIKE %...%)
- `EksternReferanse` — fritekst match
- `Bilagsnummer` — eksakt match
- `ErBokfort` — true/false
- `ErTilbakfort` — true/false

**Paginering:** Alle sokresultater pagineres med `Side` og `Antall`. Maks `Antall` = 200.

**Sortering:** Standard sortering er Bilagsnummer synkende (nyeste forst).

### FR-13: Vedlegg

- Hvert bilag kan ha 0 til N vedlegg
- Kun metadata lagres i databasen — selve filen lagres i filsystem/blob
- SHA-256 hash lagres for integritetskontroll (NBS 1: sikring av regnskapsmateriale)
- Vedlegg kan soft-deletes fra ikke-bokforte bilag
- Vedlegg pa bokforte bilag kan IKKE slettes (oppbevaringsplikt, Bokforingsloven §11, §13)
- Tillatte MIME-typer: `application/pdf`, `image/jpeg`, `image/png`, `image/tiff`, `application/xml`
- Maks filstorrelse: 25 MB per vedlegg

### FR-14: Kladd vs. direkte bokforing

- `OpprettBilagRequest.BokforDirekte = true` (standard): bilaget valideres, nummereres, og bokfores i ett steg
- `BokforDirekte = false`: bilaget opprettes som kladd, far bilagsnummer, men KontoSaldo oppdateres IKKE
- Kladd-bilag kan bokfores senere via `POST /api/bilag/{id}/bokfor`
- Kladd-bilag har `ErBokfort = false`

---

## MVA-handling

### Relevante MVA-koder

Alle MVA-koder definert i `MvaKode`-entiteten i Kontoplan-modulen er tilgjengelige for bilagsregistrering. De viktigste:

| Kode | Sats | Retning | Bruk i bilag |
|------|------|---------|--------------|
| 0 | 0% | Ingen | MVA-fri linje, ingen auto-postering |
| 1 | 25% | Inngaende | Kjop med full MVA-fradrag |
| 3 | 25% | Utgaende | Salg med alminnelig MVA |
| 11 | 15% | Inngaende | Kjop av naringsmidler |
| 13 | 12% | Inngaende | Kjop av persontransport etc. |
| 31 | 15% | Utgaende | Salg av naringsmidler |
| 33 | 12% | Utgaende | Salg av persontransport etc. |
| 5 | 0% | Utgaende | Eksport (nullsats, men rapporteres) |
| 6 | 0% | Utgaende | Utenfor MVA-loven |
| 81 | 25% | SnuddAvregning | Kjop av tjenester fra utlandet (inngaende del) |

### Beregningslogikk

```
MvaBelop = Avrund(Belop * Sats / 100, 2)
```

Avrunding: Standard bankrunding til 2 desimaler (MidpointRounding.ToEven).

### Auto-posteringsflyt

```
For hver brukerpostering der MvaKode != null og MvaKode != "0":
  1. Hent MvaKode-entitet
  2. Beregn MvaBelop
  3. Sett MvaGrunnlag = postering.Belop
  4. Sett MvaSats = MvaKode.Sats (snapshot)
  5. Sett MvaBelop pa opprinnelig postering
  6. Generer MVA-posteringslinje(r):
     - Inngaende: Debet pa InngaendeKonto
     - Utgaende: Kredit pa UtgaendeKonto
     - SnuddAvregning: Bade debet (inngaende) og kredit (utgaende)
  7. MVA-posteringer merkes med ErAutoGenerertMva = true
```

### SAF-T mapping

Posteringer med MVA mapper til SAF-T slik:

```xml
<Line>
  <RecordID>{Linjenummer}</RecordID>
  <AccountID>{Kontonummer}</AccountID>
  <DebitAmount>
    <Amount>{Belop}</Amount>
  </DebitAmount>
  <TaxInformation>
    <TaxType>MVA</TaxType>
    <TaxCode>{MvaKode}</TaxCode>
    <TaxPercentage>{MvaSats}</TaxPercentage>
    <TaxBase>{MvaGrunnlag}</TaxBase>
    <TaxAmount>
      <Amount>{MvaBelop}</Amount>
    </TaxAmount>
  </TaxInformation>
</Line>
```

---

## Service-kontrakt

### IBilagRegistreringService (utvider IBilagService)

```csharp
namespace Regnskap.Application.Features.Bilag;

using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Komplett service for bilagsregistrering, validering, bokforing og forvaltning.
/// Implementerer IBilagService-kontrakten fra Hovedbok + utvidede operasjoner.
/// </summary>
public interface IBilagRegistreringService : IBilagService
{
    // --- Opprettelse og bokforing ---

    /// <summary>
    /// Opprett bilag. Hvis BokforDirekte=true, bokfores umiddelbart.
    /// Atomisk operasjon: nummerering + validering + MVA-autogenerering + bokforing.
    /// </summary>
    new Task<BilagDto> OpprettBilagAsync(OpprettBilagRequest request, CancellationToken ct = default);

    /// <summary>
    /// Bokfor et eksisterende kladd-bilag mot hovedbok.
    /// </summary>
    Task<BilagDto> BokforBilagAsync(Guid bilagId, CancellationToken ct = default);

    // --- Validering ---

    /// <summary>
    /// Valider bilag uten a opprette. Returnerer feil, advarsler, og forhåndsvisning
    /// av auto-genererte MVA-posteringer.
    /// </summary>
    Task<BilagValideringResultatDto> ValiderBilagAsync(
        ValiderBilagRequest request, CancellationToken ct = default);

    // --- Tilbakeforing ---

    /// <summary>
    /// Tilbakefor (reverser) et bokfort bilag. Oppretter nytt bilag med speilvendte posteringer.
    /// </summary>
    Task<BilagDto> TilbakeforBilagAsync(
        TilbakeforBilagRequest request, CancellationToken ct = default);

    // --- Sok ---

    /// <summary>
    /// Sok i bilag med filtrering og paginering.
    /// </summary>
    Task<BilagSokResultatDto> SokBilagAsync(
        BilagSokRequest request, CancellationToken ct = default);

    // --- Vedlegg ---

    /// <summary>
    /// Legg til vedlegg (metadata) til et bilag.
    /// </summary>
    Task<VedleggDto> LeggTilVedleggAsync(
        LeggTilVedleggRequest request, CancellationToken ct = default);

    /// <summary>
    /// Hent alle vedlegg for et bilag.
    /// </summary>
    Task<List<VedleggDto>> HentVedleggAsync(Guid bilagId, CancellationToken ct = default);

    /// <summary>
    /// Slett vedlegg (soft delete). Kun for ikke-bokforte bilag.
    /// </summary>
    Task SlettVedleggAsync(Guid bilagId, Guid vedleggId, CancellationToken ct = default);

    // --- Bilagserier ---

    /// <summary>
    /// Hent alle bilagserier.
    /// </summary>
    Task<List<BilagSerieDto>> HentBilagSerierAsync(CancellationToken ct = default);

    /// <summary>
    /// Hent bilagserie pa kode.
    /// </summary>
    Task<BilagSerieDto> HentBilagSerieAsync(string kode, CancellationToken ct = default);
}
```

### IBilagRepository (nytt repository)

```csharp
namespace Regnskap.Domain.Features.Bilag;

using Regnskap.Domain.Features.Hovedbok;

public interface IBilagRepository
{
    // --- Bilagserier ---
    Task<BilagSerie?> HentBilagSerieAsync(string kode, CancellationToken ct = default);
    Task<List<BilagSerie>> HentAlleBilagSerierAsync(bool? erAktiv = null, CancellationToken ct = default);
    Task LeggTilBilagSerieAsync(BilagSerie serie, CancellationToken ct = default);

    // --- Bilagserienummer ---
    Task<BilagSerieNummer?> HentSerieNummerAsync(
        string serieKode, int ar, CancellationToken ct = default);
    Task LeggTilSerieNummerAsync(BilagSerieNummer serieNummer, CancellationToken ct = default);

    // --- Vedlegg ---
    Task<Vedlegg?> HentVedleggAsync(Guid id, CancellationToken ct = default);
    Task<List<Vedlegg>> HentVedleggForBilagAsync(Guid bilagId, CancellationToken ct = default);
    Task LeggTilVedleggAsync(Vedlegg vedlegg, CancellationToken ct = default);

    // --- Bilagssok (utvidet) ---
    Task<List<Bilag>> SokBilagAsync(
        int? ar = null,
        int? periode = null,
        BilagType? type = null,
        string? serieKode = null,
        DateOnly? fraDato = null,
        DateOnly? tilDato = null,
        string? kontonummer = null,
        decimal? minBelop = null,
        decimal? maxBelop = null,
        string? beskrivelse = null,
        string? eksternReferanse = null,
        int? bilagsnummer = null,
        bool? erBokfort = null,
        bool? erTilbakfort = null,
        int side = 1,
        int antall = 50,
        CancellationToken ct = default);

    Task<int> TellSokResultaterAsync(
        int? ar = null,
        int? periode = null,
        BilagType? type = null,
        string? serieKode = null,
        DateOnly? fraDato = null,
        DateOnly? tilDato = null,
        string? kontonummer = null,
        decimal? minBelop = null,
        decimal? maxBelop = null,
        string? beskrivelse = null,
        string? eksternReferanse = null,
        int? bilagsnummer = null,
        bool? erBokfort = null,
        bool? erTilbakfort = null,
        CancellationToken ct = default);

    // --- Generelt ---
    Task LagreEndringerAsync(CancellationToken ct = default);
}
```

---

## Valideringsflyt (komplett)

Nar `OpprettBilagAsync` eller `ValiderBilagAsync` kalles:

```
Steg 1: Strukturvalidering
  [ ] Minst 2 posteringslinjer (eller minst 1 brukerlinje som genererer MVA)
  [ ] Alle belop > 0
  [ ] Bilagsdato er satt
  [ ] Beskrivelse er satt og ikke tom (maks 500 tegn)
  [ ] EksternReferanse maks 200 tegn

Steg 2: Serievalidering
  [ ] Hvis SerieKode angitt: serien finnes og er aktiv
  [ ] Hvis ikke angitt: bruk "MAN" som default

Steg 3: Periodevalidering
  [ ] Finn regnskapsperiode for Bilagsdato
  [ ] Perioden finnes
  [ ] Perioden er apen (ikke sperret eller lukket)

Steg 4: Kontovalidering (per linje)
  [ ] Kontonummer finnes
  [ ] Kontoen er aktiv
  [ ] Kontoen er bokforbar
  [ ] Avdelingskode angitt hvis konto krever det
  [ ] Prosjektkode angitt hvis konto krever det

Steg 5: MVA-validering og auto-generering
  [ ] For linjer med MvaKode: koden finnes og er aktiv
  [ ] MVA-konto finnes for valgt retning
  [ ] Beregn MvaBelop, MvaGrunnlag, MvaSats
  [ ] Generer auto-MVA-posteringslinjer

Steg 6: Balansekontroll
  [ ] Sum debet (alle linjer inkl. auto-MVA) = Sum kredit
  [ ] Minimum 2 posteringer totalt (inkl. auto-MVA)

Steg 7: Nummerering (kun ved opprettelse, ikke ved validering)
  [ ] Tildel globalt bilagsnummer
  [ ] Tildel serienummer

Steg 8: Bokforing (kun hvis BokforDirekte=true)
  [ ] Oppdater KontoSaldo for alle berorte kontoer
  [ ] Sett ErBokfort=true, BokfortTidspunkt, BokfortAv
```

---

## Nye Exceptions

```csharp
namespace Regnskap.Domain.Features.Bilag;

public class BilagIkkeFunnetException : Exception
{
    public Guid BilagId { get; }
    public BilagIkkeFunnetException(Guid bilagId)
        : base($"Bilag {bilagId} finnes ikke.") => BilagId = bilagId;
}

public class BilagAlleredeBokfortException : Exception
{
    public string BilagsId { get; }
    public BilagAlleredeBokfortException(string bilagsId)
        : base($"Bilag {bilagsId} er allerede bokfort.") => BilagsId = bilagsId;
}

public class BilagAlleredeTilbakefortException : Exception
{
    public string BilagsId { get; }
    public BilagAlleredeTilbakefortException(string bilagsId)
        : base($"Bilag {bilagsId} er allerede tilbakfort.") => BilagsId = bilagsId;
}

public class BilagIkkeBokfortException : Exception
{
    public string BilagsId { get; }
    public BilagIkkeBokfortException(string bilagsId)
        : base($"Bilag {bilagsId} er ikke bokfort og kan ikke tilbakefores.")
        => BilagsId = bilagsId;
}

public class BilagSerieIkkeFunnetException : Exception
{
    public string Kode { get; }
    public BilagSerieIkkeFunnetException(string kode)
        : base($"Bilagserie {kode} finnes ikke.") => Kode = kode;
}

public class BilagSerieInaktivException : Exception
{
    public string Kode { get; }
    public BilagSerieInaktivException(string kode)
        : base($"Bilagserie {kode} er deaktivert.") => Kode = kode;
}

public class VedleggIkkeTillattSlettingException : Exception
{
    public Guid VedleggId { get; }
    public VedleggIkkeTillattSlettingException(Guid vedleggId)
        : base($"Vedlegg {vedleggId} tilhorer et bokfort bilag og kan ikke slettes.")
        => VedleggId = vedleggId;
}

public class VedleggUgyldigTypeException : Exception
{
    public string MimeType { get; }
    public VedleggUgyldigTypeException(string mimeType)
        : base($"Filtypen {mimeType} er ikke tillatt for vedlegg.")
        => MimeType = mimeType;
}

public class VedleggForStortException : Exception
{
    public long Storrelse { get; }
    public VedleggForStortException(long storrelse)
        : base($"Vedlegg er {storrelse / 1_048_576} MB, maks tillatt er 25 MB.")
        => Storrelse = storrelse;
}

public class NummereringKonfliktException : Exception
{
    public NummereringKonfliktException()
        : base("Samtidig nummerering oppdaget. Prov igjen.") { }
}
```

---

## Avhengigheter

### Moduler dette avhenger av

| Modul | Interface | Bruk |
|-------|-----------|------|
| Kontoplan | `IKontoplanRepository` | Slå opp kontoer og MVA-koder for validering |
| Hovedbok | `IHovedbokRepository` | Periodesoppslag, bilagsnummerering, KontoSaldo-oppdatering |

### Interfaces dette eksponerer

| Interface | Konsument |
|-----------|-----------|
| `IBilagRegistreringService` (implementerer `IBilagService`) | API-kontrollere, fremtidige moduler (Faktura, Bank, Lonn) |
| `IBilagRepository` | Intern bruk i BilagRegistreringService |

### Fremtidige moduler som vil bruke denne

- **Faktura (Invoicing):** Oppretter bilag ved fakturering (UtgaendeFaktura-serie)
- **Leverandorreskontro:** Oppretter bilag ved mottak av leverandorfaktura (InngaendeFaktura-serie)
- **Bankavstemmning:** Oppretter bilag for bankbevegelser (Bank-serie)
- **Lonn:** Oppretter bilag for lonsbehandling (Lonn-serie)
- **MVA-oppgjor:** Oppretter bilag for MVA-oppgjor (Auto-serie)
- **Arsavslutning:** Oppretter bilag for arsavslutning (Auto-serie)
- **SAF-T eksport:** Leser bilag og posteringer for XML-generering

---

## Implementasjonsnotat: Postering.ErAutoGenerertMva

Posteringsentiteten (`Postering.cs`) ma utvides med:

```csharp
/// <summary>
/// Om denne posteringen er automatisk generert av MVA-logikken.
/// Auto-genererte posteringer vises annerledes i UI og skal ikke redigeres direkte.
/// </summary>
public bool ErAutoGenerertMva { get; set; }
```

---

## Transaksjonsflyt — Komplett eksempel

### Eksempel: Registrering av leverandorfaktura med MVA

**Input fra bruker:**
```json
{
  "type": "InngaendeFaktura",
  "bilagsdato": "2026-03-15",
  "beskrivelse": "Kontorrekvisita fra Staples",
  "eksternReferanse": "STAPLES-2026-4521",
  "serieKode": "IF",
  "posteringer": [
    {
      "kontonummer": "6800",
      "side": "Debet",
      "belop": 4000.00,
      "beskrivelse": "Kontorrekvisita",
      "mvaKode": "1"
    },
    {
      "kontonummer": "2400",
      "side": "Kredit",
      "belop": 5000.00,
      "beskrivelse": "Leverandorgjeld Staples",
      "leverandorId": "..."
    }
  ],
  "bokforDirekte": true
}
```

**System utforer:**
1. Valider serie "IF" — aktiv, OK
2. Finn periode for 2026-03-15 — periode 2026-03, apen, OK
3. Valider konto 6800 — finnes, aktiv, bokforbar, OK
4. Valider konto 2400 — finnes, aktiv, bokforbar, OK
5. MVA-kode "1": sats 25%, inngaende, konto 2710
6. Beregn MVA: 4000 * 25/100 = 1000,00
7. Auto-generer linje: Konto 2710, Debet, 1000,00
8. Balansekontroll: Debet 4000 + 1000 = 5000, Kredit 5000. OK.
9. Tildel bilagsnummer: globalt #142, serie IF-2026-00037
10. Bokfor: oppdater KontoSaldo for 6800, 2400, 2710
11. Sett ErBokfort=true

**Resultat — 3 posteringslinjer:**

| # | Konto | Side | Belop | MvaKode | MvaBelop | AutoMva |
|---|-------|------|-------|---------|----------|---------|
| 1 | 6800 Kontorrekvisita | Debet | 4 000,00 | 1 | 1 000,00 | Nei |
| 2 | 2400 Leverandorgjeld | Kredit | 5 000,00 | - | - | Nei |
| 3 | 2710 Inngaende MVA | Debet | 1 000,00 | - | - | Ja |

**BilagsId:** 2026-00142
**SerieBilagsId:** IF-2026-00037

---

## Sekvensdiagram: OpprettBilagAsync

```
Klient                Service                   KontoplanRepo    HovedbokRepo    BilagRepo
  |                      |                           |                |              |
  |-- OpprettBilag ----->|                           |                |              |
  |                      |-- HentBilagSerie -------->|                |              |
  |                      |<--- serie ----------------|                |              |
  |                      |                           |                |              |
  |                      |-- HentPeriodeForDato -----|--------------->|              |
  |                      |<--- periode --------------|----------------|              |
  |                      |                           |                |              |
  |                      |  For hver postering:      |                |              |
  |                      |-- HentKonto ------------->|                |              |
  |                      |<--- konto ----------------|                |              |
  |                      |-- HentMvaKode ----------->|                |              |
  |                      |<--- mvaKode --------------|                |              |
  |                      |                           |                |              |
  |                      |  [Generer MVA-linjer]     |                |              |
  |                      |  [Valider balanse]        |                |              |
  |                      |                           |                |              |
  |                      |-- HentSerieNummer --------|----------------|------------->|
  |                      |<--- serieNummer ----------|----------------|--------------|
  |                      |  [TildelNummer]           |                |              |
  |                      |-- NestebilagsnummerAsync--|--------------->|              |
  |                      |<--- bilagsnr -------------|----------------|              |
  |                      |                           |                |              |
  |                      |-- LeggTilBilag -----------|--------------->|              |
  |                      |                           |                |              |
  |                      |  [Hvis BokforDirekte:]    |                |              |
  |                      |  For hver postering:      |                |              |
  |                      |-- HentKontoSaldo ---------|--------------->|              |
  |                      |  [LeggTilPostering]       |                |              |
  |                      |                           |                |              |
  |                      |-- LagreEndringer ---------|--------------->|              |
  |                      |                           |                |              |
  |<--- BilagDto --------|                           |                |              |
```

---

## Database-migrering

### Nye tabeller

```sql
CREATE TABLE BilagSerier (
    Id uniqueidentifier PRIMARY KEY,
    Kode nvarchar(10) NOT NULL UNIQUE,
    Navn nvarchar(200) NOT NULL,
    NavnEn nvarchar(200) NULL,
    StandardType int NOT NULL,
    ErAktiv bit NOT NULL DEFAULT 1,
    ErSystemserie bit NOT NULL DEFAULT 0,
    SaftJournalId nvarchar(20) NOT NULL,
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    ModifiedAt datetime2 NULL,
    ModifiedBy nvarchar(200) NULL,
    IsDeleted bit NOT NULL DEFAULT 0
);

CREATE TABLE BilagSerieNumre (
    Id uniqueidentifier PRIMARY KEY,
    BilagSerieId uniqueidentifier NOT NULL REFERENCES BilagSerier(Id),
    SerieKode nvarchar(10) NOT NULL,
    Ar int NOT NULL,
    NesteNummer int NOT NULL DEFAULT 1,
    RowVersion rowversion NOT NULL,
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    ModifiedAt datetime2 NULL,
    ModifiedBy nvarchar(200) NULL,
    IsDeleted bit NOT NULL DEFAULT 0,
    UNIQUE (SerieKode, Ar)
);

CREATE TABLE Vedlegg (
    Id uniqueidentifier PRIMARY KEY,
    BilagId uniqueidentifier NOT NULL REFERENCES Bilag(Id),
    Filnavn nvarchar(500) NOT NULL,
    MimeType nvarchar(100) NOT NULL,
    Storrelse bigint NOT NULL,
    LagringSti nvarchar(1000) NOT NULL,
    HashSha256 nvarchar(64) NOT NULL,
    Beskrivelse nvarchar(500) NULL,
    Rekkefolge int NOT NULL DEFAULT 0,
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(200) NOT NULL,
    ModifiedAt datetime2 NULL,
    ModifiedBy nvarchar(200) NULL,
    IsDeleted bit NOT NULL DEFAULT 0
);
CREATE INDEX IX_Vedlegg_BilagId ON Vedlegg(BilagId);
```

### Endringer i eksisterende Bilag-tabell

```sql
ALTER TABLE Bilag ADD BilagSerieId uniqueidentifier NULL REFERENCES BilagSerier(Id);
ALTER TABLE Bilag ADD SerieKode nvarchar(10) NULL;
ALTER TABLE Bilag ADD SerieNummer int NULL;
ALTER TABLE Bilag ADD TilbakefortFraBilagId uniqueidentifier NULL REFERENCES Bilag(Id);
ALTER TABLE Bilag ADD TilbakefortAvBilagId uniqueidentifier NULL REFERENCES Bilag(Id);
ALTER TABLE Bilag ADD ErTilbakfort bit NOT NULL DEFAULT 0;
ALTER TABLE Bilag ADD ErBokfort bit NOT NULL DEFAULT 0;
ALTER TABLE Bilag ADD BokfortTidspunkt datetime2 NULL;
ALTER TABLE Bilag ADD BokfortAv nvarchar(200) NULL;

CREATE UNIQUE INDEX IX_Bilag_Serie_Ar_Nummer
    ON Bilag(SerieKode, Ar, SerieNummer)
    WHERE SerieKode IS NOT NULL;
```

### Endringer i eksisterende Postering-tabell

```sql
ALTER TABLE Posteringer ADD ErAutoGenerertMva bit NOT NULL DEFAULT 0;
```

---

## SAF-T mapping (komplett)

### Journal-mapping

Bilagserier mapper til SAF-T Journals:

```xml
<GeneralLedgerEntries>
  <NumberOfEntries>{totalt antall bilag}</NumberOfEntries>
  <TotalDebit>{total debet}</TotalDebit>
  <TotalCredit>{total kredit}</TotalCredit>

  <Journal>
    <JournalID>{BilagSerie.SaftJournalId}</JournalID>
    <Description>{BilagSerie.Navn}</Description>
    <Type>{BilagSerie.StandardType -> SAF-T type}</Type>

    <Transaction>
      <TransactionID>{Bilag.BilagsId}</TransactionID>
      <Period>{Bilag.SaftPeriode}</Period>
      <TransactionDate>{Bilag.Bilagsdato}</TransactionDate>
      <SourceID>{Bilag.CreatedBy}</SourceID>
      <TransactionType>Normal</TransactionType>
      <Description>{Bilag.Beskrivelse}</Description>
      <SystemEntryDate>{Bilag.Registreringsdato}</SystemEntryDate>
      <GLPostingDate>{Bilag.BokfortTidspunkt}</GLPostingDate>

      <Line>
        <RecordID>{Postering.Linjenummer}</RecordID>
        <AccountID>{Postering.Kontonummer}</AccountID>
        <Description>{Postering.Beskrivelse}</Description>
        <DebitAmount>  <!-- eller CreditAmount -->
          <Amount>{Postering.Belop}</Amount>
        </DebitAmount>
        <TaxInformation>
          <TaxType>MVA</TaxType>
          <TaxCode>{Postering.MvaKode}</TaxCode>
          <TaxPercentage>{Postering.MvaSats}</TaxPercentage>
          <TaxBase>{Postering.MvaGrunnlag}</TaxBase>
          <TaxAmount>
            <Amount>{Postering.MvaBelop}</Amount>
          </TaxAmount>
        </TaxInformation>
        <CustomerID>{Postering.KundeId}</CustomerID>
        <SupplierID>{Postering.LeverandorId}</SupplierID>
      </Line>
    </Transaction>
  </Journal>
</GeneralLedgerEntries>
```

### BilagType -> SAF-T Journal/Type mapping

| BilagType | SAF-T Type |
|-----------|------------|
| Manuelt | GJ (General Journal) |
| InngaendeFaktura | PI (Purchase Invoice) |
| UtgaendeFaktura | SI (Sales Invoice) |
| Bank | BP (Bank Payment) |
| Lonn | SJ (Salary Journal) |
| Avskrivning | GJ |
| MvaOppgjor | GJ |
| Arsavslutning | GJ |
| Apningsbalanse | OB (Opening Balance) |
| Kreditnota | CN (Credit Note) |
| Korreksjon | GJ |

---

## Kontrollspor (NBS 2)

Bilagsregistrering opprettholder komplett kontrollspor:

1. **Bilag -> Postering -> Konto -> KontoSaldo:** Hver postering peker til bilag og konto. KontoSaldo oppdateres inkrementelt.
2. **Bilag -> Vedlegg:** Dokumentasjonen (kvittering, faktura) er knyttet direkte til bilaget.
3. **Tilbakeforing:** Toveis lenke mellom original og tilbakeforingsbilag.
4. **Tidsstempling:** `CreatedAt`, `Registreringsdato`, `BokfortTidspunkt` gir komplett tidshistorikk.
5. **Brukeridentitet:** `CreatedBy`, `BokfortAv` sporbar til bruker.
6. **Soft delete:** Ingen hard deletes. `IsDeleted` pa alle entiteter.
7. **Bilagsnummerering:** Fortlopende nummerering uten hull, bade globalt og per serie.
