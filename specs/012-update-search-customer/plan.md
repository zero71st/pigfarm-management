
# Implementation Plan: Update Search Customer Display from POSPOS

**Branch**: `012-update-search-customer` | **Date**: November 29, 2025 | **Spec**: `specs/012-update-search-customer/spec.md`
**Input**: Feature specification from `/specs/012-update-search-customer/spec.md`

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
Update the POSPOS customer search to display only the single most recently added customer instead of all available customers, and disable the bulk "select all" checkbox. Individual customer selection remains enabled. This streamlines the customer workflow for imports and prevents accidental bulk operations. API failures display a distinct error message. Selection state is session-scoped and clears on page reload or new search.

## Technical Context
**Language/Version**: C# .NET 8 (backend), Blazor WebAssembly (frontend)  
**Primary Dependencies**: Entity Framework Core (server), Blazor components with MudBlazor (client), Npgsql/SQLite (database)  
**Storage**: PostgreSQL (production), SQLite (development)  
**Testing**: xUnit (backend), Bunit (Blazor components)  
**Target Platform**: Web application (ASP.NET Core + Blazor WASM)  
**Project Type**: Web (full-stack: frontend + backend)  
**Performance Goals**: Sub-500ms search response time for POSPOS queries  
**Constraints**: Must preserve authentication & authorization; backward compatible with existing customer search in other contexts  
**Scale/Scope**: Single-owner farm management system; customer list typically <1000 records

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Feature-Based Architecture (PASS ✅)**:
- Feature scope: POSPOS customer search UI refinement
- Scope: Isolated to `Features/Customer/` directory; no cross-feature dependency injection changes
- Public contract: Search API endpoint response shape unchanged; filtering applied server-side
- Backwards compatibility: Existing non-POSPOS customer searches unaffected; POSPOS scope isolated via context parameter

**Data Integrity (PASS ✅)**:
- No historical data modifications
- No locked/closed records affected
- Search filtering is read-only operation
- Selection state is ephemeral (session-scoped, not persisted)

**Simplicity & Minimalism (PASS ✅)**:
- Single-owner workflow: admin streamlines customer selection
- Minimal UI changes: disable checkbox, filter results, add error message
- No institutional change control needed; straightforward requirement

**Conclusion**: Feature aligns with constitution. No violations; no complexity justification needed.

## Project Structure

### Documentation (this feature)
```
specs/012-update-search-customer/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
│   └── search-customer-api.openapi.json
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
# Web application: ASP.NET Core backend + Blazor WASM frontend
# MODIFICATION SCOPE: Enhance existing import infrastructure (component + API)

backend/
├── src/server/PigFarmManagement.Server/
│   └── Features/Customers/
│       └── CustomerImportEndpoints.cs          # MODIFY: GetCandidates()
│           └── Add source query parameter
│           └── Return latest only when source=pospos
│           └── Return all when source=all or omitted

frontend/
├── src/client/PigFarmManagement.Client/
│   └── Features/Customers/
│       └── Components/
│           └── ImportCandidatesDialog.razor    # MODIFY: Import UI
│               ├── LoadCandidates()            # Call API with source parameter
│               ├── OnSelectAllChanged()        # DISABLE: Hide/disable select-all
│               ├── FilteredCandidates          # Keep individual row selection
│               └── Rendering                   # Remove select-all from table header

tests/
├── backend/
│   └── PigFarmManagement.Server.Tests/
│       └── Features/Customers/
│           └── Add GetCandidates source filter tests
└── frontend/
    └── [Test ImportCandidatesDialog select-all disabled]
```

**Structure Decision**: Enhance existing import infrastructure rather than create new search page. Modify:
- **Backend**: `CustomerImportEndpoints.GetCandidates()` to accept `source` query parameter and apply server-side filtering
- **Frontend**: `ImportCandidatesDialog.razor` to disable select-all checkbox and call API with source parameter
- **No new migrations**: Existing customer data used; filtering at query time via POSPOS member ordering

