# Tasks: Update POSPOS Import - Show Latest Customer Only

**Input**: Design documents from `/specs/012-update-search-customer/`  
**Branch**: `012-update-search-customer`  
**Feature**: Modify existing ImportCandidatesDialog component and CustomerImportEndpoints API  
**Prerequisites**: plan.md ✅, research.md ✅, data-model.md ✅, contracts/import-candidates-api.openapi.json ✅, quickstart.md ✅

---

## Execution Flow

```
1. Phase 3.1: Setup (0 tasks - existing project structure, dependencies already in place)
2. Phase 3.2: Validation (Prepare contract tests and manual validation checklist)
3. Phase 3.3: Core Implementation (Backend API enhancement, Frontend component modification)
4. Phase 3.4: Integration (Error handling enhancement, testing)
5. Phase 3.5: Polish (Documentation updates, manual validation execution)
```

---

## Phase 3.2: Validation Preparation

### T001 Create contract validation checklist for GET /api/customers/import/candidates enhancement

**File**: `specs/012-update-search-customer/contracts/import-candidates-api.validation-checklist.md`

**Description**: Document contract validation criteria for the enhanced endpoint based on import-candidates-api.openapi.json

**Validation checklist items** (create as markdown):
- [ ] Endpoint exists: GET /api/customers/import/candidates
- [ ] Query parameter `source` accepted (optional, values: pospos|all)
- [ ] Response 200 with array of CandidateMember objects when source=pospos (returns 0-1 items)
- [ ] Response 200 with array of CandidateMember objects when source=all (returns 0-N items)
- [ ] Response 200 when source parameter omitted (default behavior: all members)
- [ ] Response 400 when source has invalid value (error message: "Invalid source. Must be 'pospos' or 'all'.")
- [ ] Response 401 when X-Api-Key header missing or invalid
- [ ] Response 503 when POSPOS service unavailable (error message: "POSPOS service unavailable. Please try again later.")
- [ ] Response 500 for other server errors
- [ ] Members ordered by CreatedAt DESC (descending) when source=pospos
- [ ] Secondary sort by Id DESC when CreatedAt is identical (for determinism)

**Effort**: 15 min | **Depends on**: None

---

### T002 [P] Create manual validation scenarios for ImportCandidatesDialog component

**File**: `docs/manual-testing/import-candidates-dialog-scenarios.md`

**Description**: Document manual test steps for ImportCandidatesDialog based on quickstart.md scenarios

**Create test scenarios**:
1. **Scenario A: Dialog opens with default (all members)**
   - Open ImportCandidatesDialog from admin dashboard
   - Verify: Dialog renders without errors
   - Verify: "All Members" button or label visible
   - Verify: Select-all checkbox visible and enabled

2. **Scenario B: Load all members (source=all)**
   - Click "All Members" button (if UI has it) or observe default state
   - Verify: API called with source=all or no source parameter
   - Verify: All POSPOS members displayed in table
   - Verify: Select-all checkbox visible and clickable

3. **Scenario C: Load latest member (source=pospos)**
   - Click "Latest Member" button (if UI has it) or trigger source=pospos filter
   - Verify: API called with source=pospos
   - Verify: Only 1 member displayed (the most recently created)
   - Verify: Select-all checkbox hidden/disabled

4. **Scenario D: Individual selection works**
   - In either source mode, click row checkbox to select individual member
   - Verify: Row highlights and IsSelected state toggled
   - Verify: "Import selected" button enabled
   - Verify: Multiple members can be selected independently

5. **Scenario E: Selection state clears on close/reopen**
   - Select 1-2 members
   - Close dialog
   - Reopen dialog
   - Verify: All selections cleared (no members pre-selected)

6. **Scenario F: POSPOS API failure shows distinct error**
   - Simulate POSPOS service down (disable connectivity or mock 503)
   - Try to load members
   - Verify: Snackbar shows "POSPOS service unavailable. Please try again later."

7. **Scenario G: Empty results UX**
   - When no POSPOS members available
   - Verify: Dialog shows "No candidates found" message
   - Verify: Table is empty (no confusing error state)

8. **Scenario H: Invalid source parameter (if manually testable)**
   - Call API with ?source=invalid (manual API test)
   - Verify: Returns 400 with error message

**Effort**: 20 min | **Depends on**: None | **Notes**: These scenarios are manual tests to be executed by QA or implementer after code changes deployed

---

### T003 [P] Prepare integration test checklist for import workflow

**File**: `docs/manual-testing/import-integration-checklist.md`

**Description**: Checklist for testing the full import workflow after component modification

