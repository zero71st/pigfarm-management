# Tasks: Recalculate consume rate when pig pen quantity changes

**Input**: Design documents from `/specs/015-description-can-you/`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/

## Execution Flow (main)
```
1. Load plan.md from feature directory
   → Tech stack: C# .NET 8, ASP.NET Core, EF Core, Blazor
   → Structure: Web app (backend/frontend split)
2. Load design documents:
   → data-model.md: PigPen, PigPenFormulaAssignment, FeedFormula entities
   → contracts/UpdatePigPenEndpoint.yml: Modified PUT endpoint
   → research.md: EF Core patterns, ceiling rounding, logging, validation
3. Generate tasks by category:
   → Validation: Contract validation checklist, manual test scenarios
   → Core: Repository recalculation logic, service validation, endpoint handling
   → Integration: Logging, transaction handling
   → Polish: Quickstart execution, documentation updates
4. Apply task rules:
   → Repository and Service are sequential (same flow)
   → Validation tasks can run in parallel
5. Number tasks sequentially (T001-T012)
6. Validate completeness:
   ✓ Contract has validation checklist
   ✓ All entities have implementation tasks
   ✓ Quickstart scenarios mapped to manual validation
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Path Conventions
This feature follows the **Web app** structure:
- Backend: `src/server/PigFarmManagement.Server/`
- Shared: `src/shared/PigFarmManagement.Shared/`
- Client: `src/client/PigFarmManagement.Client/` (no changes required)

## Phase 3.1: Setup
No setup tasks required — all dependencies and project structure already exist.

## Phase 3.2: Validation (Manual)
This phase captures contract review and manual validation preparation per quickstart.md.

- [x] **T001** [P] Review `contracts/UpdatePigPenEndpoint.yml` and document validation checklist for PUT /api/pigpens/{id}:
  - Verify 200 response includes updated `pigQty` and recalculated `formulaAssignments`
  - Verify 400 response when `IsCalculationLocked == true` with message "Cannot modify pig quantity: pen calculations are locked"
  - Verify 400 response when `pigQty < 1` or `pigQty > 100` with message "Pig quantity must be between 1 and 100"
  - Verify `assignedTotalBags` uses ceiling rounding (e.g., 9.1 → 10.0)
  - Verify `assignedBagPerPig` synced from source formula's `ConsumeRate`

- [x] **T002** [P] Extract manual test scenarios from `quickstart.md` into checklist:
  - Scenario 1: Basic recalculation (happy path)
  - Scenario 2: Locked pen rejection
  - Scenario 3: Quantity validation (0 and 101)
  - Scenario 4: Ceiling rounding verification
  - Scenario 5: Formula sync verification
  - Scenario 6: Logging verification

## Phase 3.3: Core Implementation

- [x] **T003** Add locked-pen validation to `PigPenService.UpdatePigPenAsync`:
  - File: `src/server/PigFarmManagement.Server/Features/PigPens/PigPenService.cs`
  - Before updating, check `existingPigPen.IsCalculationLocked`
  - If true, throw `InvalidOperationException("Cannot modify pig quantity: pen calculations are locked")`
  - This exception will be caught by endpoint handler and returned as 400 BadRequest

- [x] **T004** Add quantity range validation to `PigPenService.UpdatePigPenAsync`:
  - File: `src/server/PigFarmManagement.Server/Features/PigPens/PigPenService.cs`
  - Check `pigPen.PigQty` is between 1 and 100 (inclusive)
  - If invalid, throw `InvalidOperationException("Pig quantity must be between 1 and 100")`

- [x] **T005** Implement assignment recalculation logic in `PigPenRepository.UpdateAsync`:
  - File: `src/server/PigFarmManagement.Server/Infrastructure/Data/Repositories/PigPenRepository.cs`
  - After loading pig pen with `Include(p => p.FormulaAssignments)` (line 65):
    1. Detect if `PigQty` changed by comparing `pigPen.PigQty` with `entity.PigQty`
    2. If changed, iterate through `entity.FormulaAssignments.Where(a => a.IsActive && !a.IsLocked)`
    3. For each assignment:
       - Load source `FeedFormula` by `assignment.OriginalFormulaId` using `_context.FeedFormulas.FindAsync()`
       - Update `assignment.AssignedPigQuantity = pigPen.PigQty`
       - Update `assignment.AssignedBagPerPig = feedFormula.ConsumeRate`
       - Update `assignment.AssignedTotalBags = Math.Ceiling(assignment.AssignedBagPerPig * assignment.AssignedPigQuantity)`
       - Update `assignment.UpdatedAt = DateTime.UtcNow`
    4. Return updated count for logging

- [x] **T006** Add dependency injection for `IFeedFormulaRepository` or direct `DbContext` access in `PigPenRepository`:
  - File: `src/server/PigFarmManagement.Server/Infrastructure/Data/Repositories/PigPenRepository.cs`
  - Repository already has `_context` field (line 9) — use `_context.FeedFormulas.FindAsync()` directly
  - No additional DI needed

- [x] **T007** Add structured logging to `PigPenService.UpdatePigPenAsync`:
  - File: `src/server/PigFarmManagement.Server/Features/PigPens/PigPenService.cs`
  - After successful update, log change event using existing `_logger` field (line 21):
    ```csharp
    _logger.LogInformation(
        "Updated pig pen {PigPenId} quantity from {OldQty} to {NewQty} by user {UserId}, recalculated {AssignmentCount} assignments",
        pigPen.Id, oldQty, pigPen.PigQty, userId, updatedAssignmentCount
    );
    ```
  - Get `userId` from `HttpContext.User.FindFirst("user_id")?.Value` (pass via parameter or service context)
  - Get `updatedAssignmentCount` from repository return value (modify T005 to return count)

- [x] **T008** Modify `PigPenRepository.UpdateAsync` signature to return assignment update count:
  - File: `src/server/PigFarmManagement.Server/Infrastructure/Data/Repositories/PigPenRepository.cs`
  - Change return type from `Task<PigPen>` to `Task<(PigPen pigPen, int updatedAssignmentCount)>`
  - Update interface in `IRepositories.cs` if needed
  - Return tuple with pig pen model and count of updated assignments

- [x] **T009** Update `PigPenEndpoints.UpdatePigPen` handler to pass user context to service:
  - File: `src/server/PigFarmManagement.Server/Features/PigPens/PigPenEndpoints.cs`
  - Modify handler signature to inject `HttpContext` (line 116)
  - Extract `userId` from `HttpContext.User.FindFirst("user_id")?.Value`
  - Pass `userId` to service method (or modify service to accept `HttpContext`)

## Phase 3.4: Integration

- [x] **T010** Verify EF Core transaction handling for atomic updates:
  - File: `src/server/PigFarmManagement.Server/Infrastructure/Data/Repositories/PigPenRepository.cs`
  - Confirm `SaveChangesAsync()` (line 94) wraps all assignment updates in single transaction
  - No explicit transaction code needed (EF default behavior)
  - Document in code comment: "All assignment updates occur within single SaveChangesAsync transaction"

- [x] **T011** Add error handling for formula lookup failures in `PigPenRepository.UpdateAsync`:
  - File: `src/server/PigFarmManagement.Server/Infrastructure/Data/Repositories/PigPenRepository.cs`
  - If `FeedFormula` not found by `OriginalFormulaId`, log warning and skip that assignment (don't fail entire update)
  - Use `_logger.LogWarning("Formula {FormulaId} not found for assignment {AssignmentId}", ...)`

## Phase 3.5: Polish

- [ ] **T012** Execute `quickstart.md` manual validation scenarios:
  - File: `specs/015-description-can-you/quickstart.md`
  - Run all 6 scenarios against local server
  - Document results (pass/fail) in quickstart.md or separate validation report
  - Fix any failures before marking feature complete

- [x] **T013** [P] Update `.github/copilot-instructions.md` Feature 015 summary:
  - File: `.github/copilot-instructions.md`
  - Already updated via `update-agent-context.ps1` script
  - Verify entry exists in "## Feature" section or recent changes
  - Add manual note if needed: "Feature 015: Auto-recalculate pig pen formula assignments when PigQty changes"

## Dependencies

**Sequential dependencies**:
- T003 → T007 (service validation blocks logging since logging needs validated data)
- T005 → T008 (repository recalc blocks signature change)
- T008 → T007 (signature change blocks service logging implementation)
- T009 → T007 (user context extraction blocks service logging)
- T003-T011 → T012 (all implementation blocks manual validation)

**Parallel opportunities**:
- T001 [P] T002 (validation tasks independent)
- T013 [P] T012 (documentation update independent of validation)

**Critical path**: T003 → T004 → T005 → T008 → T009 → T007 → T010 → T011 → T012

## Parallel Example
```bash
# Launch validation tasks together:
# Terminal 1
# Review contracts/UpdatePigPenEndpoint.yml (T001)

