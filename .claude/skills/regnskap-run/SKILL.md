---
name: regnskap:run
description: "Full development pipeline for the Norwegian accounting system (regnskapssystem). Use when implementing a feature, module, or component. Triggers on: 'implement', 'build', 'create', 'develop', 'make', 'add module', 'new feature', 'kontoplan', 'hovedbok', 'bilag', 'faktura', 'leverandør', 'kunde', 'bank', 'rapport', 'MVA', 'mva', 'periodeavslutning', 'reskontro', 'SAF-T', 'EHF'. Also triggers on: 'run the pipeline', 'build this', 'implement this', 'start development', 're-run', 'try again', 'fix and rebuild'."
---

# Regnskap Orchestrator

Koordinerer det komplette utviklingsteamet for regnskapssystemet: arkitekt designer spesifikasjon, backend og frontend implementerer i parallell, og revisor validerer compliance. Bruker subagent-mønster med Pipeline + Fan-out + Producer-Reviewer.

## Iron Law

```
ALDRI SHIP KODE SOM BRYTER DOBBELT BOKHOLDERI-INVARIANTEN ELLER NORSK REGNSKAPSLOV.
Compliance-feil fra revisjon er ALLTID blokkerende. Ingen unntak.
```

## Process

### Phase 0: Context Check

1. Check `_workspace/regnskap/` directory:
   - **Exists with status.md** → read status, resume from last incomplete phase
   - **Does not exist** → first run, create directory

2. Read the task input — which module/feature to build
3. Read existing codebase state: `ls src/` to understand what's already implemented
4. Save input to `_workspace/regnskap/input-{task}.md`

### Phase 1: Architecture (Arkitekt)

Spawn the architect agent to design the specification:

```
Agent(
  description: "Design {module} specification",
  prompt: "Read the task: {task_description}.
    Read existing code context: {existing_modules}.
    Read the legal reference: _workspace/harness/norwegian-accounting-law-reference.md
    Produce a complete specification following your output protocol.
    Save to _workspace/regnskap/spec-{module}.md",
  subagent_type: "regnskap-arkitekt",
  model: "opus"
)
```

**Quality gate:** Read the spec. Verify it has all required sections (datamodell, API-kontrakt, forretningsregler, MVA-håndtering, avhengigheter). If incomplete, retry once with specific feedback.

### Phase 2: Implementation (Backend + Frontend in parallel)

Spawn backend and frontend agents in parallel:

```
Agent(
  description: "Implement {module} backend",
  prompt: "Read the specification: _workspace/regnskap/spec-{module}.md
    Read existing code: src/
    Implement all backend code per spec.
    Run dotnet build to verify.
    Run dotnet test to verify.
    Report files created.",
  subagent_type: "backend",
  run_in_background: true
)

Agent(
  description: "Implement {module} frontend",
  prompt: "Read the specification: _workspace/regnskap/spec-{module}.md
    Read existing components: src/components/, src/pages/
    Implement all frontend code per spec.
    Report files created.",
  subagent_type: "frontend",
  run_in_background: true
)
```

Wait for both to complete. Collect results.

### Phase 3: Audit (Revisjon)

Spawn the auditor:

```
Agent(
  description: "Audit {module} compliance",
  prompt: "Read the specification: _workspace/regnskap/spec-{module}.md
    Read the legal reference: _workspace/harness/norwegian-accounting-law-reference.md
    Read ALL implemented code for this module.
    Produce a full audit report following your output protocol.
    Save to _workspace/regnskap/audit-{module}.md",
  subagent_type: "revisjon",
  model: "opus"
)
```

**Decision based on audit verdict:**

| Verdict | Action |
|---------|--------|
| GODKJENT | Proceed to Phase 4 |
| KREVER_ENDRING | Extract MUST_FIX items, go to Phase 2.5 (Fix Cycle) |
| STOPPET | Report to user immediately. Do not attempt auto-fix. |

### Phase 2.5: Fix Cycle (max 2 iterations)

If audit finds MUST_FIX issues:

1. Extract MUST_FIX findings from audit report
2. Determine which agent(s) need to fix: backend, frontend, or both
3. Re-spawn affected agent(s) with fix instructions:

```
Agent(
  description: "Fix {module} backend audit findings",
  prompt: "Read the audit report: _workspace/regnskap/audit-{module}.md
    Fix ALL MUST_FIX items related to backend.
    The following issues must be resolved:
    {list_of_must_fix_items}
    Do NOT change code unrelated to these findings.
    Run dotnet build and dotnet test after fixes.",
  subagent_type: "backend"
)
```

4. Re-run audit (Phase 3)
5. If still KREVER_ENDRING after 2 cycles → report remaining issues to user

### Phase 4: Completion

1. Update `_workspace/regnskap/status.md`:
   ```
   module: {module}
   phase_1_spec: complete
   phase_2_backend: complete
   phase_2_frontend: complete
   phase_3_audit: GODKJENT
   completed_at: {timestamp}
   ```

2. Report to user:
   - Module implemented
   - Files created/modified
   - Audit status
   - Any remaining SHOULD_FIX items (non-blocking)

## Module Dependency Order

When building the complete system, follow this order:

```
1. Kontoplan (foundation — no dependencies)
2. Hovedbok (depends on: kontoplan)
3. Bilagsregistrering (depends on: hovedbok, kontoplan)
4. MVA-håndtering (depends on: kontoplan, bilag)
5. Leverandørreskontro (depends on: bilag, hovedbok)
6. Kundereskontro (depends on: bilag, hovedbok)
7. Fakturering (depends on: kundereskontro, MVA)
8. Bankavstemming (depends on: hovedbok, leverandør, kunde)
9. Rapportering (depends on: alle moduler)
10. Periodeavslutning (depends on: hovedbok, rapportering)
```

If the user asks to "build the accounting system" without specifying a module, start with #1 and work through sequentially.

## Error Handling

| Scenario | Action |
|----------|--------|
| Architect fails | Retry once. If still fails, report partial analysis to user. |
| Backend build fails | Read error, spawn backend agent with fix instructions |
| Frontend compile fails | Read error, spawn frontend agent with fix instructions |
| Audit finds STOPPET | STOP immediately. Report to user. Never auto-fix compliance failures. |
| Fix cycle exhausted | Report remaining MUST_FIX to user with auditor's notes |
| Context too large | Split module into sub-features, process sequentially |
