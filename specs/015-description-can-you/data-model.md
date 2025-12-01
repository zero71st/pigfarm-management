# Data Model: Recalculate consume rate when pig pen quantity changes

**Feature**: 015-description-can-you  
**Date**: 2025-12-01

## Overview

This feature requires **no schema changes** — all entities and fields already exist. The feature modifies business logic to recalculate existing `PigPenFormulaAssignment` fields when `PigPen.PigQty` changes.

## Entities Involved

### PigPen (existing)
Primary entity being updated. No schema changes required.

**Relevant Attributes**:
- `Id` (Guid, PK): Pig pen identifier
- `PigQty` (int): Number of pigs in pen — **field being modified**
- `IsCalculationLocked` (bool): Prevents updates when `true` (FR-006)
- `UpdatedAt` (DateTime): Timestamp updated on every save

**Validation Rules** (FR-004):
- `PigQty` must be between 1 and 100 (inclusive)
- Updates rejected when `IsCalculationLocked == true`

**Relationships**:
- One-to-many with `PigPenFormulaAssignment` (via `PigPenId` foreign key)

---

### PigPenFormulaAssignment (existing)
Child entity being recalculated when parent `PigPen.PigQty` changes. No schema changes required.

**Relevant Attributes** (all existing):
- `Id` (Guid, PK): Assignment identifier
- `PigPenId` (Guid, FK): Reference to parent pig pen
- `OriginalFormulaId` (Guid, FK): Reference to source `FeedFormula`
- `AssignedPigQuantity` (int): Snapshot of pig quantity — **recalculated from PigPen.PigQty**
- `AssignedBagPerPig` (decimal): Bags per pig — **synced from FeedFormula.ConsumeRate**
- `AssignedTotalBags` (decimal): Total bags — **computed as ceiling(AssignedBagPerPig * AssignedPigQuantity)**
- `IsActive` (bool): Only active assignments are recalculated
- `IsLocked` (bool): Locked assignments are never modified (FR-003)
- `UpdatedAt` (DateTime): Timestamp updated on recalculation

**Calculation Formula** (FR-002):
```
AssignedPigQuantity = PigPen.PigQty (updated)
AssignedBagPerPig = FeedFormula.ConsumeRate (synced from source formula)
AssignedTotalBags = Math.Ceiling(AssignedBagPerPig * AssignedPigQuantity)
```

**Mutability Rules**:
- **Mutable** (recalculated): `IsActive == true` AND `IsLocked == false`
- **Immutable** (preserved): `IsLocked == true` OR `IsActive == false`

**Relationships**:
- Many-to-one with `PigPen` (via `PigPenId`)
- Many-to-one with `FeedFormula` (via `OriginalFormulaId`)

---

### FeedFormula (existing, read-only)
Source entity providing latest `ConsumeRate` value for sync. No modifications.

**Relevant Attributes**:
- `Id` (Guid, PK): Formula identifier
- `ConsumeRate` (decimal): Bags per pig — **source of truth for AssignedBagPerPig**

**Usage in recalculation**:
- Loaded via `OriginalFormulaId` from each `PigPenFormulaAssignment`
- Current `ConsumeRate` value copied to `AssignedBagPerPig` (sync operation)

---

## State Transitions

### PigPen Update Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ User submits PUT /api/pigpens/{id} with updated PigQty         │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
         ┌───────────────────────────────┐
         │ Validate: IsCalculationLocked │
         └───────┬───────────────────────┘
                 │
        ┌────────┴────────┐
        │                 │
        ▼                 ▼
  ✅ false            ❌ true
  Continue         Return 400 BadRequest
                   "Cannot modify pig quantity: 
                    pen calculations are locked"
        │
        ▼
┌────────────────────────────┐
│ Validate: PigQty range     │
│ (1-100 inclusive)          │
└───────┬────────────────────┘
        │
   ┌────┴────┐
   │         │
   ▼         ▼
✅ valid   ❌ invalid
Continue  Return 400 BadRequest
          "Pig quantity must be 
           between 1 and 100"
   │
   ▼
┌─────────────────────────────────────┐
│ Load PigPen with FormulaAssignments │
│ (EF Include)                        │
└───────┬─────────────────────────────┘
        │
        ▼
┌────────────────────────────────────────────────────┐
│ For each PigPenFormulaAssignment:                  │
│   IF IsActive == true AND IsLocked == false:       │
│     1. Load FeedFormula by OriginalFormulaId       │
│     2. AssignedPigQuantity ← PigPen.PigQty        │
│     3. AssignedBagPerPig ← FeedFormula.ConsumeRate│
│     4. AssignedTotalBags ← ceiling(2 * 3)         │
│     5. UpdatedAt ← DateTime.UtcNow                │
└────────┬───────────────────────────────────────────┘
         │
         ▼
