# Revisjonsrapport: Kontoplan

**Dato:** 2026-04-06
**Revisor:** Revisjonsagent (automatisert)
**Modul:** Kontoplan (Chart of Accounts)
**Kildekode revidert:** Domain, Application, Infrastructure, Api, Tests

---

## Sammendrag

- Antall MUST_FIX: 5
- Antall SHOULD_FIX: 9
- Antall OK: 3
- Samlet status: **KREVER_ENDRING**

---

## Sjekkpunkter

### 1. Dobbelt bokholderi-integritet

- [x] Debet = kredit valideres i domenet -- Ikke direkte relevant for Kontoplan-modulen. Kontoplan definerer kontostrukturen. Selve debet/kredit-balansering haandteres av Hovedbok/Bilag-modulen.
- [x] Kontotype-til-normalbalanse-mapping er korrekt implementert (FR-10)
- [x] Kontoklasse-til-kontotype-konsistens valideres (FR-11)

- **Status: OK**
- Kontoplan-modulen legger et solid grunnlag for dobbelt bokholderi ved aa definere riktige normalbalanser og kontotyper. Mappingen i `KontoService.BestemNormalbalanse()` og `ValiderKontotypeForKontoklasse()` er korrekt.

---

### 2. Revisjonsspor

- [x] CreatedAt/CreatedBy paa alle entiteter -- Via `AuditableEntity` base class
- [x] ModifiedAt/ModifiedBy paa alle entiteter -- Via `AuditableEntity` base class
- [x] Soft delete (IsDeleted) -- Implementert i `AuditableEntity`, global query filter i `RegnskapDbContext`, og `DbContext.SaveChanges()` intercepter `EntityState.Deleted`
- [ ] Ingen hard delete i koden -- Ingen hard delete funnet i Kontoplan-koden
- [ ] CreatedBy settes fra faktisk bruker

- **Status: MUST_FIX**

**Funn M-1: CreatedBy/ModifiedBy er hardkodet til "system"**
- Fil: `D:\Code\Regnskap\src\Regnskap.Infrastructure\Persistence\RegnskapDbContext.cs:51`
- Alvorlighet: MUST_FIX
- Beskrivelse: `CreatedBy` og `ModifiedBy` settes til `"system"` i stedet for den faktiske innloggede brukeren. Bokforingsloven krever sporbarhet til hvem som utforte handlingen (Bokforingsloven ss 4, punkt 7: Sporbarhet/kontrollspor).
- Foreslatt fix: Injiser `IHttpContextAccessor` eller en `ICurrentUserService` og hent brukeridentitet derfra. TODO-kommentaren i koden bekrefter at dette er kjent.

---

### 3. Regnskapsloven-compliance

- [x] Kontoplanen folger NS 4102-strukturen (Kontoklasse 1-8, Kontogruppe 10-89, Konto 1000-8999)
- [x] Kontonummerering er konsistent og validert
- [x] Soft delete bevarer historikk (oppbevaringsplikt)
- [ ] Kontoplan kan ikke endres etter periodeavslutning

- **Status: SHOULD_FIX**

**Funn S-1: Ingen periodebeskyttelse paa kontoplanendringer**
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Kontoplan\KontoService.cs:127-148`
- Alvorlighet: SHOULD_FIX
- Beskrivelse: Kontoer kan endres og slettes uten aa sjekke om de tilhorer en avsluttet regnskapsperiode. Regnskapsloven (ss 4-1) og god regnskapsskikk krever at data i avsluttede perioder er uforanderlige. For Kontoplan-modulen er dette mindre kritisk enn for posteringer, men bor varsles.
- Foreslatt fix: Legg til sjekk mot periodestatus foer sletting/vesentlig endring av kontoer som har posteringer i avsluttede perioder.

---

### 4. MVA-korrekthet

- [x] Riktige MVA-koder definert i seed data (0, 1, 3, 5, 6, 11, 13, 14, 15, 31, 33)
- [x] MVA-satser er korrekte (25%, 15%, 12%, 0%)
- [x] SAF-T StandardTaxCode mapping finnes
- [x] MvaRetning-enum inkluderer Ingen, Inngaende, Utgaende, SnuddAvregning
- [ ] FR-17: MVA-kontokoblinger valideres korrekt
- [ ] FR-16: MVA-kode-konsistens med kontotype

- **Status: MUST_FIX**

**Funn M-2: FR-17 MVA-kontokoblinger er for svak**
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Kontoplan\MvaKodeService.cs:126-138`
- Alvorlighet: MUST_FIX
- Beskrivelse: `ValidateMvaKontokoblinger()` bryter ikke ved manglende konto for Utgaende og Inngaende MVA-koder. Ifoolge spec FR-17: "Utgaende MVA-koder maa ha UtgaendeKontoId satt" og "Inngaende MVA-koder maa ha InngaendeKontoId satt". Koden har kommentar "Warn but don't block" men dette bryter med spesifikasjonen som sier "maa ha". Unntak for kode 5 (utforsel 0%) boer haandteres eksplisitt i stedet for aa ignorere regelen generelt.
- Foreslatt fix: Kast `ArgumentException` for Utgaende uten UtgaendeKontoId og Inngaende uten InngaendeKontoId, med unntak for koder med sats 0% (som kode 5 og 6).

