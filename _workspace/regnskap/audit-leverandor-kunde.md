# Revisjonsrapport: Leverandorreskontro og Kundereskontro

**Dato:** 2026-04-06
**Revisor:** Revisjonsagent
**Omfang:** Kombinert revisjon av leverandorreskontro (AP) og kundereskontro (AR) moduler
**Versjon:** 1.0

---

## Sammendrag

| Kategori | Antall |
|----------|--------|
| MUST_FIX | 8 |
| SHOULD_FIX | 11 |
| OK | 12 |
| **Samlet status** | **KREVER_ENDRING** |

---

## Sjekkpunkter

### 1. Dobbelt bokholderi-integritet

- [x] Leverandorfaktura: Debet kostnad + Debet MVA = Kredit 2400 (verifisert i `ByggBilagPosteringer`)
- [x] Kreditnota speilvendt (Leverandor): Debet 2400, Kredit kostnad, Kredit MVA
- [ ] Kundefaktura: Bilag opprettes IKKE ved registrering
- [ ] Kundeinnbetaling: Bilag opprettes IKKE
- [ ] Purregebyr: Bilag opprettes IKKE
- [ ] Tap pa fordringer: Bilag opprettes IKKE
- [x] Leverandorbetaling: Bilag-kontering dokumentert i modell (Debet 2400, Kredit 1920)

**Status: MUST_FIX**

**Funn 1.1 (MUST_FIX) -- Kundefaktura oppretter ikke bilag**
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Kundereskontro\KundeFakturaService.cs:140`
- Beskrivelse: `RegistrerFakturaAsync` lagrer fakturaen men oppretter aldri et bilag via `IBilagRegistreringService`. Spesifikasjonen FR-K01 krever at bilaget med posteringer (Debet 1500, Kredit inntektskonto, Kredit utg. MVA) opprettes automatisk. Leverandormodulen gjor dette korrekt. Dette er et brudd pa dobbelt bokholderi-prinsippet og Bokforingsloven 4 (fullstendighet).
- Foreslatt fix: Injiser `IBilagRegistreringService` i `KundeFakturaService` og opprett bilag identisk med leverandormodulens monster (se `LeverandorFakturaService.RegistrerFakturaAsync`), men med speilvendte kontoer (Debet 1500, Kredit inntekt, Kredit 2700).

**Funn 1.2 (MUST_FIX) -- Kundeinnbetaling oppretter ikke bilag**
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Kundereskontro\KundeFakturaService.cs:352`
- Beskrivelse: `RegistrerInnbetalingInternal` har en TODO-kommentar: "Opprett bilag: Debet 1920 Bank, Kredit 1500 Kundefordringer". Uten dette bilaget oppstar det avvik mellom kundereskontro og hovedbok. Brudd pa Bokforingsloven 4 (sporbarhet, fullstendighet).
- Foreslatt fix: Opprett bilag via `IBilagRegistreringService` med Debet 1920, Kredit 1500. Sett `BilagId` pa `KundeInnbetaling`.

**Funn 1.3 (MUST_FIX) -- Purregebyr oppretter ikke bilag**
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Kundereskontro\PurringService.cs:121`
- Beskrivelse: TODO-kommentar: "Opprett bilag: Debet 1500, Kredit 3400". Purregebyr legges til `GjenstaendeBelop` men bokfores ikke. Hovedboken mangler posteringen. Brudd pa dobbelt bokholderi-invarianten.
- Foreslatt fix: Injiser `IBilagRegistreringService` og opprett bilag for purregebyr. Sett `GebyrBilagId` pa `Purring`-entiteten.

**Funn 1.4 (MUST_FIX) -- Tap pa fordringer oppretter ikke bilag**
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Kundereskontro\KundeFakturaService.cs:198`
- Beskrivelse: TODO-kommentar: "Opprett bilag via IBilagRegistreringService". FR-K14 krever Debet 7830, Kredit 1500, pluss tilbakeforing av utg. MVA (Debet 2700, Kredit 7830). Uten dette er tap-avskrivninger ubokforte.
- Foreslatt fix: Implementer bilagsopprettelse for tap med korrekte posteringer inkl. MVA-tilbakeforing.

---

### 2. Revisjonsspor

- [x] `AuditableEntity` base class brukes pa alle entiteter (CreatedAt/CreatedBy, ModifiedAt/ModifiedBy)
- [x] Soft delete (IsDeleted) implementert i `LeverandorService.SlettAsync` og `KundeService.SlettAsync`
- [x] Ingen hard delete funnet i koden
- [x] Unique-indekser med `HasFilter("IsDeleted = false")` for leverandorer og kunder
- [x] Betalingsforslag har `GodkjentAv` og `GodkjentTidspunkt`