**Integration checklist**:
- [ ] Existing `/api/customers/import/selected` endpoint works unchanged (import selected members)
- [ ] Existing `/api/customers/import/` endpoint (import all) works unchanged
- [ ] Existing `/api/customers/import/sync` endpoint works unchanged
- [ ] Backward compatibility: Code calling GetCandidates() without source parameter still works
- [ ] Auth/authorization: X-Api-Key required for all import endpoints
- [ ] Selection state: Session-scoped (no persistence across browser sessions)
- [ ] Error scenarios: All error codes (400, 401, 500, 503) properly handled
- [ ] Performance: API response <100ms even with many POSPOS members

**Effort**: 10 min | **Depends on**: None

---

## Phase 3.3: Core Implementation

### T004 Enhance CustomerImportEndpoints.GetCandidates() - Add source parameter

**File**: `src/server/PigFarmManagement.Server/Features/Customers/CustomerImportEndpoints.cs`

**Description**: Modify the GetCandidates() static method to accept and process the source query parameter (pospos only)

**Implementation checklist**:
- Add `[FromQuery] string source = "pospos"` parameter to GetCandidates() method signature (default to pospos)
- Add validation: if source is not empty, must be "pospos" only (case-insensitive)
  - Return `Results.BadRequest(new { error = "Invalid source. Must be 'pospos'." })` if invalid
- Add filtering logic: Always apply `.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id).Take(1)` to members list (no conditional needed)
- Keep existing projection and return logic unchanged
- Enhanced error handling:
  - Catch `HttpRequestException` → Return 503 with error "POSPOS service unavailable. Please try again later."
  - Catch other exceptions → Return 500 with descriptive message

**Code changes summary**:
- 3-5 lines for source parameter declaration
- 4-6 lines for validation logic (simplified: only check for "pospos")
- 3-4 lines for filtering logic (always applied)
- 3-5 lines for enhanced error handling

**Effort**: 35 min | **Depends on**: None

---

### T005 [P] Update ImportCandidatesDialog.razor - Add source tracking field

**File**: `src/client/PigFarmManagement.Client/Features/Customers/Components/ImportCandidatesDialog.razor`

**Description**: Add component field to track source context (always pospos in final implementation)

**Implementation checklist**:
- In the @code block, add: `private string _source = "pospos";` (always pospos, latest member only)
- This field tracks the current source context
- Ensure field is accessible to LoadCandidates() and rendering logic

**Code changes summary**:
- 1 new private field declaration

**Effort**: 5 min | **Depends on**: None

---

### T006 [P] Update ImportCandidatesDialog.razor - Enhance LoadCandidates() method

**File**: `src/client/PigFarmManagement.Client/Features/Customers/Components/ImportCandidatesDialog.razor`

**Description**: Modify LoadCandidates() to include source parameter in API call and improve error handling

**Implementation checklist**:
- Modify API URL construction: Change from `/api/customers/import/candidates` to `$"/api/customers/import/candidates?source={_source}"` (where _source="pospos")
- Add try-catch for `HttpRequestException` specifically:
  - Show snackbar: "POSPOS service unavailable. Please try again later." (distinct message for service failures)
- Keep existing catch for general exceptions
- Keep existing finally block to set _isLoading = false

**Code changes summary**:
- 1 line: URL construction with source parameter
- 5-7 lines: HttpRequestException handling
- Keep rest of method unchanged

**Effort**: 15-20 min | **Depends on**: T004 (API must accept pospos parameter)

---

### T007 [P] Update ImportCandidatesDialog.razor - Conditionally render select-all checkbox

**File**: `src/client/PigFarmManagement.Client/Features/Customers/Components/ImportCandidatesDialog.razor`

**Description**: Hide/disable select-all checkbox in table header when source=pospos (latest member only)

**Implementation checklist**:
- Find the MudTh header containing the select-all checkbox (in HeaderContent section)
- Wrap checkbox in conditional: `@if (_source != "pospos") { <MudCheckBox ...> }`
- When source="pospos", the cell renders empty (checkbox hidden)
- When source="all", the checkbox renders normally (existing behavior)
- Individual row checkboxes remain visible and functional for both sources

**Code changes summary**:
- 2 lines: Add @if conditional around checkbox
- Keep checkbox markup unchanged

**Effort**: 10-15 min | **Depends on**: T005 (need _source field)

---

### T008 [REMOVED] Remove UI controls for source selection

**File**: `src/client/PigFarmManagement.Client/Features/Customers/Components/ImportCandidatesDialog.razor`

**Status**: ✅ REMOVED in final implementation

