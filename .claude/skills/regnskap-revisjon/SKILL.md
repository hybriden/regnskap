---
name: regnskap:revisjon
description: "Run a compliance audit on the accounting system code. Use when checking if code follows Norwegian accounting law, double-entry bookkeeping rules, SAF-T compliance, or MVA correctness. Triggers on: 'audit', 'revisjon', 'compliance check', 'sjekk compliance', 'er dette lovlig', 'SAF-T check', 'MVA-sjekk', 'review accounting code', 'regnskapsrevisjon', 'kvalitetskontroll', 'verify compliance', 'dobbelt bokholderi sjekk', 'balance check'."
---

# Regnskap Revisjon

Kjører en compliance-revisjon på eksisterende kode uten å implementere noe nytt. Bruker revisjonsagenten til å gjennomgå koden mot norsk lov og regnskapsprinsipper.

## Iron Law

```
REVISJONEN ENDRER ALDRI KODE. DEN OBSERVERER OG RAPPORTERER.
Fiks-forslag er kun forslag — implementering skjer via regnskap:run.
```

## Process

### Step 1: Scope Detection

Determine what to audit:
- **Specific module:** User says "audit the kontoplan module" → audit `src/**/Features/Kontoplan/`
- **Specific concern:** User says "check MVA compliance" → audit MVA-related code across all modules
- **Full system:** User says "full audit" or "revisjon" → audit everything

### Step 2: Context Gathering

1. Read all source code in scope
2. Read relevant specifications from `_workspace/regnskap/spec-*.md`
3. Read the legal reference: `_workspace/harness/norwegian-accounting-law-reference.md`

### Step 3: Audit Execution

Spawn the auditor agent:

```
Agent(
  description: "Compliance audit: {scope}",
  prompt: "You are running a standalone compliance audit.
    Scope: {scope_description}
    
    Read ALL code files in: {file_paths}
    Read the legal reference: _workspace/harness/norwegian-accounting-law-reference.md
    Read relevant specifications: {spec_paths}
    
    Produce your full audit report.
    Pay special attention to:
    - Dobbelt bokholderi-integritet
    - Revisjonsspor (audit trail)
    - Bokføringsloven compliance
    - MVA-korrekthet
    - SAF-T-kompatibilitet
    
    Save report to _workspace/regnskap/audit-standalone-{date}.md",
  subagent_type: "revisjon",
  model: "opus"
)
```

### Step 4: Report Delivery

Present the audit report to the user with:
1. **Sammendrag** — overall status (GODKJENT / KREVER_ENDRING / STOPPET)
2. **Kritiske funn** — all MUST_FIX items
3. **Anbefalinger** — all SHOULD_FIX items
4. **Neste steg** — suggest using `regnskap:run` to fix issues if any

## Audit Types

| Type | Scope | When to Use |
|------|-------|-------------|
| **Modul-revisjon** | Single module | After implementing a module |
| **MVA-revisjon** | MVA logic across system | Before MVA-melding period |
| **SAF-T-revisjon** | SAF-T export and data model | Before first SAF-T submission |
| **Full revisjon** | Entire system | Before production launch |
| **Endring-revisjon** | Recently changed files | After major refactoring |
