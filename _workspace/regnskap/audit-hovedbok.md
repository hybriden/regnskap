# Revisjonsrapport: Hovedbok (General Ledger)

**Revisjonsdato:** 2026-04-06
**Revisor:** Revisjonsagent (automatisert)
**Modul:** Hovedbok
**Status:** Fase 1 (Perioder, Saldo, Kontoutskrift) -- Bilag-bokforing er definert som interface, ikke implementert enna.

---

## Sammendrag

- Antall MUST_FIX: **4**
- Antall SHOULD_FIX: **6**
- Antall OK: **6**
- Samlet status: **KREVER_ENDRING**

Hovedbok-modulen har en solid domenmodell med korrekt dobbelt bokholderi-validering i Bilag-entiteten. Periodehåndtering og saldooppslag er godt implementert. Imidlertid mangler den faktiske bilagbokforingstjenesten (IBilagService har ingen implementasjon), og periodeavstemmingen har flere "stub"-kontroller som alltid returnerer OK uten faktisk verifisering. I tillegg er det en race condition i bilagsnummertildelingen og avvik mellom spec og implementasjon i HasFilter-syntaks.

---

## Sjekkpunkter

### 1. Dobbelt bokholderi-integritet

- [x] Debet = kredit valideres i domenet
- [x] Balansesjekk ved bilagsopprettelse (ValiderBalanse i Bilag.cs)
- [ ] Balansesjekk ved bilagsendring (ikke relevant -- bilag er uforanderlige)
- **Status: OK**
- **Funn:** `Bilag.ValiderBalanse()` (Bilag.cs:97-108) sjekker korrekt at posteringer >= 2 og at `SumDebet().Verdi == SumKredit().Verdi`. Bruker `Belop`-value object med `decimal` som unngår floating-point-feil. `AccountingBalanceException` kastes med tydelig differanse-melding. Saldobalanse-sjekken i `HovedbokService.HentSaldobalanseAsync()` returnerer `ErIBalanse`-flagg basert på totalDebet == totalKredit.

  Merk: Den faktiske bokforingslogikken som kaller `ValiderBalanse()` finnes ikke enna (IBilagService er interface uten implementasjon). Valideringen i domenet er korrekt, men det mangler en service som faktisk bruker den. Se sjekkpunkt 6.

---

### 2. Revisjonsspor

- [x] CreatedAt/CreatedBy på alle entiteter (via AuditableEntity)
- [x] ModifiedAt/ModifiedBy på alle entiteter (via AuditableEntity)
- [x] Soft delete (IsDeleted) på alle entiteter
- [ ] Ingen hard delete i koden
- **Status: OK**
- **Funn:** Alle fire domeneentiteter (Regnskapsperiode, Bilag, Postering, KontoSaldo) arver fra `AuditableEntity` som har `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`, og `IsDeleted`. Alle unike indekser bruker `HasFilter` med `IsDeleted = false` for å respektere soft delete. Ingen hard delete-operasjoner finnes i repository-koden.

  **SHOULD_FIX:** `PeriodeService.EndrePeriodeStatusAsync()` setter `LukketAv = "system"` (PeriodeService.cs:100) med en TODO-kommentar. Denne bor hente faktisk brukernavn fra HttpContext/brukerkontekst.
  - **Fil:** `D:\Code\Regnskap\src\Regnskap.Application\Features\Hovedbok\PeriodeService.cs:100`
  - **Foreslatt fix:** Injiser `ICurrentUserService` eller tilsvarende og bruk faktisk brukernavn.

---

### 3. Regnskapsloven-compliance (Bokforingsloven)

- [x] Bilagsnummerering er fortlopende (NestebilagsnummerAsync bruker MAX+1)
- [ ] Bokforing uten ugrunnet opphold (timestamp finnes, men ingen advarsel)
- [x] Sporbarhet fra rapport til bilag (BilagsId i kontoutskrift)
- **Status: MUST_FIX**

#### Funn 3a: Race condition i bilagsnummertildeling (MUST_FIX)