**Funn S-2: FR-16 MVA-konsistens-varsling mangler**
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Kontoplan\KontoService.cs:52-125`
- Alvorlighet: SHOULD_FIX
- Beskrivelse: Spec FR-16 krever at systemet skal "varsle (ikke blokkere) ved uvanlige kombinasjoner" av MVA-kode og kontotype. For eksempel balansekontoer med MVA-kode, eller inntektskontoer med inngaaende MVA. Denne valideringen/varslingen er ikke implementert.
- Foreslatt fix: Legg til en warning/response-header eller et felt i opprett-response som varsler om uvanlige kombinasjoner.

**Funn S-3: Seed-data mangler MVA-kontokoblinger**
- Fil: `D:\Code\Regnskap\src\Regnskap.Infrastructure\Features\Kontoplan\KontoplanSeedData.cs:152-168`
- Alvorlighet: SHOULD_FIX
- Beskrivelse: `HentStandardMvaKoder()` returnerer tupler uten kontokoblinger (UtgaendeKontoId/InngaendeKontoId). Spec-tabellen viser at kode 1 skal kobles til konto 2710, kode 3 til konto 2700, osv. Seed-logikken maa koble MVA-koder til de riktige balansekontoene.
- Foreslatt fix: Utvid seed-logikken til aa slaa opp MVA-kontoer etter seeding og sette FK-referanser.

---

### 5. SAF-T-kompatibilitet

- [x] AccountID = Konto.Kontonummer
- [x] StandardAccountID = Konto.StandardAccountId (obligatorisk, validert)
- [x] AccountType hardkodet "GL" (haandteres i eksport, ikke lagret)
- [x] GroupingCategory = Konto.GrupperingsKategori
- [x] GroupingCode = Konto.GrupperingsKode
- [x] TaxCode/StandardTaxCode paa MvaKode
- [ ] GrupperingsKategori er nullable -- boer varsles

- **Status: SHOULD_FIX**

**Funn S-4: GrupperingsKategori og GrupperingsKode er nullable uten varsling**
- Fil: `D:\Code\Regnskap\src\Regnskap.Domain\Features\Kontoplan\Konto.cs:50-55`
- Alvorlighet: SHOULD_FIX
- Beskrivelse: Ifoolge spec FR-13: "Alle kontoer boer ha GrupperingsKategori og GrupperingsKode satt. Systemet skal varsle brukeren hvis disse mangler." Feltene er nullable (korrekt), men det finnes ingen varsling til brukeren naar de mangler. SAF-T v1.30 krever dette.
- Foreslatt fix: Legg til varsling i opprett/oppdater-response naar GrupperingsKategori er null.

**Funn S-5: Import/Eksport-endepunkter mangler**
- Fil: Mangler helt
- Alvorlighet: SHOULD_FIX
- Beskrivelse: Spec definerer `POST /kontoer/importer` og `GET /kontoer/eksporter` (inkludert SAF-T XML-format). Disse endepunktene er ikke implementert. SAF-T-eksport er spesielt viktig for compliance (Bokforingsforskriften krever SAF-T-eksport).
- Foreslatt fix: Implementer import/eksport-endepunktene som definert i spec, med SAF-T XML-generering for GeneralLedgerAccounts-seksjonen.

---

### 6. Spesifikasjonsoverensstemmelse

**Endepunkter:**

| Spec-endepunkt | Implementert | Status |
|---|---|---|
| GET /kontogrupper | Ja | OK |
| GET /kontogrupper/{gruppekode} | Ja | OK |
| GET /kontoer | Ja | OK |
| GET /kontoer/{kontonummer} | Ja | OK |
| POST /kontoer | Ja | OK |
| PUT /kontoer/{kontonummer} | Ja | OK |
| DELETE /kontoer/{kontonummer} | Ja | OK |
| POST /kontoer/{kontonummer}/deaktiver | Ja | OK |
| POST /kontoer/{kontonummer}/aktiver | Ja | OK |
| GET /kontoer/oppslag | Ja | OK |
| POST /kontoer/importer | **NEI** | MANGLER |
| GET /kontoer/eksporter | **NEI** | MANGLER |
| GET /mva-koder | Ja | OK |
| GET /mva-koder/{kode} | Ja | OK |
| POST /mva-koder | Ja | OK |
| PUT /mva-koder/{kode} | Ja | OK |

**Forretningsregler:**

| Regel | Implementert | Status |
|---|---|---|
| FR-1: Kontonummerformat | Ja | OK |
| FR-2: Kontonummer immutable | Ja (PUT tar ikke kontonummer i body) | OK |
| FR-3: Kontonummer-gruppe-konsistens | Ja | OK |
| FR-4: Underkonto-prefix | Ja | OK |
| FR-5: Systemkontoer kan ikke slettes | Ja | OK |
| FR-6: Systemkontoer beskyttede felter | **DELVIS** | MUST_FIX |
| FR-7: Konto med posteringer kan ikke slettes | Ja (stub) | OK |
| FR-8: Konto med aktive underkontoer | Ja | OK |
| FR-9: Deaktiverte kontoer avvises | Ja | OK |
| FR-10: Kontotype-normalbalanse-mapping | Ja | OK |
| FR-11: Kontoklasse-kontotype-konsistens | Ja | OK |
| FR-12: StandardAccountID obligatorisk | Ja | OK |
| FR-13: GroupingCategory varsling | **NEI** | SHOULD_FIX |
| FR-14: StandardAccountID kontoklasse-match | Ja | OK |
| FR-15: Standard MVA-kode er forslag | Ja (design) | OK |
| FR-16: MVA-konsistens varsling | **NEI** | SHOULD_FIX |
| FR-17: MVA-kontokoblinger | **DELVIS** | MUST_FIX |
| FR-18: Import validerer regler | **NEI** | SHOULD_FIX |
| FR-19: Erstatt-modus beskytter systemkontoer | **NEI** | SHOULD_FIX |
| FR-20: Eksport SAF-T-format | **NEI** | SHOULD_FIX |

- **Status: MUST_FIX**

**Funn M-3: FR-6 Systemkonto-beskyttelse mangler i OppdaterKontoAsync**
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Kontoplan\KontoService.cs:127-148`
- Alvorlighet: MUST_FIX
- Beskrivelse: `OppdaterKontoAsync` sjekker IKKE om kontoen er en systemkonto foer den endrer felter. Spec FR-6 sier at Kontotype, Normalbalanse, ErSystemkonto, og KontogruppeId ikke kan endres paa systemkontoer. Selv om PUT-requesten ikke inneholder disse feltene direkte, mangler det en eksplisitt sjekk som blokkerer eventuelle forsook. Dessuten kan `OppdaterKontoRequest` sette `ErAktiv` og `ErBokforbar` fritt, men spec sier ikke at disse er beskyttet -- likevel er det ingen validering overhodet for systemkontoer.
- Foreslatt fix: Legg til sjekk i `OppdaterKontoAsync`: Hvis konto.ErSystemkonto, valider at beskyttede felter ikke endres. For felter som ikke er i PUT-request (Kontotype, Normalbalanse, etc.), er dette delvis haandtert av at de ikke er i DTO-en -- men en eksplisitt sjekk og bruk av `SystemkontoFeltEndringException` er mer robust.