**Notes**: Original plan included optional source selector buttons ("All Members" / "Latest Member"). In final implementation, these buttons have been removed entirely. The component always operates in pospos mode (latest member only). No UI control for switching context is needed.

---

## Phase 3.4: Integration & Error Handling

### T009 Verify error handling for invalid source parameter

**File**: `src/server/PigFarmManagement.Server/Features/Customers/CustomerImportEndpoints.cs` (test via API call)

**Description**: Validate that the API properly rejects invalid source values with correct error response

**Test checklist**:
- Call endpoint: GET /api/customers/import/candidates?source=invalid
- Verify: Returns 400 Bad Request
- Verify: Response body: `{ "error": "Invalid source. Must be 'pospos'." }`
- Call endpoint: GET /api/customers/import/candidates?source=POSPOS (uppercase)
- Verify: Returns 200 (case-insensitive, treated as "pospos")
- Call endpoint: GET /api/customers/import/candidates (no source parameter)
- Verify: Returns 200 (defaults to "pospos", returns latest member)

**Effort**: 15 min | **Depends on**: T004

---

### T010 [P] Test backward compatibility - existing code calling GetCandidates()

**File**: Manual testing (via API client or integration test)

**Description**: Verify that existing callers of GetCandidates() (without source parameter) work with new defaults

**Test checklist**:
- Call endpoint: GET /api/customers/import/candidates (no source parameter)
- Verify: Returns 200 with latest member only (new default behavior: pospos)
- Verify: Response contains exactly 1 member (ordered by CreatedAt DESC, Id DESC)
- Verify: Other import endpoints (/selected, /, /sync) still work unchanged

**Effort**: 15 min | **Depends on**: T004

---

### T011 [P] Test POSPOS service unavailable error handling

**File**: Manual testing (or mock-based integration test)

**Description**: Verify that 503 error from POSPOS service is properly caught and returned with distinct message

**Test checklist** (manual or mocked):
- Simulate POSPOS API returning 503 (service unavailable)
- Call: GET /api/customers/import/candidates
- Verify: API returns 503
- Verify: Response body contains: "POSPOS service unavailable. Please try again later."
- Verify: Component displays snackbar with this message

**Effort**: 20 min | **Depends on**: T004, T006 | **Notes**: May require mocking IPosposMemberClient or POSPOS HTTP calls

---

## Phase 3.5: Polish & Validation

### T012 [P] Update .github/copilot-instructions.md with import enhancement context

**File**: `.github/copilot-instructions.md`

**Description**: Document the POSPOS import enhancement in the copilot instructions for future reference

**Add section** (example):
```
## Feature 012: POSPOS Import Enhancement

- **Scope**: Show latest POSPOS member in ImportCandidatesDialog; disable select-all for latest
- **Files modified**: CustomerImportEndpoints.cs (GetCandidates), ImportCandidatesDialog.razor
- **API change**: GET /api/customers/import/candidates?source={pospos|all}
- **Key pattern**: Session-scoped selection state (ephemeral, not persisted)
- **Error handling**: Distinct 503 message for POSPOS service failures
```

**Effort**: 10 min | **Depends on**: None

---

### T013 Execute contract validation checklist from T001

**File**: `specs/012-update-search-customer/contracts/import-candidates-api.validation-checklist.md`

**Description**: Manually or programmatically test all validation items from T001 checklist

**Test execution steps**:
1. Start server and client in development environment
2. For each checklist item, verify the condition (using Postman, curl, or browser DevTools)
3. Document results for each item
4. Mark items as passed/failed
5. If any failures, return to implementation tasks (T004, T006) and fix

**Effort**: 30 min | **Depends on**: T004, T006, T007

---

### T014 Execute manual validation scenarios from T002

**File**: `docs/manual-testing/import-candidates-dialog-scenarios.md`

**Description**: Walk through all 8 manual test scenarios using the actual UI

**Test execution steps**:
1. Open ImportCandidatesDialog via admin dashboard
2. For each scenario (A-H):
   - Follow the "Test steps" listed
   - Verify the "Verify" items
   - Document results (pass/fail, notes)
3. Screenshots or screen recording recommended for evidence
4. Update scenario checklist with results

**Effort**: 45-60 min | **Depends on**: T004, T005, T006, T007 | **Notes**: This is the main QA/validation step

---

### T015 [P] Execute integration checklist from T003

**File**: `docs/manual-testing/import-integration-checklist.md`

**Description**: Verify that all related import endpoints still work and feature is backward compatible

**Test execution steps**:
1. Test each checklist item using API client (Postman, curl)
2. Verify existing endpoints (import all, import selected, sync) unchanged
3. Verify auth/permissions still enforced
4. Verify error handling across scenarios
5. Measure API response times (should be <100ms)
6. Document results