`HovedbokRepository.NestebilagsnummerAsync()` (HovedbokRepository.cs:66-72) henter MAX(bilagsnummer)+1 uten noen form for låsing. Ved samtidige foresporsler kan to bilag få samme nummer. Selv om det finnes et unikt indeks i databasen (BilagConfiguration.cs:26-28) som vil kaste en feil ved duplikat, gir dette en dårlig brukeropplevelse og kan i verste fall skape hull i nummereringen hvis den ene transaksjonen feiler og den andre lykkes.

- **Fil:** `D:\Code\Regnskap\src\Regnskap.Infrastructure\Features\Hovedbok\HovedbokRepository.cs:66-72`
- **Foreslatt fix:** Bruk en database-sekvens (SQL Server SEQUENCE), pessimistisk låsing (SELECT FOR UPDATE), eller serialiserbar transaksjon for å sikre atomisk nummertildeling. Alternativt: bruk retry-logikk ved duplikatnummerfeil.

#### Funn 3b: Periodeavstemming sjekker ikke faktisk fortlopende nummerering (MUST_FIX)

`PeriodeService.KjorPeriodeavstemmingAsync()` (PeriodeService.cs:159-164) har kontrollen "FortlopendeNummer" som alltid returnerer "OK" uten å faktisk sjekke at bilagsnumrene er fortlopende uten hull. Bokforingsloven paragraf 4 nr. 7 krever kontrollspor med fortlopende nummerering.

- **Fil:** `D:\Code\Regnskap\src\Regnskap.Application\Features\Hovedbok\PeriodeService.cs:159-164`
- **Foreslatt fix:** Implementer faktisk kontroll: hent alle bilagsnumre for aret, sorter, og verifiser at det ikke finnes hull (dvs. sekvensen er 1, 2, 3, ... N uten manglende verdier).

#### Funn 3c: Ingen advarsel om ajourhold (SHOULD_FIX)

Bokforingsloven paragraf 7 krever bokforing "uten ugrunnet opphold" med spesifikke frister (minst hver 4. måned, daglig for kontantsalg). Systemet registrerer `Registreringsdato` på bilag (bra), men det finnes ingen mekanisme for å advare brukeren om bokforing er forsinket relativt til rapporteringsfrister.

- **Foreslatt fix:** Legg til en tjeneste som sammenligner siste bokforingsdato mot gjeldende frister og returnerer advarsler.

---

### 4. MVA-korrekthet

- [x] MVA-felt finnes på Postering (MvaKode, MvaBelop, MvaGrunnlag, MvaSats)
- [ ] MVA beregnes korrekt (ikke implementert enna)
- [x] MVA-grunnlag og MVA-belop er separate felt
- [x] Mapping til SAF-T MVA-koder (felt er kompatible)
- **Status: SHOULD_FIX**
- **Funn:** MVA-feltene er korrekt definert i domenemodellen med separate felt for grunnlag, belop og sats. Snapshot-prinsippet (FR-HOV-015) er ivaretatt i modellen. Imidlertid finnes det ingen implementert MVA-beregningslogikk -- dette avhenger av IBilagService-implementasjonen som ikke eksisterer enna. DTOene eksponerer MVA-felt korrekt.

  **SHOULD_FIX:** Nar IBilagService implementeres, verifiser at MVA-beregning automatisk fyller MvaBelop, MvaGrunnlag og MvaSats basert pa MvaKode-oppslag.

---

### 5. SAF-T-kompatibilitet

- [x] Alle pakreve SAF-T-felt finnes i modellen
- [x] TransactionID er unik og sporbar (BilagsId = "YYYY-NNNNN")
- [x] AccountID via Kontonummer (denormalisert)
- [x] DebitAmount/CreditAmount via Side + Belop
- [x] SystemEntryDate via Registreringsdato
- [x] TransactionDate via Bilagsdato
- [x] CustomerID/SupplierID pa Postering
- [x] TaxInformation-felt (MvaKode, MvaSats, MvaGrunnlag, MvaBelop)
- **Status: OK**
- **Funn:** Domenemodellen dekker alle SAF-T GeneralLedgerEntries-felt. BilagType-enum mapper til SAF-T Journal/Type-koder. Postering har alle nodvendige felt inkludert KundeId, LeverandorId, og fullstendig MVA-informasjon. SaftPeriode-egenskapen gir korrekt periodemapping.

  Merk: Selve SAF-T-eksportmekanismen er ikke implementert i denne modulen (forventes som egen modul), men datamodellen er fullt kompatibel.

