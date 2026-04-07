# Revisjonsrapport: Fakturering + Bankavstemming

**Dato:** 2026-04-06
**Revisor:** Revisjonsagent (Claude Opus 4.6)
**Moduler:** Fakturering (Modul 7), Bankavstemming (Modul 8)
**Kilder:** spec-faktura.md, spec-bank.md, norwegian-accounting-law-reference.md, implementert kode

---

## Sammendrag

- Antall MUST_FIX: 6
- Antall SHOULD_FIX: 9
- Antall OK: 11
- Samlet status: **KREVER_ENDRING**

---

## FAKTURERING (Modul 7)

### 1. Dobbelt bokholderi-integritet

- [ ] Debet = kredit valideres i domenet
- [ ] Balansesjekk ved bilagsopprettelse
- [ ] Balansesjekk ved bilagsendring
- **Status: MUST_FIX**
- **Funn:** `FaktureringService.UtstedeFakturaAsync` (linje 178-179) har en TODO-kommentar: "Integrer med IBilagRegistreringService for automatisk bilag (FR-F06)". Bilag opprettes IKKE ved utstedelse. Spesifikasjonen FR-F06/FR-F10 steg 9-10 krever at det automatisk opprettes bilag med posteringer (debet 1500 Kundefordringer, kredit 3xxx Salgsinntekt, kredit 2710 Utg. MVA). Dette er helt fravarende i implementasjonen.
- **Fil:** `D:\Code\Regnskap\src\Regnskap.Application\Features\Fakturering\FaktureringService.cs:178-179`
- **Foreslatt fix:** Implementer bilagsopprettelse i `UtstedeFakturaAsync`:
  1. Injiser `IBilagRegistreringService` eller `IHovedbokRepository`
  2. Opprett Bilag med type UtgaendeFaktura
  3. Grupper linjer per inntektskonto og opprett kredit-posteringer
  4. Grupper per MVA-kode og opprett kredit-posteringer paa MVA-konto (2710/2711/2712)
  5. Opprett debet-postering paa 1500 Kundefordringer
  6. Valider debet = kredit
  7. Sett `faktura.BilagId`
  8. Opprett KundeFaktura i kundereskontro og sett `faktura.KundeFakturaId`

### 2. Revisjonsspor

- [x] CreatedAt/CreatedBy paa alle entiteter (arver AuditableEntity)
- [x] ModifiedAt/ModifiedBy paa alle entiteter
- [x] Soft delete (IsDeleted) -- brukt i KansellerFakturaAsync
- [x] Ingen hard delete i koden
- **Status: OK**
- **Funn:** Alle entiteter arver `AuditableEntity`. Kansellering setter `IsDeleted = true`. Ingen hard delete pavist.

### 3. Regnskapsloven-compliance

- [x] Bilagsnummerering er fortlopende (FakturaNummerserie med UPDLOCK-logikk i repo)
- [ ] Bokforing uten ugrunnet opphold (timestamp)
- [x] Sporbarhet fra rapport til bilag
- **Status: MUST_FIX**
- **Funn 3a:** Nummerserie-repoet (`FakturaRepository.NesteNummerAsync`) mangler pessimistisk laas. Spesifikasjonen FR-F01 krever "pessimistisk laas / UPDLOCK" for aa sikre ubrudt nummerserie under samtidige foresporsler. Koden bruker vanlig `FirstOrDefaultAsync` uten laas.
- **Fil:** `D:\Code\Regnskap\src\Regnskap.Infrastructure\Features\Fakturering\FakturaRepository.cs:39-59`
- **Foreslatt fix:** Bruk `FromSqlRaw` med `UPDLOCK, HOLDLOCK` hint, eller bruk `IsolationLevel.Serializable` for transaksjonen som tildeler nummer. Eksempel:
  ```csharp
  var serie = await _db.FakturaNummerserie
      .FromSqlRaw("SELECT * FROM FakturaNummerserie WITH (UPDLOCK, HOLDLOCK) WHERE Ar = {0} AND Dokumenttype = {1}", aar, (int)type)
      .FirstOrDefaultAsync(ct);
  ```

### 4. MVA-korrekthet

