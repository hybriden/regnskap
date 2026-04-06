# Domain Analysis — Regnskapssystem

## Technical Stack
- **Backend:** ASP.NET Core 9 Web API
- **ORM:** Entity Framework Core 9
- **Database:** PostgreSQL 16
- **Frontend:** React 19 + TypeScript + Vite
- **Reporting:** React PDF / server-side PDF generation
- **Architecture:** Modular monolith with feature folders
- **External formats:** SAF-T (XML), EHF/PEPPOL (invoice), CAMT.053 (bank), MVA-melding

## Domain Modules

### 1. Kontoplan (Chart of Accounts)
- NS 4102 standard kontoplan
- Kontohierarki: klasse → gruppe → konto
- Kontotyper: eiendel, gjeld, egenkapital, inntekt, kostnad
- Brukerdefinerte underkontoer
- Import/eksport av kontoplan

### 2. Hovedbok (General Ledger)
- Dobbelt bokholderi — debet = kredit invariant
- Posteringer med bilagsnummer, dato, beløp, konto, motkonto
- Saldooppslag per konto, periode, dimensjon
- Låsing av perioder

### 3. Bilagsregistrering (Journal Entries)
- Bilagsnummerering (sekvensiell, per serie)
- Manuell og automatisk bilagsføring
- Vedlegg (kvitteringer, fakturakopier)
- Tilbakeføring av bilag
- MVA-kode per linje

### 4. Leverandørreskontro (Accounts Payable)
- Leverandørregister med organisasjonsnummer
- Inngående fakturamottak og registrering
- Betalingsforslag og betalingsfiler (pain.001)
- Aldersfordeling (aging)
- Automatisk matching mot bankbevegelser

### 5. Kundereskontro (Accounts Receivable)
- Kunderegister med organisasjonsnummer/fødselsnummer
- Utgående faktura-oppfølging
- Innbetalingsregistrering
- Purring og inkasso-varsler
- Aldersfordeling
- KID-nummer generering

### 6. Bankavstemming (Bank Reconciliation)
- Import av kontoutskrift (CAMT.053 / ISO 20022)
- Automatisk matching (beløp, KID, referanse)
- Manuell matching
- Avstemmingsrapport
- Flere bankkontoer

### 7. Rapportering (Financial Reporting)
- Resultatregnskap (income statement)
- Balanse (balance sheet)
- Kontantstrømoppstilling (cash flow statement)
- Saldobalanse (trial balance)
- Hovedboksutskrift
- Dimensjonsrapporter
- Sammenligning mot budsjett og forrige periode

### 8. MVA-håndtering (VAT)
- Norske MVA-koder (0%, 12%, 15%, 25%)
- MVA-beregning per bilagslinje
- MVA-oppgjør per termin (annenhver måned / årlig)
- MVA-melding (RF-0002) for Altinn
- SAF-T eksport (Standard Audit File - Tax)
- Omvendt avgiftsplikt (reverse charge)

### 9. Fakturering (Invoicing)
- Fakturagenerering fra ordre/timer/abonnement
- Kreditnota
- EHF/PEPPOL elektronisk faktura
- Fakturalayout med logo og betalingsinfo
- Automatisk bokføring ved fakturering

### 10. Periodeavslutning (Period Closing)
- Månedlig avstemming og lukking
- Årsavslutning med disponering av resultat
- Overføring av inngående balanse
- Avskrivninger
- Periodisering av inntekter/kostnader

## Complexity Assessment

| Akse | Score | Begrunnelse |
|------|-------|-------------|
| Bredde | 5/5 | 10 distinkte moduler med ulike bekymringsområder |
| Dybde | 5/5 | Strenge invarianter (dobbelt bokholderi), juridiske krav |
| Integrasjon | 4/5 | Bank (CAMT), skatt (SAF-T/Altinn), faktura (EHF/PEPPOL) |
| Kvalitet | 5/5 | Regnskapsloven, Bokføringsloven, revisjonsplikt |
| Drift | 3/5 | Revisjonsspor obligatorisk, dataoppbevaring 5 år |

## Norwegian Legal Requirements
- **Regnskapsloven** — Financial reporting obligations
- **Bokføringsloven** — Bookkeeping requirements, retention
- **Merverdiavgiftsloven** — VAT rules
- **SAF-T** — Mandatory digital audit file format since 2020
- **EHF** — Electronic invoicing for public sector (mandatory), private (recommended)