## Phase 0: Outline & Research
1. **Extract unknowns from Technical Context**: ✅ All resolved
   - Stack fully identified (C# .NET 8, Blazor, EF Core)
   - Existing infrastructure: CustomerImportEndpoints, ImportCandidatesDialog component
   - POSPOS integration: Already in place via IPosposMemberClient
   - Storage confirmed (PostgreSQL + SQLite)
   - Testing frameworks defined

2. **Key research areas (addressed in spec + existing code analysis)**:
   - Existing API: `/api/customers/import/candidates` returns all POSPOS members via `GetCandidates()`
   - Existing component: ImportCandidatesDialog with `_selectAll` checkbox and `FilteredCandidates` filtering
   - POSPOS API failure handling: Component catches exceptions in LoadCandidates() and shows snackbar
   - Selection state: Session-scoped via component `_candidates` list with `IsSelected` field
   - Customer ordering: POSPOS members returned from API; newest determined by CreatedAt field

3. **Findings consolidated**: Modification scope clarified—enhance existing import endpoints and component rather than create new search page

**Output**: research.md with findings (Phase 0 artifact below)

## Phase 1: Design & Contracts
*Prerequisites: research.md complete* ✅

1. **Components identified** → `data-model.md`:
   - ImportCandidatesDialog (Blazor component: existing, to be enhanced)
   - CustomerImportEndpoints (API: existing, to be enhanced)
   - No new DTOs required; existing CandidateMember class extended with source context

2. **API modifications** from functional requirements:
   - Endpoint: `GET /api/customers/import/candidates?source=pospos` → returns latest POSPOS member only
   - Endpoint: `GET /api/customers/import/candidates?source=all` → returns all POSPOS members (existing behavior)
   - Endpoint: `GET /api/customers/import/candidates` (no param) → returns all (backward compatible)
   - Error response: Catch and return via snackbar; distinct message for POSPOS service failures
   - Contract output: `/contracts/import-candidates-api.openapi.json` (specification of enhanced endpoint)

3. **Component modifications**:
   - ImportCandidatesDialog.razor: Disable/hide select-all checkbox header
   - Maintain individual row selection capability
   - Call API with source parameter (default: all, enhancement: pospos source)
   - Keep session-scoped selection state

4. **Agent context update**:
   - Update `.github/copilot-instructions.md` with import enhancement scope & API changes
   - Document existing component modification pattern

**Output**: data-model.md, /contracts/*, quickstart.md, updated copilot-instructions.md

## Phase 2: Design Contracts & Validation Artifacts ✅ COMPLETED

**Artifacts Generated** (Phase 2 execution within /plan scope):

### 1. API Contracts: `contracts/search-customer-api.openapi.json`
OpenAPI 3.0 specification with:
- **Endpoint**: `GET /api/customers/search` 
  - Query parameters: `source` (enum: "pospos" | "all"), `searchText`, `status`
  - Security: X-Api-Key header authentication
  - Response: `CustomerSearchResponse` with `customers[]` and `error` field
- **Schemas**:
  - `Customer`: Full customer object (id, code, firstName, lastName, status, createdAt, externalId)
  - `CustomerSearchResponse`: `{ customers: Customer[], error?: string, totalCount?: number }`
- **Error Responses**:
  - 400: Invalid request (invalid source parameter)
  - 401: Unauthorized (missing API key)
  - 500: POSPOS service error (returns error message in response)

### 2. Manual Validation Guide: `quickstart.md`
Comprehensive testing and deployment verification document with:
- **Setup Checklist**: Prerequisites, test data requirements (3 local + 1 POSPOS customer)
- **10 Main Validation Scenarios**:
  1. POSPOS source returns single latest customer
  2. All source returns multiple active customers
  3. Select-all checkbox is disabled
  4. Individual customer selection works
  5. Selection state clears on page reload (session-scoped)
  6. Empty results show appropriate message
  7. POSPOS API failure shows distinct error
  8. Database query uses index for performance
  9. Authorization enforced (API key required)
  10. Invalid source parameter returns error
- **2 Regression Tests**: Backward compatibility, CRUD operations unaffected
- **Deployment Sign-Off**: QA, Product Owner, Tech Lead approval tracking
- **Issues & Observations**: Template for capturing findings during validation

**Deliverables Status**:
- [x] API contracts (OpenAPI JSON)
- [x] Validation scenarios (10 main + 2 regression)
- [x] Deployment checklist
- [x] Test data preparation guide

## Phase 2+ Task Planning Approach
*Phase 2 (contract/artifact generation) COMPLETE. Phase 3+ (task generation/implementation) deferred to /tasks command*

**Task Generation Strategy** (Phase 3 - deferred to `/tasks` command):
- Load `.specify/templates/tasks-template.md` as base
- Generate tasks from Phase 1 design docs (contracts, data model, quickstart)
- Backend tasks: Update CustomerRepository query, Update CustomerEndpoints, Add error handling
- Frontend tasks: Update CustomerService HTTP calls, Update SearchCustomerPage component, Disable select-all checkbox
- Each component/service update → implementation task
- Manual validation → acceptance validation task

**Ordering Strategy**:
- Dependency order: Backend API changes first, then frontend components
- Risk order: API failure handling implemented before UI changes
- Parallel execution: Backend and frontend can develop in parallel (contract-first)

**Estimated Output**: 12-15 numbered, ordered tasks in tasks.md

**NEXT STEP**: Execute `/tasks` command to generate tasks.md with prioritized implementation tasks

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*No violations detected—feature is straightforward and aligns with constitution.*

| Item | Status |
|------|--------|
| Architecture complexity | ✅ PASS - Isolated to Feature/Customer; no new patterns |
| Data model changes | ✅ PASS - No schema changes; filtering at query time |
| Backwards compatibility | ✅ PASS - Non-POSPOS searches unaffected |
| Integration scope | ✅ PASS - Existing POSPOS integration leveraged |


## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command) — All clarifications pre-resolved; no unknowns
- [x] Phase 1: Design complete (/plan command) — Contracts, data model, quickstart defined below
- [x] Phase 2: Contracts & Artifacts complete (/plan command) — search-customer-api.openapi.json, quickstart.md generated
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved (clarifications section in spec)
- [x] No complexity deviations
- [x] API contracts validated
- [x] Validation scenarios documented

---

## Phase 0 Artifact: Research

**Decision: POSPOS Search Filtering Strategy**
- **Choice**: Server-side filtering with `OrderByDescending(CreatedAt).Take(1)` on CustomerRepository
- **Rationale**: 
  - Reduces payload to single customer (efficient)
  - Consistent with existing POSPOS integration patterns
  - Minimizes client-side logic
- **Alternatives considered**: 
  - Client-side filtering after fetching all (rejected: inefficient for large datasets)
  - Cached result strategy (rejected: doesn't reduce query load)

**Decision: Selection State Management**
- **Choice**: Session-scoped, client-side state (no persistence)
- **Rationale**: 
  - Feature scope: single user, temporary selection
  - Aligns with clarification (user selected option C)
  - Simplifies backend; avoids database churn
- **Alternatives considered**:
  - Database persistence (rejected: overkill, premature storage of ephemeral state)
  - LocalStorage persistence (rejected: user experience would suggest data is saved)

**Decision: API Error Handling**
- **Choice**: Distinct error message for POSPOS API failures
- **Rationale**: 
  - Clarification specifies option A: show service error
  - Differentiates from "no customers" (empty result)
  - Guides user to retry vs. accept empty result
- **Alternatives considered**:
  - Generic error (rejected: ambiguous)
  - Fallback to cached data (rejected: would hide real issues)

---

## Phase 1 Artifact: Data Model

**Entities**:

### Customer (no schema changes)
- `Id: Guid` (primary key)
- `CreatedAt: DateTime` (used for ordering—existing field)
- `Code: string` (customer identifier—existing field)
- `Status: int` (active/inactive—existing field)
- Other fields: unchanged from schema

**DTOs (request/response contracts)**:

### CustomerSearchRequest
```
{
  "source": "pospos" | "other",  // NEW: scopes search context
  "filters": { ... }              // Existing filters (unchanged)
}
```

### CustomerSearchResponse
```
{
  "customers": [ { Customer } ],  // Returns 1 customer for POSPOS, N for others
  "error": null | "string"        // NEW: error message if API fails
}
```

### SelectionState (Client-side only)
```
{
  "selectedCustomerId": Guid | null,  // NEW: current selection
  "sourceContext": "pospos" | "other" // NEW: where selected customer comes from
}
```

**State Transitions**:
- Init: no selection
- User clicks row → `selectedCustomerId` set (session-scoped)
- New search → reset to null
- Page reload → reset to null

---

## Phase 1 Artifact: API Contracts

**Endpoint: Search Customer**

```
GET /api/customers/search?source=pospos&filters=...

Response 200 OK:
{
  "customers": [
    {
      "id": "uuid",
      "code": "CUST001",
      "createdAt": "2025-11-29T10:30:00Z",
      "status": 1,
      ...
    }
  ],
  "error": null
}

Response 200 OK (no results):
{
  "customers": [],
  "error": null,
  "message": "No customers found"  // Client renders: "No customers found"
}

Response 500 Internal Server Error (POSPOS API down):
{
  "customers": [],
  "error": "POSPOS service unavailable. Please try again later."
}
```

**OpenAPI Contract**: See `contracts/search-customer-api.openapi.json` (generated)

---

## Phase 1 Artifact: Quickstart (Manual Validation)

**Scenario 1: POSPOS Search Returns Latest Customer**
1. Navigate to Customer Search page
2. Select "POSPOS" source from dropdown
3. Click "Search"
4. Verify: Table displays exactly ONE customer (the most recently created)
5. Verify: "Select All" checkbox in table header is disabled/grayed
6. Verify: Can click on customer row to select it (highlights or visual feedback)

**Scenario 2: POSPOS Search Returns No Results**
1. Navigate to Customer Search page
2. Select "POSPOS" source
3. Click "Search"
4. Verify: Table displays empty with message "No customers found"
5. Verify: "Select All" checkbox remains disabled

**Scenario 3: POSPOS API Fails**
1. (Simulate POSPOS API outage or network error)
2. Navigate to Customer Search page
3. Select "POSPOS" source
4. Click "Search"
5. Verify: Error message displays "POSPOS service unavailable. Please try again later."
6. Verify: User can retry search once service recovers

**Scenario 4: Selection State is Session-Scoped**
1. Search and select a customer (checkbox highlights)
2. Perform a new search (different POSPOS query)
3. Verify: Previous selection is cleared
4. Refresh page (F5)
5. Verify: Selection is cleared (no persistence)

---

*Based on Constitution v2.1.1 - See `/memory/constitution.md`*
