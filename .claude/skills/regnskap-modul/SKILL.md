---
name: regnskap:modul
description: "Implement a specific accounting module. Use when the user asks to build a single module like 'kontoplan', 'hovedbok', 'bilag', 'faktura', 'leverandør', 'kunde', 'bank', 'MVA', 'rapporter', or 'periodeavslutning'. Triggers on: 'bygg modul', 'implementer modul', 'lag kontoplan', 'lag hovedbok', 'opprett bilag-modul', 'build the chart of accounts', 'implement general ledger', 'create invoicing module'. Shortcut to regnskap:run for single-module tasks."
---

# Regnskap Modul

Snarvei for å implementere én enkelt regnskapsmodul. Delegerer til `regnskap:run` med modulnavn som input.

## Iron Law

```
DENNE SKILL-EN ER EN SNARVEI. ALL LOGIKK DELEGERES TIL regnskap:run.
Aldri implementer direkte — bruk alltid hele pipelinen.
```

## Process

1. **Parse module name** from user input:
   | User says | Module |
   |-----------|--------|
   | kontoplan, chart of accounts, kontokart | `kontoplan` |
   | hovedbok, general ledger, GL | `hovedbok` |
   | bilag, journal entries, voucher, postering | `bilag` |
   | leverandør, accounts payable, AP, leverandørreskontro | `leverandor` |
   | kunde, accounts receivable, AR, kundereskontro | `kunde` |
   | bank, bankavstemming, reconciliation | `bank` |
   | rapport, rapportering, reports, balanse, resultat | `rapporter` |
   | mva, MVA, VAT, merverdiavgift | `mva` |
   | faktura, invoice, invoicing, EHF | `faktura` |
   | periode, periodeavslutning, period close, årsavslutning | `periode` |

2. **Check dependencies:** Read `_workspace/regnskap/status.md` to verify required modules are built.
   - If dependencies are missing, inform the user and suggest building them first.

3. **Invoke regnskap:run** with the parsed module name.

## Dependency Matrix

| Module | Requires |
|--------|----------|
| kontoplan | (none) |
| hovedbok | kontoplan |
| bilag | hovedbok |
| mva | kontoplan, bilag |
| leverandor | bilag, hovedbok |
| kunde | bilag, hovedbok |
| faktura | kunde, mva |
| bank | hovedbok, leverandor, kunde |
| rapporter | all above |
| periode | hovedbok, rapporter |