**Status: OK**

---

### 3. Regnskapsloven-compliance

- [x] Bilagsnummerering er fortlopende (leverandor: `NesteInternNummerAsync`, kunde: `NesteNummer`)
- [x] Bokforing uten ugrunnet opphold: Mottaksdato registreres automatisk (`DateOnly.FromDateTime(DateTime.UtcNow)`)
- [x] Sporbarhet fra faktura til bilag via `BilagId` (leverandor)
- [ ] Sporbarhet fra faktura til bilag mangler (kunde)
- [x] Leverandor: Bilagserie "IF" for inngaende faktura

**Funn 3.1 (MUST_FIX) -- Kundefaktura mangler bilagsserie "UF"**
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Kundereskontro\KundeFakturaService.cs`
- Beskrivelse: FR-K01 punkt 3 krever at bilag opprettes i serie "UF" (Utgaende Faktura) med BilagType.UtgaendeFaktura. Siden bilag ikke opprettes i det hele tatt (se funn 1.1), mangler dette fullstendig.
- Foreslatt fix: Inkluderes i fix for funn 1.1. Bruk serie "UF" og `BilagType.UtgaendeFaktura`.

**Funn 3.2 (SHOULD_FIX) -- Kundefakturanummer-sekvens er ikke gapfri**
- Fil: `D:\Code\Regnskap\src\Regnskap.Infrastructure\Features\Kundereskontro\KundeReskontroRepository.cs:76`
- Beskrivelse: `NesteNummer()` bruker `Max(Fakturanummer) + 1` uten `IgnoreQueryFilters()`. Hvis en faktura soft-deletes, kan neste nummer skape hull i sekvensen. Leverandor-repository bruker korrekt `IgnoreQueryFilters()` for tilsvarende. Bokforingsforskriften 5-1-1/5-2-1 krever kontrollerbar, sammenhengende sekvens.
- Foreslatt fix: Legg til `.IgnoreQueryFilters()` i `NesteNummer()`, identisk med leverandor-repositoryet.

**Status: MUST_FIX**

---

### 4. MVA-korrekthet

- [x] MVA beregnes per linje i bade leverandor- og kundefakturaservice
- [x] MVA-grunnlag (BelopEksMva) og MVA-belop (MvaBelop) er separate felt
- [x] Avrunding til 2 desimaler (`Math.Round(..., 2)`)
- [x] Snapshot av MVA-sats lagres pa hver linje (`MvaSats`)

**Funn 4.1 (SHOULD_FIX) -- Leverandor bruker inngaende MVA-koder, kunde bruker utgaende**
- Fil: `LeverandorFakturaService.cs:350-358` og `KundeFakturaService.cs:402-414`
- Beskrivelse: Leverandor-modulen bruker koder "1", "11", "13", "14" (inngaende). Kundemod bruker "3", "31", "33" (utgaende). Dette er korrekt isolert sett, men bade metoder er hardkodet. Spesifikasjonen refererer til SAF-T TaxCode-mapping og MvaKode-entitet. En hardkodet lookup risikerer feil ved satsendringer.
- Foreslatt fix: Refaktorer til a hente MVA-satser fra `MvaKode`-entiteten via repository, som bade spec og TODO-kommentarer allerede antyder.

**Funn 4.2 (SHOULD_FIX) -- Leverandor bokforer inngaende MVA pa 2710, spesifikasjon anbefaler 1600-serien**
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Leverandorreskontro\LeverandorFakturaService.cs:392`
- Beskrivelse: FR-L01 sier "Debet pa inngaende MVA-konto (1600-serien)". Implementasjonen bruker hardkodet "2710". NS 4102 har bade 2710 (Kalkulatorisk inng. MVA / fradragsberettiget MVA) og 1600-serien. Konto 2710 er teknisk akseptabelt, men det er avvik fra spec-teksten. Avklaring med arkitekt anbefales.
- Status: REVIEW_NEEDED

**Status: SHOULD_FIX**

---

### 5. SAF-T-kompatibilitet

- [x] Leverandor: `SaftSupplierId` mapping til `Leverandornummer`
- [x] Kunde: `SaftCustomerId` mapping til `Kundenummer`
- [x] Adresse-felter mapper korrekt (StreetName, PostalCode, City, Country)
- [x] Organisasjonsnummer for TaxRegistration
- [x] LeverandorId/KundeId pa posteringer (leverandor-bilag)
- [ ] KundeId settes ikke pa posteringer (kundebilag eksisterer ikke)
- [x] BankAccount-felter pa leverandor (Bankkontonummer, IBAN, BIC)

