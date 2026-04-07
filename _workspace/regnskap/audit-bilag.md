# Revisjonsrapport: Bilagsregistrering

**Dato:** 2026-04-06
**Revisor:** Revisjonsagent (Claude Opus 4.6)
**Modul:** Bilagsregistrering (Journal Entry Registration)
**Versjon:** Forste revisjon
**Spesifikasjon:** spec-bilag.md
**Lovgrunnlag:** Bokforingsloven, Regnskapsloven, Merverdiavgiftsloven, SAF-T

---

## Sammendrag

- Antall MUST_FIX: **4**
- Antall SHOULD_FIX: **8**
- Antall OK: **10**
- Samlet status: **KREVER_ENDRING**

Implementasjonen er solid og dekker de fleste forretningsregler korrekt. Dobbelt bokholderi-invarianten, MVA-autoposteringer (inkludert snudd avregning), og tilbakeforinglogikk er godt implementert med god testdekning. Det er imidlertid fire compliance-kritiske funn som ma fikses for lovparagrafheten.

---

## Sjekkpunkter

### 1. Dobbelt bokholderi-integritet

- [x] Debet = kredit valideres i domenet (`Bilag.ValiderBalanse()`)
- [x] Balansesjekk ved bilagsopprettelse (linje 183 i BilagRegistreringService.cs)
- [x] Balansesjekk ved tilbakeforing (linje 307)
- [x] Minimum 2 posteringer validert
- [x] Positive belop validert (FR-3)
- [x] MVA-autoposteringer inngaar i balansesjekk

**Status: OK**

Bilag.ValiderBalanse() kaster AccountingBalanceException dersom sum debet != sum kredit. Metoden bruker `Belop.Verdi` (eksakt desimalsammenligning). Testene dekker balansert bilag, ubalansert bilag, og MVA-scenarier.

---

### 2. Revisjonsspor

- [x] CreatedAt/CreatedBy pa alle entiteter (arves fra AuditableEntity)
- [x] ModifiedAt/ModifiedBy pa alle entiteter (arves fra AuditableEntity)
- [x] Soft delete (IsDeleted) for vedlegg
- [x] Ingen hard delete i koden (vedlegg bruker `IsDeleted = true`)
- [ ] BokfortAv er hardkodet til "system"

**Status: SHOULD_FIX**

**Funn S-1:** `BokfortAv` er satt til `"system"` (BilagRegistreringService.cs:574). Bokforingsloven krever sporbarhet til den som utforte handlingen. Det ma hentes fra brukeridentitet.
- **Fil:** `src/Regnskap.Application/Features/Bilagsregistrering/BilagRegistreringService.cs:574`
- **Foreslaatt fix:** Inject en `ICurrentUserService` og bruk `currentUser.UserName` for `BokfortAv`. Det staar allerede `// TODO: Hent fra brukeridentitet` i koden.

---

### 3. Bokforingsloven-compliance

- [x] Bilagsnummerering er fortlopende (globalt per aar + per serie)
- [x] Bokforing med timestamp (`Registreringsdato = DateTime.UtcNow`)
- [x] Sporbarhet via BilagsId og SerieBilagsId
- [ ] Concurrency retry ved nummereringkonflikt mangler
- [ ] BilagStatus-enum definert men ikke brukt

**Status: MUST_FIX**

**Funn M-1: Manglende retry ved nummereringkonflikt (Bokforingsloven paragaraf 5)**
Spesifikasjonen (FR-4) krever retry opp til 3 ganger ved `DbUpdateConcurrencyException` for a hindre hull i nummerserien. `BilagSerieNummer` har `RowVersion` konfigurert som concurrency token, men `TildelSerieNummerAsync` (linje 442-458) har ingen retry-logikk. Ved samtidige transaksjoner vil en `DbUpdateConcurrencyException` propagere ukontrollert og potensielt foraarsake hull i nummerserien.
- **Fil:** `src/Regnskap.Application/Features/Bilagsregistrering/BilagRegistreringService.cs:442-458`
- **Foreslaatt fix:** Wrap nummertildeling i en retry-loop:
```csharp
private async Task<int> TildelSerieNummerAsync(BilagSerie serie, int ar, CancellationToken ct)
{
    for (int attempt = 0; attempt < 3; attempt++)
    {
        try
        {
            var serieNummer = await _bilagRepo.HentSerieNummerAsync(serie.Kode, ar, ct);
            if (serieNummer == null) { /* opprett som naa */ }
            return serieNummer.TildelNummer();
        }
        catch (DbUpdateConcurrencyException) when (attempt < 2)
        {
            // Retry - last inn ferske data
        }
    }
    throw new NummereringKonfliktException();
}
```

