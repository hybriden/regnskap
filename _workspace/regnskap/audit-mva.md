# Revisjonsrapport: MVA-handtering

**Dato:** 2026-04-06
**Revisor:** Claude Opus 4.6 (revisjonsagent)
**Modul:** MVA-handtering (VAT Management)
**Revisjonsnummer:** 1
**Grunnlag:** spec-mva.md, norwegian-accounting-law-reference.md, implementert kode

---

## Sammendrag

- Antall MUST_FIX: **5**
- Antall SHOULD_FIX: **8**
- Antall OK: **10**
- Samlet status: **KREVER_ENDRING**

---

## Sjekkpunkter

### 1. Dobbelt bokholderi-integritet

- [x] MVA-oppgjorsberegning bruker aggregerte verdier fra allerede bokforte posteringer (debet=kredit garantert ved bilagsregistrering)
- [x] Snudd avregning resulterer i netto 0 (korrekt resultatnoytralt)
- [ ] Oppgjorsbilag (BokforOppgjor) er IKKE implementert — kan ikke verifisere at bilag balanserer
- Status: **SHOULD_FIX**
- Funn: BokforOppgjorAsync er ikke implementert (TODO-kommentar i IMvaOppgjorService.cs linje 7). Spec FR-MVA-05 beskriver konterings-mal for oppgjorsbilag som nulstiller MVA-kontoer. Uten denne funksjonaliteten forblir MVA-kontoene uoppgjort og balansen feilaktig mellom terminer.
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Mva\IMvaOppgjorService.cs:7`
- Foreslatt fix: Implementer BokforOppgjorAsync som oppretter bilag iht. FR-MVA-05 konterings-mal via IBilagService.

### 2. Revisjonsspor

- [x] Alle entiteter arver fra AuditableEntity (CreatedAt/CreatedBy, ModifiedAt/ModifiedBy, IsDeleted)
- [x] Soft delete pa MvaTermin, MvaOppgjor, MvaOppgjorLinje, MvaAvstemming, MvaAvstemmingLinje
- [x] Reberegning av oppgjor bruker soft delete pa gammelt oppgjor (MvaOppgjorService.cs linje 33)
- [ ] BeregnetAv, AvstemmingAv, AvsluttetAv er hardkodet til "system" — mangler brukeridentitet
- Status: **SHOULD_FIX**
- Funn: Tre steder brukes "system" som placeholder istedenfor faktisk brukeridentitet:
  - `MvaOppgjorService.cs:91` — `BeregnetAv = "system"`
  - `MvaAvstemmingService.cs:65` — `AvstemmingAv = "system"`
  - `MvaMeldingService.cs:72` — `AvsluttetAv = "system"`
- Foreslatt fix: Injiser IHttpContextAccessor eller ICurrentUserService for a hente faktisk brukeridentitet. Bokforingsloven §4 krever sporbarhet til person.

### 3. Regnskapsloven-compliance

- [x] Bilagsnummerering er fortlopende (handtert i Bilag-modulen, riktig avhengighet)
- [x] Tidsstempling pa oppgjor og avstemming (BeregnetTidspunkt, AvstemmingTidspunkt)
- [x] Sporbarhet fra MVA-sammenstilling til bilag til postering (MvaPosteringDetalj inneholder BilagId, PosteringId, Bilagsnummer)
- Status: **OK**

### 4. MVA-korrekthet

- [x] MVA-satser: 25% (alminnelig), 15% (naringsmiddel), 12% (lav sats) — korrekt iht. merverdiavgiftsloven §1-3 og law reference §4.1
- [x] 0% for fritatt/utforsel (kode 5, 6) — korrekt
- [x] Snudd avregning-koder 81/82, 86/87, 91/92 i par — korrekt iht. mval §4.4
- [x] MvaTilBetaling = SumUtgaende + SumSnuddUtg - SumInngaende - SumSnuddIng — korrekt formel
- [x] RF-0002 poststruktur med 12 poster — korrekt iht. spec FR-MVA-03
- Status: **OK**

### 5. RF-0002 Post-mapping

- [x] StandardTaxCode -> RfPostnummer mapping korrekt for alle 12 poster
- [x] Testet med Theory/InlineData for alle 12 koder (MvaOppgjorServiceTests.cs linje 272-288)
- [ ] Koder 86/87/91/92 mangler i RF-0002 post-mapping
- Status: **MUST_FIX**
- Funn F5-1: Spec definerer snudd avregning-koder 86, 87, 91, 92 i SAF-T-mapping-tabellen, men `TilordneRfPostnummer()` returnerer 0 for disse kodene. Kode 87 og 92 er utgaende-varianter av snudd avregning (klimakvoter/gull og varer fra utlandet) og bor tilordnes RF-0002 poster, ellers rapporteres ikke disse belopene i MVA-meldingen.
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Mva\MvaOppgjorService.cs:124-142`
- Foreslatt fix: Legg til mapping for kode 86->12, 87->6, 91->12, 92->6 (eller egne poster etter Skatteetatens gjeldende RF-0002-spesifikasjon). Alternativt: Avklar med Skatteetaten om disse skal rapporteres pa eksisterende poster eller nye poster.

