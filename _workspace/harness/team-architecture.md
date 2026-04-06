# Team Architecture — Regnskapssystem

## Execution Mode
Subagents — agents work independently with orchestrator as sole coordinator

## Pattern
Pipeline + Fan-out + Producer-Reviewer (composite)

```
[Arkitekt] → designs spec
     ↓
┌→ [Backend] →┐
├→ [Frontend] →├→ [Revisjon] → Approved? → Done
└──────────────┘        ↓ (Nei)
                   Fix loop (max 2 sykluser)
```

## Why Subagents
- Architect produces complete specifications before implementation starts
- Backend and frontend work from specs, no mid-task communication needed
- Auditor validates after implementation, findings go back through orchestrator
- Simpler coordination, lower overhead for iterative development

## Agent Roster

### regnskap-arkitekt
- **Role:** Accounting domain expert and system architect — designs data models, API contracts, business rules per Norwegian standards
- **Justification:** Deep accounting domain knowledge (NS 4102, Regnskapsloven, MVA, SAF-T) that would overload implementation agents
- **Type:** custom (.claude/agents/regnskap-arkitekt.md)
- **Model:** opus
- **Inputs:** Module name, feature description, existing code context
- **Outputs:** Data model specs, API contracts, business rule definitions, MVA mapping
- **Communicates with:** Orchestrator only (subagent mode)

### backend
- **Role:** ASP.NET Core backend developer — implements EF Core entities, services, APIs, migrations
- **Justification:** Implementation expertise distinct from domain design and UI work; parallelizable with frontend
- **Type:** custom (.claude/agents/backend.md)
- **Model:** default (inherits)
- **Inputs:** Architect's spec (models, APIs, business rules)
- **Outputs:** C# source files, EF migrations, unit tests
- **Communicates with:** Orchestrator only

### frontend
- **Role:** React/TypeScript frontend developer — implements UI components, forms, report views
- **Justification:** Frontend expertise distinct from backend; parallelizable with backend
- **Type:** custom (.claude/agents/frontend.md)
- **Model:** default (inherits)
- **Inputs:** Architect's spec (UI requirements, API contracts)
- **Outputs:** React components, pages, hooks, frontend tests
- **Communicates with:** Orchestrator only

### revisjon
- **Role:** Compliance auditor and QA — validates accounting correctness, Norwegian law compliance, test coverage
- **Justification:** Audit perspective must be independent from implementation to catch blind spots; deep compliance knowledge
- **Type:** custom (.claude/agents/revisjon.md)
- **Model:** opus
- **Inputs:** Implemented code, architect's spec, Norwegian accounting standards
- **Outputs:** Audit report with findings (MUST_FIX, SHOULD_FIX, OK), test gaps, compliance status
- **Communicates with:** Orchestrator only

## Data Flow

### Dispatch (per task)
1. Orchestrator → Arkitekt: "Design module X" → receives spec
2. Orchestrator → Backend + Frontend (parallel): "Implement spec" → receives code
3. Orchestrator → Revisjon: "Audit implementation against spec" → receives report
4. If MUST_FIX → loop back to step 2 with fix instructions (max 2 cycles)

### Artifacts
- `_workspace/regnskap/spec-{module}.md` — Architect's specification
- `_workspace/regnskap/audit-{module}.md` — Auditor's report
- `_workspace/regnskap/status.md` — Pipeline status checkpoint

## Error Handling

| Scenario | Handling |
|----------|----------|
| Architect fails to produce spec | Retry with more context. If still fails, escalate to user. |
| Backend/frontend implementation fails | Retry once with error context. If still fails, report partial progress. |
| Audit finds MUST_FIX issues | Feed findings back to backend/frontend, max 2 fix cycles |
| Audit finds compliance violation | STOP. Report to user immediately — never ship non-compliant accounting code |
| Fix cycle exhausted (2 cycles) | Report remaining issues to user with auditor's notes |