---

### 6. Spesifikasjonsoverensstemmelse

- [x] GET /api/perioder/{ar} implementert
- [x] POST /api/perioder/opprett-ar implementert
- [x] PUT /api/perioder/{ar}/{periode}/status implementert
- [x] GET /api/perioder/{ar}/{periode}/avstemming implementert
- [x] GET /api/kontoutskrift/{kontonummer} implementert
- [x] GET /api/saldobalanse/{ar}/{periode} implementert
- [x] GET /api/saldo/{kontonummer} implementert
- [ ] POST /api/bilag -- IKKE implementert (IBilagService mangler impl.)
- [ ] GET /api/bilag/{id} -- IKKE implementert
- [ ] GET /api/bilag -- IKKE implementert
- **Status: MUST_FIX**

#### Funn 6a: IBilagService mangler implementasjon (MUST_FIX)

IBilagService er kun et interface uten implementasjon. Kommentaren sier "Full implementasjon kommer i Modul 3 (Bilag)", og det er ikke registrert i DI-kontaineren (HovedbokServiceExtensions.cs registrerer ikke IBilagService). Dette betyr at kjerneoperasjonen -- a opprette og bokfore bilag -- ikke kan utfores. Uten bilagbokforing er modulen ufullstendig som hovedbok.

- **Fil:** `D:\Code\Regnskap\src\Regnskap.Application\Features\Hovedbok\IBilagService.cs`
- **Fil:** `D:\Code\Regnskap\src\Regnskap.Api\Features\Hovedbok\HovedbokServiceExtensions.cs`
- **Foreslatt fix:** Implementer IBilagService med full bilagbokforing inkludert: periodevalidering, kontovalidering, balansesjekk, MVA-beregning, saldooppdatering, og bilagsnummertildeling -- alt i en atomisk transaksjon (FR-HOV-014).

#### Funn 6b: Spesifikasjonen definerer IHovedbokService med bilag-metoder, men implementasjonen deler dette i tre interfaces (SHOULD_FIX)

Spec-en definerer ett samlet `IHovedbokService` med bade bilag-, saldo- og periodemetoder. Implementasjonen deler dette i `IHovedbokService` (saldo/kontoutskrift), `IPeriodeService` (perioder), og `IBilagService` (bilag). Denne oppdelingen er arkitektonisk forsvarlig og bedre enn spec-en, men bor dokumenteres som et bevisst avvik.

#### Funn 6c: Repository-signatur avviker fra spec (SHOULD_FIX)

Spec-en definerer `HentBilagForPeriodeAsync(int ar, int periode, ...)` der `periode` er pakreved. Implementasjonen har `HentBilagForPeriodeAsync(int ar, int? periode = null, ...)` der `periode` er valgfri. Dette er et funksjonelt avvik som gir mer fleksibilitet men avviker fra kontrakten.

- **Fil:** `D:\Code\Regnskap\src\Regnskap.Domain\Features\Hovedbok\IHovedbokRepository.cs:19-23`

#### Funn 6d: HasFilter-syntaks avviker mellom spec og implementasjon (MUST_FIX)

Spec-en bruker SQL Server-syntaks: `HasFilter("IsDeleted = 0")`. Implementasjonen bruker PostgreSQL-syntaks: `HasFilter("\"IsDeleted\" = false")`. Disse er ulike og kun en av dem vil fungere avhengig av databaseleverandor. Dette ma vaere konsistent med prosjektets valgte database.