### 6. Snudd avregning-handtering

- [x] IsSnuddAvregningUtgaende identifiserer 82, 87, 92 — korrekt
- [x] IsSnuddAvregningInngaende identifiserer 81, 86, 91 — korrekt
- [x] Netto-effekt er 0 for ren snudd avregning — verifisert i test
- Status: **OK**

### 7. SAF-T TaxTable-kompatibilitet

- [ ] SAF-T TaxTable-generering (FR-MVA-07) er IKKE implementert
- [ ] Endepunkt GET /api/saft/taxtable finnes ikke i koden
- Status: **MUST_FIX**
- Funn F7-1: Spec definerer et endepunkt `GET /api/saft/taxtable` og tjeneste `GenererSaftTaxTableAsync()`, samt komplett XML-struktur for TaxTable. DTO-ene (SaftTaxTableDto, SaftTaxCodeDetailDto) finnes i MvaDtos.cs, men det finnes ingen implementerende service eller controller. SAF-T TaxTable er obligatorisk for SAF-T-rapportering (Bokforingsforskriften §7-8).
- Fil: Mangler implementasjon
- Foreslatt fix: Implementer SaftTaxTable-generering i MvaMeldingService (eller egen service) som leser aktive MvaKoder og mapper til SaftTaxCodeDetailDto. Opprett controller-endepunkt.

### 8. Filing Deadlines (Innleveringsfrister)

- [x] Termin 1 (jan-feb): frist 10. april — korrekt
- [x] Termin 2 (mar-apr): frist 10. juni — korrekt
- [x] Termin 3 (mai-jun): frist 31. august (forlenget sommerfrist) — korrekt
- [x] Termin 4 (jul-aug): frist 10. oktober — korrekt
- [x] Termin 5 (sep-okt): frist 10. desember — korrekt
- [x] Termin 6 (nov-des): frist 10. februar neste ar — korrekt
- [x] Arstermin: frist 10. mars neste ar — korrekt
- [x] Skuddarslogikk for februar (28 vs 29) — korrekt
- [x] ErForfalt-beregning tar hensyn til Innsendt/Betalt-status — korrekt
- Status: **OK**
- Merknad: Alle frister samsvarer med Skatteforvaltningsloven §8-3 og spec FR-MVA-01.

### 9. Rounding Rules (Avrundingsregler)

- [x] RF-0002 belop rundes til hele kroner med MidpointRounding.ToEven (MvaMeldingService.cs linje 113-114) — korrekt iht. FR-MVA-09
- [x] Database-kolonner bruker HasPrecision(18, 2) for alle pengebelop — korrekt
- [ ] Avrundingsdifferanse vises IKKE eksplisitt pa meldingen
- Status: **SHOULD_FIX**
- Funn F9-1: Spec FR-MVA-09 sier "Avrundingsdifferanse akkumuleres og vises pa meldingen". MvaMeldingDto inneholder ikke et felt for avrundingsdifferanse. Selve avrundingen er korrekt (Math.Round med ToEven), men differansen kommuniseres ikke til brukeren.
- Foreslatt fix: Legg til felt `AvrundingsDifferanse` i MvaMeldingDto, beregnet som sum av (pre-avrundet belop - avrundet belop) for alle poster.

### 10. Spesifikasjonsoverensstemmelse

#### 10a. Endepunkter