**Funn S-2: BilagStatus-enum definert men ikke brukt**
`BilagStatus` (Kladd, Validert, Bokfort, Tilbakfort) er definert i `Enums.cs` men Bilag-entiteten bruker separate bool-flagg (`ErBokfort`, `ErTilbakfort`). Dette er ikke en lovbrudd, men gir inkonsistens mot spesifikasjonen og kan gjore statuslogikk fragil.
- **Fil:** `src/Regnskap.Domain/Features/Bilagsregistrering/Enums.cs`
- **Foreslaatt fix:** Legg til en `Status`-property pa Bilag som utledes fra flaggene, eller bruk enum direkte.

---

### 4. MVA-korrekthet

- [x] Riktige MVA-koder brukes (hentes fra MvaKode-entitet)
- [x] MVA beregnes korrekt (`Math.Round(belop * sats / 100m, 2, MidpointRounding.ToEven)`)
- [x] MVA-grunnlag og MVA-belop er separate felt pa Postering
- [x] Inngaaende MVA: Debet pa InngaendeKonto
- [x] Utgaaende MVA: Kredit pa UtgaendeKonto
- [x] Snudd avregning: Bade debet (inngaaende) og kredit (utgaaende)
- [x] MVA-sats snapshot ved bokforingstidspunkt (MvaSats pa Postering)
- [x] ErAutoGenerertMva-flagg satt pa auto-posteringer
- [ ] HentMvaKontoAsync bruker ineffektiv metode

**Status: SHOULD_FIX**

**Funn S-3: Ineffektiv MVA-kontooppslag**
`HentMvaKontoAsync` (linje 508-519) henter ALLE bokforbare kontoer og filtrerer pa ID i minnet. Med mange kontoer blir dette en ytelsesflaskehals. Koden har ogsa en `// TODO: Avklar med arkitekt`.
- **Fil:** `src/Regnskap.Application/Features/Bilagsregistrering/BilagRegistreringService.cs:508-519`
- **Foreslaatt fix:** Legg til `Task<Konto?> HentKontoMedIdAsync(Guid id)` pa `IKontoplanRepository`.

---

### 5. SAF-T-kompatibilitet

- [x] TransactionID: `BilagsId` (`2026-00042`) er unik per aar
- [x] TransactionDate: `Bilagsdato` (DateOnly)
- [x] SystemEntryDate: `Registreringsdato` (DateTime)
- [x] Description: `Beskrivelse` pa bade Bilag og Postering
- [x] JournalID via `BilagSerie.SaftJournalId`
- [x] AccountID: `Kontonummer` denormalisert pa Postering
- [x] DebitAmount/CreditAmount: via `BokforingSide` og `Belop`
- [x] TaxInformation: `MvaKode`, `MvaSats`, `MvaGrunnlag`, `MvaBelop` pa Postering
- [x] CustomerID/SupplierID: pa Postering
- [x] RecordID: `Linjenummer` pa Postering

**Status: OK**

SAF-T-feltene er komplett representert i datamodellen. Alle obligatoriske felter for GeneralLedgerEntries/Journal/Transaction/Line er tilgjengelige.

---

### 6. Spesifikasjonsoverensstemmelse

#### 6.1 Endepunkter

| Spesifikasjon | Implementert | Status |
|---|---|---|
| POST /api/bilag | Ja (BilagController) | OK |
| GET /api/bilag/{id} | Ja | OK |
| GET /api/bilag/nummer/{ar}/{bilagsnummer} | Ja | OK |
| GET /api/bilag/serie/{serieKode}/{ar}/{serieNummer} | Ja | OK |
| POST /api/bilag/sok | Ja | OK |
| POST /api/bilag/valider | Ja | OK |
| POST /api/bilag/{id}/bokfor | Ja | OK |
| POST /api/bilag/{id}/tilbakefor | Ja | OK |
| POST /api/bilag/{id}/vedlegg | Ja | OK |
| GET /api/bilag/{id}/vedlegg | Ja | OK |
| DELETE /api/bilag/{id}/vedlegg/{vedleggId} | Ja | OK |
| GET /api/bilagserier | Ja | OK |
| GET /api/bilagserier/{kode} | Ja | OK |
| POST /api/bilagserier | Ja | OK |
| PUT /api/bilagserier/{kode} | Ja | OK |