**Funn M-4: Import/Eksport ikke implementert (2 endepunkter)**
- Alvorlighet: MUST_FIX (SAF-T-eksport er lovpaakrevet)
- Beskrivelse: `POST /kontoer/importer` og `GET /kontoer/eksporter` mangler. SAF-T-eksport er lovpaakrevet ifoolge Bokforingsforskriften ss 7-8. Uten dette kan ikke systemet produsere paakrevd rapportering.
- Foreslatt fix: Implementer begge endepunktene som beskrevet i spec. Prioriter SAF-T-eksport (format=saft).

---

### 7. Testkvalitet

- [x] FR-1: Kontonummerformat -- 6 tester (for kort, for langt, starter med 0, starter med 9, bokstaver, gyldig)
- [x] FR-10: Normalbalanse-mapping -- Theory med alle 5 kontotyper
- [x] FR-11: Kontoklasse-kontotype -- 5 tester (klasse 1, 2 gjeld, 2 egenkapital, 8 inntekt, 8 kostnad)
- [x] FR-5: Systemkonto sletting -- 1 test
- [x] FR-9: Inaktiv konto avvises -- 1 test
- [x] FR-3: Gruppe-mismatch -- 1 test
- [x] FR-14: StandardAccountId mismatch -- 1 test
- [x] Avledede egenskaper (Kontoklasse, ErBalansekonto, ErUnderkonto) -- 5 tester
- [x] OpprettKonto happy path -- 1 test
- [x] Duplikat kontonummer -- 1 test
- [x] KontoFinnesOgErAktiv -- 3 tester
- [x] Deaktiver/Aktiver -- 2 tester
- [ ] MvaKodeService tester mangler helt
- [ ] FR-4: Underkonto-prefix test mangler
- [ ] FR-7/FR-8: Sletting med posteringer/underkontoer test mangler
- [ ] OppdaterKonto tester mangler
- [ ] API-lag/controller tester mangler
- [ ] Import/eksport tester mangler

