# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]`  
**Created**: [DATE]  
**Status**: Draft  
**Input**: User description: "$ARGUMENTS"

## Execution Flow (main)
````markdown
# Feature Specification: Recalculate consume rate when pig pen quantity changes

**Feature Branch**: `015-description-can-you`  
**Created**: 2025-12-01  
**Status**: Draft  
**Input**: User description: "Can you recalculate when update pig pen? my point is when pig pen change I want to update pig pen quantity and recalculate consume rate."

## Execution Flow (main)
```
1. Parse user description from Input
   → If empty: ERROR "No feature description provided"
2. Extract key concepts from description
   → Identify: actor (admin/user), action (update pig pen), data (pig quantity, formula consume rate), constraints (locked calculations, historical data)
3. Identify unclear aspects and mark them for clarification
4. Produce User Scenarios & Acceptance Criteria based on intended behavior
5. Generate Functional Requirements that are testable
6. Identify Key Entities and attributes affected
7. Run Review Checklist and surface any [NEEDS CLARIFICATION] markers
8. Return: SUCCESS (spec ready for planning)
```

---

## ⚡ Quick Guidelines
- ✅ Focus on WHAT the system must do when pig pen quantity changes and WHY (business intent: keep feed assignment accuracy)
- ❌ Avoid low-level implementation details (exact file paths, method names) in acceptance criteria — keep them testable
- 👥 Written for product and QA stakeholders; developers will use this to implement and estimate

### Section Requirements
- **Mandatory sections**: Completed below

---

## Clarifications

### Session 2025-12-01
- Q: When the system recalculates `AssignedTotalBags = AssignedBagPerPig × AssignedPigQuantity`, how should fractional results be handled? → A: Round up (ceiling) - always deliver at least the calculated amount
- Q: When `PigQty` changes, should the system update `AssignedBagPerPig` from the source formula's current `ConsumeRate`, or preserve the existing assignment's `AssignedBagPerPig`? → A: Update `AssignedBagPerPig` from source formula's latest `ConsumeRate` - sync with current formula
- Q: What is the maximum allowable `PigQty` value for business validation? → A: 100
- Q: When a pig pen has `IsCalculationLocked == true` (e.g., after force-close), how should updates to `PigQty` be handled? → A: Block update - return validation error preventing any change to locked pens

---

## User Scenarios & Testing *(mandatory)*

### Primary User Story
As a farm manager (or system user with pig-pen edit permissions), when I update the pig quantity for a pig pen, the system MUST update the pig pen record and recalculate feed formula assignment values (consume rates expressed as bag-per-pig and total bags) so that feed planning and financial summaries remain accurate.

### Acceptance Scenarios
1. **Given** a pig pen with existing active `PigPenFormulaAssignment` entries, **When** an authorized user updates the pig pen's `PigQty` from N to M via the update flow, **Then** the pig pen's `PigQty` is persisted and each active `PigPenFormulaAssignment` is updated so that:
   - `AssignedPigQuantity` = M
   - `AssignedBagPerPig` is updated from the source formula's current `ConsumeRate` (synced with latest formula)
   - `AssignedTotalBags` = ceiling(`AssignedBagPerPig` * M) (recomputed with round-up)
   - Timestamps are updated appropriately where applicable

2. **Given** a pig pen whose calculations are locked (`IsCalculationLocked == true`), **When** a user attempts to change the `PigQty`, **Then** the system prevents the update and returns a validation error (e.g., "Cannot modify pig quantity: pen calculations are locked").

3. **Given** the pig pen has both active and inactive/locked formula assignments, **When** `PigQty` changes, **Then** only assignments considered mutable (active and not locked) are recalculated.

4. **Given** the update is malformed (negative quantity, zero, or exceeds 100), **When** user submits update, **Then** the system returns validation errors (e.g., "Pig quantity must be between 1 and 100").