┌──────────────────────────────┐
│ SaveChangesAsync()           │
│ (atomic EF transaction)      │
└────────┬─────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────┐
│ Log change event:                              │
│ "Updated pig pen {Id} qty from {old} to {new} │
│  recalculated {count} assignments"            │
└────────┬───────────────────────────────────────┘
         │
         ▼
┌──────────────────────┐
│ Return 200 OK        │
│ (updated PigPen DTO) │
└──────────────────────┘
```

### Assignment State Transitions

Each `PigPenFormulaAssignment` transitions through states:

**Before update**:
```
State: { AssignedPigQuantity: N, AssignedBagPerPig: X, AssignedTotalBags: Y }
```

**After update** (if mutable):
```
State: { AssignedPigQuantity: M, AssignedBagPerPig: X', AssignedTotalBags: Y' }
```

Where:
- `M` = new `PigPen.PigQty`
- `X'` = `FeedFormula.ConsumeRate` (may differ from old `X` if formula changed)
- `Y'` = `Math.Ceiling(X' * M)`

**Immutable assignments** (locked or inactive): no state change.

---

## Calculation Examples

### Example 1: Simple recalculation (no formula change)

**Initial state**:
- `PigPen.PigQty` = 10
- `FeedFormula.ConsumeRate` = 2.5 bags/pig
- `PigPenFormulaAssignment`:
  - `AssignedPigQuantity` = 10
  - `AssignedBagPerPig` = 2.5
  - `AssignedTotalBags` = 25.0

**User updates** `PigPen.PigQty` to 12

**Recalculation**:
1. `AssignedPigQuantity` ← 12
2. `AssignedBagPerPig` ← 2.5 (unchanged from formula)
3. `AssignedTotalBags` ← `Math.Ceiling(2.5 * 12)` = `Math.Ceiling(30.0)` = **30.0**

**Final state**:
- `AssignedPigQuantity` = 12
- `AssignedBagPerPig` = 2.5
- `AssignedTotalBags` = 30.0

---

### Example 2: Recalculation with formula change

**Initial state**:
- `PigPen.PigQty` = 10
- `FeedFormula.ConsumeRate` was 2.5, now updated to 3.0 (admin changed formula)
- `PigPenFormulaAssignment`:
  - `AssignedPigQuantity` = 10
  - `AssignedBagPerPig` = 2.5 (stale)
  - `AssignedTotalBags` = 25.0

**User updates** `PigPen.PigQty` to 12

**Recalculation** (syncs with latest formula):
1. `AssignedPigQuantity` ← 12
2. `AssignedBagPerPig` ← 3.0 (**synced from formula's new ConsumeRate**)
3. `AssignedTotalBags` ← `Math.Ceiling(3.0 * 12)` = `Math.Ceiling(36.0)` = **36.0**

**Final state**:
- `AssignedPigQuantity` = 12
- `AssignedBagPerPig` = 3.0 (updated)
- `AssignedTotalBags` = 36.0

---

### Example 3: Ceiling rounding

**Initial state**:
- `PigPen.PigQty` = 10
- `FeedFormula.ConsumeRate` = 1.3 bags/pig

**User updates** `PigPen.PigQty` to 7

**Recalculation**:
1. `AssignedPigQuantity` ← 7
2. `AssignedBagPerPig` ← 1.3
3. `AssignedTotalBags` ← `Math.Ceiling(1.3 * 7)` = `Math.Ceiling(9.1)` = **10.0**

**Result**: 9.1 bags rounds up to 10 bags (delivers at least calculated amount per clarification)

---

## Data Integrity Guarantees

### Atomicity
- All assignment updates occur within single EF `SaveChangesAsync()` transaction
- If any update fails, entire operation rolls back (all-or-nothing)

### Immutability Preservation
- Locked assignments (`IsLocked == true`) are never modified
- Locked pens (`IsCalculationLocked == true`) reject updates at validation layer

### Audit Trail
- `UpdatedAt` timestamp records when recalculation occurred
- Structured log event captures: pen ID, old qty, new qty, user, assignment count (FR-005)

### Consistency
- `AssignedBagPerPig` always synced with source formula's latest `ConsumeRate`
- `AssignedTotalBags` always computed from current `AssignedBagPerPig` and `AssignedPigQuantity`
- No stale or orphaned assignment values after update

---

## No Schema Migrations Required

**Conclusion**: All necessary fields and relationships exist in current schema. This feature is a pure business-logic change with no DDL (Data Definition Language) statements.

**Verification**: Compare with existing entities in `src/shared/PigFarmManagement.Shared/Domain/Entities.cs` and `src/server/PigFarmManagement.Server/Infrastructure/Data/Entities/`.