**Funn 5.1 (MUST_FIX) -- SAF-T SupplierID/CustomerID pa posteringer mangler for kundebilag**
- Beskrivelse: Siden kundefaktura ikke oppretter bilag (funn 1.1), mangler `CustomerID` pa posteringer i GeneralLedgerEntries. SAF-T v1.30 krever SupplierID/CustomerID pa relevante posteringslinjer. Leverandormodulen gjor dette korrekt. Fikses som del av funn 1.1.

**Funn 5.2 (SHOULD_FIX) -- SAF-T opening/closing balance for leverandorer og kunder**
- Beskrivelse: SAF-T v1.30 krever opening/closing balance for Suppliers og Customers i MasterFiles. Datamodellen har ingen dedikerte felter for dette, og ingen eksportlogikk er implementert. Dette gar utenfor modulens scope (SAF-T-eksport er sannsynligvis en egen modul), men modellen bor forberede dette.
- Foreslatt fix: Sikre at apne poster-rapporten kan brukes til a beregne SAF-T saldoer, evt. legg til dedikert SAF-T-eksportlogikk.

**Status: MUST_FIX** (pga manglende CustomerID)

---

### 6. Spesifikasjonsoverensstemmelse

#### Leverandorreskontro

| Regel | Implementert | Status |
|-------|-------------|--------|
| FR-L01: Registrering med bilag | Ja | OK |
| FR-L02: Forfallsdato-beregning | Ja, inkl. helg-logikk | OK |
| FR-L03: Kreditnota med speilvendte posteringer | Ja | OK |
| FR-L04: Betalingsforslag | Ja | OK |
| FR-L05: pain.001-generering | Ja | SHOULD_FIX (se under) |
| FR-L06: Betalingsregistrering med bilag | Modell ok, men ingen service | SHOULD_FIX |
| FR-L07: Apne poster | Ja | OK |
| FR-L08: Aldersfordeling | Ja | OK |
| FR-L09: Leverandorutskrift | Ja | OK |
| FR-L10: Duplikatkontroll | Ja (blokkering) | OK |
| FR-L11: Leverandornummer | Manuell tildeling | OK |

#### Kundereskontro

| Regel | Implementert | Status |
|-------|-------------|--------|
| FR-K01: Registrering med bilag | NEI (bilag mangler) | MUST_FIX |
| FR-K02: Forfallsdato inkl. helg | Ja | OK |
| FR-K03: Kreditnota | Delvis (mangler bilag) | MUST_FIX |
| FR-K04: KID-generering | Ja (MOD10/MOD11) | OK |
| FR-K05: Innbetalingsregistrering | Delvis (mangler bilag) | MUST_FIX |
| FR-K06: CAMT.053 KID-matching | Delvis (DTO definert, ingen parser) | SHOULD_FIX |
| FR-K07: Purring | Ja (mangler bilag for gebyr) | MUST_FIX |
| FR-K08: Forsinkelsesrente | Nei (beregnes ikke) | SHOULD_FIX |
| FR-K09: Apne poster | Ja | OK |
| FR-K10: Aldersfordeling | Ja | OK |
| FR-K11: Kundeutskrift | Ja | OK |
| FR-K12: Kundenummer | Manuell tildeling | OK |
| FR-K13: Kredittgrensekontroll | Ja (blokkering, spec sier advarsel) | SHOULD_FIX |
| FR-K14: Tap pa fordringer | Delvis (mangler bilag og MVA-tilbakeforing) | MUST_FIX |

**Funn 6.1 (SHOULD_FIX) -- pain.001 mangler Dbtr-element**
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Leverandorreskontro\BetalingsforslagService.cs:256-262`
- Beskrivelse: pain.001 XML har `DbtrAcct` men mangler `Dbtr` (debitor name/identification) som er obligatorisk i ISO 20022 pain.001.001.03. Banken vil sannsynligvis avvise filen.
- Foreslatt fix: Legg til `<Dbtr><Nm>Bedriftsnavn</Nm><Id><OrgId><Othr><Id>ORG_NR</Id></Othr></OrgId></Id></Dbtr>` mellom `ReqdExctnDt` og `DbtrAcct`.

**Funn 6.2 (SHOULD_FIX) -- pain.001 DbtrAcct bruker Othr direkte istedenfor Id/Othr/Id**
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Leverandorreskontro\BetalingsforslagService.cs:259-261`
- Beskrivelse: `<Othr>` brukes direkte med `FraKontonummer` som innhold. Korrekt pain.001 struktur er `<Id><Othr><Id>KONTONR</Id></Othr></Id>`. Det innlagte kontonummeret havner som tekst-innhold i `<Othr>` istedenfor i et `<Id>`-element.
- Foreslatt fix: Endre til `writer.WriteStartElement("Othr"); writer.WriteElementString("Id", forslag.FraKontonummer); writer.WriteEndElement();`