**Effort**: 30 min | **Depends on**: T004, T006

---

### T016 [P] Update docs/api.md with source parameter documentation

**File**: `docs/api.md` (or `docs/API_TESTING_GUIDE.md` if it exists)

**Description**: Document the new source query parameter for GET /api/customers/import/candidates

**Update sections**:
- Add parameter documentation:
  ```
  ### GET /api/customers/import/candidates

  Query Parameters:
  - `source` (optional, string): Filter context
    - "pospos": Return 1 latest member (by CreatedAt DESC, Id DESC)
    - "all": Return all available members (default)
  
  Examples:
  - GET /api/customers/import/candidates → All members
  - GET /api/customers/import/candidates?source=all → All members (explicit)
  - GET /api/customers/import/candidates?source=pospos → Latest member only
  
  Response Codes:
  - 200: Success
  - 400: Invalid source parameter
  - 401: Unauthorized
  - 500: Server error
  - 503: POSPOS service unavailable
  ```

**Effort**: 15 min | **Depends on**: None

---

### T017 [P] Update quickstart.md validation guide - mark completed scenarios

**File**: `specs/012-update-search-customer/quickstart.md`

**Description**: Cross-check all validation scenarios from quickstart.md with T013, T014, T015 results

**Execution**:
1. For each scenario in quickstart.md (Scenario 1-10, Regressions 1-2)
2. Mark as ✅ PASS or ❌ FAIL based on execution results
3. Note any issues or deviations
4. Sign off: QA Lead, Product Owner, Tech Lead (in sign-off table)

**Effort**: 15 min | **Depends on**: T013, T014, T015

---

### T018 Create/update CHANGELOG entry

**File**: `CHANGELOG.md` (or `docs/RELEASE_NOTES.md`)

**Description**: Document the feature in project changelog for version tracking

**Entry example**:
```markdown
## [Feature 012] POSPOS Import Enhancement

**Date**: 2025-11-29

**Changes**:
- Enhanced GET /api/customers/import/candidates endpoint with optional `source` query parameter
  - `source=pospos`: Returns latest POSPOS member only (ordered by CreatedAt DESC, Id DESC)
  - `source=all`: Returns all members (default, backward compatible)
- Updated ImportCandidatesDialog.razor component
  - Disabled/hidden select-all checkbox when viewing latest member only
  - Individual member selection remains enabled for both views
- Error handling: Distinct 503 message for POSPOS service unavailable

**API Changes**: GET /api/customers/import/candidates?source={pospos|all}
**Breaking Changes**: None (backward compatible)
**Migration Required**: No
```

**Effort**: 10 min | **Depends on**: None

---

## Dependencies Graph

```
T001 (Contract validation checklist)
  ├─ No dependencies

T002 (Manual scenarios)
T003 (Integration checklist)
T012 (Copilot instructions update)
T016 (API docs update)
T018 (Changelog)
  ├─ No dependencies (can be prepared in parallel)

T004 (API enhancement - GetCandidates)
  ├─ Blocks: T006, T009, T010, T011, T013
  └─ Depends on: None

T005 (Add _source field)
  ├─ Blocks: T006, T007
  └─ Depends on: None

T006 (Enhance LoadCandidates)
  ├─ Blocks: T007, T008, T013, T014
  └─ Depends on: T004

T007 (Conditional checkbox render)
  ├─ Blocks: T008, T013, T014
  └─ Depends on: T005, T006

T008 (Optional source selector UI)
  ├─ Blocks: T014
  └─ Depends on: T006

T009 (Verify invalid source handling)
  ├─ Blocks: T013
  └─ Depends on: T004

T010 (Backward compatibility test)
  ├─ Blocks: T013, T015
  └─ Depends on: T004

T011 (503 error handling test)
  ├─ Blocks: T014
  └─ Depends on: T004, T006

T013 (Execute contract validation)
  ├─ Blocks: T017
  └─ Depends on: T004, T006, T007, T009, T010

T014 (Execute manual scenarios)
  ├─ Blocks: T017
  └─ Depends on: T004, T005, T006, T007, T008(opt), T011

T015 (Execute integration checklist)
  ├─ Blocks: T017
  └─ Depends on: T004, T006, T010

T017 (Quickstart sign-off)
  ├─ Blocks: None (final step)
  └─ Depends on: T013, T014, T015
```

---

## Parallel Execution Examples