- [x] Riktige MVA-koder brukes (3, 31, 33, 5, 6)
- [x] MVA beregnes korrekt per linje med MidpointRounding.AwayFromZero
- [x] MVA-grunnlag og MVA-belop er separate felt
- [x] Mapping til SAF-T MVA-koder (MapTilEhfTaxCategory)
- **Status: OK**
- **Funn:** Beregningslogikken i `FakturaBeregning.BeregnLinje` folger FR-F02 korrekt. MVA beregnes per linje (ikke paa totalnivaa), konsistent med EHF-standarden. Tester verifiserer 25%, 15%, 12%, og 0% satser.

### 5. SAF-T-kompatibilitet

- [x] Alle paakreve SAF-T-felt finnes i modellen (InvoiceNo, CustomerInfo, InvoiceDate, Lines)
- [x] StandardAccountID mapping via Kontonummer
- [ ] TransactionID er unik og sporbar
- **Status: MUST_FIX**
- **Funn:** Fordi bilag ikke opprettes ved utstedelse (se punkt 1), finnes det ingen `BilagId` / `TransactionID` paa utstedte fakturaer. SAF-T SourceDocuments > SalesInvoices krever `TransactionID` som kobler til GeneralLedgerEntries. Uten bilag er denne koblingen brutt.
- **Fil:** `D:\Code\Regnskap\src\Regnskap.Domain\Features\Fakturering\Faktura.cs:132` -- BilagId forblir null etter utstedelse
- **Foreslatt fix:** Loses automatisk naar bilagsopprettelse implementeres (punkt 1).

### 6. Spesifikasjonsoverensstemmelse

- [x] Alle endepunkter fra spec er implementert (GET/POST/PUT/DELETE fakturaer, utstede, kreditnota, ehf, pdf)
- [x] Alle forretningsregler fra spec er implementert (FR-F01..FR-F10, med unntak av FR-F06)
- [ ] Alle valideringsregler fra spec er implementert
- **Status: SHOULD_FIX**
- **Funn 6a:** `FaktureringService.OpprettFakturaAsync` validerer IKKE at `MvaKode` eksisterer i MvaKode-tabellen. Spesifikasjonen sier "maa eksistere i MvaKode-tabell". Koden bruker MvaSats fra DTO uten aa sla opp i databasen.
- **Fil:** `D:\Code\Regnskap\src\Regnskap.Application\Features\Fakturering\FaktureringService.cs:57-61`
- **Foreslatt fix:** Injiser `IMvaRepository` og valider at `linjeReq.MvaKode` eksisterer og er aktiv. Hent MvaSats fra databasen i stedet for DTO.

- **Funn 6b:** Koden validerer IKKE at `KontoId` er en aktiv konto i klasse 3 (inntekt). Spesifikasjonen krever dette.
- **Fil:** `D:\Code\Regnskap\src\Regnskap.Application\Features\Fakturering\FaktureringService.cs:57-61`
- **Foreslatt fix:** Injiser `IKontoplanRepository`, sla opp KontoId, valider at kontonummer starter med "3".

- **Funn 6c:** EHF-validering (`EhfService.ValiderEhfXml`) er veldig enkel -- sjekker bare at CustomizationID finnes. Spec sier "Valider generert XML mot UBL 2.1 skjema (XSD-validering)".
- **Fil:** `D:\Code\Regnskap\src\Regnskap.Application\Features\Fakturering\EhfService.cs:57-73`
- **Foreslatt fix:** Legg til XSD-validering mot UBL 2.1 Invoice og CreditNote skjemaer, eller bruk PEPPOL BIS validation artefacts.

### 7. Testkvalitet (Fakturering)

- [x] Unit tests for linjeberegning, totalberegning, MVA-gruppering
- [x] Edge cases testet (avrunding, 0% MVA, rabatt)
- [x] MVA-beregning testet med faktiske norske satser (25%, 15%, 12%)
- [x] Forfallsdato-beregning med helg-haandtering
- [x] EHF TaxCategory mapping testet
- [ ] Kreditnota-overskridelse ikke testet tilstrekkelig
- **Status: SHOULD_FIX**
- **Funn 7a:** Mangler test for delvis kreditering med spesifikke linjer (request.Linjer != null).
- **Funn 7b:** Mangler test for at kreditnotaens `Krediteringsaarsak` er tom/null (skal kaste exception).
- **Funn 7c:** Mangler test for EHF-generering av kreditnota (bare Invoice testes, ikke CreditNote).
- **Funn 7d:** Mangler test for oppdatering av faktura (OppdaterFakturaAsync).
- **Foreslatt fix:** Legg til tester for disse scenariene i `FaktureringServiceTests.cs` og `EhfServiceTests.cs`.