- **Status: SHOULD_FIX**

**Funn S-6: Kun 1 testfil -- MvaKodeService er helt utestet**
- Fil: `D:\Code\Regnskap\tests\Regnskap.Tests\Features\Kontoplan\KontoServiceTests.cs`
- Alvorlighet: SHOULD_FIX
- Beskrivelse: Det finnes kun tester for `KontoService`. `MvaKodeService` har ingen tester. Viktige forretningsregler som FR-17 (MVA-kontokoblinger), opprettelse og oppdatering av MVA-koder, og feilhaandtering er utestet.
- Foreslatt fix: Opprett `MvaKodeServiceTests.cs` med tester for opprett, oppdater, hent, og kontokoblingsvalidering.

**Funn S-7: Manglende tester for FR-4, FR-7, FR-8, og OppdaterKonto**
- Fil: `D:\Code\Regnskap\tests\Regnskap.Tests\Features\Kontoplan\KontoServiceTests.cs`
- Alvorlighet: SHOULD_FIX
- Beskrivelse: Flere forretningsregler har ingen testdekning. Underkonto-prefix (FR-4), sletting med posteringer (FR-7), sletting med underkontoer (FR-8), og oppdatering av kontoer er ikke testet.
- Foreslatt fix: Legg til tester for disse scenariene. FR-7 kan testes naar `KontoHarPosteringerAsync` ikke lenger er en stub.

---

### 8. Sikkerhet

- [x] Ingen SQL injection -- EF Core parameteriserer alle spoorringer automatisk. Ingen raa SQL funnet.
- [ ] Autorisasjon paa alle endepunkter
- [x] Sensitive data -- Ingen sensitiv data i Kontoplan-modulen

- **Status: MUST_FIX**

**Funn M-5: Ingen autorisasjon paa noen endepunkter**
- Fil: `D:\Code\Regnskap\src\Regnskap.Api\Features\Kontoplan\KontoController.cs:8-9`, `KontogruppeController.cs:8-9`, `MvaKodeController.cs:8-9`
- Alvorlighet: MUST_FIX
- Beskrivelse: Ingen av de tre controllerne har `[Authorize]`-attributt. Alle endepunkter er tilgjengelige for anonyme brukere. Bokforingsloven ss 4 punkt 9 (Sikring) krever beskyttelse mot uautorisert tilgang og modifisering. Skriveoperasjoner (POST, PUT, DELETE) er spesielt kritiske.
- Foreslatt fix: Legg til `[Authorize]` paa controller-nivaa. Vurder roller: Les-tilgang for alle autentiserte, skrivetilgang for regnskapsforere, og admin-tilgang for sletting/systemkonto-endringer.

---

## Funnliste oppsummert

### MUST_FIX (5)

