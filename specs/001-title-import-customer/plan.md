
# Implementation Plan: Import customer from POSPOS API (memory-only)

**Branch**: `001-title-import-customer` | **Date**: 2025-09-27 | **Spec**: `specs/001-title-import-customer/spec.md`
**Input**: Feature specification at `specs/001-title-import-customer/spec.md`

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
[Extract from feature spec: primary requirement + technical approach from research]

## Technical Context
**Language/Version**: C# / .NET 8 (net8.0) - server project targets `net8.0`
**Primary Dependencies**: Existing solution: ASP.NET Core Web API, Swashbuckle (6.5.0). EF Core packages exist in repo but the importer will be memory-only for now.
**Storage**: In-memory runtime store + lightweight mapping file `customer_id_mapping.json` at repo root (manual backup recommended).
**Testing**: xUnit / existing .NET test approach (not created in Phase 1 per feature constraint)
**Target Platform**: Server: local development (Windows dev / Linux CI). Client: Blazor WebAssembly/Server (existing client project).
**Project Type**: Web application (frontend + backend in same solution).
**Performance Goals**: Low-throughput administrative import; best-effort pagination and retry for POSPOS API.
**Constraints**: Memory-only importer (no DB changes), no authentication for initial endpoints; POSPOS credentials passed via environment variables.
**Scale/Scope**: Small import (admin-triggered), not designed for bulk high-throughput at this stage.

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

[Gates determined based on constitution file]

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
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->
```
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

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

3. **Generate contract tests** from contracts:
   - One test file per endpoint
   - Assert request/response schemas
   - Tests must fail (no implementation yet)

4. **Extract test scenarios** from user stories:
   - Each story → integration test scenario
   - Quickstart test = story validation steps

5. **Update agent file incrementally** (O(1) operation):
   - Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot`
     **IMPORTANT**: Execute it exactly as specified above. Do not add or remove any arguments.
   - If exists: Add only NEW tech from current plan
   - Preserve manual additions between markers
   - Update recent changes (keep last 3)
   - Keep under 150 lines for token efficiency
   - Output to repository root

**Output**: data-model.md, /contracts/*, failing tests, quickstart.md, agent-specific file

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Load `.specify/templates/tasks-template.md` as base
- Generate tasks from Phase 1 design docs (contracts, data model, quickstart)
- Each contract → contract test task [P]
- Each entity → model creation task [P] 
- Each user story → integration test task
- Implementation tasks to make tests pass

**Ordering Strategy**:
- TDD order: Tests before implementation 
- Dependency order: Models before services before UI
- Mark [P] for parallel execution (independent files)

**Estimated Output**: 25-30 numbered, ordered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |


## Progress Tracking
*This checklist is updated during execution flow*

 **Phase Status**:
 - [x] Phase 0: Research complete (/plan command)
 - [x] Phase 1: Design complete (/plan command)
 - [ ] Phase 2: Task planning complete (/plan command - describe approach only)
 - [ ] Phase 3: Tasks generated (/tasks command)
 - [ ] Phase 4: Implementation complete
 - [ ] Phase 5: Validation passed
 - [x] Phase 2 (partial): Models, Pospos client, and MappingStore scaffolded
 - [ ] Phase 3: Tasks generated (/tasks command)
 - [ ] Phase 4: Implementation complete
 - [ ] Phase 5: Validation passed

 **Phase Status**:
 - [x] Phase 0: Research complete (/plan command)
 - [x] Phase 1: Design complete (/plan command)
 - [ ] Phase 2: Task planning complete (/plan command - describe approach only)
 - [ ] Phase 3: Tasks generated (/tasks command)
 - [ ] Phase 4: Implementation complete
 - [ ] Phase 5: Validation passed

 **Gate Status**:
 - [x] Initial Constitution Check: PASS
 - [ ] Post-Design Constitution Check: PASS
 - [x] All NEEDS CLARIFICATION resolved
 - [ ] Complexity deviations documented

**Post-Design Check**
- [x] Post-Design Constitution Check: PASS — The design follows the constitution's feature-first guidance: small backend endpoint, no new services, docs and mapping file documented.

## Next steps (Phase 2)
- Run `/tasks` to generate `tasks.md` for implementation. Suggested top tasks:
   1. Create server endpoints: POST `/import/customers` and GET `/import/customers/summary` (skeleton controllers).
   2. Implement `PosposImporter` service: in-memory store + HTTP client to POSPOS (env var driven).
   3. Mapping persistence: read/write `customer_id_mapping.json` safely (backup on write).
   4. Wire minimal UI trigger in client (Import dialog) to call POST endpoint and display summary.
   5. Add smoke test script or small integration test to call endpoints locally.

Once Phase 2 is generated, pick a single task (server endpoints) and implement it as a small PR from branch `001-title-import-customer`.

## Recent work
- Scaffolding implemented: models, `PosposClient`, `FileMappingStore`, `PosposImporter` (in-memory importer). Build succeeded locally (`dotnet build` passed).


---
*Based on Constitution v2.1.1 - See `/memory/constitution.md`*