### 8. EHF/PEPPOL BIS 3.0 korrekthet

- [x] CustomizationID og ProfileID korrekte PEPPOL BIS 3.0-verdier
- [x] Invoice typeCode 380, CreditNote typeCode 381
- [x] BuyerReference (BT-10) inkludert
- [x] OrderReference (BT-13) inkludert
- [x] Seller/Buyer med PartyName, PostalAddress, PartyTaxScheme, PartyLegalEntity
- [x] PaymentMeans med KID og bankkonto
- [x] TaxTotal med TaxSubtotal per sats
- [x] LegalMonetaryTotal korrekt
- [x] InvoiceLine med Item, ClassifiedTaxCategory, Price
- [ ] CreditNote mangler PaymentMeans
- [ ] InvoiceLine hardkoder "NOK" i stedet for faktura.Valutakode
- **Status: SHOULD_FIX**
- **Funn 8a:** `GenererCreditNote` utelater `PaymentMeans`. For kreditnotaer med refusjon trengs betalingsinformasjon.
- **Fil:** `D:\Code\Regnskap\src\Regnskap.Application\Features\Fakturering\EhfService.cs:123-158`
- **Funn 8b:** `GenererInvoiceLine` og `GenererCreditNoteLine` hardkoder `currencyID="NOK"` i LineExtensionAmount og PriceAmount (linje 307-308, 330-331). Bor bruke `faktura.Valutakode` (riktig brukt i TaxTotal og LegalMonetaryTotal).
- **Fil:** `D:\Code\Regnskap\src\Regnskap.Application\Features\Fakturering\EhfService.cs:307,330`
- **Foreslatt fix:** Pass `valutakode` til linjegenereringene, eller lagre `Valutakode` paa FakturaLinje.

### 9. Bokforingsforskriften 5-1-1 (salgsdokument-krav)

- [x] Nr. 1: Fortlopende nummer (Fakturanummer)
- [x] Nr. 2-3: Selgers navn/adresse/org.nr (Selskapsinfo)
- [x] Nr. 4: Kjopers navn/adresse (Kunde via FK)
- [x] Nr. 5-6: Beskrivelse, antall, enhetspris (FakturaLinje)
- [x] Nr. 7: Leveringsdato (Leveringsdato felt)
- [x] Nr. 8: Fakturadato
- [x] Nr. 9: Forfallsdato
- [x] Nr. 10: Sum ekskl. MVA (BelopEksMva)
- [x] Nr. 11: MVA spesifisert per sats (FakturaMvaLinje)
- [x] Nr. 12: Sum inkl. MVA (BelopInklMva)
- **Status: OK**

### 10. Sikkerhet (Fakturering)

- [x] Ingen SQL injection-muligheter (bruker EF Core parameterisert)
- [x] Autorisasjon paa alle endepunkter ([Authorize])
- [x] Sensitive data haandtert korrekt
- **Status: OK**

---

## BANKAVSTEMMING (Modul 8)

### 11. Dobbelt bokholderi-integritet (Bank)

- [ ] Bilagsopprettelse ved innbetalings-matching (FR-B08)
- [ ] Bilagsopprettelse ved utbetalings-matching (FR-B09)
- [ ] Bilagsopprettelse ved direkte bokforing av umatchede (FR-B05)
- **Status: MUST_FIX**
- **Funn:** `BankMatchingService` oppretter BankbevegelseMatch men oppretter ALDRI bilag. Spesifikasjonen FR-B08 krever at ved innbetalings-match opprettes bilag (Debet 1920 Bank / Kredit 1500 Kundefordringer). FR-B09 krever tilsvarende for utbetalinger. FR-B05 krever bilag for direkte bokforing. Ingen av disse er implementert.
- **Fil:** `D:\Code\Regnskap\src\Regnskap.Application\Features\Bank\BankMatchingService.cs` -- hele filen
- **Foreslatt fix:**
  1. Injiser `IHovedbokRepository` (eller `IBilagRegistreringService`) i `BankMatchingService`
  2. Ved KID-match/manuell match av innbetaling: opprett bilag D:1920 / K:1500
  3. Oppdater `KundeFaktura.GjenstaendeBelop` og status
  4. Ved EndToEndId/manuell match av utbetaling: koble til eksisterende bilag
  5. Sett `bevegelse.BilagId` etter bilagsopprettelse