- **Filer:**
  - `D:\Code\Regnskap\src\Regnskap.Infrastructure\Features\Hovedbok\RegnskapsperiodeConfiguration.cs:26`
  - `D:\Code\Regnskap\src\Regnskap.Infrastructure\Features\Hovedbok\BilagConfiguration.cs:28`
  - `D:\Code\Regnskap\src\Regnskap.Infrastructure\Features\Hovedbok\PosteringConfiguration.cs:62`
  - `D:\Code\Regnskap\src\Regnskap.Infrastructure\Features\Hovedbok\KontoSaldoConfiguration.cs:35`
- **Foreslatt fix:** Avklar hvilken database prosjektet bruker og bruk konsistent syntaks. Hvis PostgreSQL: navaerende implementasjon er korrekt. Oppdater spec-en. Likeledes for indeks-filtrene for nullable kolonner (KundeId, LeverandorId etc.) som bruker quoted column names.

---

### 7. Testkvalitet

- [x] Unit tests for periodestyring (OpprettPerioder, statusoverganger)
- [x] Unit tests for saldobalanse og saldooppslag
- [x] Unit tests for kontoutskrift med lopende balanse
- [x] Unit tests for domenevalidering (Bilag balanse, KontoSaldo beregning, Regnskapsperiode)
- [x] Edge cases: ugyldige statusoverganger, ikke-eksisterende perioder/kontoer
- [ ] Ingen tester for bilagbokforing (IBilagService ikke implementert)
- [ ] Ingen tester for MVA-beregning
- [ ] Ingen tester for concurrent bilagsnummertildeling
- **Status: SHOULD_FIX**

#### Funn 7a: Stub-kontroller i periodeavstemming er ikke testet (SHOULD_FIX)

Testene verifiserer at periodeavstemming kjorer og at ForrigePeriodeLukket-sjekken fungerer, men de tre stub-kontrollene (SaldoKontroll, AlleKontoerHarSaldo, FortlopendeNummer) testes ikke med faktiske data som ville avdekke feil.

- **Fil:** `D:\Code\Regnskap\tests\Regnskap.Tests\Features\Hovedbok\PeriodeServiceTests.cs`
- **Foreslatt fix:** Legg til tester som setter opp data som ville feile disse kontrollene, og verifiser at kontrollene faktisk oppdager feilene (nar de er implementert).

#### Funn 7b: Kontoutskrift IB-beregning bruker full tabell-scan (SHOULD_FIX)

`HovedbokService.HentKontoutskriftAsync()` (HovedbokService.cs:30-31) henter ALLE posteringer for fraDato for å beregne inngaende balanse med `int.MaxValue` som antall. For kontoer med mange historiske posteringer kan dette vaere svært tregt.

- **Fil:** `D:\Code\Regnskap\src\Regnskap.Application\Features\Hovedbok\HovedbokService.cs:30-31`
- **Foreslatt fix:** Bruk KontoSaldo-tabellen for å hente inngaende balanse for perioden i stedet for å summere alle historiske posteringer. KontoSaldo er nettopp designet for dette formalet.

---

### 8. Sikkerhet

- [x] Autorisasjon på alle endepunkter ([Authorize] på begge kontrollere)
- [x] Ingen SQL injection-muligheter (bruker EF Core med LINQ)
- [x] Input-validering i kontrollere (Enum.TryParse for status, BadRequest for ugyldige verdier)
- [ ] Ingen rollebasert tilgang (bare [Authorize], ingen [Authorize(Roles = ...)])
- **Status: OK**
- **Funn:** Begge kontrollere bruker `[Authorize]`-attributt. EF Core med LINQ eliminerer SQL injection. Alle unntak handteres med passende HTTP-statuskoder. Ingen sensitiv data eksponeres unodig.

  **Merknad:** Det finnes ingen rollebasert tilgangskontroll. Periodelukking og -sperring bor sannsynligvis kreve en spesifikk rolle (f.eks. "Regnskapsansvarlig"). Dette er ikke et krav i navaerende spec, men er anbefalt for produksjon.

---

## Detaljert funn-oversikt