**Funn 6.3 (SHOULD_FIX) -- FR-L06 betalingsregistrering har ingen service-metode**
- Beskrivelse: Det finnes ikke en service-metode for a registrere en gjennomfort betaling (oppdatere `GjenstaendeBelop`, opprette `LeverandorBetaling`, opprette bilag). Repository har `LeggTilBetalingAsync`, men ingen orkestreringslogikk i service-laget.
- Foreslatt fix: Implementer `RegistrerBetalingAsync` i `ILeverandorFakturaService` eller `IBetalingsforslagService`.

**Funn 6.4 (SHOULD_FIX) -- FR-K08 Forsinkelsesrente er ikke implementert**
- Beskrivelse: PurringService beregner ikke forsinkelsesrente. Feltet `Forsinkelsesrente` pa `Purring` settes til `Belop.Null` og beregnes aldri. Spesifikasjonen krever beregning ihht forsinkelsesrenteloven (styringsrente + 8pp).
- Foreslatt fix: Implementer renteberegning i `PurringService.OpprettPurringerAsync`. Bruk konfigurerbar rente.

**Funn 6.5 (SHOULD_FIX) -- FR-K13 Kredittgrensekontroll blokkerer, spec sier advarsel**
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Kundereskontro\KundeFakturaService.cs:87-88`
- Beskrivelse: Implementasjonen kaster `KredittgrenseOverskredetException` som blokkerer. FR-K13 sier "advarsel (ikke blokkering, konfigurerbart)".
- Foreslatt fix: Returner advarsel i DTO istedenfor a kaste exception, eller gjor det konfigurerbart.

**Funn 6.6 (SHOULD_FIX) -- CAMT.053-import er ikke implementert**
- Beskrivelse: FR-K06 krever CAMT.053-parsing og auto-matching. DTOer (`Camt053ImportResultDto`, `Camt053TransaksjonDto`) er definert, men ingen parser eller service er implementert.
- Foreslatt fix: Implementer `ICamt053ImportService` med XML-parser for CAMT.053 og matching-logikk.

**Status: MUST_FIX** (pga kundebilag-mangler)

---

### 7. Testkvalitet

#### Leverandorreskontro

| Testomrade | Tester | Status |
|-----------|--------|--------|
| LeverandorService CRUD | 7 tester | OK |
| Duplikatkontroll nummer/orgnr | 2 tester | OK |
| Egendefinert betalingsbetingelse | 1 test | OK |
| Fakturaregistrering med bilag | 3 tester | OK |
| Forfallsdato + helg | 3 tester | OK |
| Duplikat faktura | 1 test | OK |
| Sperring/godkjenning statusoverganger | 4 tester | OK |
| Aldersfordeling | 1 test | OK |
| Betalingsforslag generering | 3 tester | OK |
| Betalingsforslag godkjenning | 1 test | OK |
| pain.001-generering | 2 tester | OK |
| EkskluderLinje | 1 test | OK |
| Kansellering | 2 tester | OK |

#### Kundereskontro

| Testomrade | Tester | Status |
|-----------|--------|--------|
| KundeService CRUD | 4 tester | OK |
| Duplikatkontroll | 1 test | OK |
| Org.nr-validering | 1 test | OK |
| Egendefinert betingelse | 1 test | OK |
| Saldo-beregning | 1 test | OK |
| Fakturaregistrering | 4 tester | OK |
| Innbetaling (full/delvis) | 2 tester | OK |
| KID-matching | 1 test | OK |
| Kredittgrensekontroll | 1 test | OK |
| Tap pa fordringer | 2 tester | OK |
| Forfallsdato + helg | 4 tester | OK |
| Aldersfordeling | 1 test | OK |
| KID MOD10 | 7 tester | OK |
| KID MOD11 | 5 tester | OK |

**Funn 7.1 (SHOULD_FIX) -- Manglende tester for purring-funksjonalitet**
- Beskrivelse: `PurringService` har ingen enhetstester. Det finnes ingen `PurringServiceTests.cs`. Forretningsreglene for 14-dagers minimum mellom purringer, gebyrfri forste purring, og gebyrbetaling pa andre/tredje purring er utestet.
- Foreslatt fix: Opprett `PurringServiceTests.cs` med tester for alle FR-K07-regler.

**Funn 7.2 (SHOULD_FIX) -- Manglende test for MarkerSendt**
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Kundereskontro\PurringService.cs:157`
- Beskrivelse: `MarkerSendtAsync` kaster `NotImplementedException`. Metoden er deklarert i interface men ikke implementert.
- Foreslatt fix: Implementer metoden og legg til test.