# Terminal 2  
# Extract quickstart.md scenarios (T002)
```

## Task Execution Order (Recommended)

**Phase 1: Validation Preparation** (can run in parallel):
1. T001 — Contract validation checklist
2. T002 — Manual test scenario extraction

**Phase 2: Core Implementation** (sequential):
3. T003 — Locked-pen validation
4. T004 — Quantity range validation
5. T005 — Repository recalculation logic
6. T006 — Verify DI setup (quick check)
7. T008 — Repository signature change
8. T009 — Endpoint user context extraction
9. T007 — Service logging implementation

**Phase 3: Integration** (sequential):
10. T010 — Transaction handling verification
11. T011 — Error handling for missing formulas

**Phase 4: Polish** (can run in parallel):
12. T012 — Execute quickstart scenarios
13. T013 — Update documentation

## Notes
- **No schema migrations** required (all entities exist)
- **No frontend changes** required (UI already supports PUT endpoint)
- **EF Core automatic change tracking** handles assignment updates
- **Existing error handling** in endpoints catches service exceptions and returns 400 BadRequest
- **Atomic updates** guaranteed by EF Core transaction scope

## Task Generation Rules Applied

1. **From Contracts** (`UpdatePigPenEndpoint.yml`):
   - ✅ T001: Contract validation checklist
   - ✅ T003-T004: Validation error responses (400)
   - ✅ T005: Recalculation side effect implementation

2. **From Data Model** (`data-model.md`):
   - ✅ T005: PigPenFormulaAssignment recalculation logic
   - ✅ T010: Atomicity guarantee verification
   - ✅ T011: Error handling for missing formulas

3. **From Research** (`research.md`):
   - ✅ T005: EF Core Include() pattern
   - ✅ T005: Math.Ceiling() rounding
   - ✅ T007: Structured logging pattern
   - ✅ T003-T004: Validation error pattern

4. **From Quickstart** (`quickstart.md`):
   - ✅ T002: Manual test scenario extraction
   - ✅ T012: Execute all 6 scenarios

5. **Ordering Rules**:
   - ✅ Validation preparation before implementation
   - ✅ Repository layer before service layer
   - ✅ Service layer before endpoint layer
   - ✅ Implementation before polish/validation

## Validation Checklist

*GATE: Use this checklist before marking feature complete*

- [x] All contract validation points documented (T001)
- [x] All manual test scenarios extracted (T002)
- [x] Locked-pen validation implemented (T003)
- [x] Quantity range validation implemented (T004)
- [x] Assignment recalculation logic implemented (T005)
- [x] Structured logging implemented (T007)
- [x] Repository signature updated (T008)
- [x] User context extraction implemented (T009)
- [x] Transaction handling verified (T010)
- [x] Error handling for missing formulas (T011)
- [ ] All quickstart scenarios pass (T012) - Ready for manual testing
- [x] Documentation updated (T013)

**Estimated Completion Time**: 4-6 hours (including testing)

**Implementation Status**: ✅ Core implementation complete (2025-12-01)
- Build: ✅ Successful with 2 warnings (unrelated to feature)
- Server: ✅ Running on http://localhost:5000
- Files Modified: PigPenService.cs, PigPenRepository.cs, PigPenEndpoints.cs, IRepositories.cs
- Documentation: ✅ Updated (.github/copilot-instructions.md)
- Next Step: Execute manual validation scenarios (T012)

**Risk Areas**:
- Formula lookup failures (mitigated by T011 error handling)
- Concurrent updates to same pen (mitigated by EF optimistic concurrency - existing behavior)
- Performance with many assignments (mitigated by single Include() query - validated in research.md)

---

**Ready for implementation** — start with T001-T002 (validation prep), then proceed through T003-T012 in order.