- **Funn tillegg:** API-endepunktet `POST /api/bankbevegelser/{id}/bokfor` (FR-B05 direkte bokforing) er spesifisert med DTO `BokforBankbevegelseRequest` i DTOs-filen, men endepunktet er IKKE implementert i `BankbevegelseController`.
- **Fil:** `D:\Code\Regnskap\src\Regnskap.Api\Features\Bank\BankbevegelseController.cs` -- mangler `bokfor`-endepunkt

### 12. Revisjonsspor (Bank)

- [x] CreatedAt/CreatedBy paa alle entiteter (AuditableEntity)
- [x] ModifiedAt/ModifiedBy
- [x] Soft delete paa Bankkonto (via ErAktiv flag i stedet for IsDeleted)
- [ ] Hard delete av matchinger i FjernMatchinger
- **Status: SHOULD_FIX**
- **Funn:** `BankRepository.FjernMatchinger` bruker `RemoveRange` som er hard delete. Revisjonsagenten krever soft delete. Matchinger bor markeres som slettet i stedet for aa fjernes fysisk.
- **Fil:** `D:\Code\Regnskap\src\Regnskap.Infrastructure\Features\Bank\BankRepository.cs:99-105`
- **Foreslatt fix:** Legg til `IsDeleted`-flagg paa BankbevegelseMatch (arver allerede fra AuditableEntity) og bruk soft delete i stedet for `RemoveRange`. Filtrer bort slettede matchinger i queries.

### 13. CAMT.053 parsing

- [x] Namespace-deteksjon (stotter v02, v06, v08)
- [x] GrpHdr/MsgId parsing
- [x] Duplikatkontroll paa MeldingsId
- [x] Stmt/Acct/Id/IBAN matching mot bankkonto
- [x] Saldo-parsing (OPBD/CLBD)
- [x] Ntry-parsing med CdtDbtInd, Amt, BookgDt, ValDt
- [x] KID-parsing fra CdtrRefInf/Ref
- [x] EndToEndId-parsing
- [x] Motpart-parsing (Dbtr/Nm for inn, Cdtr/Nm for ut)
- [x] SHA-256 hash av originalfil
- [x] Original filsti lagres
- **Status: OK**
- **Merknad:** CAMT.053 implementasjonen er solid. Stotter flere namespace-versjoner. En mindre forbedring ville vaere aa ogsaa matche paa norsk kontonummer (TODO-kommentar i kode), men dette er ikke kritisk da IBAN er standard.

### 14. KID matching-noyaktighet

- [x] Prioritet 1: KID-match paa innbetalinger (konfidens 1.0/0.95)
- [x] Prioritet 2: EndToEndId-match paa utbetalinger (konfidens 0.9)
- [x] Prioritet 3: Belop + dato-match med +/- 5 dager (konfidens 0.7)
- [x] Fler-treff gir ingen automatisk match (krever manuell)
- [ ] KID-validering (MOD10/MOD11) mangler
- **Status: SHOULD_FIX**
- **Funn:** Spesifikasjonen FR-B02 sier "Valider KID (MOD10/MOD11)" som steg 1 i KID-matching. Koden sjekker bare om `bevegelse.KidNummer` er satt og soker etter matchende faktura, men validerer IKKE at KID-nummeret er et gyldig MOD10/MOD11-nummer for KID-nummeret finnes i databasen.
- **Fil:** `D:\Code\Regnskap\src\Regnskap.Application\Features\Bank\BankMatchingService.cs:49-51`
- **Foreslatt fix:** Kall `KidGenerator.Valider(bevegelse.KidNummer)` for sjekke kontrollsiffer for matching. Ugyldig KID bor logges men ikke matche automatisk.

### 15. Bilagsopprettelse ved bank (gjentatt fra punkt 11)

