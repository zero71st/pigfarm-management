
# Implementation Plan: Enhanced Customer Management

**Branch**: `008-update-manage-customer` | **Date**: October 5, 2025 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/008-update-manage-customer/spec.md`

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
Enhanced customer management system allowing deletion of customers, manual and automatic POS updates, Google Maps location integration, and view mode switching between card (default) and table layouts. Technical approach leverages existing .NET 8 Blazor architecture with MudBlazor UI components, Entity Framework Core for data persistence, and Google Maps API integration for location display.

## Technical Context
**Language/Version**: C# .NET 8  
**Primary Dependencies**: Blazor WebAssembly, MudBlazor UI, Entity Framework Core, Google Maps JavaScript API  
**Storage**: SQLite (development), Supabase PostgreSQL (production)  
**Testing**: xUnit, Bunit (Blazor component testing)  
**Target Platform**: Web browsers (Blazor WebAssembly client-server architecture)
**Project Type**: web - frontend (Blazor WASM) + backend (.NET Core Web API)  
**Performance Goals**: <500ms customer list loading, <200ms location map rendering  
**Constraints**: Existing customer data preservation, POS API integration compatibility  
**Scale/Scope**: ~1000 customers, Google Maps API rate limits

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

✅ **Mission Alignment**: Feature supports core farm operations management (customer lifecycle)  
✅ **Data Integrity**: Customer deletion includes safety checks for active relationships; POS sync preserves authoritative external data  
✅ **Simplicity**: Leverages existing architecture patterns and UI components without introducing new complexity  
✅ **Feature Architecture**: Follows existing feature-based structure under `src/client/Features/Customers` and `src/server/Features/Customers`  
✅ **Single Owner**: Implementation controlled by project owner with appropriate documentation

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
└── src/server/PigFarmManagement.Server/
    ├── Features/Customers/           # Customer management feature
    │   ├── CustomerEndpoints.cs      # API endpoints
    │   ├── CustomerService.cs        # Business logic
    │   └── CustomerRepository.cs     # Data access
    ├── Infrastructure/Data/
    │   ├── Entities/CustomerEntity.cs # Updated with location fields
    │   └── Migrations/               # Location coordinate fields migration
    └── Services/
        └── PosposImporter.cs         # Enhanced POS sync service

frontend/
└── src/client/PigFarmManagement.Client/
    ├── Features/Customers/
    │   ├── Components/
    │   │   ├── CustomerManagement.razor      # Enhanced with delete & view switching
    │   │   ├── EditCustomerDialog.razor      # Enhanced with location fields
    │   │   ├── CustomerLocationMap.razor     # NEW: Google Maps integration
    │   │   └── CustomerTableView.razor       # NEW: Table view component
    │   ├── Services/CustomerService.cs       # Enhanced with delete & location
    │   └── Pages/                            # Existing structure maintained
    └── wwwroot/                              # Google Maps API integration

tests/
├── src/server/PigFarmManagement.Server.Tests/
│   ├── Features/Customers/           # Customer feature tests
│   └── Integration/                  # API integration tests
└── src/client/PigFarmManagement.Client.Tests/
    └── Features/Customers/           # Blazor component tests
```

**Structure Decision**: Web application structure selected based on existing Blazor WebAssembly + .NET Core Web API architecture. Feature-based organization maintained under `Features/Customers` following constitutional guidance.

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
- Load `.specify/templates/tasks-template.md` as base structure
- Generate tasks from Phase 1 design artifacts (data-model.md, contracts/, quickstart.md)
- Database migration tasks for location and deletion fields
- Enhanced CustomerEntity and Customer model tasks
- New component creation: CustomerLocationMap, CustomerTableView, ViewModeService
- API endpoint enhancements: deletion validation, location management, POS sync
- Blazor component enhancements: CustomerManagement with view switching
- Google Maps JavaScript integration and IJSRuntime setup
- Test creation for all new functionality (contract tests, component tests, integration tests)

**Ordering Strategy**:
- TDD approach: Contract tests and failing component tests first [P]
- Database layer: Migration and entity updates [P]
- Service layer: Enhanced CustomerService, new LocationService, ViewModeService
- API layer: New endpoints and enhanced existing endpoints  
- Component layer: New components, then enhanced existing components
- Integration layer: Google Maps setup, POS sync enhancements
- Validation and quickstart execution

**Estimated Output**: 35-40 numbered, ordered tasks covering:
- 5 database/entity tasks
- 8 service and repository tasks  
- 12 API and contract tasks
- 10 component and UI tasks
- 5 testing and validation tasks

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
- [x] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS  
- [x] All NEEDS CLARIFICATION resolved
- [x] Complexity deviations documented (none required)

---
*Based on Constitution v2.1.1 - See `/memory/constitution.md`*
