# Norwegian Accounting Law - Comprehensive Reference for Accounting Software

> Legal foundation document for building accounting software compliant with Norwegian law.
> Last updated: April 2026. Always verify against current Lovdata sources before production deployment.

---

## Table of Contents

1. [Regnskapsloven (Accounting Act)](#1-regnskapsloven)
2. [Bokforingsloven (Bookkeeping Act)](#2-bokforingsloven)
3. [Bokforingsforskriften (Bookkeeping Regulation)](#3-bokforingsforskriften)
4. [Merverdiavgiftsloven (VAT Act)](#4-merverdiavgiftsloven)
5. [SAF-T Requirements](#5-saf-t-requirements)
6. [EHF / PEPPOL Electronic Invoicing](#6-ehf-peppol)
7. [Skatteforvaltningsloven (Tax Administration Act)](#7-skatteforvaltningsloven)
8. [NBS (Norwegian Bookkeeping Standards)](#8-nbs)
9. [NS 4102 Chart of Accounts](#9-ns-4102)
10. [MVA Codes and SAF-T Tax Code Mapping](#10-mva-codes)
11. [MVA Reporting Periods](#11-mva-reporting-periods)
12. [SAF-T XML Structure](#12-saf-t-xml-structure)
13. [Bank Formats (CAMT.053 / pain.001)](#13-bank-formats)
14. [KID Number Format](#14-kid-number)
15. [EHF Invoice Format](#15-ehf-invoice-format)
16. [Penalties and Sanctions](#16-penalties)
17. [Software vs User Responsibility Matrix](#17-responsibility-matrix)

---

## 1. Regnskapsloven

**Full name:** Lov om arsregnskap m.v. (Lov 1998-07-17 nr. 56)
**Lovdata:** https://lovdata.no/lov/1998-07-17-56

### 1.1 Who Must Keep Accounts (Regnskapsplikt)

The following entities have **regnskapsplikt** (obligation to prepare annual financial statements):

| Entity Type | Threshold |
|---|---|
| Aksjeselskap (AS) | Always |
| Allmennaksjeselskap (ASA) | Always |
| Samvirkeforetak / economic associations | Sales revenue > NOK 2,000,000 |
| Eierseksjonssameier | 21 or more sections |
| Other associations/foundations | Assets > NOK 20,000,000 OR > 20 FTEs |
| Enkeltpersonforetak (ENK) | If total assets > NOK 20M or > 20 FTEs |
| NUF (foreign branch) | Always |

**Distinction:** Bokforingsplikt (bookkeeping obligation) applies more broadly -- essentially all businesses with revenue > NOK 50,000 or anyone obligated to file tax returns / MVA reports. Regnskapsplikt additionally requires annual financial statements filed with Regnskapsregisteret.

### 1.2 Key Paragraphs

| Paragraph | Topic | Software Relevance |
|---|---|---|
| 1-2 | Regnskapsplikt definitions | Determine which users need full annual accounts |
| 3-1 | Arsregnskapet shall include results, balance, notes | System must produce these reports |
| 3-2 | Resultatregnskap (income statement) structure | Must follow prescribed format |
| 3-2a | Balance sheet structure | Must follow prescribed format |
| 3-3 | Arsberetning (annual report) | Required for larger entities |
| 4-1 | Fundamental accounting principles | System must support accrual accounting |
| 4-2 to 4-5 | Transaction principle, matching, cautious, best estimate | Core logic constraints |
| 5-1 to 5-4 | Valuation rules | Affects calculation engines |
| 6-1 to 6-3 | Small enterprises (sma foretak) exemptions | NRS 8 simplified rules |
| 8-2 | Filing with Regnskapsregisteret | Annual accounts submission deadline |

### 1.3 Fundamental Accounting Principles (4-1)

The software must support these principles:

1. **Transaksjonsprinsippet** -- transactions recorded at fair value at transaction date
2. **Opptjeningsprinsippet** -- revenue recognized when earned
3. **Sammenstillingsprinsippet** -- expenses matched with related revenue
4. **Forsiktighetsprinsippet** -- unrealized losses recognized, unrealized gains not
5. **Sikringsprinsippet** -- hedging transactions treated together
6. **God regnskapsskikk** -- generally accepted accounting practice
7. **Kongruensprinsippet** -- all income/expenses through the income statement
8. **Fortsatt drift** -- going concern assumption
9. **Konsistens** -- consistent application of principles across periods

### 1.4 Small Enterprise Exemptions (Sma Foretak)

Criteria for sma foretak (must satisfy at least two of three for two consecutive years):

- Sales revenue < NOK 70,000,000
- Balance sheet total < NOK 35,000,000
- Average number of employees < 50

Simplifications allowed:
- Can deviate from certain accrual/matching rules per NRS 8
- Significantly reduced note requirements
- No arsberetning requirement (if no employees, or fewer than 10)
- Can use simplified income statement format

### 1.5 Annual Accounts Content

The annual accounts (arsregnskap) must contain:

1. **Resultatregnskap** (Income Statement)
2. **Balanse** (Balance Sheet)
3. **Kontantstromoppstilling** (Cash Flow Statement) -- required for large enterprises
4. **Noter** (Notes to the accounts)
5. **Arsberetning** (Directors' Report) -- required for larger entities

### 1.6 Filing Deadlines

| Event | Deadline |
|---|---|
| Annual accounts approval | Within 6 months of fiscal year end |
| Filing with Regnskapsregisteret | July 31 (for Dec 31 fiscal year end) |
| Late filing fee | Starts accumulating after deadline |

### 1.7 Retention Period

Annual financial statements: **5 years** after the end of the fiscal year (aligned with bokforingsloven).

---

## 2. Bokforingsloven

**Full name:** Lov om bokforing (Lov 2004-11-19 nr. 73)
**Lovdata:** https://lovdata.no/lov/2004-11-19-73

### 2.1 Key Paragraphs for Software

| Paragraph | Topic | Software Requirement |
|---|---|---|
| 2 | Bokforingsplikt -- who must keep books | User classification logic |
| 3 | Bokforingsprinsipper | All transactions must be recorded |
| 4 | Grunnleggende bokforingsprinsipper | 10 fundamental principles (see below) |
| 5 | Spesifikasjoner av pliktig regnskapsrapportering | System must generate all required specifications |
| 6 | Dokumentasjon av regnskapsopplysninger | Documentation requirements for all entries |
| 7 | Bokforing og ajourhold | Timeliness requirements |
| 8 | Bokforingssprak | Norwegian, Swedish, Danish, or English |
| 9 | Regnskapssystem | System requirements |
| 10 | Bilag og dokumentasjon | Voucher documentation requirements |
| 11 | Oppbevaringsplikt | Retention periods |
| 13 | Oppbevaringstid og -sted | Specific retention times |
| 14 | Elektronisk oppbevaring | Electronic storage requirements |

### 2.2 Ten Fundamental Bookkeeping Principles (4)

The software **MUST** enforce or support:

1. **Regnskapslovgivning** -- compliance with accounting legislation
2. **Fullstendighet** -- all transactions must be recorded
3. **Realitet** -- recorded information must reflect actual transactions
4. **Noyaktighet** -- accurate recording of amounts and information
5. **Ajourhold** -- timely recording (see deadlines below)
6. **Dokumentasjon** -- every entry must have supporting documentation
7. **Sporbarhet (kontrollspor)** -- complete audit trail from report to source document and back
8. **Oppbevaring** -- secure storage for required periods
9. **Sikring** -- protection against unauthorized modification, destruction, and loss
10. **God bokforingsskikk** -- generally accepted bookkeeping practice

### 2.3 Ajourhold (Timeliness) Requirements (7)

| Requirement | Deadline |
|---|---|
| Minimum update frequency | Every 4 months |
| For each reporting period | Before reporting deadline |
| MVA-registered businesses (bimonthly) | Before each MVA term deadline |
| Businesses with employees | Monthly (before a-melding deadline on 5th) |
| Cash register businesses | Daily for cash sales |

**Software enforcement:** The system should warn users when bookkeeping is not up to date relative to reporting deadlines.

### 2.4 Required Specifications (5)

The system must be able to produce these specifications at any time for each reporting period (minimum every 4 months):

1. **Bokforingsspesifikasjon** -- posting specification (journal)
2. **Kontospesifikasjon** -- account specification (general ledger detail)
3. **Kundespesifikasjon** -- customer specification (accounts receivable subledger)
4. **Leverandorspesifikasjon** -- supplier specification (accounts payable subledger)
5. **Merverdiavgiftsspesifikasjon (MVA-spesifikasjon)** -- VAT specification
6. **Spesifikasjon av losnn og trekk** -- salary and deductions specification
7. **Spesifikasjon av uttak** -- specification of personal withdrawals
8. **Prosjektspesifikasjon** -- for projects exceeding 5x grunnbelopet (5G)

### 2.5 Documentation Requirements (10)

Every booked transaction must have documentation containing:

- Date
- Parties involved (buyer/seller with name and org. number)
- Clear description of goods/services
- Quantity and price
- Payment terms
- VAT amounts in NOK
- Reference number (sequential)

### 2.6 Retention Periods (13)

| Material Type | Retention Period | Category |
|---|---|---|
| Annual accounts | 5 years | Primary |
| Bookkeeping specifications | 5 years | Primary |
| Documentation of booked transactions (vouchers) | 5 years | Primary |
| Sales documents (invoices issued) | 5 years | Primary |
| Purchase documents (invoices received) | 5 years | Primary |
| Bank statements | 5 years | Primary |
| Contracts relevant to posted items | 5 years | Primary |
| Correspondence with accounting significance | 5 years | Primary |
| Chart of accounts | 5 years | Primary |
| Employee time records (salary basis) | 5 years | Primary |
| Documentation of accounting system | 3.5 years | Secondary |
| Documentation of balance sheet items | 3.5 years | Secondary |
| Price lists effective at transaction time | 3.5 years | Secondary |
| Agreements on electronic data interchange (EDI etc.) | 3.5 years | Secondary |

**From which date:** The retention period is counted from the end of the fiscal year.

**Electronic storage:** Permitted, provided the material remains readable and printable throughout the retention period. Must be stored in Norway or in a country with which Norway has a tax agreement. Must be protected against unauthorized modification.

---

## 3. Bokforingsforskriften

**Full name:** Forskrift om bokforing (FOR-2004-12-01-1558)
**Lovdata:** https://lovdata.no/dokument/SF/forskrift/2004-12-01-1558

### 3.1 Specification Requirements (Chapter 3)

#### 3.1.1 Kontospesifikasjon (3-1)

Must contain for each account per period:
- Account number and account name
- All items in ordered sequence
- Documentation date and documentation reference
- Opening and closing balance

#### 3.1.2 Kundespesifikasjon (3-1)

Must contain per period:
- Customer code and name
- All items in ordered sequence with date and reference
- Opening and closing balance
- **Must include cash sales > NOK 40,000 incl. MVA paid in cash**

#### 3.1.3 Leverandorspesifikasjon (3-1)

Must contain per period:
- Supplier code, name, and organization number
- All items in ordered sequence with date and reference
- Opening and closing balance
- **Must include cash purchases > NOK 40,000 incl. MVA paid in cash**

### 3.2 Sales Document Requirements (Chapter 5)

#### 3.2.1 Mandatory Content of Sales Documents / Invoices (5-1-1)

The software must ensure every invoice contains:

1. **Sequential number** (fortlopende nummer) -- unique, machine-assigned
2. **Seller's name and address**
3. **Seller's organization number** followed by "MVA" if VAT-registered
4. **Buyer's name and address** (or org. number)
5. **Description of goods/services** -- clear identification
6. **Quantity and unit price**
7. **Date of delivery / service period**
8. **Invoice date** (utstedelsesdato)
9. **Payment due date** and payment terms
10. **Total amount excl. MVA**
11. **MVA amount in NOK** -- broken down by rate
12. **Total amount incl. MVA**
13. "Foretaksregisteret" -- if seller is AS, ASA, or NUF

#### 3.2.2 Invoice Numbering (5-1-1 / 5-2-1)

- Numbers must be **pre-printed or machine-assigned**
- Must form a **controllable, unbroken sequence**
- Multiple number series allowed if each forms a complete sequence
- Gaps must be explainable

#### 3.2.3 Timing of Invoice Issuance (5-2-1)

- Invoices must be issued **within one month** after delivery
- For ongoing/periodic services: at least once per month or per agreed period
- For advance payments: upon receipt of payment

### 3.3 Documentation of Purchases (Chapter 5-5)

Purchase documentation must contain:
- Seller's name and address
- Seller's organization number
- Description of goods/services
- Date and amount
- MVA amount specified

### 3.4 Cash Register Requirements (Chapter 5a)

Businesses with cash sales must use a **product-declared cash register system** that meets kassasystemforskrifta (FOR-2015-12-18-1616):

- X-reports and Z-reports capability
- Daily Z-report production
- Receipt with specified content (amount, date, VAT, payment method, cash point ID)
- Integrated cash drawer per cash point (with exceptions)
- Product declaration filed with Skatteetaten

### 3.5 Electronic Availability (Chapter 7)

- All accounting material that must be kept 5 years must be **electronically available** for 3.5 years
- "Electronically available" means searchable, sortable, and retrievable in a structured format
- SAF-T export capability satisfies this requirement

### 3.6 System Documentation (7-2)

The accounting system must have documentation covering:
- Description of the system and its functions
- Input/output formats
- Calculation rules
- How the audit trail (kontrollspor) works
- Data flow between modules
- Access controls

---

## 4. Merverdiavgiftsloven

**Full name:** Lov om merverdiavgift (Lov 2009-06-19 nr. 58)
**Lovdata:** https://lovdata.no/lov/2009-06-19-58

### 4.1 VAT Rates (2026)

| Rate | Percentage | Applies To |
|---|---|---|
| Standard rate | **25%** | Most goods and services |
| Reduced rate (food) | **15%** | Foodstuffs, non-alcoholic beverages, water supply, sewerage |
| Reduced rate (services) | **12%** | Passenger transport, accommodation, cinema, museums, amusement parks, sports events, domestic ferries |
| Zero rate | **0%** | Exports, newspapers, books, periodicals, used electric vehicles |
| Exempt | N/A | Healthcare, education, financial services, residential rental, cultural services, insurance |

### 4.2 VAT Registration

| Threshold | Requirement |
|---|---|
| NOK 50,000 in taxable turnover (12 months) | Mandatory MVA registration |
| Voluntary registration | Possible for certain activities (e.g., property rental to VAT-registered tenants) |

### 4.3 Key Paragraphs

| Paragraph | Topic | Software Relevance |
|---|---|---|
| 1-3 | VAT rates | Rate lookup table |
| 2-1 | Taxable supply of goods | Classification of sales |
| 3-1 to 3-32 | Exempt supplies | Must not charge VAT on these |
| 6-1 to 6-36 | Zero-rated supplies | Zero rate but input VAT deductible |
| 8-1 | Input VAT deduction right | Full deduction for business use |
| 8-2 | Proportional deduction | Mixed use goods/services |
| 8-3 | No deduction | Personal use, representation, cars |
| 8-5 | Adjustment of input VAT (justeringsregler) | Capital goods over NOK 100,000 |
| 11-1 | Reporting periods | Bimonthly default |
| 11-4 | Annual reporting for small businesses | Under NOK 1M turnover |
| 15-10 | Invoice requirements for VAT purposes | Aligns with bokforingsforskriften 5-1-1 |

### 4.4 Reverse Charge (Snudd Avregning)

Import of services from abroad: buyer must calculate and report output VAT (reverse charge):
- Applies to services from foreign suppliers without Norwegian VAT registration
- Reported on the MVA return
- SAF-T codes 14, 15 (input side), 52 (output side)

### 4.5 VAT Deduction Rules

| Category | Deduction |
|---|---|
| Fully taxable business use | 100% deduction |
| Mixed use (taxable + exempt) | Proportional deduction (8-2) |
| Representation costs | No deduction |
| Personal motor vehicles | No deduction (with narrow exceptions) |
| Food/drink for employees | No deduction (with exceptions for overtime/travel) |

### 4.6 Justeringsregler (Adjustment Rules) for Capital Goods

For capital goods (kapitalvarer) > NOK 100,000:
- **Real property:** 10-year adjustment period
- **Machinery/equipment:** 5-year adjustment period
- If use changes between taxable/exempt, VAT must be adjusted proportionally

---

## 5. SAF-T Requirements

**Authority:** Skatteetaten (Norwegian Tax Administration)
**Legal basis:** Bokforingsforskriften 7-8
**Effective:** January 1, 2020 (version 1.20); January 1, 2025 (version 1.30 mandatory)

### 5.1 Who Must Comply

| Criteria | SAF-T Obligation |
|---|---|
| Bokforingspliktig + digital accounting + turnover >= NOK 5,000,000 | **Mandatory** |
| Bokforingspliktig + digital accounting + >= 600 transactions/year | **Mandatory** |
| Below both thresholds | **Exempt** |

### 5.2 When SAF-T Must Be Produced

SAF-T is **not** submitted routinely. It must be produced:
- Upon request during a **tax audit** (bokettersyn)
- Within a **reasonable time** after Skatteetaten requests it
- Covering the periods requested

### 5.3 Version Requirements

| Period | Required Schema Version |
|---|---|
| Fiscal year 2024 and earlier | Version 1.20 accepted |
| Fiscal year 2025 onwards | **Version 1.30 mandatory** |

### 5.4 Key Changes in Version 1.30

- New balance account structure for customers and suppliers
- New method for presenting VAT in transactions
- Some previously voluntary elements become **mandatory**
- Three new fields added
- Technical adjustments (data types, character limits)
- **GroupingCategory** checkbox is mandatory from Jan 1, 2025

### 5.5 Technical Requirements

- Format: **XML** validated against XSD schema
- Encoding: **UTF-8**
- Max file size: **2 GB** per individual XML file
- Can be split: Header+MasterFiles in first file, GeneralLedgerEntries in subsequent files
- All MasterFiles nodes must be in the **first file**
- Every XML file must validate against the schema

### 5.6 Official Resources

| Resource | URL |
|---|---|
| Skatteetaten SAF-T page | https://www.skatteetaten.no/en/business-and-organisation/start-and-run/best-practices-accounting-and-cash-register-systems/saf-t-financial/ |
| GitHub repo (XSD, code lists, test files) | https://github.com/Skatteetaten/saf-t |
| Standard Tax Codes CSV | https://github.com/Skatteetaten/saf-t/blob/master/Standard%20Tax%20Codes/CSV/Standard_Tax_Codes.csv |
| Standard Accounts 4-char CSV | https://github.com/Skatteetaten/saf-t/blob/master/General%20Ledger%20Standard%20Accounts/CSV/General_Ledger_Standard_Accounts_4_character.csv |
| Technical documentation | https://www.skatteetaten.no/en/business-and-organisation/start-and-run/best-practices-accounting-and-cash-register-systems/saf-t-financial/documentation/ |

---

## 6. EHF / PEPPOL

### 6.1 Current Status and Timeline

| Date | Requirement |
|---|---|
| April 2, 2019 | **B2G mandatory** -- all public authorities must receive/process e-invoices (EHF) |
| January 1, 2027 | **B2B mandatory issuance** -- all businesses must issue e-invoices |
| January 1, 2030 | **Mandatory digital bookkeeping and e-invoice reception** |

### 6.2 Format Requirements

| Standard | Details |
|---|---|
| Format | EHF Billing 3.0 = PEPPOL BIS Billing 3.0 |
| Base standard | UBL 2.1 (Universal Business Language) |
| EU compliance | EN 16931 |
| Network | PEPPOL eDelivery Network |
| Access point | Must use a registered PEPPOL Access Point provider |

### 6.3 When EHF Is Required

- **Now:** All invoices to Norwegian public sector entities
- **2027:** All B2B invoices between Norwegian businesses
- **2030:** All businesses must be able to receive and automatically process e-invoices

### 6.4 EHF Invoice Mandatory Elements

See [Section 15](#15-ehf-invoice-format) for complete field list.

### 6.5 Legal Basis

- Forskrift om elektronisk faktura i offentlige anskaffelser (FOR-2019-04-01-444)
- Planned amendments to bokforingsloven for B2B mandate (expected 2026)

---

## 7. Skatteforvaltningsloven

**Full name:** Lov om skatteforvaltning (Lov 2016-05-27 nr. 14)
**Lovdata:** https://lovdata.no/lov/2016-05-27-14

### 7.1 Key Reporting Obligations

| Report | Frequency | Deadline | Who |
|---|---|---|---|
| **MVA-melding** | Bimonthly (default) | 1 month + 10 days after term end | VAT-registered businesses |
| **MVA-melding** | Annual (if approved) | March 10 following year | Small businesses < NOK 1M |
| **A-melding** | Monthly | 5th of following month | All employers |
| **Skattemelding (personal)** | Annual | End of April | Individuals |
| **Skattemelding (company)** | Annual | End of May | AS, ENK with accounting obligation |
| **Arsoppgjor** | Annual | May 31 | Companies |
| **Aksjonaerregisteroppgaven** | Annual | January 31 | AS/ASA |

### 7.2 Key Paragraphs

| Paragraph | Topic | Software Relevance |
|---|---|---|
| 8-1 | General duty to provide information | Must support data export |
| 8-2 | Skattemelding for income and wealth | Annual tax return support |
| 8-3 | Skattemelding for MVA | VAT return generation |
| 8-6 | A-melding | Payroll reporting |
| 8-12 | Third-party reporting | Interest, dividends, etc. |
| 10-1 | Payment deadlines | Track payment obligations |
| 14-3 to 14-7 | Administrative penalties | Penalty calculations |

### 7.3 A-melding Content

Monthly employer report via Altinn containing:
- Employee personal details (fodselsnummer)
- Salary payments with tax deduction codes
- Employer's tax (arbeidsgiveravgift) calculation
- Tax withholding (forskuddstrekk) amounts
- Pension and benefit information
- Employment details (start/end dates, percentage)

### 7.4 Digital Submission

All reporting to Skatteetaten is digital via:
- **Altinn** portal (altinn.no) -- most filings
- **Skatteetaten API** -- for system-to-system integration
- **ID-porten** authentication required

---

## 8. NBS (Norsk Bokforingsstandard)

**Publisher:** Norsk Regnskapsstiftelse (Norwegian Accounting Standards Board)
**Website:** https://www.regnskapsstiftelsen.no/bokforing/bokforingsstandarder/

### 8.1 Complete List of Standards

| Standard | Title (Norwegian) | Title (English) | Last Updated |
|---|---|---|---|
| NBS 1 | Sikring av regnskapsmateriale | Security of accounting material | April 2025 |
| NBS 2 | Kontrollsporet | Audit trail | April 2025 |
| NBS 3 | Elektronisk tilgjengelighet i 3,5 ar | Electronic accessibility for 3.5 years | April 2025 |
| NBS 4 | Elektronisk fakturering | Electronic invoicing | April 2025 |
| NBS 5 | Dokumentasjon av balansen | Documentation of balance sheet | April 2025 |
| NBS 6 | Bruk av tekstbehandlings- og regnearkprogrammer | Use of text processing and spreadsheet programs | November 2025 |
| NBS 7 | Dokumentasjon av betalingstransaksjoner | Documentation of payment transactions | November 2025 |
| NBS 8 | Sideordnede spesifikasjoner | Supplementary specifications | November 2025 |

### 8.2 NBS 1 -- Security of Accounting Material

Key requirements for software:
- Data must be protected against unauthorized access, modification, destruction, and loss
- Backup procedures must be in place
- Access controls and user authentication
- Logging of changes to accounting data
- Physical and logical security measures

### 8.3 NBS 2 -- Audit Trail (Kontrollspor)

**Critical for software design.** The audit trail must:
- Enable tracing from any financial report **down to** the individual transaction and its source document
- Enable tracing from any source document **up to** the reports it affects
- Be documented (how system-generated entries can be verified)
- Include references between related records
- Cover all system-generated automatic postings

The audit trail documentation should:
- Take its starting point in mandatory financial reporting
- Describe how the trail can be followed from report items via posted information to source documents
- Cover both manual and automatic/system-generated entries

### 8.4 NBS 3 -- Electronic Accessibility

- All primary accounting material must be electronically accessible for **3.5 years**
- "Electronically accessible" means: searchable, sortable, retrievable in structured format
- SAF-T export capability satisfies this requirement
- Must be possible to reproduce data in the same structured format throughout the period

### 8.5 NBS 4 -- Electronic Invoicing

- Covers requirements for electronic invoice formats
- EHF/PEPPOL compliance
- Requirements for converting paper invoices to electronic format
- Scanning and OCR requirements
- Original document retention rules when converting formats

### 8.6 NBS 5 -- Balance Sheet Documentation

- All balance sheet items must be documented/reconciled at fiscal year end
- Bank accounts reconciled against bank statements
- Accounts receivable/payable reconciled against subledgers
- Inventory physically counted or reconciled
- Fixed assets register maintained

### 8.7 NBS 6 -- Spreadsheets

- Spreadsheets used as part of bookkeeping must be protected against modification
- Must be able to reproduce the spreadsheet as it was at any point during the retention period
- Version control is required

### 8.8 NBS 7 -- Payment Transaction Documentation

- All payments must be documented
- Bank transaction references must link to accounting entries
- Electronic payment files must be retained

### 8.9 NBS 8 -- Supplementary Specifications

- Sideordnede spesifikasjoner (sub-specifications) can be used for customer/supplier items
- Must be reconcilable to the main specifications
- Permitted following regulatory amendment of June 27, 2024

---

## 9. NS 4102 Chart of Accounts

**Standard:** NS 4102:2023 (current version)
**Publisher:** Standard Norge
**URL:** https://standard.no/fagomrader/kontoplan-for-regnskap/

### 9.1 Account Number Structure

```
[X][Y][ZZ]
 |  |  |
 |  |  +-- Individual account (00-99)
 |  +----- Account group (0-9)
 +-------- Account class (1-8)
```

Four-digit account numbers. The 2023 revision uses five digits (ending in 0) for large enterprises; can be truncated to four for systems that only support four digits.

Total: **531 accounts** (337 result accounts in classes 3-8, 194 balance accounts in classes 1-2).

### 9.2 Account Classes Overview

| Class | Name (NO) | Name (EN) | Financial Statement |
|---|---|---|---|
| 1 | Eiendeler | Assets | Balance Sheet |
| 2 | Egenkapital og gjeld | Equity and Liabilities | Balance Sheet |
| 3 | Salgsinntekt og driftsinntekt | Sales and Operating Revenue | Income Statement |
| 4 | Varekostnad | Cost of Goods | Income Statement |
| 5 | Lonnskostnad | Payroll Costs | Income Statement |
| 6 | Avskrivninger, nedskrivninger og andre driftskostnader | Depreciation and Other Operating Costs | Income Statement |
| 7 | Annen driftskostnad | Other Operating Expenses | Income Statement |
| 8 | Finansinntekter, finanskostnader, skatt, ekstraordinaere poster | Financial Items, Tax, Extraordinary Items | Income Statement |

### 9.3 Detailed Account Groups

#### Class 1 -- Assets (Eiendeler)

| Group | Range | Description |
|---|---|---|
| 10 | 1000-1099 | Immaterielle eiendeler (Intangible assets) |
| 11 | 1100-1199 | Tomter, bygninger og annen fast eiendom (Land, buildings, real property) |
| 12 | 1200-1299 | Transportmidler, maskiner, inventar (Vehicles, machinery, fixtures) |
| 13 | 1300-1399 | Finansielle anleggsmidler (Financial fixed assets) |
| 14 | 1400-1499 | Varelager og forskudd til leverandorer (Inventory and advances to suppliers) |
| 15 | 1500-1599 | Kortsiktige fordringer (Short-term receivables) |
| 16 | 1600-1699 | Merverdiavgift, opptjente inntekter (VAT receivables, accrued revenue) |
| 17 | 1700-1799 | Forskuddsbetalt kostnad, pabegynt arbeid (Prepaid expenses, WIP) |
| 18 | 1800-1899 | Kortsiktige finansinvesteringer (Short-term financial investments) |
| 19 | 1900-1999 | Bankinnskudd, kontanter og lignende (Bank deposits, cash) |

**Key accounts:**
- 1500: Kundefordringer (Accounts receivable)
- 1900: Bankinnskudd / Kasse (Bank/Cash)
- 1920: Bankinnskudd (Bank deposits)
- 1930: Bankinnskudd skattetrekk (Tax withholding bank account)

#### Class 2 -- Equity and Liabilities (Egenkapital og gjeld)

| Group | Range | Description |
|---|---|---|
| 20 | 2000-2099 | Innskutt egenkapital (Contributed equity / share capital) |
| 21 | 2100-2199 | Opptjent egenkapital (Retained earnings) |
| 22 | 2200-2299 | Langsiktig gjeld (Long-term debt) |
| 23 | 2300-2399 | Annen langsiktig gjeld (Other long-term debt) |
| 24 | 2400-2499 | Leverandorgjeld (Accounts payable) |
| 25 | 2500-2599 | Skattetrekk og offentlige avgifter (Tax withholding and public duties) |
| 26 | 2600-2699 | Skyldig merverdiavgift (VAT payable) |
| 27 | 2700-2799 | Skyldig arbeidsgiveravgift, lonnsrelatert gjeld (Employer tax, payroll liabilities) |
| 28 | 2800-2899 | Annen kortsiktig gjeld (Other short-term liabilities) |
| 29 | 2900-2999 | Annen gjeld og egenkapitalposter (Other liabilities and equity items) |

**Key accounts:**
- 2000: Aksjekapital (Share capital)
- 2050: Annen innskutt egenkapital
- 2400: Leverandorgjeld (Accounts payable)
- 2500: Skattetrekk (Tax deductions payable)
- 2600: Utgaende merverdiavgift (Output VAT)
- 2610: Inngaende merverdiavgift (Input VAT -- contra)
- 2700: Utgaende merverdiavgift, oppgjorskonto (MVA settlement)
- 2710: Skyldig arbeidsgiveravgift (Employer tax payable)
- 2770: Skyldig arbeidsgiveravgift av feriepenger (Employer tax on holiday pay)
- 2780: Palapt feriepenger (Accrued holiday pay)
- 2800: Avsatt utbytte (Dividend payable)

#### Class 3 -- Sales Revenue (Salgsinntekt)

| Group | Range | Description |
|---|---|---|
| 30 | 3000-3099 | Salgsinntekt, avgiftspliktig (Sales revenue, VAT liable) |
| 31 | 3100-3199 | Salgsinntekt, avgiftsfri (Sales revenue, VAT exempt) |
| 32 | 3200-3299 | Salgsinntekt, utenfor avgiftsomradet (Sales outside VAT scope) |
| 33 | 3300-3399 | Offentlige tilskudd/refusjoner (Public grants/refunds) |
| 34 | 3400-3499 | Offentlige avgifter vedr. omsetning (Public duties on sales) |
| 35 | 3500-3599 | Uopptjent inntekt (Deferred revenue) |
| 36 | 3600-3699 | Leieinntekt (Rental income) |
| 37-38 | 3700-3899 | Annen driftsinntekt (Other operating income) |
| 39 | 3900-3999 | Annen driftsrelatert inntekt (Other operating-related income) |

#### Class 4 -- Cost of Goods (Varekostnad)

| Group | Range | Description |
|---|---|---|
| 40 | 4000-4099 | Varekjop for videreforhandling (Purchases for resale) |
| 41 | 4100-4199 | Varekjop for egenproduksjon (Purchases for own production) |
| 42 | 4200-4299 | Halvfabrikata (Semi-finished goods) |
| 43 | 4300-4399 | Innkjop av varer/tjenester for viderefakturering (Purchases for re-invoicing) |
| 44-45 | 4400-4599 | Fremmedytelser, underentreprise (Subcontracting) |
| 46-47 | 4600-4799 | Annet varekjop (Other purchases) |
| 48 | 4800-4899 | Frakt, toll etc. (Freight, customs) |
| 49 | 4900-4999 | Beholdningsendring (Inventory change) |

#### Class 5 -- Payroll Costs (Lonnskostnad)

| Group | Range | Description |
|---|---|---|
| 50 | 5000-5099 | Lonn til ansatte (Salaries) |
| 51 | 5100-5199 | Feriepenger (Holiday pay) |
| 52 | 5200-5299 | Annen oppgavepliktig godtgjorelse (Other reportable compensation) |
| 53 | 5300-5399 | Annen oppgavepliktig godtgjorelse (cont.) |
| 54 | 5400-5499 | Arbeidsgiveravgift (Employer tax / social security) |
| 55-58 | 5500-5899 | Annen personalkostnad (Other personnel costs, pensions, insurance) |
| 59 | 5900-5999 | Annen personalkostnad (Other personnel costs) |

#### Class 6 -- Depreciation and Other Operating Costs

| Group | Range | Description |
|---|---|---|
| 60 | 6000-6099 | Avskrivning (Depreciation) |
| 61 | 6100-6199 | Frakt og transport (Freight and transport) |
| 62 | 6200-6299 | Energi, brensel etc. (Energy, fuel) |
| 63 | 6300-6399 | Kostnader lokaler (Premises costs / rent) |
| 64 | 6400-6499 | Leie maskiner, inventar etc. (Lease of equipment) |
| 65 | 6500-6599 | Verktoy, inventar u/aktiveringsplikt (Tools, minor equipment) |
| 66 | 6600-6699 | Reparasjon og vedlikehold (Repair and maintenance) |
| 67 | 6700-6799 | Fremmedtjenester (External services, auditor, lawyer) |
| 68 | 6800-6899 | Kontorkostnad, trykksaker etc. (Office supplies) |
| 69 | 6900-6999 | Kommunikasjon (Telephone, postage, data) |

#### Class 7 -- Other Operating Expenses

| Group | Range | Description |
|---|---|---|
| 70 | 7000-7099 | Reisekostnad (Travel expenses) |
| 71 | 7100-7199 | Bilkostnad, transportmidler (Vehicle costs) |
| 72 | 7200-7299 | Provisjonskostnad (Commissions) |
| 73 | 7300-7399 | Salgskostnad (Sales costs, advertising, marketing) |
| 74 | 7400-7499 | Kontingent og gave (Memberships and gifts) |
| 75 | 7500-7599 | Forsikringspremie (Insurance premiums) |
| 76-77 | 7600-7799 | Annen kostnad (Other costs, licenses, patents) |
| 78 | 7800-7899 | Tap pa fordringer (Bad debt losses) |
| 79 | 7900-7999 | Annen driftskostnad (Other operating expenses) |

#### Class 8 -- Financial Items and Tax

| Group | Range | Description |
|---|---|---|
| 80 | 8000-8099 | Finansinntekter (Financial income, interest income) |
| 81 | 8100-8199 | Annen finansinntekt (Other financial income, dividends, gains) |
| 82-83 | 8200-8399 | Verdiendring finansielle instrumenter (Value changes, financial instruments) |
| 84 | 8400-8499 | Finanskostnad (Financial expense, interest expense) |
| 85 | 8500-8599 | Annen finanskostnad (Other financial expense, losses) |
| 86-87 | 8600-8799 | Verdiendring/nedskrivning finansielle eiendeler (Write-downs, financial assets) |
| 88 | 8800-8899 | Arsresultat (Net profit/loss, extraordinary items) |
| 89 | 8900-8999 | Skattekostnad (Tax expense, including deferred tax) |

### 9.4 SAF-T Standard Account Mapping

Every account in the system must be mapped to a SAF-T standard account. Skatteetaten provides both 2-character and 4-character standard account lists:
- 2-character: https://github.com/Skatteetaten/saf-t/blob/master/General%20Ledger%20Standard%20Accounts/CSV/General_Ledger_Standard_Accounts_2_character.csv
- 4-character: https://github.com/Skatteetaten/saf-t/blob/master/General%20Ledger%20Standard%20Accounts/CSV/General_Ledger_Standard_Accounts_4_character.csv

---

## 10. MVA Codes and SAF-T Tax Code Mapping

### 10.1 Complete SAF-T Standard Tax Codes

The following are the standard tax codes defined by Skatteetaten. Internal system MVA codes must be mapped to these for SAF-T export and MVA-melding.

**Official source:** https://github.com/Skatteetaten/saf-t/blob/master/Standard%20Tax%20Codes/CSV/Standard_Tax_Codes.csv

#### Output VAT Codes (Utgaende MVA -- Sales)

| Code | Description (NO) | Description (EN) | Rate |
|---|---|---|---|
| 3 | Utgaende mva, alminnelig sats | Output VAT, standard rate | 25% |
| 31 | Utgaende mva, middels sats | Output VAT, middle reduced rate | 15% |
| 33 | Utgaende mva, lav sats | Output VAT, low reduced rate | 12% |
| 5 | Utgaende mva, fritt utforsel | Output VAT, export (zero-rated) | 0% |
| 6 | Utgaende mva, omsetning utenfor mva-loven | Output VAT, outside VAT Act scope | 0% |
| 51 | Utgaende mva, innforsel av varer, alminnelig sats | Output VAT, import of goods, standard rate | 25% |
| 52 | Utgaende mva, innforsel av varer, middels sats | Output VAT, import of goods, middle rate | 15% |

#### Input VAT Codes (Inngaende MVA -- Purchases)

| Code | Description (NO) | Description (EN) | Rate |
|---|---|---|---|
| 1 | Inngaende mva, alminnelig sats | Input VAT, standard rate | 25% |
| 11 | Inngaende mva, middels sats | Input VAT, middle reduced rate | 15% |
| 13 | Inngaende mva, lav sats | Input VAT, low reduced rate | 12% |
| 14 | Inngaende mva, innforsel av varer, alminnelig sats | Input VAT, import of goods, standard rate | 25% |
| 15 | Inngaende mva, innforsel av varer, middels sats | Input VAT, import of goods, middle rate | 15% |

#### Special Codes

| Code | Description (NO) | Description (EN) | Rate |
|---|---|---|---|
| 0 | Ingen mva-behandling | No VAT treatment | N/A |
| 20 | Ingen mva-behandling (kjop) | No VAT treatment (purchases) | N/A |
| 21 | Kjop med kompensasjonsrett | Purchase with VAT compensation right | 25% |
| 22 | Kjop med kompensasjonsrett, middels sats | Purchase with compensation right, middle rate | 15% |
| 25 | Kjop med kompensasjonsrett, lav sats | Purchase with compensation right, low rate | 12% |

#### Reverse Charge Codes (Snudd avregning)

| Code | Description | Rate |
|---|---|---|
| 81 | Kjop av tjenester fra utlandet, alminnelig sats (inngaende) | 25% |
| 82 | Kjop av tjenester fra utlandet, alminnelig sats (utgaende) | 25% |
| 83 | Kjop av tjenester fra utlandet med kompensasjon, alminnelig sats | 25% |
| 86 | Kjop av klimakvoter/gull, alminnelig sats (inngaende) | 25% |
| 87 | Kjop av klimakvoter/gull, alminnelig sats (utgaende) | 25% |
| 88 | Kjop av klimakvoter/gull med kompensasjon | 25% |
| 91 | Kjop av varer fra utlandet, alminnelig sats (inngaende) | 25% |
| 92 | Kjop av varer fra utlandet, alminnelig sats (utgaende) | 25% |

#### Fish Codes (Rafi -- Raw Fish)

| Code | Description | Rate |
|---|---|---|
| 12 | Inngaende mva, rafisksats | 11.11% |
| 32 | Utgaende mva, rafisksats | 11.11% |

### 10.2 MVA-melding Code Mapping

The MVA-melding (VAT return) uses these same SAF-T standard codes. The digital VAT return filed via Altinn maps transaction data aggregated by these codes. There are approximately **25 mandatory codes** (if there is activity) and additional voluntary codes.

### 10.3 Software Implementation Notes

- Internal MVA codes can use any numbering scheme
- Each internal code **must** be mapped to a SAF-T StandardTaxCode
- The mapping is stored in the SAF-T TaxCodeDetails section under MasterFiles
- The XML element `mvaKodeRegnskapssystem` maps internal codes to standard codes

---

## 11. MVA Reporting Periods

### 11.1 Standard Bimonthly Terms (Alminnelig)

| Term | Period | Filing/Payment Deadline |
|---|---|---|
| 1. termin | January -- February | April 10 |
| 2. termin | March -- April | June 10 |
| 3. termin | May -- June | August 31 |
| 4. termin | July -- August | October 10 |
| 5. termin | September -- October | December 10 |
| 6. termin | November -- December | February 10 (following year) |

**Note:** 3. termin has a later deadline (August 31) due to summer holidays.

### 11.2 Annual Term (Arsoppgave)

| Criteria | Details |
|---|---|
| Eligibility | VAT-registered for at least 1 year AND turnover < NOK 1,000,000 |
| Application deadline | February 1 of the fiscal year |
| Period | January -- December |
| Filing/Payment deadline | **March 10** following year |

### 11.3 Special Schemes

| Scheme | Period | Who |
|---|---|---|
| Primary industries (jordbruk, skogbruk, fiske) | Annual | Farmers, foresters, fishers |
| Monthly reporting | Monthly | Certain large businesses (voluntary) |

### 11.4 Filing Requirements

- MVA-melding must be filed **even if no activity** (null-melding)
- Filed digitally via Altinn / Skatteetaten
- Based on SAF-T codes since 2022 (digital MVA-melding)
- Payment must be made by the same deadline as filing
- Interest charged on late payment

---

## 12. SAF-T XML Structure

### 12.1 High-Level Structure

```xml
<?xml version="1.0" encoding="UTF-8"?>
<AuditFile xmlns="urn:StandardAuditFile-Taxation-Financial:NO">
  <Header>...</Header>
  <MasterFiles>...</MasterFiles>
  <GeneralLedgerEntries>...</GeneralLedgerEntries>
</AuditFile>
```

### 12.2 Header (Mandatory)

| Element | Required | Description |
|---|---|---|
| AuditFileVersion | Yes | "1.30" |
| AuditFileCountry | Yes | "NO" |
| AuditFileDateCreated | Yes | Date file was generated |
| SoftwareCompanyName | Yes | Name of software vendor |
| SoftwareID | Yes | Name of the software |
| SoftwareVersion | Yes | Version of the software |
| Company | Yes | Company information block |
| Company/RegistrationNumber | Yes | Organization number |
| Company/Name | Yes | Company name |
| Company/Address | Yes | Address block |
| Company/Contact | No | Contact information |
| Company/TaxRegistration | Yes | VAT registration info |
| Company/BankAccount | No | Bank account info |
| DefaultCurrencyCode | Yes | "NOK" |
| SelectionCriteria | Yes | Period start/end dates |
| HeaderComment | No | Free text |
| TaxAccountingBasis | Yes | "A" (general), "S" (tax accounting) |
| TaxEntity | No | Tax entity reference |
| UserID | No | User who generated file |

### 12.3 MasterFiles (Mandatory)

Must include all master data used in the period:

| Section | Required | Content |
|---|---|---|
| GeneralLedgerAccounts | Yes | All GL accounts with StandardAccountID mapping |
| Customers | Yes (if any) | Customer master data |
| Suppliers | Yes (if any) | Supplier master data |
| TaxTable | Yes | Tax code definitions with StandardTaxCode mapping |
| AnalysisTypeTable | Conditional | Analysis dimensions (cost centers, projects) |
| Owners | No | Company owner information |

#### GeneralLedgerAccounts Required Fields

| Element | Required | Description |
|---|---|---|
| AccountID | Yes | Account number |
| AccountDescription | Yes | Account name |
| StandardAccountID | Yes | Mapping to NS 4102 standard account |
| AccountType | Yes | "GL" |
| OpeningDebitBalance / OpeningCreditBalance | Yes | Opening balance |
| ClosingDebitBalance / ClosingCreditBalance | Yes | Closing balance |
| GroupingCategory | Yes (v1.30) | Reporting category (e.g., RF-1167) |
| GroupingCode | Yes (v1.30) | Code within grouping |

#### Customer/Supplier Required Fields

| Element | Required | Description |
|---|---|---|
| CustomerID / SupplierID | Yes | Internal ID |
| Name | Yes | Full name |
| Address | Yes | Address block |
| Contact | No | Contact info |
| TaxRegistration | No | Organization number |
| OpeningDebitBalance / OpeningCreditBalance | Yes (v1.30) | Opening balance |
| ClosingDebitBalance / ClosingCreditBalance | Yes (v1.30) | Closing balance |

#### TaxTable Required Fields

| Element | Required | Description |
|---|---|---|
| TaxCodeDetails/TaxCode | Yes | Internal tax code |
| TaxCodeDetails/Description | Yes | Description |
| StandardTaxCode | Yes | SAF-T standard code mapping |
| TaxPercentage | Yes | Rate |
| Country | Yes | "NO" |
| BaseRate | No | |

### 12.4 GeneralLedgerEntries (Mandatory)

| Element | Required | Description |
|---|---|---|
| NumberOfEntries | Yes | Total count of journal entries |
| TotalDebit | Yes | Sum of all debits |
| TotalCredit | Yes | Sum of all credits |
| Journal | Yes | Container for entries |
| Journal/JournalID | Yes | Journal identifier |
| Journal/Description | Yes | Journal name |
| Journal/Type | Yes | Journal type |
| Transaction | Yes | Individual journal entry |
| Transaction/TransactionID | Yes | Unique entry ID |
| Transaction/Period | Yes | Accounting period (1-12) |
| Transaction/TransactionDate | Yes | Posting date |
| Transaction/Description | Yes | Entry description |
| Transaction/SystemEntryDate | Yes | Date entered in system |
| Transaction/GLPostingDate | Yes | GL posting date |
| Line | Yes | Individual line in entry |
| Line/RecordID | Yes | Unique line ID |
| Line/AccountID | Yes | GL account |
| Line/DebitAmount / CreditAmount | Yes | Amount |
| Line/Description | Yes | Line description |
| Line/CustomerID / SupplierID | Conditional | If customer/supplier transaction |
| Line/TaxInformation | Conditional | VAT details if applicable |

#### TaxInformation Sub-Elements (per line)

| Element | Required | Description |
|---|---|---|
| TaxType | Yes | "MVA" |
| TaxCode | Yes | Internal tax code |
| TaxPercentage | Yes | Rate applied |
| TaxBase | Yes | Base amount |
| TaxAmount | Yes | Tax amount |

### 12.5 Norwegian-Specific Extensions (v1.30)

- **GroupingCategory / GroupingCode:** Maps accounts to Norwegian tax form categories (e.g., RF-1167 Naringsoppgave)
- **Customer/Supplier opening and closing balances:** Mandatory in v1.30
- **VAT presentation change:** New method for presenting VAT at transaction line level

---

## 13. Bank Formats

### 13.1 CAMT.053 -- Bank-to-Customer Account Statement

| Property | Value |
|---|---|
| Standard | ISO 20022 |
| Message type | camt.053.001.02 |
| Purpose | Import daily bank statements into accounting system |
| Encoding | UTF-8 |
| Direction | Bank -> Customer |

#### Key Elements

```xml
<BkToCstmrStmt>
  <GrpHdr>              <!-- Group header -->
    <MsgId/>            <!-- Message ID -->
    <CreDtTm/>          <!-- Creation date/time -->
  </GrpHdr>
  <Stmt>                <!-- Statement -->
    <Id/>               <!-- Statement ID -->
    <ElctrncSeqNb/>     <!-- Sequence number -->
    <LglSeqNb/>         <!-- Legal sequence number -->
    <CreDtTm/>          <!-- Creation date -->
    <Acct>              <!-- Account info -->
      <Id><IBAN/></Id>  <!-- Account IBAN -->
      <Ccy/>            <!-- Currency -->
    </Acct>
    <Bal>               <!-- Balance(s) -->
      <Tp><CdOrPrtry><Cd/></CdOrPrtry></Tp>  <!-- OPBD/CLBD -->
      <Amt Ccy="NOK"/>  <!-- Amount -->
      <CdtDbtInd/>      <!-- CRDT/DBIT -->
      <Dt><Dt/></Dt>    <!-- Date -->
    </Bal>
    <Ntry>              <!-- Entry (transaction) -->
      <Amt Ccy="NOK"/>  <!-- Amount -->
      <CdtDbtInd/>      <!-- CRDT/DBIT -->
      <BookgDt/>        <!-- Booking date -->
      <ValDt/>          <!-- Value date -->
      <BkTxCd/>         <!-- Bank transaction code -->
      <NtryDtls>
        <TxDtls>
          <Refs>
            <EndToEndId/>   <!-- Reference from pain.001 -->
            <MsgId/>
          </Refs>
          <RmtInf>
            <Strd>
              <CdtrRefInf>
                <Ref/>      <!-- KID number -->
              </CdtrRefInf>
            </Strd>
          </RmtInf>
        </TxDtls>
      </NtryDtls>
    </Ntry>
  </Stmt>
</BkToCstmrStmt>
```

#### Software Requirements for CAMT.053 Import

- Parse XML and extract all entries (Ntry)
- Match KID numbers to outstanding invoices for auto-reconciliation
- Import opening/closing balances for bank reconciliation
- Handle multiple statements per file
- Handle multiple currencies (though NOK is primary)

### 13.2 pain.001 -- Customer Credit Transfer Initiation

| Property | Value |
|---|---|
| Standard | ISO 20022 |
| Message type | pain.001.001.03 |
| Purpose | Send payment instructions from accounting system to bank |
| Encoding | UTF-8 |
| Direction | Customer -> Bank |

#### Key Elements

```xml
<CstmrCdtTrfInitn>
  <GrpHdr>
    <MsgId/>                  <!-- Unique message ID -->
    <CreDtTm/>                <!-- Creation date/time -->
    <NbOfTxs/>                <!-- Number of transactions -->
    <CtrlSum/>                <!-- Control sum (total amount) -->
    <InitgPty><Nm/></InitgPty>
  </GrpHdr>
  <PmtInf>                    <!-- Payment information block -->
    <PmtInfId/>               <!-- Payment info ID -->
    <PmtMtd>TRF</PmtMtd>     <!-- Transfer -->
    <NbOfTxs/>
    <CtrlSum/>
    <PmtTpInf/>               <!-- Payment type info -->
    <ReqdExctnDt/>            <!-- Requested execution date -->
    <Dbtr><Nm/></Dbtr>        <!-- Debtor (payer) -->
    <DbtrAcct>
      <Id><IBAN/></Id>        <!-- Payer's IBAN -->
    </DbtrAcct>
    <DbtrAgt>
      <FinInstnId><BIC/></FinInstnId>  <!-- Payer's bank BIC -->
    </DbtrAgt>
    <CdtTrfTxInf>             <!-- Individual payment -->
      <PmtId>
        <InstrId/>            <!-- Instruction ID -->
        <EndToEndId/>         <!-- End-to-end reference -->
      </PmtId>
      <Amt>
        <InstdAmt Ccy="NOK"/> <!-- Amount -->
      </Amt>
      <CdtrAgt>
        <FinInstnId><BIC/></FinInstnId>  <!-- Beneficiary bank -->
      </CdtrAgt>
      <Cdtr><Nm/></Cdtr>     <!-- Creditor (payee) -->
      <CdtrAcct>
        <Id><IBAN/></Id>      <!-- Payee's IBAN or BBAN -->
      </CdtrAcct>
      <RmtInf>
        <Strd>
          <CdtrRefInf>
            <Ref/>            <!-- KID number -->
          </CdtrRefInf>
        </Strd>
      </RmtInf>
    </CdtTrfTxInf>
  </PmtInf>
</CstmrCdtTrfInitn>
```

#### Norwegian-Specific Considerations

- Norwegian bank accounts can be specified as BBAN (11-digit) or IBAN
- KID number is placed in the structured remittance information (CdtrRefInf/Ref)
- Most Norwegian banks support pain.001.001.03 via their corporate banking portals
- Domestic NOK payments typically use BBAN; international payments use IBAN+BIC

---

## 14. KID Number Format

### 14.1 Overview

| Property | Value |
|---|---|
| Full name | Kundeidentifikasjon (Customer Identification) |
| Length | 2-25 digits |
| Last digit | Check digit (MOD10 or MOD11) |
| Purpose | Identify customer and invoice for automatic payment matching |
| Agreement | Must be agreed with bank before use |

### 14.2 Structure

```
[Customer/Invoice reference digits][Check digit]
```

The content and arrangement of digits (other than the check digit) is determined by agreement between the business and their bank. Common patterns:

- Customer number + invoice number + check digit
- Invoice number only + check digit
- Date-based reference + check digit

### 14.3 MOD10 (Luhn) Algorithm

Used for generating the check digit. Steps:

1. Starting from the **rightmost** digit of the payload (before check digit), moving left
2. **Double** every second digit
3. If the doubled value > 9, **subtract 9** (or sum the two digits)
4. Sum all digits (doubled and undoubled)
5. Check digit = **(10 - (sum mod 10)) mod 10**

**Example:** Payload = `123456`

```
Digits:       1   2   3   4   5   6
Weights:      1   2   1   2   1   2     (from right, alternating)
Multiply:     1   4   3   8   5  12
Adjust >9:    1   4   3   8   5   3     (12 -> 1+2=3, or 12-9=3)
Sum:          1 + 4 + 3 + 8 + 5 + 3 = 24
Check digit:  (10 - (24 mod 10)) mod 10 = (10 - 4) mod 10 = 6
```

KID number: **1234566**

### 14.4 MOD11 Algorithm

Alternative check digit algorithm. Steps:

1. Starting from the **rightmost** digit of the payload, assign weights: 2, 3, 4, 5, 6, 7, 2, 3, 4, 5, 6, 7, ... (cycling)
2. Multiply each digit by its weight
3. Sum all products
4. Remainder = sum mod 11
5. Check digit = **11 - remainder**

**Special cases:**
- If result = 11, check digit = **0**
- If result = 10, this payload **cannot be used** (skip to next)

**Example:** Payload = `12345`

```
Digits:       1   2   3   4   5
Weights:      6   5   4   3   2     (from right: 2,3,4,5,6)
Products:     6  10  12  12  10
Sum:          50
Remainder:    50 mod 11 = 6
Check digit:  11 - 6 = 5
```

KID number: **123455**

### 14.5 Software Implementation Requirements

- Support **both** MOD10 and MOD11 algorithms (configurable per business)
- Generate KID numbers automatically when creating invoices
- Validate incoming KID numbers on payment matching
- KID length and structure must be configurable
- Bank agreement details should be stored in settings

---

## 15. EHF Invoice Format

### 15.1 Overview

| Property | Value |
|---|---|
| Standard | EHF Billing 3.0 = PEPPOL BIS Billing 3.0 |
| Format | UBL 2.1 XML |
| Specification | https://docs.peppol.eu/poacc/billing/3.0/ |
| Norwegian extensions | https://anskaffelser.dev/postaward/g3/spec/current/billing-3.0/norway/ |
| Document types | Invoice (UBL Invoice) and Credit Note (UBL CreditNote) |

### 15.2 Mandatory Header Elements

| Business Term | UBL Element | Required | Description |
|---|---|---|---|
| BT-1 | cbc:ID | Yes | Invoice number |
| BT-2 | cbc:IssueDate | Yes | Invoice issue date |
| BT-3 | cbc:InvoiceTypeCode | Yes | 380 (invoice), 381 (credit note), 393 (factored) |
| BT-5 | cbc:DocumentCurrencyCode | Yes | Currency (NOK) |
| BT-6 | cbc:TaxCurrencyCode | Conditional | If tax in different currency |
| BT-9 | cbc:DueDate | Yes | Payment due date |
| BT-10 | cbc:BuyerReference | Yes* | Buyer reference (*or PO reference required) |
| BT-13 | cac:OrderReference/cbc:ID | Yes* | Purchase order reference (*or buyer ref) |
| BT-23 | cbc:ProfileID | Yes | `urn:fdc:peppol.eu:2017:poacc:billing:01:1.0` |
| BT-24 | cbc:CustomizationID | Yes | `urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0` |

### 15.3 Mandatory Seller (AccountingSupplierParty) Elements

| Business Term | UBL Path | Required | Description |
|---|---|---|---|
| BT-27 | cac:Party/cac:PartyName/cbc:Name | Yes | Seller trading name |
| BT-28 | cac:Party/cac:PostalAddress | Yes | Seller address |
| BT-29 | cac:Party/cac:PartyLegalEntity/cbc:RegistrationName | Yes | Legal name |
| BT-30 | cac:Party/cac:PartyLegalEntity/cbc:CompanyID | Yes | Organization number |
| BT-31 | cac:Party/cac:PartyTaxScheme/cbc:CompanyID | Yes (if VAT reg) | MVA number (org.nr + "MVA") |
| BT-34 | cac:Party/cac:PostalAddress/cac:Country/cbc:IdentificationCode | Yes | "NO" |
| BT-35 | cac:Party/cac:PartyLegalEntity/cbc:CompanyLegalForm | Norway req. | "Foretaksregisteret" (for AS/ASA/NUF) |

### 15.4 Mandatory Buyer (AccountingCustomerParty) Elements

| Business Term | UBL Path | Required | Description |
|---|---|---|---|
| BT-44 | cac:Party/cac:PartyName/cbc:Name | Yes | Buyer name |
| BT-45 | cac:Party/cac:PostalAddress | Yes | Buyer address |
| BT-46 | cac:Party/cac:PartyLegalEntity/cbc:RegistrationName | Yes | Legal name |
| BT-47 | cac:Party/cac:PartyLegalEntity/cbc:CompanyID | Recommended | Organization number |
| BT-55 | cac:Party/cac:PostalAddress/cac:Country/cbc:IdentificationCode | Yes | Country code |

### 15.5 Mandatory Payment Information

| Business Term | UBL Path | Required | Description |
|---|---|---|---|
| BT-81 | cac:PaymentMeans/cbc:PaymentMeansCode | Yes | 30 (bank transfer), 58 (SEPA), etc. |
| BT-84 | cac:PaymentMeans/cac:PayeeFinancialAccount/cbc:ID | Yes (if credit transfer) | IBAN or BBAN |
| BT-86 | cac:PaymentMeans/cac:PayeeFinancialAccount/cac:FinancialInstitutionBranch/cbc:ID | Conditional | BIC (for international) |

### 15.6 Mandatory Totals

| Business Term | UBL Path | Required | Description |
|---|---|---|---|
| BT-106 | cac:LegalMonetaryTotal/cbc:LineExtensionAmount | Yes | Sum of line net amounts |
| BT-109 | cac:LegalMonetaryTotal/cbc:TaxExclusiveAmount | Yes | Total excl. VAT |
| BT-110 | cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount | Yes | Total incl. VAT |
| BT-112 | cac:LegalMonetaryTotal/cbc:PayableAmount | Yes | Amount to pay |
| BT-113 | cac:TaxTotal/cbc:TaxAmount | Yes | Total VAT amount |

### 15.7 Mandatory VAT Breakdown

| Business Term | UBL Path | Required | Description |
|---|---|---|---|
| BT-116 | cac:TaxSubtotal/cbc:TaxableAmount | Yes | Taxable amount per rate |
| BT-117 | cac:TaxSubtotal/cbc:TaxAmount | Yes | Tax amount per rate |
| BT-118 | cac:TaxSubtotal/cac:TaxCategory/cbc:ID | Yes | Tax category (S, Z, E, etc.) |
| BT-119 | cac:TaxSubtotal/cac:TaxCategory/cbc:Percent | Yes | Tax rate |

### 15.8 Mandatory Invoice Line Elements

| Business Term | UBL Path | Required | Description |
|---|---|---|---|
| BT-126 | cac:InvoiceLine/cbc:ID | Yes | Line identifier |
| BT-129 | cac:InvoiceLine/cbc:InvoicedQuantity | Yes | Quantity |
| BT-130 | cac:InvoiceLine/cbc:LineExtensionAmount | Yes | Line net amount |
| BT-131 | cac:InvoiceLine/cac:Item/cbc:Name | Yes | Item name |
| BT-146 | cac:InvoiceLine/cac:Price/cbc:PriceAmount | Yes | Unit price |
| BT-151 | cac:InvoiceLine/cac:Item/cac:ClassifiedTaxCategory/cbc:ID | Yes | Tax category |
| BT-152 | cac:InvoiceLine/cac:Item/cac:ClassifiedTaxCategory/cbc:Percent | Yes | Tax rate |

### 15.9 Norwegian-Specific Requirements

1. **"Foretaksregisteret"** must appear for sellers registered as AS, ASA, or NUF
2. **Buyer reference (BT-10)** or purchase order reference is mandatory
3. **BBAN and IBAN** must be provided as credit transfer for Norwegian payments
4. **Country codes** mandatory for both buyer and seller (ISO 3166-1)
5. **Each invoice line must specify VAT rate** even if all lines have the same rate

---

## 16. Penalties and Sanctions

### 16.1 Criminal Penalties

Under Straffeloven (Criminal Code) 392-394:

| Violation | Penalty |
|---|---|
| Breach of accounting/bookkeeping provisions | Fines or imprisonment up to 2 years |
| Serious/willful breach | Imprisonment up to 6 years |
| Negligent breach | Fines or imprisonment up to 1 year |

### 16.2 Administrative Penalties

| Violation | Penalty |
|---|---|
| Missing/non-compliant cash register system | NOK 13,450 (10x rettsgebyr); double for repeat within 1 year |
| Late filing of MVA-melding | Late fee calculated based on amount owed |
| Late filing of a-melding | Fees per day/employee |
| Late filing of arsregnskap | Accumulating fees from Regnskapsregisteret |
| Failure to keep books / produce SAF-T | Discretionary fine from Skatteetaten |
| Failure to produce personnel lists | Overtredelsesgebyr |
| Non-cooperation with tax audit | Discretionary fine |

### 16.3 Forced Dissolution

Persistent failure to file annual accounts can lead to the **forced dissolution** (tvangsopplosning) of the company by Bronnysundregistrene.

### 16.4 Tax Consequences

- Skjonnsfastsettelse (discretionary tax assessment) if books are insufficient
- Loss of VAT deduction rights if documentation is inadequate
- Tilleggsskatt (additional tax penalty) of 20% for incorrect/incomplete reporting
- Increased tilleggsskatt of 40% for gross negligence or 60% for willful evasion

---

## 17. Software vs User Responsibility Matrix

### 17.1 What the SOFTWARE Must Enforce

| Requirement | How to Enforce |
|---|---|
| Sequential invoice numbering without gaps | Auto-generate numbers; prevent manual override |
| All mandatory invoice fields present | Validation before invoice can be issued |
| MVA calculated correctly per rate | Automatic calculation; no manual VAT override |
| MVA amounts in NOK | Auto-convert if foreign currency |
| Audit trail (kontrollspor) | Immutable log; link all entries to source documents |
| Retention periods | Prevent deletion within 5 years; warn at 3.5 years |
| SAF-T export capability | Built-in SAF-T v1.30 XML generator |
| Account mapping to SAF-T standard accounts | Require mapping during account setup |
| MVA code mapping to SAF-T StandardTaxCode | Require mapping during tax code setup |
| Specifications generation | Built-in reports for all 8 required specifications |
| Electronic availability for 3.5 years | Searchable, sortable data; SAF-T export |
| EHF/PEPPOL output | Generate valid UBL 2.1 XML per PEPPOL BIS 3.0 |
| KID number generation and validation | Implement both MOD10 and MOD11 |
| Double-entry bookkeeping | Every debit must have equal credit |
| Period locking | Prevent posting to locked/closed periods |
| Ajourhold warnings | Alert when bookkeeping falls behind reporting deadlines |
| Bank reconciliation support | CAMT.053 import, auto-matching |
| Payment file generation | pain.001 export with KID |
| User access controls | Role-based access; segregation of duties |
| Change logging | Log who changed what and when |

### 17.2 What the USER Is Responsible For

| Requirement | Software Support |
|---|---|
| Correct classification of transactions | Suggest defaults; allow override with audit trail |
| Timely bookkeeping (ajourhold) | Warn when behind; user must act |
| Correct VAT rate selection per item/service | Provide rate lookup; user confirms |
| Filing reports with authorities (Altinn) | Generate data/files; user submits |
| Balance sheet documentation at year-end | Provide reconciliation tools; user performs |
| Physical inventory counts | Provide inventory module; user counts |
| Backup procedures (if self-hosted) | Provide tools; user configures schedule |
| Chart of accounts setup | Provide NS 4102 template; user customizes |
| Bank agreement for KID numbers | User arranges; software generates per agreement |
| PEPPOL Access Point registration | User selects provider; software integrates |
| Annual accounts approval and signing | Generate reports; user approves |
| Hiring a revisor (auditor) if required | Software provides data access; user engages auditor |

### 17.3 Shared Responsibilities

| Requirement | Software Role | User Role |
|---|---|---|
| Correct accounting principles | Enforce double-entry; provide period controls | Select appropriate treatment per NRS |
| VAT deduction correctness | Calculate proportional deduction | Classify business vs. personal use |
| Justeringsregler (capital goods) | Track adjustment periods and changes | Report change of use |
| Data security | Encryption, access controls, logging | Password management, user training |
| Archiving and storage | Provide compliant storage | Ensure backups, verify accessibility |

---

## Appendix A: Key Legal References

| Law/Regulation | Lovdata URL |
|---|---|
| Regnskapsloven | https://lovdata.no/lov/1998-07-17-56 |
| Bokforingsloven | https://lovdata.no/lov/2004-11-19-73 |
| Bokforingsforskriften | https://lovdata.no/dokument/SF/forskrift/2004-12-01-1558 |
| Merverdiavgiftsloven | https://lovdata.no/lov/2009-06-19-58 |
| Skatteforvaltningsloven | https://lovdata.no/lov/2016-05-27-14 |
| Kassasystemforskrifta | https://lovdata.no/dokument/SF/forskrift/2015-12-18-1616 |
| EHF forskrift (offentlige anskaffelser) | FOR-2019-04-01-444 |

## Appendix B: Key External Resources

| Resource | URL |
|---|---|
| Skatteetaten SAF-T | https://www.skatteetaten.no/en/business-and-organisation/start-and-run/best-practices-accounting-and-cash-register-systems/saf-t-financial/ |
| Skatteetaten SAF-T GitHub | https://github.com/Skatteetaten/saf-t |
| PEPPOL BIS Billing 3.0 | https://docs.peppol.eu/poacc/billing/3.0/ |
| EHF Norway extensions | https://anskaffelser.dev/postaward/g3/spec/current/billing-3.0/norway/ |
| Norsk Regnskapsstiftelse (NBS) | https://www.regnskapsstiftelsen.no/bokforing/bokforingsstandarder/ |
| Standard Norge (NS 4102) | https://standard.no/fagomrader/kontoplan-for-regnskap/ |
| Altinn | https://altinn.no |
| Skatteetaten MVA-melding GitHub | https://skatteetaten.github.io/mva-meldingen/ |

## Appendix C: Typical MVA Account Setup (NS 4102)

| Account | Name | Purpose |
|---|---|---|
| 2700 | Utgaende merverdiavgift, oppgjorskonto | MVA settlement account |
| 2710 | Utgaende merverdiavgift, 25% | Output VAT 25% |
| 2711 | Utgaende merverdiavgift, 15% | Output VAT 15% |
| 2712 | Utgaende merverdiavgift, 12% | Output VAT 12% |
| 2714 | Utgaende merverdiavgift, innforsel 25% | Output VAT on imports 25% |
| 2715 | Utgaende merverdiavgift, innforsel 15% | Output VAT on imports 15% |
| 2720 | Inngaende merverdiavgift, 25% | Input VAT 25% |
| 2721 | Inngaende merverdiavgift, 15% | Input VAT 15% |
| 2722 | Inngaende merverdiavgift, 12% | Input VAT 12% |
| 2724 | Inngaende merverdiavgift, innforsel 25% | Input VAT on imports 25% |
| 2725 | Inngaende merverdiavgift, innforsel 15% | Input VAT on imports 15% |
| 2740 | Oppgjorskonto merverdiavgift | MVA settlement clearing |

---

*This document is a reference compilation based on Norwegian law as of April 2026. Always consult the current versions of laws on Lovdata.no and current guidance from Skatteetaten before making compliance decisions in production software.*