| # | Funn | Fil | Begrunnelse |
|---|---|---|---|
| M-1 | CreatedBy/ModifiedBy hardkodet "system" | RegnskapDbContext.cs:51 | Bokforingsloven ss 4.7: Sporbarhet |
| M-2 | FR-17: MVA-kontokoblinger ikke validert | MvaKodeService.cs:126-138 | Spec-avvik + MVA-feil ved bokforing |
| M-3 | FR-6: Systemkonto-beskyttelse mangler | KontoService.cs:127-148 | Spec-avvik, kan korrumpere kontoplanen |
| M-4 | Import/Eksport mangler (SAF-T) | Mangler filer | Bokforingsforskriften ss 7-8: SAF-T-eksport |
| M-5 | Ingen autorisasjon | Alle controllere | Bokforingsloven ss 4.9: Sikring |

### SHOULD_FIX (9)

| # | Funn | Fil | Begrunnelse |
|---|---|---|---|
| S-1 | Ingen periodebeskyttelse | KontoService.cs | God regnskapsskikk |
| S-2 | FR-16: MVA-konsistens-varsling mangler | KontoService.cs | Spec-avvik |
| S-3 | Seed-data mangler MVA-kontokoblinger | KontoplanSeedData.cs | Funksjonelt gap |
| S-4 | FR-13: GrupperingsKategori-varsling mangler | Konto.cs | SAF-T v1.30 compliance |
| S-5 | Import/Eksport endepunkter mangler | Mangler filer | Spec-avvik |
| S-6 | MvaKodeService er utestet | Mangler testfil | Kvalitetssikring |
| S-7 | Manglende tester for FR-4, FR-7, FR-8 | KontoServiceTests.cs | Kvalitetssikring |
| S-8 | Kontogruppe-antall inkluderer slettede | KontoplanRepository.cs:20-24 | HentAlleKontogrupperAsync inkluderer alle Kontoer (ogsaa slettede) i count |
| S-9 | Feilkoder i API folger ikke spec noyaktig | KontoController.cs:114 | Spec definerer spesifikke feilkoder (KONTO_NUMMER_UGYLDIG etc.), men controller bruker generisk "VALIDERING_FEIL" |

---

## Detalj: S-8 Kontogruppe-antall inkluderer slettede

- Fil: `D:\Code\Regnskap\src\Regnskap.Infrastructure\Features\Kontoplan\KontoplanRepository.cs:18-24`
- Beskrivelse: `HentAlleKontogrupperAsync` bruker `.Include(g => g.Kontoer)` uten filter paa `IsDeleted`. EF Core global query filter gjelder for hovedentiteten men ikke alltid for navigasjonsegenskaper ved Include. `HentKontogruppeAsync` filtrerer korrekt (`.Where(k => !k.IsDeleted)`), men `HentAlleKontogrupperAsync` gjor det ikke.
- Foreslatt fix: Legg til `.Where(k => !k.IsDeleted)` i Include for HentAlleKontogrupperAsync, eller verifiser at global filter ogsaa gjelder for filtered includes.

## Detalj: S-9 Feilkoder folger ikke spec

- Fil: `D:\Code\Regnskap\src\Regnskap.Api\Features\Kontoplan\KontoController.cs:113-115`
- Beskrivelse: POST /kontoer returnerer `{ kode: "VALIDERING_FEIL", melding: ... }` for alle ArgumentException. Spec definerer spesifikke koder som `KONTO_NUMMER_UGYLDIG`, `KONTO_NUMMER_OPPTATT`, `KONTO_GRUPPE_MISMATCH`, `KONTO_SAF_T_UGYLDIG`, `KONTO_OVERORDNET_UGYLDIG`, `KONTO_UNDERKONTO_PREFIX`. Klienter kan ikke programmatisk skille mellom feiltyper.
- Foreslatt fix: Bruk spesifikke exception-typer i stedet for generiske ArgumentException, eller bruk feilkoder i exception-meldingen som controlleren kan mappe til riktige feilkoder.

---

## Konklusjon

Kontoplan-modulen har en solid grunnstruktur med riktig NS 4102-hierarki, korrekte domenemodeller, og god bruk av DDD-prinsipper med feature-folder-organisering. Audit trail er paa plass via AuditableEntity, og soft delete haandteres korrekt i DbContext.

De 5 MUST_FIX-funnene maa utbedres foer modulen er produksjonsklar:
1. **Brukeridentitet i audit trail** -- lovpaakrevet
2. **MVA-kontokoblinger** -- nodvendig for korrekt MVA-bokforing
3. **Systemkonto-beskyttelse** -- nodvendig for integriteten i kontoplanen
4. **SAF-T-eksport** -- lovpaakrevet
5. **Autorisasjon** -- lovpaakrevet

Etter utbedring av MUST_FIX-funn boer modulen re-revideres.