| Spec-endepunkt | Implementert | Status |
|---|---|---|
| GET /api/mva/terminer?ar={ar} | Ja | OK |
| GET /api/mva/terminer/{id} | Ja | OK |
| POST /api/mva/terminer/generer | Ja | OK |
| POST /api/mva/terminer/{terminId}/oppgjor/beregn | Ja | OK |
| GET /api/mva/terminer/{terminId}/oppgjor | Ja | OK |
| POST /api/mva/terminer/{terminId}/oppgjor/bokfor | **NEI** | MUST_FIX |
| GET /api/mva/terminer/{terminId}/melding | Ja | OK |
| POST /api/mva/terminer/{terminId}/melding/marker-innsendt | Ja | OK |
| POST /api/mva/terminer/{terminId}/avstemming/kjor | Ja | OK |
| GET /api/mva/terminer/{terminId}/avstemming | Ja | OK |
| GET /api/mva/terminer/{terminId}/avstemming/historikk | Ja | OK |
| POST /api/mva/terminer/{terminId}/avstemming/{id}/godkjenn | Ja | OK |
| GET /api/mva/sammenstilling?ar={ar}&termin={termin} | **NEI** | MUST_FIX |
| GET /api/mva/sammenstilling/detalj?ar={ar}&termin={termin}&mvaKode={kode} | **NEI** | MUST_FIX |
| GET /api/saft/taxtable | **NEI** | MUST_FIX (se sjekkpunkt 7) |

- Status: **MUST_FIX**
- Funn F10-1: Fire endepunkter fra spec er ikke implementert:
  1. `POST /api/mva/terminer/{terminId}/oppgjor/bokfor` — MVA-oppgjorsbilag
  2. `GET /api/mva/sammenstilling` — MVA-konto sammenstilling (FR-MVA-06)
  3. `GET /api/mva/sammenstilling/detalj` — Detaljert sammenstilling per MVA-kode
  4. `GET /api/saft/taxtable` — SAF-T TaxTable
- Foreslatt fix: Implementer manglende endepunkter. Sammenstilling er spesielt viktig fordi den tilfredsstiller Bokforingsloven §5 nr. 5 (MVA-spesifikasjon).

#### 10b. Forretningsregler

| Regel | Implementert | Status |
|---|---|---|
| FR-MVA-01: Generering av terminer | Ja, komplett | OK |
| FR-MVA-02: Beregning av oppgjor | Ja, komplett | OK |
| FR-MVA-03: RF-0002 poststruktur | Ja, delvis (koder 86/87/91/92 mangler) | SHOULD_FIX |
| FR-MVA-04: Avstemming | Delvis, BeregnetFraPosteringer alltid 0 | MUST_FIX |
| FR-MVA-05: Bokforing oppgjorsbilag | Ikke implementert | MUST_FIX |
| FR-MVA-06: Sammenstilling | Ikke implementert | MUST_FIX |
| FR-MVA-07: SAF-T TaxTable | Ikke implementert | MUST_FIX |
| FR-MVA-08: Terminstatusoverganger | Delvis | SHOULD_FIX |
| FR-MVA-09: Avrunding | Implementert, mangler diff-visning | SHOULD_FIX |
| FR-MVA-10: Null-melding | Ja | OK |

#### 10c. Avstemming BeregnetFraPosteringer = 0