Allerede dekket i punkt 11. MUST_FIX.

### 16. SAF-T-kompatibilitet (Bank)

- [x] MasterFiles > GeneralLedgerAccounts (1920-serien) via Hovedbokkonto-kobling
- [ ] GeneralLedgerEntries mangler fordi bilag ikke opprettes
- **Status: MUST_FIX**
- **Funn:** Uten bilagsopprettelse (punkt 11) vil bankbevegelser ikke reflekteres i GeneralLedgerEntries i SAF-T. Dette er et compliance-brudd.
- **Foreslatt fix:** Loses automatisk naar bilagsopprettelse implementeres.

### 17. Spesifikasjonsoverensstemmelse (Bank)

- [x] Alle bankonto-endepunkter implementert
- [x] Import-endepunkt implementert
- [x] Bevegelser-endepunkter implementert (hent, match, splitt, ignorer, fjern)
- [x] Auto-match og match-forslag implementert
- [x] Avstemming (hent, oppdater, godkjenn, rapport) implementert
- [ ] `POST /api/bankbevegelser/{id}/bokfor` mangler
- [ ] `POST /api/bankkontoer/{id}/auto-match` feilplassert respons-moenster
- **Status: MUST_FIX**
- **Funn 17a:** Bokfor-endepunktet (FR-B05) er ikke implementert. DTO `BokforBankbevegelseRequest` finnes i `BankDtos.cs` men endepunktet mangler i `BankbevegelseController`.
- **Fil:** `D:\Code\Regnskap\src\Regnskap.Api\Features\Bank\BankbevegelseController.cs`
- **Foreslatt fix:** Implementer endepunktet som:
  1. Validerer motkonto (aktiv, bokforbar)
  2. Oppretter bilag (debet/kredit avhengig av retning)
  3. Haandterer eventuell MVA
  4. Setter `bevegelse.Status = Bokfort` og `bevegelse.BilagId`

- **Funn 17b:** `POST /api/bankkontoer/{id}/auto-match` returnerer anonymt objekt `{ AntallMatchet }` i stedet for en typet response.
- **Fil:** `D:\Code\Regnskap\src\Regnskap.Api\Features\Bank\BankkontoController.cs:155-159`

### 18. Testkvalitet (Bank)

- [x] CAMT.053 import: gyldig XML, saldoer, duplikat, feil IBAN, ugyldig XML, flere bevegelser, KID-parsing
- [x] Matching: KID-match, delbetaling, EndToEndId, belop/dato, fler-treff, manuell match, splitt, sum-validering, fjern match
- [x] Avstemming: opprett, eksisterende, tidsavgrensninger, godkjenning, uforklart differanse
- [x] Bankkontonummer MOD11-validering
- [ ] Mangler test for direkte bokforing av bankbevegelse (FR-B05)
- [ ] Mangler test for saldo-validering (UtgaendeSaldo = InngaendeSaldo + SumInn - SumUt)
- [ ] Mangler test for innbetalings-registrering i kundereskontro (FR-B08)
- **Status: SHOULD_FIX**
- **Funn:** Tester dekker matching-logikk godt, men mangler tester for:
  1. FR-B05: Direkte bokforing (endepunkt ikke implementert enna)
  2. Saldo-balanse-validering i import (spec punkt 6 i FR-B01)
  3. FR-B08/FR-B09: Reskontro-oppdatering ved match
- **Foreslatt fix:** Legg til disse testene etter at bilagsopprettelse er implementert.

### 19. Avstemming-logikk

- [x] Differanse-beregning: SaldoBank - SaldoHovedbok
- [x] ForklartDifferanse = UtestaaendeBetalinger + InnbetalingerITransitt + AndreDifferanser
- [x] UforklartDifferanse = Differanse - ForklartDifferanse
- [x] Godkjenning krever UforklartDifferanse = 0 eller DifferanseForklaring
- [x] Status: Avstemt vs AvstemtMedDifferanse
- [x] GodkjentAv og GodkjentTidspunkt settes
- **Status: OK**

### 20. Sikkerhet (Bank)

- [x] [Authorize] paa begge controllers
- [x] EF Core parameterisert (ingen SQL injection)
- [x] Filhash for integritetskontroll av importerte filer
- **Status: OK**