#### 6.2 Forretningsregler

| Regel | Implementert | Testet | Status |
|---|---|---|---|
| FR-1: Dobbelt bokholderi | Ja | Ja | OK |
| FR-2: Minimum posteringer | Ja | Ja | OK |
| FR-3: Positive belop | Ja | Ja | OK |
| FR-4: Fortlopende nummerering | Delvis (mangler retry) | Ja (happy path) | MUST_FIX |
| FR-5: Bilagserier | Ja | Ja | OK |
| FR-6: Periodevalidering | Ja | Ja | OK |
| FR-7: Kontovalidering | Ja | Ja | OK |
| FR-8: MVA-validering | Ja | Ja | OK |
| FR-9: MVA-autopostering | Ja | Ja | OK |
| FR-10: Tilbakeforing | Ja | Ja | OK |
| FR-11: Bokforing mot hovedbok | Ja | Ja | OK |
| FR-12: Bilagssok | Ja | Ikke dedikert | SHOULD_FIX |
| FR-13: Vedlegg | Ja | Ja | OK |
| FR-14: Kladd vs. direkte | Ja | Ja | OK |

#### 6.3 DTOer og feilkoder

- [x] Alle request/response DTOer implementert i henhold til spec
- [x] Alle feilkoder fra spec returneres korrekt av API
- [x] Paginering med maks 200 per side (BilagSokService linje 22)
- [ ] `IBilagRegistreringService`-interface fra spec er ikke implementert

**Status: MUST_FIX**

**Funn M-2: Manglende IBilagRegistreringService-interface**
Spesifikasjonen definerer `IBilagRegistreringService` som et eksplisitt interface som utvider `IBilagService`. Implementasjonen bruker `BilagRegistreringService` som konkret klasse uten dette interfacet. API-controllerne injiserer den konkrete klassen direkte.
- **Fil:** `src/Regnskap.Api/Features/Bilagsregistrering/BilagServiceExtensions.cs:13`
- **Fil:** `src/Regnskap.Api/Features/Bilagsregistrering/BilagController.cs:18`
- **Foreslaatt fix:** Opprett `IBilagRegistreringService` som definert i spec. La `BilagRegistreringService` implementere det. Inject via interface i controllerne. Dette forbedrer testbarhet og folger Dependency Inversion Principle.

---

### 7. Testkvalitet

- [x] FR-1: Balansert og ubalansert bilag
- [x] FR-2: Minimum posteringer
- [x] FR-3: Negativt belop
- [x] FR-4: Bilagsnummerering (happy path)
- [x] FR-5: Serier (gyldig, ukjent, inaktiv)
- [x] FR-6: Periodevaliering (apen, lukket, sperret, ikke-eksisterende)
- [x] FR-7: Kontovalidering (ikke funnet, inaktiv, ikke-bokforbar, krever avdeling/prosjekt)
- [x] FR-8/9: MVA (inngaaende, utgaaende, snudd avregning, ukjent kode)
- [x] FR-10: Tilbakeforing (suksess, ikke-bokfort, allerede tilbakfort)
- [x] FR-11: Bokforing (direkte, kladd, KontoSaldo-oppdatering)
- [x] FR-13: Vedlegg (gyldig, ugyldig MIME, slett fra bokfort)
- [x] FR-14: Kladd vs. direkte
- [ ] Mangler test for concurrency-retry (FR-4)
- [ ] Mangler test for vedlegg storelse over maks
- [ ] Mangler dedikert test for FR-12 (bilagssok)
- [ ] Mangler test for tilbakeforing med MVA-posteringer

**Status: SHOULD_FIX**

**Funn S-4: Manglende tester for viktige kanttilfeller**
1. Ingen test for `NummereringKonfliktException` / concurrency retry
2. Ingen test for `VedleggForStortException` (overstor fil)
3. Ingen dedikerte tester for `BilagSokService` / søk-filtrering
4. Ingen test for tilbakeforing av bilag MED MVA-posteringer (verifisering at MVA-linjer ogsa speilvendes)
- **Foreslaatt fix:** Legg til tester for disse fire scenariene. Spesielt punkt 4 er viktig for MVA-korreksjonsrapportering.

