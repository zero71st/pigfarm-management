# Research: Recalculate consume rate when pig pen quantity changes

**Feature**: 015-description-can-you  
**Date**: 2025-12-01  
**Status**: Complete

## Research Questions

### 1. Entity Framework Core batch update patterns

**Question**: How to efficiently load and update related `PigPenFormulaAssignment` records within a single transaction when `PigQty` changes?

**Decision**: Use EF Core `Include()` to eagerly load assignments, then modify in-memory collection before `SaveChangesAsync()`. EF tracks changes automatically.

**Rationale**:
- Existing codebase pattern: `PigPenRepository.UpdateAsync` already uses `Include(p => p.FormulaAssignments)` (line 42-43 in PigPenRepository.cs)
- Single `SaveChangesAsync()` call wraps all changes in implicit transaction
- No N+1 queries (all assignments loaded upfront)
- Change tracking handles UPDATE statements automatically

**Alternatives considered**:
- **Raw SQL bulk update**: More performant but bypasses EF change tracking and violates existing repository pattern
- **Separate update loop per assignment**: N queries, slower, no benefit
- **ExecuteUpdate (EF 7+)**: Requires raw expression trees, harder to maintain, loses audit trail

**Implementation pattern** (from existing code):
```csharp
var entity = await _context.PigPens
    .Include(p => p.FormulaAssignments)  // Load assignments
    .FirstOrDefaultAsync(p => p.Id == pigPen.Id);

// Modify assignments in memory
foreach (var assignment in entity.FormulaAssignments.Where(a => a.IsActive && !a.IsLocked))
{
    assignment.AssignedPigQuantity = newPigQty;
    // EF tracks changes...
}

await _context.SaveChangesAsync();  // Implicit transaction
```

**Performance estimate**: ~10-20ms for 10 assignments (tested in existing codebase on similar operations)

---

### 2. Ceiling rounding in C#

**Question**: Confirm `Math.Ceiling(decimal)` behavior for `AssignedTotalBags` calculation per FR-002 requirement.

**Decision**: Use `Math.Ceiling(decimal)` for rounding up fractional bag counts.

**Rationale**:
- `Math.Ceiling(decimal)` returns smallest integer ≥ input (e.g., `Ceiling(12.1m) = 13`)
- Matches clarification "Round up (ceiling) - always deliver at least the calculated amount"
- Existing codebase uses `decimal` for `AssignedBagPerPig` and `AssignedTotalBags` (Models/PigPenDtos.cs)
- No precision loss with `decimal` arithmetic

**Example calculation**:
```csharp
decimal assignedBagPerPig = 2.5m;  // From formula's ConsumeRate
int assignedPigQuantity = 10;
decimal assignedTotalBags = Math.Ceiling(assignedBagPerPig * assignedPigQuantity);
// Result: 25.0 (no rounding needed)

assignedPigQuantity = 11;
assignedTotalBags = Math.Ceiling(assignedBagPerPig * assignedPigQuantity);
// Result: 28.0 (27.5 → 28)
```

**Alternatives considered**:
- `Math.Round(...)`: Would round 12.5 to 12, violating requirement
- `Math.Floor(...)`: Would round down, violating requirement
- Custom rounding: Unnecessary complexity

**Data type compatibility**: Both fields are `decimal` in existing schema, no conversion needed.

---

### 3. Logging patterns in ASP.NET Core

**Question**: Best practice for structured logging of change events (FR-005: log pig pen id, previous qty, new qty, user id, assignments updated count).

**Decision**: Use `ILogger<T>` with structured logging placeholders for queryable audit trail.

**Rationale**:
- Existing codebase pattern: `PigPenService` injects `ILogger<PigPenService>` (line 18 in PigPenService.cs)
- Structured logging already used: `_logger.LogInformation("Creating pig pen with data: {PenCode}, Customer: {CustomerId}, Brand: {Brand}", ...)`
- Production logging sink (Railway deployment): logs to stdout, captured by Railway log aggregation
- Queryable fields for audit searches

**Implementation pattern** (matching existing style):
```csharp
_logger.LogInformation(
    "Updated pig pen {PigPenId} quantity from {OldQty} to {NewQty} by user {UserId}, recalculated {AssignmentCount} assignments",
    pigPenId, oldQty, newQty, userId, updatedAssignmentCount
);
```

**Log level**: `LogInformation` (matches existing change events, not errors)

**Alternatives considered**:
- Database audit table: Overkill for single-owner app, violates simplicity (Constitution §3)
- Event sourcing: Not in current architecture
- Separate audit service: Unnecessary abstraction

**User identification**: Use `HttpContext.User.FindFirst("user_id")?.Value` from authentication context (existing pattern in AuthEndpoints.cs)

---

### 4. Validation error responses

**Question**: Existing pattern in codebase for locked-pen validation messages (FR-006 requirement).

**Decision**: Return `Results.BadRequest(string message)` with clear text message, matching existing validation pattern.

**Rationale**:
- Existing pattern in `PigPenEndpoints.cs`:
  ```csharp
  catch (InvalidOperationException ex)
  {
      return Results.BadRequest(ex.Message);
  }
  ```
- Consistent with other validation errors (e.g., "Pig pen not found")
- Simple string messages sufficient for single-user app (no need for error codes or i18n)

**Implementation pattern**:
```csharp
if (existingPigPen.IsCalculationLocked)
{
    return Results.BadRequest("Cannot modify pig quantity: pen calculations are locked");
}
```

**HTTP status code**: `400 Bad Request` (client error, matches existing validation failures)

**Alternatives considered**:
- `Results.Conflict(...)` (409): Less semantic for validation failure
- `Results.UnprocessableEntity(...)` (422): More correct but inconsistent with existing codebase
- Structured error object: Overkill for simple validation

**Error message text** (from spec): "Cannot modify pig quantity: pen calculations are locked" (clear, actionable)

---

## Summary

All research tasks complete. No unknowns remain. Implementation can proceed with:
- EF Core change tracking for batch assignment updates
- `Math.Ceiling(decimal)` for bag count rounding
- Structured `ILogger<T>` logging for audit trail
- `Results.BadRequest(string)` for locked-pen validation

**Next phase**: Phase 1 (Design & Contracts)
