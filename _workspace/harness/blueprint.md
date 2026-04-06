# Harness Blueprint — Regnskapssystem

## Design Decisions

### Why Subagents over Agent Teams
The workflow is sequential with one parallel phase: architect designs → backend+frontend implement → auditor validates. Agents don't need to share discoveries mid-task because:
1. The architect produces complete specs before implementation starts
2. Backend and frontend work from the same spec independently
3. The auditor validates after implementation, not during
4. The orchestrator handles the review loop

### Why 4 Agents (not more, not fewer)
- **Architect + Auditor can't merge:** Architect designs forward-looking specs; auditor critiques backward-looking code. Different mental models.
- **Backend + Frontend can't merge:** Would serialize parallel work and overload context with both .NET and React patterns.
- **No dedicated "test" agent:** Testing is built into backend (xUnit) and auditor (test coverage check). A separate test agent would overlap with both.
- **No dedicated "deploy" agent:** Deployment is out of scope for initial development.

### Why Pipeline + Fan-out + Producer-Reviewer
- **Pipeline:** Architecture → Implementation → Audit is inherently sequential
- **Fan-out:** Backend and frontend implementation is embarrassingly parallel
- **Producer-Reviewer:** Implementation → Audit → Fix → Re-audit loop ensures compliance

### Legal Reference as Shared Context
Norwegian accounting law is extensive. Rather than duplicating knowledge across agents, a single reference document (`norwegian-accounting-law-reference.md`) is loaded on-demand by the architect and auditor. This follows Progressive Disclosure — implementation agents don't need legal details, only the spec.

## Data Flow

```
User task
    ↓
Orchestrator (regnskap:run)
    ↓
[1] Arkitekt → spec-{module}.md
    ↓
[2] Backend (parallel) → code in src/
    Frontend (parallel) → code in src/
    ↓
[3] Revisjon → audit-{module}.md
    ↓
    ├→ GODKJENT → Done
    └→ KREVER_ENDRING → [2.5] Fix → [3] Re-audit (max 2x)
```

## Artifact Locations
- Specifications: `_workspace/regnskap/spec-{module}.md`
- Audit reports: `_workspace/regnskap/audit-{module}.md`
- Pipeline status: `_workspace/regnskap/status.md`
- Legal reference: `_workspace/harness/norwegian-accounting-law-reference.md`
- Domain analysis: `_workspace/harness/domain-analysis.md`
- Team architecture: `_workspace/harness/team-architecture.md`