---

### 8. Sikkerhet

- [x] `[Authorize]` pa alle controllere (BilagController, BilagSerieController, VedleggController)
- [x] Parameterbinding via `[FromBody]` og rute-constraints (`{id:guid}`, `{ar:int}`)
- [x] Ingen raae SQL-spørringer (bruker EF Core)
- [x] SHA-256 hash for vedleggsintegritet
- [ ] Ingen inputvalidering av Beskrivelse-lengde pa API-nivaa
- [ ] Ingen rate limiting pa opprettelse

**Status: SHOULD_FIX**

**Funn S-5: Manglende inputvalidering pa API-nivaa**
Controllerene validerer ikke input for form. Det er EF Core maxlength-constraints (Beskrivelse: 500 tegn), men feil vil forst avdekkes ved databaselagring (runtime exception) i stedet for tidlig validering med tydelig feilmelding.
- **Foreslaatt fix:** Legg til FluentValidation eller DataAnnotations pa request DTOer.

---

### 9. Periodelaasing (Lock Enforcement)

- [x] Apen/Lukket/Sperret valideres ved opprettelse
- [x] Apen/Lukket/Sperret valideres ved bokforing av kladd
- [x] Apen/Lukket/Sperret valideres ved tilbakeforing
- [x] Valideringservicen sjekker periode separat

**Status: OK**

Periodevalidering er konsekvent implementert pa alle inngangspunkter. Tester dekker alle tre periodestatus-varianter.

---

### 10. EF Core-konfigurasjon

- [x] Unik indeks pa (Ar, Bilagsnummer) med IsDeleted-filter
- [x] Unik indeks pa (SerieKode, Ar, SerieNummer) med null+IsDeleted-filter
- [x] RowVersion concurrency token pa BilagSerieNummer
- [x] Restrict delete pa alle FK-relasjoner
- [x] Ytelsesindekser pa Bilagsdato, Type, RegnskapsperiodeId, ErBokfort, ErTilbakfort
- [x] Seed-data for 8 standardserier

**Status: OK**

**Funn S-6: Unik indeksfilter bruker ulik syntaks**
BilagConfiguration bruker `"IsDeleted" = false` (linje 28) og `"SerieKode" IS NOT NULL AND "IsDeleted" = false` (linje 47) med dobbelfnutter. Dette er PostgreSQL-syntaks. Spesifikasjonen bruker `[SerieKode] IS NOT NULL` (SQL Server-syntaks). Sorg for at riktig syntaks brukes for valgt database-provider.
- **Fil:** `src/Regnskap.Infrastructure/Features/Hovedbok/BilagConfiguration.cs:28,47`

---

### 11. Tilbakeforing (FR-10)

- [x] Kun bokforte bilag kan tilbakefores
- [x] Et bilag kan kun tilbakefores en gang
- [x] Tilbakeforingsbilag faar egne bilagsnummer i KOR-serien
- [x] Alle posteringer speilvendes (Debet <-> Kredit)
- [x] Toveis-link via TilbakefortFraBilagId / TilbakefortAvBilagId
- [x] Originalbilaget markeres med ErTilbakfort = true
- [x] Tilbakeforingsbilag bokfores umiddelbart
- [x] Tilbakeforingsdato sjekkes mot apen periode
- [x] MVA-posteringer speilvendes med MVA-felter kopiert

**Status: OK**

Tilbakeforinglogikken er korrekt og komplett. Bade bruker- og auto-MVA-posteringer speilvendes med identiske belop og motsatt side. KontoSaldo oppdateres korrekt for tilbakeforingsbilag.

---

### 12. Atomisk transaksjonshlandtering

- [x] Alt lagres i ett kall til `LagreEndringerAsync` (EF Core Unit of Work)
- [x] Bilag + posteringer + nummerering i en SaveChanges
- [ ] Ingen eksplisitt databasetransaksjon

**Status: MUST_FIX**