| # | Funn | Alvorlighet | Sjekkpunkt | Fil |
|---|------|-------------|------------|-----|
| 1 | Race condition i bilagsnummertildeling | MUST_FIX | 3 | HovedbokRepository.cs:66-72 |
| 2 | Periodeavstemming: FortlopendeNummer er stub | MUST_FIX | 3 | PeriodeService.cs:159-164 |
| 3 | IBilagService mangler implementasjon | MUST_FIX | 6 | IBilagService.cs |
| 4 | HasFilter-syntaks avviker fra spec | MUST_FIX | 6 | Alle *Configuration.cs |
| 5 | LukketAv hardkodet til "system" | SHOULD_FIX | 2 | PeriodeService.cs:100 |
| 6 | Ingen ajourhold-advarsel | SHOULD_FIX | 3 | -- |
| 7 | MVA-beregning ikke implementert | SHOULD_FIX | 4 | -- |
| 8 | Periodeavstemming: SaldoKontroll er stub | SHOULD_FIX | 3 | PeriodeService.cs:136-140 |
| 9 | Periodeavstemming: AlleKontoerHarSaldo er stub | SHOULD_FIX | 3 | PeriodeService.cs:153-157 |
| 10 | Kontoutskrift IB bruker full tabell-scan | SHOULD_FIX | 7 | HovedbokService.cs:30-31 |

---

## Vurdering av kritiske krav

### Debet = Kredit invariant
**BESTATT.** Domenemodellen har korrekt validering i `Bilag.ValiderBalanse()`. Value object `Belop` bruker `decimal` for presisjon. Tester dekker balansert, ubalansert, og for-faa-linjer-scenarioer.

### Periodelukking forhindrer uautorisert postering
**BESTATT (i domenelaget).** `Regnskapsperiode.ValiderApen()` kaster `PeriodeLukketException` for bade Sperret og Lukket status. Statusoverganger er korrekt validert med gyldige/ugyldige overganger. Lukking er permanent (ingen vei tilbake). Men: den faktiske bruken av `ValiderApen()` i bilagbokforing er avhengig av IBilagService-implementasjonen som ikke eksisterer enna.

### KontoSaldo inkrementell beregning
**BESTATT (i domenelaget).** `KontoSaldo.LeggTilPostering()` oppdaterer SumDebet/SumKredit korrekt. UtgaendeBalanse beregnes som IB + SumDebet - SumKredit. Tester verifiserer korrekt oppforsel. Men: ingen service kaller denne metoden enna.

### Bilag-uforanderlighet
**BESTATT.** Ingen oppdateringsmetoder finnes for Bilag eller Postering i repository-interfacet. Spec-en sier eksplisitt at korreksjoner gjores via nye korreksjonsbilag (BilagType.Korreksjon). Soft delete er det eneste alternativet, og hard delete er ikke mulig via repository.

### SAF-T GeneralLedgerEntries-kompatibilitet
**BESTATT.** Alle pakreve felt er til stede i domenemodellen. Mapping-kommentarer i koden samsvarer med SAF-T-spec-en.

### Bokforingsloven compliance
**DELVIS BESTATT.** Fortlopende nummerering er designet inn, men har race condition (MUST_FIX). Registreringsdato fanges. Sporbarhet fra kontoutskrift til bilag er implementert. Men: ingen advarsel om ajourhold-frister.

---

## Konklusjon

Hovedbok-modulen har et solid fundament med korrekt domenemodellering, god SAF-T-kompatibilitet, og gjennomtenkt periodehåndtering. De fire MUST_FIX-funnene fordeler seg i to kategorier:

1. **Manglende implementasjon** (IBilagService) -- dette er den storste mangelen og forhindrer modulen fra å vaere funksjonell som hovedbok.
2. **Tekniske feil** (race condition, stub-kontroller, HasFilter-syntaks) som ma fikses for a sikre dataintegritet og korrekt drift.

Modulen **kan ikke godkjennes** i navaerende tilstand, men den trenger ikke redesign. Anbefalt handlingsplan:

1. Implementer IBilagService med atomisk bokforing
2. Fiks race condition i bilagsnummertildeling
3. Implementer faktiske periodeavstemmingskontroller
4. Avklar og standardiser HasFilter-syntaks mot valgt database
5. Legg til tester for bilagbokforing og MVA-beregning