- Status: **MUST_FIX**
- Funn F10c-1: I `MvaAvstemmingService.KjorAvstemmingAsync()` (linje 45) er `beregnet` hardkodet til `0m` med TODO-kommentar "Avklar med arkitekt om presist matching mot kontonummer". Dette betyr at avstemming ALLTID viser fullt avvik for alle kontoer, noe som gjor hele avstemmingsfunksjonaliteten ubrukelig. Avstemming er pakrevd for innsending av MVA-melding.
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Mva\MvaAvstemmingService.cs:45`
- Foreslatt fix: Implementer matching mellom aggregerte MVA-posteringer og MVA-kontoer. Bruk MvaKode sin kontotilknytning (UtgaendeKontoId/InngaendeKontoId) for a knytte aggregeringer til riktige kontonumre.

### 11. Terminstatusoverganger

- [x] Apen -> Beregnet (MvaOppgjorService.cs linje 104)
- [x] Beregnet -> Apen (reberegning tillatt, soft delete av gammelt oppgjor)
- [x] Beregnet -> Avstemt (MvaAvstemmingService.cs linje 113)
- [x] Avstemt -> Innsendt (MvaMeldingService.cs linje 70)
- [ ] Beregnet -> Apen (automatisk ved nye posteringer i terminperioden) — IKKE implementert
- [ ] Innsendt -> Betalt — IKKE implementert
- [ ] Validering av ugyldige overganger er ufullstendig
- Status: **SHOULD_FIX**
- Funn F11-1: Spec FR-MVA-08 definerer at status automatisk skal ga tilbake til Apen nar nye posteringer bokfores i terminperioden (K-4). Denne automatikken er ikke implementert. Innsendt -> Betalt-overgangen mangler ogsa (lavere prioritet, kan vaere separat modul).
- Foreslatt fix: Legg til en hendelseslytter/hook i BilagRegistreringService som sjekker om en terminperiode er i status Beregnet/Avstemt og reapner den ved nye posteringer.

### 12. Testkvalitet

- [x] Unit tests for beregning med utgaende og inngaende MVA
- [x] Unit tests for alle tre MVA-satser (25%, 15%, 12%)
- [x] Unit tests for snudd avregning (resultatnoytralt)
- [x] Fullt eksempel fra spesifikasjonen testet (FR-MVA-02)
- [x] Null-melding testet (ingen posteringer)
- [x] Edge cases: termin ikke apen, termin ikke funnet, last oppgjor
- [x] Reberegning testet (soft delete av gammelt oppgjor)
- [x] Negativt resultat (tilgode) testet
- [x] RF-0002 postnummer mapping testet med Theory/InlineData
- [x] Snudd avregning identifikasjon testet
- [x] Termingenerering testet (6 terminer, arstermin, frister, skuddar, duplikater, ugyldig ar)
- [x] Avstemming basistester (kjor, hent, godkjenn, historikk)
- [ ] Ingen tester for MvaMeldingService (GenererMvaMeldingAsync, MarkerInnsendtAsync)
- [ ] Ingen tester for RF-0002 avrunding til hele kroner
- [ ] Ingen tester for null-melding via MvaMeldingService
- [ ] Ingen tester for MarkerInnsendt-valideringer (krever Avstemt status)
- Status: **SHOULD_FIX**
- Funn F12-1: MvaMeldingService har ingen dedikerte unit tests. Denne servicen inneholder viktig forretningslogikk:
  - RF-0002 postgenerering med summering
  - Avrunding til hele kroner
  - Null-melding-stotte
  - Statusvalidering ved innsending (krever Avstemt)
  - Lasting av oppgjor ved innsending
- Foreslatt fix: Opprett MvaMeldingServiceTests.cs med tester for:
  - Korrekt RF-0002 poststruktur
  - Avrunding til hele kroner
  - Null-melding (ingen posteringer)
  - MarkerInnsendt med korrekt status
  - MarkerInnsendt uten godkjent avstemming (skal kaste)
  - MarkerInnsendt uten oppgjor (skal kaste)

### 13. Sikkerhet

- [x] [Authorize]-attributt pa alle tre controllere (MvaTerminController, MvaOppgjorController, MvaAvstemmingController)
- [x] Ingen rad SQL — all datatilgang via EF Core LINQ
- [x] Input-validering pa ar-parameter (2000-2099)
- [x] Guid-baserte IDer (ikke sekvensielle, beskytter mot enumeration)
- [ ] Ingen rollebasert tilgangskontroll (Authorize uten roller/policy)
- Status: **SHOULD_FIX**
- Funn F13-1: Alle endepunkter krever autentisering, men det er ingen differensiering mellom roller. MVA-innsending og oppgjorsbokforing bor kreve en spesifikk rolle (f.eks. "Regnskapsforer" eller "Admin"). En vanlig bruker bor ikke kunne sende inn MVA-melding.
- Foreslatt fix: Definer og implementer [Authorize(Policy = "MvaInnlevering")] eller tilsvarende for sensitive operasjoner (beregn, bokfor, marker-innsendt, godkjenn).

### 14. EF Core-konfigurasjon

- [x] Alle entiteter har primarnokkel, indekser, og korrekt precision
- [x] Unik indeks pa (Ar, Termin) med IsDeleted-filter
- [x] Unik indeks pa MvaTerminId for oppgjor (1:1)
- [x] Unik indeks pa (MvaOppgjorId, MvaKode) for linjer
- [x] Unik indeks pa (MvaAvstemmingId, Kontonummer) for avstemmingslinjer
- [ ] Mangler query filter (HasQueryFilter) pa alle entiteter for soft delete
- Status: **MUST_FIX**
- Funn F14-1: Spec definerer `HasQueryFilter(e => !e.IsDeleted)` pa alle MVA-entiteter (MvaTermin, MvaOppgjor, MvaOppgjorLinje, MvaAvstemming, MvaAvstemmingLinje). Implementasjonen i MvaTerminConfiguration.cs mangler disse query-filtrene. Uten global query filter vil slettet data returneres i alle queries, noe som bryter med soft delete-monsteret.
- Fil: `D:\Code\Regnskap\src\Regnskap.Infrastructure\Features\Mva\MvaTerminConfiguration.cs`
- Foreslatt fix: Legg til `builder.HasQueryFilter(e => !e.IsDeleted)` i Configure-metoden for alle fem entitets-konfigurasjoner.

### 15. MvaMeldingService SumInngaende-beregning

- Status: **SHOULD_FIX**
- Funn F15-1: I MvaMeldingService.GenererMvaMeldingAsync() (linje 28-29) beregnes `sumInngaende` fra poster 7-12, men spec sier "Sum inngaende MVA (fradrag) = Post 7-12 sum MVA-belop" mens MvaMeldingDto-spec i linje 691 sier "Post 7-10 summert". Det er en intern inkonsistens i spec, men koden summerer 7-12 som er korrekt per RF-0002 meldingsformatet.
- Merknad: Ingen kodendring nodvendig, men spec-dokumentet bor oppdateres (MvaMeldingDto-kommentaren "Post 7-10" bor endres til "Post 7-12").

### 16. Filstruktur

- [x] Spec angir Exceptions.cs og MvaAggregeringDto.cs som separate filer, men i implementasjonen er DTO-er samlet i IMvaRepository.cs og exceptions i MvaExceptions.cs. Avviket er akseptabelt og vel begrunnet — samlede filer reduserer filantall.
- [x] Spec foreslaar en samlet IMvaService, men implementasjonen splitter i fire interfaces (IMvaTerminService, IMvaOppgjorService, IMvaAvstemmingService, IMvaMeldingService). Dette er bedre separasjon av ansvar.
- [x] DI-registrering i MvaServiceExtensions.cs er komplett for alle implementerte services.
- Status: **OK**

---

## Detaljert MUST_FIX-liste (prioritert)

| Nr | Funn | Fil | Alvorlighet | Hjemmel |
|---|---|---|---|---|
| 1 | Avstemming BeregnetFraPosteringer hardkodet til 0 | MvaAvstemmingService.cs:45 | Kritisk — gjor avstemming ubrukelig | Bokforingsloven §4 (noyaktighet) |
| 2 | HasQueryFilter mangler pa alle 5 entiteter | MvaTerminConfiguration.cs | Kritisk — slettet data lekker i queries | Spec EF Core-konfigurasjon |
| 3 | SAF-T TaxTable ikke implementert | Mangler | Hoy — lovpakrevd for SAF-T | Bokforingsforskriften §7-8 |
| 4 | Sammenstilling (MVA-spesifikasjon) ikke implementert | Mangler | Hoy — lovpakrevd rapport | Bokforingsloven §5 nr. 5 |
| 5 | Oppgjorsbilag (BokforOppgjor) ikke implementert | Mangler | Hoy — MVA-kontoer forblir uoppgjort | FR-MVA-05, god regnskapsskikk |

## Detaljert SHOULD_FIX-liste

| Nr | Funn | Fil | Alvorlighet |
|---|---|---|---|
| 1 | Brukeridentitet hardkodet til "system" (3 steder) | MvaOppgjorService.cs:91, MvaAvstemmingService.cs:65, MvaMeldingService.cs:72 | Middels — mangler kontrollspor |
| 2 | Koder 86/87/91/92 mangler i RF-0002 post-mapping | MvaOppgjorService.cs:124-142 | Middels — disse kodene rapporteres ikke |
| 3 | Avrundingsdifferanse ikke vist i MvaMeldingDto | MvaMeldingService.cs | Lav |
| 4 | Automatisk reaapning ved nye posteringer (K-4) mangler | Mangler hendelseslytter | Middels |
| 5 | Innsendt -> Betalt overgang mangler | Mangler | Lav |
| 6 | MvaMeldingService har ingen unit tests | Mangler | Middels — viktig forretningslogikk utestet |
| 7 | Rollebasert tilgangskontroll mangler | Alle controllere | Middels |
| 8 | BokforOppgjor endepunkt mangler i controller | MvaOppgjorController.cs | (del av MUST_FIX 5) |

---

## Konklusjon

MVA-modulens kjernefunksjonalitet for beregning, termingenerering, og oppgjor er solid implementert med korrekte MVA-satser, RF-0002 poststruktur, og snudd avregning-handtering. Testdekningen for oppgjorsberegning er god med relevante edge cases. Frister er korrekte per norsk lovgivning.

Hovedutfordringene er:
1. **Avstemmingen er ubrukelig** (BeregnetFraPosteringer = 0) — dette blokkerer hele MVA-oppgjorsflyten i praksis.
2. **Manglende EF Core query filters** gjor at soft delete ikke fungerer korrekt.
3. **Tre lovpakrevde funksjoner mangler** (SAF-T TaxTable, MVA-sammenstilling, oppgjorsbilag).

Modulen krever endring for 5 MUST_FIX-funn for godkjennes for produksjon.