**Funn M-3: Manglende eksplisitt transaksjon**
Spesifikasjonen (FR-11) krever: "Hele operasjonen (bilagopprettelse + nummerering + saldooppdatering) MA skje i EN databasetransaksjon for a sikre konsistens." EF Core sin `SaveChangesAsync` gjoer alt i en transaksjon for det som er sporet av DbContext, men nummeroppslagene (HentSerieNummerAsync, NestebilagsnummerAsync) skjer utenfor denne transaksjonen. Mellom oppslag og lagring kan en annen request ta samme nummer.
- **Fil:** `src/Regnskap.Application/Features/Bilagsregistrering/BilagRegistreringService.cs:81-196`
- **Foreslaatt fix:** Wrap hele `OpprettOgBokforBilagAsync` i en `IDbContextTransaction`:
```csharp
await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
try
{
    // ... eksisterende logikk ...
    await _hovedbokRepo.LagreEndringerAsync(ct);
    await transaction.CommitAsync(ct);
}
catch
{
    await transaction.RollbackAsync(ct);
    throw;
}
```
Alternativt: Inject `RegnskapDbContext` eller et `IUnitOfWork`-interface.

---

### 13. Bilagserier — systemserie-beskyttelse

- [x] Nye serier kan opprettes
- [ ] Systemserier kan deaktiveres via OppdaterBilagSerieAsync

**Status: MUST_FIX**

**Funn M-4: Systemserier kan deaktiveres/endres**
`OppdaterBilagSerieAsync` (BilagRegistreringService.cs:424-438) tillater a sette `ErAktiv = false` pa systemserier. Spesifikasjonen (FR-5) og BilagSerie-entiteten sier at "Systemserie kan ikke slettes eller deaktiveres." Ingen sjekk for `ErSystemserie`.
- **Fil:** `src/Regnskap.Application/Features/Bilagsregistrering/BilagRegistreringService.cs:424-438`
- **Foreslaatt fix:**
```csharp
if (serie.ErSystemserie && !request.ErAktiv)
    throw new InvalidOperationException("Systemserier kan ikke deaktiveres.");
```

---

## Oppsummering av funn

### MUST_FIX (4)

| # | Funn | Alvorlighet | Lov/Spec |
|---|------|-------------|----------|
| M-1 | Manglende retry ved nummereringkonflikt | Kan foraarsake hull i nummerserien | Bokforingsloven 5, FR-4 |
| M-2 | Manglende IBilagRegistreringService-interface | Avviker fra spesifikasjon, bryter DIP | Spec: Service-kontrakt |
| M-3 | Manglende eksplisitt databasetransaksjon | Race condition ved nummerering | FR-11, data-integritet |
| M-4 | Systemserier kan deaktiveres | Bryter med domeneregelen | Spec FR-5, BilagSerie.ErSystemserie |

### SHOULD_FIX (8)

| # | Funn | Alvorlighet |
|---|------|-------------|
| S-1 | BokfortAv hardkodet til "system" | Mangelfull revisjonsspor |
| S-2 | BilagStatus-enum definert men ikke brukt | Inkonsistens |
| S-3 | Ineffektiv MVA-kontooppslag (henter alle kontoer) | Ytelse |
| S-4 | Manglende tester for kanttilfeller | Testdekning |
| S-5 | Manglende inputvalidering pa API-nivaa | Brukeropplevelse |
| S-6 | Ulik DB-syntaks i indeksfiltre | Portabilitet |
| S-7 | Ingen test for bilagssok (FR-12) | Testdekning |
| S-8 | TilbakefortAvBilag ignorert i EF-config | Navigasjon ikke brukbar via EF queries |

---

## Konklusjon

Bilagsregistrering-modulen er **godt implementert** med korrekt dobbelt bokholderi-logikk, komplett MVA-autopostering (inkludert snudd avregning), og grundig tilbakeforinglogikk. Alle endepunkter fra spesifikasjonen er implementert, og testdekningen er god for de sentrale forretningsreglene.

De fire MUST_FIX-funnene dreier seg om:
1. **Transaksjonssikkerhet** (M-1, M-3): Nummereringslogikken mangler retry og eksplisitt transaksjon, noe som kan foraarsake hull eller duplikater ved samtidige brukere. Dette er compliance-kritisk per Bokforingsloven paragraf 5.
2. **Spesifikasjonsetterlevelse** (M-2): Manglende interface bryter med arkitekturens kontrakt.
3. **Domeneregelbeskyttelse** (M-4): Systemserier kan utilsiktet deaktiveres.

Anbefaling: Fiks MUST_FIX-funn og re-revider for **GODKJENT**-status.