**Status: SHOULD_FIX**

---

### 8. Sikkerhet

- [x] `[Authorize]` pa alle kontrollere (bade leverandor og kunde)
- [x] Guid-baserte IDer (ikke sekvensielle, vanskelig a gjette)
- [x] Ingen ra SQL (kun EF Core LINQ)
- [x] Ingen sensitiv data i feilmeldinger

**Status: OK**

---

### 9. KID-nummer korrekthet

- [x] MOD10 (Luhn) implementert korrekt i `KidGenerator.BeregnMod10`
- [x] MOD11 implementert med korrekt vektrekke (2,3,4,5,6,7)
- [x] MOD11 returnerer -1 for ugyldig rest (10), kaster exception
- [x] KID-lengde valideres (2-25 siffer)
- [x] KID-format: 13 siffer (6+6+1 kontrollsiffer)
- [x] Padding av kundenummer og fakturanummer
- [x] Validering: sjekker alle-siffer, lengde, kontrollsiffer
- [x] Testet grundig (12 tester)

**Status: OK**

---

### 10. pain.001 format

- [x] Korrekt namespace: `urn:iso:std:iso:20022:tech:xsd:pain.001.001.03`
- [x] GrpHdr med MsgId, CreDtTm, NbOfTxs, CtrlSum, InitgPty
- [x] PmtInf med PmtMtd="TRF"
- [x] CdtTrfTxInf per betaling med EndToEndId
- [x] KID i RmtInf/Strd/CdtrRefInf/Ref
- [x] Fritekst i RmtInf/Ustrd nar KID mangler
- [x] IBAN/BBAN-stotte i CdtrAcct
- [ ] Dbtr-element mangler (funn 6.1)
- [ ] DbtrAcct/Id/Othr/Id-struktur feil (funn 6.2)
- [ ] Cdtr-element (kreditor navn) mangler
- [ ] CdtrAgt (BIC for utenlandske) ikke alltid inkludert

**Funn 10.1 (SHOULD_FIX) -- Cdtr-element mangler i pain.001**
- Fil: `BetalingsforslagService.cs:265-313`
- Beskrivelse: Hver `CdtTrfTxInf` mangler `<Cdtr><Nm>LeverandorNavn</Nm></Cdtr>`. Dette er et obligatorisk element i pain.001.
- Foreslatt fix: Legg til Cdtr med leverandorens navn mellom Amt og CdtrAcct.

**Status: SHOULD_FIX** (filen genereres men er ikke fullstendig validerbar)

---

### 11. Bokforingsloven reskontrokrav

- [x] Leverandorspesifikasjon (3-1): Leverandorkode, navn, org.nr, alle poster med dato og referanse
- [x] Kundespesifikasjon (3-1): Kundekode, navn, alle poster med dato og referanse
- [x] Apne poster = leverandorgjeld 2400 / kundefordringer 1500 (konseptuelt)
- [x] Aldersfordeling implementert for begge moduler
- [x] Utskrift (periode-rapport) implementert for begge moduler

**Funn 11.1 (SHOULD_FIX) -- Leverandorutskrift inkluderer ikke betalinger som separate linjer**
- Fil: `D:\Code\Regnskap\src\Regnskap.Application\Features\Leverandorreskontro\LeverandorFakturaService.cs:269-309`
- Beskrivelse: Leverandorutskriften itererer kun over fakturaer. Betalinger (LeverandorBetaling) som er registrert som separate transaksjoner vises ikke. Kontoutskriften bor vise: faktura (kredit), betaling (debet) med bankreferanse, for fullstendig kontoutskrift ihht bokforingsforskriften 3-1.
- Foreslatt fix: Inkluder betalinger fra `f.Betalinger` i utskriften, tilsvarende slik kundeutskriften inkluderer innbetalinger.