### Edge Cases
- If the pig pen has zero formula assignments, updating `PigQty` should still update the pig pen record but do nothing for assignments.
- If the source formula's `ConsumeRate` has changed since assignment creation, the recalc will sync `AssignedBagPerPig` with the latest value from the formula.
- Concurrent updates: two users update `PigQty` simultaneously — last-write-wins or optimistic concurrency should be defined.

## Requirements *(mandatory)*

### Functional Requirements
- **FR-001**: System MUST persist `PigQty` when a pig pen is updated via the existing update endpoint.
- **FR-002**: System MUST, immediately after persisting a `PigQty` change, recalculate mutable `PigPenFormulaAssignment` fields as follows:
  - Set `AssignedPigQuantity` = updated `PigQty`.
  - Update `AssignedBagPerPig` from the source formula's current `ConsumeRate` (sync with latest formula value).
  - Recompute `AssignedTotalBags` = ceiling(`AssignedBagPerPig` * `AssignedPigQuantity`) using round-up (ceiling) to ensure at least the calculated amount is delivered.
- **FR-003**: System MUST NOT modify assignments that are locked (`IsLocked == true`) or otherwise marked immutable.
- **FR-004**: System MUST validate new `PigQty` before applying changes: must be a positive integer between 1 and 100 (inclusive).
- **FR-005**: System MUST log a change event that includes pig pen id, previous quantity, new quantity, user id (actor), and number of assignments updated.
- **FR-006**: System MUST prevent updates to `PigQty` when `IsCalculationLocked == true` and return a validation error with clear message indicating the pen is locked.



### Key Entities *(include if feature involves data)*
- **PigPen**: Represents an active pen. Key attributes involved: `Id`, `CustomerId`, `PenCode`, `PigQty`, `IsCalculationLocked`, `SelectedBrand`, `CreatedAt`, `UpdatedAt`.
- **PigPenFormulaAssignment**: Per-pen assignment of a feed formula. Key attributes: `Id`, `PigPenId`, `OriginalFormulaId`, `ProductCode`, `AssignedPigQuantity`, `AssignedBagPerPig`, `AssignedTotalBags`, `IsActive`, `IsLocked`, `AssignedAt`, `UpdatedAt`.
- **FeedFormula**: Source formula providing `ConsumeRate` (bags/pig) used when creating assignments. Key attributes: `Id`, `Name`, `ConsumeRate`, `Brand`, `CategoryName`.

---

## Review & Acceptance Checklist

### Content Quality
- [x] Focused on user value and business needs
- [x] No low-level implementation details in acceptance criteria
- [x] Mandatory sections completed

### Requirement Completeness
- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous  
- [x] Success criteria are measurable
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

---

## Execution Status
*Updated by main() during processing*

- [x] User description parsed
- [x] Key concepts extracted
- [x] Ambiguities marked
- [x] User scenarios defined
- [x] Requirements generated
- [x] Entities identified
- [x] Review checklist passed

---

## Implementation Status

**Date Completed**: 2025-12-01  
**Status**: ✅ Implementation Complete - Ready for Manual Testing

**Implementation Summary**:
- All 13 tasks (T001-T013) completed
- Backend modifications: 4 files (PigPenService.cs, PigPenRepository.cs, PigPenEndpoints.cs, IRepositories.cs)
- Validation: Locked-pen check, quantity range (1-100)
- Recalculation: Active assignments updated with ceiling rounding and formula sync
- Logging: Structured log events with pen ID, quantities, user ID, assignment count
- Build: ✅ Successful
- Server: ✅ Running on http://localhost:5000

**Next Steps**:
- Execute 6 manual validation scenarios in quickstart.md
- Verify all acceptance criteria met
- Deploy to production if validation passes

**Modified Files**:
```
src/server/PigFarmManagement.Server/
├── Features/PigPens/
│   ├── PigPenService.cs          (validation + logging)
│   └── PigPenEndpoints.cs        (user context extraction)
└── Infrastructure/Data/Repositories/
    ├── PigPenRepository.cs       (recalculation logic + tuple return)
    └── IRepositories.cs          (interface signature update)
```

---

````