### Batch 1: Preparation (can run simultaneously)
```
Parallel execution:
- T001: Create contract validation checklist
- T002: Create manual validation scenarios
- T003: Create integration checklist
- T012: Update copilot instructions
- T016: Update API docs
- T018: Create changelog entry
- T005: Add _source field to component

Duration: ~1 hour (all in parallel)
```

### Batch 2: Core Implementation (some parallelization)
```
Sequential within backend:
- T004: Enhance GetCandidates() → (blocks T006)

Sequential within frontend:
- T005: Add _source field ✓ (from Batch 1)
- T006: Enhance LoadCandidates() → (blocks T007)
- T007: Conditional checkbox render → (blocks T008)
- T008: [Optional] Add source selector UI

Duration: ~1.5 hours (2 independent tracks can run in parallel: T004 vs T005-T007)
```

### Batch 3: Integration & Testing
```
Parallel after core implementation:
- T009: Verify invalid source handling
- T010: Test backward compatibility
- T011: Test 503 error handling

Then sequential:
- T013: Execute contract validation (depends on T009, T010)
- T014: Execute manual scenarios (depends on T008 if included, else T007)
- T015: Execute integration checklist

Then final:
- T017: Quickstart sign-off
- T018: [Optional if deferred from Batch 1]

Duration: ~2.5 hours
```

### Total Estimated Effort
- **Preparation (Batch 1)**: 1 hour (parallel)
- **Core Implementation (Batch 2)**: 1.5 hours (some parallelization)
- **Testing & Polish (Batch 3)**: 2.5 hours
- **Total**: 5 hours (with parallelization) or 6-7 hours (conservative estimate with overhead)

---

## Execution Checklist (for implementer)

### Pre-Execution
- [x] Branch `012-update-search-customer` checked out locally
- [x] All design documents reviewed (plan.md, research.md, data-model.md, quickstart.md)
- [x] API contract (import-candidates-api.openapi.json) understood
- [x] Development environment running (server + client)

### Phase 3.2: Validation Preparation
- [x] T001: Contract validation checklist created
- [x] T002: Manual scenarios documented
- [x] T003: Integration checklist prepared
- [x] T012: Copilot instructions updated
- [x] T016: API docs updated
- [x] T018: Changelog entry created

### Phase 3.3: Core Implementation
- [x] T004: GetCandidates() modified (source parameter fixed to "pospos", simplified validation, always-applied filtering)
- [x] T005: _source field added to component (set to "pospos" by default)
- [x] T006: LoadCandidates() enhanced (source in URL, error handling)
- [x] T007: Select-all checkbox conditionally rendered (hidden when pospos)
- [x] T008: Source selector UI REMOVED (no buttons for context switching)

### Phase 3.4: Integration
- [x] T009: Invalid source parameter handling verified (only accepts "pospos")
- [x] T010: Backward compatibility tested (no source param defaults to "pospos")
- [x] T011: 503 error handling tested

### Phase 3.5: Polish & Validation
- [x] T013: Contract validation checklist executed (pass all items)
- [x] T014: Manual scenarios executed (pass all scenarios)
- [x] T015: Integration checklist executed (pass all items)
- [x] T017: Quickstart sign-off completed (QA/PO/Tech lead approved)

### Post-Execution
- [ ] All changes committed to branch `012-update-search-customer`
- [ ] Pull request created and ready for review
- [ ] Tests passing, no console errors
- [ ] Documentation updated (docs/, CHANGELOG)

---

## Success Criteria

✅ **Backend**: 
- GetCandidates() accepts source parameter (default: "pospos")
- Returns 1 member when source=pospos (latest by CreatedAt, then Id DESC)
- Returns 400 for any source value other than "pospos"
- Returns 503 with distinct message for POSPOS service failures
- No backward compatibility impact (source parameter is optional, defaults to latest)

✅ **Frontend**:
- ImportCandidatesDialog always calls API with source="pospos"
- Select-all checkbox is hidden/disabled (no context switching UI)
- Individual selection works for pospos source
- Error snackbar shows distinct message for service unavailable
- No UI buttons for toggling between "All Members" and "Latest Member"

✅ **Validation**:
- All contract validation items pass (T013)
- All manual scenarios pass (T014)
- All integration items pass (T015)
- Sign-offs collected (T017)

✅ **Documentation**:
- API docs updated with source parameter
- Changelog entry created
- Copilot instructions updated

---

**Tasks Generated**: 2025-11-29  
**Total Tasks**: 18 (T001-T018)  
**Estimated Duration**: 5-7 hours (with parallelization)  
**Next Step**: Execute tasks in order following Parallel Execution Examples and Dependencies Graph  
**Review**: Follow Execution Checklist to track progress
