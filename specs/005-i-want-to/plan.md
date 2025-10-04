
# Implementation Plan: Import POSPOS Stock to Feed Formula

**Branch**: `005-i-want-to` | **Date**: 2025-10-01 | **Spec**: [link to spec.md]
**Input**: Feature specification from `/specs/005-i-want-to/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from file system structure or context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, `GEMINI.md` for Gemini CLI, `QWEN.md` for Qwen Code or `AGENTS.md` for opencode).
7. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
Import product data from POSPOS system into Feed Formula entities to enable display of product code, name, cost, and unit name in pigpen feed history, and use cost from Feed Formula with special prices from POSPOS transaction order lists to determine profit. Quantity uses stock from POSPOS transaction order list. Technical approach: Create PosposProductClient for low-level HTTP communication, FeedFormulaService for business logic and data transformation, FeedFormulaEndpoints for API layer, update FeedFormula record to mirror POSPOS product fields, handle one-to-many relationship per invoice, and manage failure modes including network timeout, service unavailable, and rate limiting (10 requests/minute).

## Technical Context
**Language/Version**: C# .NET 8  
**Primary Dependencies**: Blazor WebAssembly, .NET Core Web API, Entity Framework Core  
**Storage**: SQLite (development), Supabase PostgreSQL (production)  
**Target Platform**: Web application (Blazor)  
**Project Type**: Web application (frontend + backend)  
**Performance Goals**: Import completes in <10 seconds total for small datasets  
**Constraints**: Handle network timeout and service unavailable failures gracefully; Respect POSPOS API rate limit (10 requests/minute)  
**Scale/Scope**: Small farm management (<100 products, <10 transactions/day)  
**User Roles**: Farm managers only

## Component Responsibilities

### PosposProductClient (Services/PosposProductClient.cs)
**Low-level HTTP Client Layer**
- Handle HTTP communication with POSPOS API
- Implement rate limiting (10 requests/minute)
- Manage authentication and API keys
- Handle network timeouts and retries
- Parse raw JSON responses
- Return structured data or throw appropriate exceptions

### FeedFormulaService (Services/FeedFormulaService.cs) 
**Business Logic Layer**
- Orchestrate product import workflow
- Use PosposProductClient to fetch data
- Transform POSPOS data to FeedFormula entities
- Handle business rules (duplicate detection, validation)
- Persist data to database
- Return operation results with success/failure status

### FeedFormulaEndpoints (Features/Feeds/FeedFormulaEndpoints.cs)
**API Layer**
- Define HTTP endpoints (POST /api/feed-formulas)
- Validate incoming requests
- Call FeedFormulaService methods
- Handle HTTP response formatting
- Return appropriate HTTP status codes
- Map service results to API responses

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Mission alignment: ✅ Supports pig pen and feed management mission.
- Data integrity: ✅ Imports historical stock data without overwriting existing records.
- Simplicity: ✅ Straightforward import logic, no complex dependencies.
- Feature-based architecture: ✅ Adds to existing feed management feature.
- Ownership: ✅ Single owner project, changes approved.

**Status**: PASS - No violations detected.

## Project Structure

### Documentation (this feature)
```
specs/[###-feature]/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
backend/
├── src/server/
│   ├── Features/Feeds/  # FeedFormulaEndpoints (API layer - HTTP requests/responses)
│   ├── Infrastructure/Data/  # Entity updates, DB migrations
│   └── Services/  # PosposProductClient (HTTP client), FeedFormulaService (business logic)

frontend/
├── src/client/
│   ├── Features/Feeds/  # UI for feed history display
│   └── Shared/  # Updated models
```

**Structure Decision**: Web application structure with backend (API) and frontend (Blazor). Feature-based architecture with clear separation: FeedFormulaEndpoints handle HTTP API layer for feed formula operations, FeedFormulaService manages business logic, PosposProductClient handles low-level HTTP communication.

## Phase 0: Outline & Research
1. **Extract unknowns from Technical Context** above:
   - For each NEEDS CLARIFICATION → research task
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:
   ```
   For each unknown in Technical Context:
     Task: "Research {unknown} for {feature context}"
   For each technology choice:
     Task: "Find best practices for {tech} in {domain}"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

**Output**: research.md with all NEEDS CLARIFICATION resolved

## Phase 1: Design & Contracts
*Prerequisites: research.md complete*

1. **Extract entities from feature spec** → `data-model.md`:
   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Generate API contracts** from functional requirements:
   - For each user action → endpoint
   - Use standard REST/GraphQL patterns
   - Output OpenAPI/GraphQL schema to `/contracts/`

3. **Update agent file incrementally** (O(1) operation):
   - Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot`
     **IMPORTANT**: Execute it exactly as specified above. Do not add or remove any arguments.
   - If exists: Add only NEW tech from current plan
   - Preserve manual additions between markers
   - Update recent changes (keep last 3)
   - Keep under 150 lines for token efficiency
   - Output to repository root

**Output**: data-model.md, /contracts/*, quickstart.md, agent-specific file

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Load tasks-template.md as base
- Generate tasks from Phase 1 design docs (contracts, data model, quickstart)
- Each API contract → implementation task [P]
- Each entity → model update task [P] 
- Implementation tasks for FeedFormula updates, import logic, UI updates

**Ordering Strategy**:
- Dependency order: Models before services before UI
- Mark [P] for parallel execution (independent files)

**Estimated Output**: 15-20 numbered, ordered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (execute quickstart.md, performance validation)

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |

## Progress Tracking
- [x] Initial Constitution Check: PASS
- [x] Phase 0: Research complete (research.md created)
- [x] Phase 1: Design complete (data-model.md, contracts/, quickstart.md created)
- [x] Post-Design Constitution Check: PASS
- [x] Phase 2: Task planning complete (tasks.md created with service architecture updates)
- [x] Architecture Refinement: Component responsibilities defined (PosposProductClient, FeedFormulaService, FeedFormulaEndpoints)
- [x] Phase 3: Implementation (T001-T005: DB migration, PosposProductClient, FeedFormulaService, FeedFormulaEndpoints) - **COMPLETED October 2, 2025**
- [ ] Phase 4: Validation (execute quickstart.md, performance validation)

**Phase Status**:
- [ ] Phase 0: Research complete (/plan command)
- [ ] Phase 1: Design complete (/plan command)
- [ ] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [ ] Initial Constitution Check: PASS
- [ ] Post-Design Constitution Check: PASS
- [ ] All NEEDS CLARIFICATION resolved
- [ ] Complexity deviations documented

---
*Based on Constitution v2.1.1 - See `/memory/constitution.md`*