**Status: SHOULD_FIX**

---

### 12. Purregebyrer per inkassoloven

- [x] Forste purring gebyrfri (korrekt: `gebyr = 0` for Purring1)
- [x] Minimum 14 dager mellom purringer (kontrollert)
- [x] Gebyr = 1/10 av inkassosatsen, satt til NOK 70 (rimelig for 2026)
- [ ] Forsinkelsesrente ikke implementert (FR-K08)
- [x] Sperrede kunder/fakturaer ekskluderes fra purring
- [x] Minimum 3 purringer for tap-avskrivning

**Status: SHOULD_FIX** (forsinkelsesrente mangler)

---

## Kritiske funn oppsummert

### MUST_FIX (8 stk)

| # | Funn | Modul | Alvorlighet |
|---|------|-------|-------------|
| 1.1 | Kundefaktura oppretter ikke bilag | Kunde | Dobbelt bokholderi brudd |
| 1.2 | Kundeinnbetaling oppretter ikke bilag | Kunde | Dobbelt bokholderi brudd |
| 1.3 | Purregebyr oppretter ikke bilag | Kunde | Dobbelt bokholderi brudd |
| 1.4 | Tap pa fordringer oppretter ikke bilag + mangler MVA-tilbakeforing | Kunde | Dobbelt bokholderi brudd |
| 3.1 | Kundefaktura mangler bilagserie "UF" | Kunde | Regnskapsloven compliance |
| 3.2 | Fakturanummer-sekvens kan fa hull (mangler IgnoreQueryFilters) | Kunde | Bokforingsforskriften 5-1-1 |
| 5.1 | SAF-T CustomerID mangler pa posteringer | Kunde | SAF-T compliance |
| 6.5* | (Reklassifisert fra 6-serien) Se 1.1-1.4 | Kunde | - |

*Funn 5.1 og 3.1 loses automatisk nar funn 1.1 fikses.

### SHOULD_FIX (11 stk)

| # | Funn | Modul |
|---|------|-------|
| 4.1 | Hardkodede MVA-satser | Begge |
| 4.2 | Inngaende MVA konto 2710 vs spec 1600-serien | Leverandor |
| 6.1 | pain.001 mangler Dbtr-element | Leverandor |
| 6.2 | pain.001 DbtrAcct feil struktur | Leverandor |
| 6.3 | Betalingsregistrering mangler service-metode | Leverandor |
| 6.4 | Forsinkelsesrente ikke implementert | Kunde |
| 6.5 | Kredittgrensekontroll blokkerer vs advarsel | Kunde |
| 6.6 | CAMT.053-import ikke implementert | Kunde |
| 7.1 | Ingen tester for PurringService | Kunde |
| 10.1 | pain.001 mangler Cdtr-element | Leverandor |
| 11.1 | Leverandorutskrift mangler betalingslinjer | Leverandor |

---

## Konklusjon

**Leverandorreskontro** er solid implementert med korrekt bilagsoppretting, dobbelt bokholderi, duplikatkontroll, betalingsforslag, og pain.001-generering. De gjenvarende funnene er hovedsakelig forbedringer (pain.001 struktur, betalingsregistrering).

**Kundereskontro** har en fundamental mangel: **ingen bilag opprettes for noen transaksjonstype** (faktura, innbetaling, purregebyr, tap). Dette betyr at kundereskontroens saldo ikke avspeiles i hovedboken, noe som er et direkte brudd pa dobbelt bokholderi-prinsippet og Bokforingsloven. Alle fire bilagsmangler (funn 1.1-1.4) ma fikses for at modulen er compliance-klar.

KID-generering, purrelogikk, aldersfordeling, og utskrifter er godt implementert. Testkvaliteten er god for bade leverandor og kunde, men purring-tester mangler helt.

**Samlet vurdering: KREVER_ENDRING**

Prioritert fikseliste:
1. Injiser `IBilagRegistreringService` i `KundeFakturaService` og opprett bilag for faktura/kreditnota (funn 1.1, 3.1, 5.1)
2. Opprett bilag for innbetalinger (funn 1.2)
3. Opprett bilag for purregebyr (funn 1.3)
4. Opprett bilag for tap inkl. MVA-tilbakeforing (funn 1.4)
5. Legg til `IgnoreQueryFilters()` i `KundeReskontroRepository.NesteNummer()` (funn 3.2)
6. Fiks pain.001 struktur (funn 6.1, 6.2, 10.1)
7. Implementer PurringService-tester (funn 7.1)