### 21. Bankkonto-validering (FR-B07)

- [x] MOD11-validering implementert korrekt
- [x] Stotter formattering med punktum
- [x] Tester for gyldige og ugyldige kontonumre
- **Status: OK**
- **Merknad:** Validering ligger i `BankkontoController` (API-laget). Ideelt sett bor den vaere i Domain eller Application-laget for gjenbruk, men dette er en arkitektur-merknad, ikke et compliance-problem.

---

## Oppsummering av funn

### MUST_FIX (6)

| # | Funn | Modul | Lovhjemmel/spec |
|---|------|-------|-----------------|
| 1 | Bilag opprettes IKKE ved fakturautstedelse | Fakturering | Bokforingsloven 6, FR-F06 |
| 2 | Nummerserie mangler pessimistisk laas | Fakturering | Bokforingsforskriften 5-2-1 |
| 3 | SAF-T TransactionID (BilagId) er null etter utstedelse | Fakturering | SAF-T krav |
| 4 | Bilag opprettes IKKE ved bankmatching/-bokforing | Bank | Bokforingsloven 6, FR-B05/B08/B09 |
| 5 | Bokfor-endepunkt (FR-B05) ikke implementert | Bank | FR-B05 |
| 6 | SAF-T GeneralLedgerEntries mangler for bankbevegelser | Bank | SAF-T krav |

### SHOULD_FIX (9)

| # | Funn | Modul |
|---|------|-------|
| 1 | MvaKode-validering mot database mangler | Fakturering |
| 2 | KontoId-validering (klasse 3) mangler | Fakturering |
| 3 | EHF XSD-validering er kun overfladisk | Fakturering |
| 4 | CreditNote mangler PaymentMeans | Fakturering |
| 5 | Hardkodet "NOK" i EHF InvoiceLine/CreditNoteLine | Fakturering |
| 6 | Manglende tester for delvis kreditering, kreditnota-EHF, oppdatering | Fakturering |
| 7 | Hard delete av BankbevegelseMatch | Bank |
| 8 | KID-validering (MOD10/MOD11) mangler i auto-matching | Bank |
| 9 | Manglende tester for FR-B05, saldo-balanse, reskontro-oppdatering | Bank |

### OK (11)

| # | Sjekkpunkt |
|---|-----------|
| 1 | Revisjonsspor (Fakturering) |
| 2 | MVA-korrekthet |
| 3 | Bokforingsforskriften 5-1-1 felter |
| 4 | EHF/PEPPOL BIS 3.0 struktur (hovedsakelig) |
| 5 | Sikkerhet (Fakturering) |
| 6 | CAMT.053 parsing |
| 7 | Avstemming-logikk |
| 8 | Sikkerhet (Bank) |
| 9 | Bankkonto MOD11-validering |
| 10 | DI-registrering korrekt for begge moduler |
| 11 | API-kontrakt med DTOs, mapper, controller-struktur |

---

## Anbefalt fix-rekkefolge

1. **Bilagsopprettelse ved fakturautstedelse (MUST_FIX 1, 3)** -- Denne er kritisk for lovpaalagt bokforing og SAF-T. Blokkerer ogsaa bankavstemmingsmodulen.
2. **Bilagsopprettelse ved bankmatching (MUST_FIX 4, 5, 6)** -- Krever at punkt 1 er paa plass.
3. **Pessimistisk laas paa nummerserie (MUST_FIX 2)** -- Raske-fix, viktig for dataintegritet ved samtidige brukere.
4. **SHOULD_FIX i prioritert rekkefolge** -- MvaKode/KontoId-validering, EHF-forbedringer, tester.

---

## Verdict: KREVER_ENDRING

6 MUST_FIX funn. Kjerneproblemet er at bilagsopprettelse mangler i baade fakturering og bankavstemming. Uten bilag er bokforingen ufullstendig, og SAF-T-eksport vil mangle transaksjoner. Dette er et brudd paa Bokforingsloven 6 (dokumentasjon) og 7 (bokforing).

Modulenes datamodell, beregningslogikk, EHF-generering, CAMT.053-parsing og matching-algoritmer er solide. Etter at bilagsopprettelse er implementert og de ovrige MUST_FIX er fikset, kan modulene re-revideres for godkjenning.
