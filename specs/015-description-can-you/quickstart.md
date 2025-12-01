# Quickstart: Recalculate consume rate when pig pen quantity changes

**Feature**: 015-description-can-you  
**Date**: 2025-12-01  
**Estimated Time**: 15-20 minutes

## Overview

This quickstart provides step-by-step manual validation scenarios to verify the recalculation feature works correctly. Execute these scenarios after implementation is complete.

**Prerequisites**:
- Server running locally (`dotnet run --project src/server/PigFarmManagement.Server --urls http://localhost:5000`)
- Valid API key (obtain via `POST /api/auth/login` or use admin API key from environment)
- HTTP client (Postman, curl, or PowerShell `Invoke-RestMethod`)
- At least one pig pen with formula assignments in the database

---

## Scenario 1: Basic Recalculation (Happy Path)

**Goal**: Verify that updating `PigQty` recalculates active assignments correctly.

### Setup
1. Identify a pig pen with active formula assignments:
   ```powershell
   $apiKey = "your-api-key-here"
   $pen = Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens" -Headers @{"X-Api-Key"=$apiKey} | Select-Object -First 1
   $penId = $pen.id
   Write-Host "Using pen: $($pen.penCode) with $($pen.pigQty) pigs"
   ```

2. Record initial state:
   ```powershell
   $initial = Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens/$penId" -Headers @{"X-Api-Key"=$apiKey}
   $initialQty = $initial.pigQty
   $initialAssignments = $initial.formulaAssignments | Where-Object { $_.isActive -and -not $_.isLocked }
   Write-Host "Initial quantity: $initialQty"
   $initialAssignments | Format-Table assignedPigQuantity, assignedBagPerPig, assignedTotalBags
   ```

### Execution
3. Update `PigQty` to a new value (e.g., increase by 2):
   ```powershell
   $newQty = $initialQty + 2
   $updateBody = @{ pigQty = $newQty } | ConvertTo-Json
   $updated = Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens/$penId" `
       -Method PUT `
       -Headers @{"X-Api-Key"=$apiKey; "Content-Type"="application/json"} `
       -Body $updateBody
   ```

### Verification
4. Check that `PigQty` was updated:
   ```powershell
   if ($updated.pigQty -ne $newQty) {
       Write-Error "FAIL: PigQty not updated. Expected $newQty, got $($updated.pigQty)"
   } else {
       Write-Host "✅ PASS: PigQty updated to $newQty"
   }
   ```

5. Check that formula assignments were recalculated:
   ```powershell
   $updatedAssignments = $updated.formulaAssignments | Where-Object { $_.isActive -and -not $_.isLocked }
   foreach ($assignment in $updatedAssignments) {
       $expectedTotal = [Math]::Ceiling($assignment.assignedBagPerPig * $newQty)
       if ($assignment.assignedPigQuantity -ne $newQty) {
           Write-Error "FAIL: Assignment $($assignment.id) pig quantity not updated"
       } elseif ($assignment.assignedTotalBags -ne $expectedTotal) {
           Write-Error "FAIL: Assignment $($assignment.id) total bags incorrect. Expected $expectedTotal, got $($assignment.assignedTotalBags)"
       } else {
           Write-Host "✅ PASS: Assignment $($assignment.id) recalculated correctly"
       }
   }
   ```

6. Check that `UpdatedAt` timestamp changed:
   ```powershell
   if ($updated.updatedAt -le $initial.updatedAt) {
       Write-Error "FAIL: UpdatedAt timestamp not updated"
   } else {
       Write-Host "✅ PASS: UpdatedAt timestamp updated"
   }
   ```

**Expected Result**: All checks pass. `PigQty` updated, all active assignments recalculated with correct `assignedTotalBags` using ceiling rounding.

---

## Scenario 2: Locked Pen Rejection

**Goal**: Verify that locked pens (`IsCalculationLocked == true`) reject `PigQty` updates.

### Setup
1. Find or create a locked pig pen:
   ```powershell
   $lockedPen = Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens" -Headers @{"X-Api-Key"=$apiKey} |
       Where-Object { $_.isCalculationLocked -eq $true } | Select-Object -First 1
   
   if (-not $lockedPen) {
       Write-Host "No locked pen found. Force-closing a pen to create one..."
       $penToLock = Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens" -Headers @{"X-Api-Key"=$apiKey} | Select-Object -First 1
       $lockedPen = Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens/$($penToLock.id)/force-close" `
           -Method POST -Headers @{"X-Api-Key"=$apiKey}
   }
   
   $lockedPenId = $lockedPen.id
   Write-Host "Using locked pen: $($lockedPen.penCode)"
   ```

### Execution
2. Attempt to update `PigQty`:
   ```powershell
   $newQty = $lockedPen.pigQty + 5
   $updateBody = @{ pigQty = $newQty } | ConvertTo-Json
   
   try {
       $result = Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens/$lockedPenId" `
           -Method PUT `
           -Headers @{"X-Api-Key"=$apiKey; "Content-Type"="application/json"} `
           -Body $updateBody
       Write-Error "FAIL: Update should have been rejected but succeeded"
   } catch {
       $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
       if ($errorResponse.error -like "*locked*") {
           Write-Host "✅ PASS: Update rejected with locked pen error: $($errorResponse.error)"
       } else {
           Write-Error "FAIL: Wrong error message: $($errorResponse.error)"
       }
   }
   ```

**Expected Result**: HTTP 400 Bad Request with message "Cannot modify pig quantity: pen calculations are locked".

---

## Scenario 3: Quantity Validation (Out of Range)

**Goal**: Verify that `PigQty` values outside 1-100 range are rejected.

### Execution
1. Attempt to set `PigQty` to 0 (below minimum):
   ```powershell
   $pen = Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens" -Headers @{"X-Api-Key"=$apiKey} |
       Where-Object { -not $_.isCalculationLocked } | Select-Object -First 1
   
   $updateBody = @{ pigQty = 0 } | ConvertTo-Json
   
   try {
       $result = Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens/$($pen.id)" `
           -Method PUT `
           -Headers @{"X-Api-Key"=$apiKey; "Content-Type"="application/json"} `
           -Body $updateBody
       Write-Error "FAIL: Zero quantity should have been rejected"
   } catch {
       $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
       if ($errorResponse.error -like "*between 1 and 100*") {
           Write-Host "✅ PASS: Zero quantity rejected: $($errorResponse.error)"
       } else {
           Write-Error "FAIL: Wrong error message: $($errorResponse.error)"
       }
   }
   ```

2. Attempt to set `PigQty` to 101 (above maximum):
   ```powershell
   $updateBody = @{ pigQty = 101 } | ConvertTo-Json
   
   try {
       $result = Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens/$($pen.id)" `
           -Method PUT `
           -Headers @{"X-Api-Key"=$apiKey; "Content-Type"="application/json"} `
           -Body $updateBody
       Write-Error "FAIL: Quantity 101 should have been rejected"
   } catch {
       $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
       if ($errorResponse.error -like "*between 1 and 100*") {
           Write-Host "✅ PASS: Quantity 101 rejected: $($errorResponse.error)"
       } else {
           Write-Error "FAIL: Wrong error message: $($errorResponse.error)"
       }
   }
   ```

**Expected Result**: Both requests return HTTP 400 with validation error "Pig quantity must be between 1 and 100".

---

## Scenario 4: Ceiling Rounding Verification

**Goal**: Verify that fractional `assignedTotalBags` values are rounded up using ceiling.

### Setup
1. Find a pen with formula assignment where `assignedBagPerPig` has decimal places:
   ```powershell
   $pen = Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens" -Headers @{"X-Api-Key"=$apiKey} |
       Where-Object {
           $_.formulaAssignments |
           Where-Object { $_.isActive -and $_.assignedBagPerPig -match '\.\d+' }
       } | Select-Object -First 1
   
   if (-not $pen) {
       Write-Host "⚠️ SKIP: No pen with decimal assignedBagPerPig found"
       return
   }
   
   $penId = $pen.id
   $assignment = $pen.formulaAssignments | Where-Object { $_.isActive } | Select-Object -First 1
   Write-Host "Using pen $($pen.penCode) with assignedBagPerPig = $($assignment.assignedBagPerPig)"
   ```

### Execution
2. Update `PigQty` to a value that will produce fractional result:
   ```powershell
   # Example: If assignedBagPerPig = 1.3, newQty = 7 → 1.3 * 7 = 9.1 → ceiling = 10
   $newQty = 7
   $updateBody = @{ pigQty = $newQty } | ConvertTo-Json
   $updated = Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens/$penId" `
       -Method PUT `
       -Headers @{"X-Api-Key"=$apiKey; "Content-Type"="application/json"} `
       -Body $updateBody
   ```

### Verification
3. Check ceiling rounding:
   ```powershell
   $updatedAssignment = $updated.formulaAssignments | Where-Object { $_.id -eq $assignment.id }
   $exactTotal = $updatedAssignment.assignedBagPerPig * $newQty
   $expectedCeiling = [Math]::Ceiling($exactTotal)
   
   Write-Host "Exact calculation: $($updatedAssignment.assignedBagPerPig) * $newQty = $exactTotal"
   Write-Host "Expected ceiling: $expectedCeiling"
   Write-Host "Actual assignedTotalBags: $($updatedAssignment.assignedTotalBags)"
   
   if ($updatedAssignment.assignedTotalBags -eq $expectedCeiling) {
       Write-Host "✅ PASS: Ceiling rounding applied correctly"
   } else {
       Write-Error "FAIL: Expected $expectedCeiling, got $($updatedAssignment.assignedTotalBags)"
   }
   ```

**Expected Result**: `assignedTotalBags` equals `Math.Ceiling(assignedBagPerPig * newQty)`, rounding up fractional values.

---

## Scenario 5: Formula Sync Verification

**Goal**: Verify that `assignedBagPerPig` is synced from source formula's latest `ConsumeRate`.

### Setup
1. Manually update a feed formula's `ConsumeRate` in the database or via admin endpoint (if available):
   ```powershell
   # This step requires database access or admin endpoint - adapt to your environment
   Write-Host "⚠️ Manual step: Update a FeedFormula.ConsumeRate value in the database"
   Write-Host "Example SQL: UPDATE FeedFormulas SET ConsumeRate = 3.5 WHERE Id = '...'"
   Read-Host "Press Enter when formula updated"
   ```

2. Identify a pig pen using that formula:
   ```powershell
   $formulaId = "your-updated-formula-guid"
   $pen = Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens" -Headers @{"X-Api-Key"=$apiKey} |
       Where-Object {
           $_.formulaAssignments | Where-Object { $_.originalFormulaId -eq $formulaId }
       } | Select-Object -First 1
   
   $oldAssignment = $pen.formulaAssignments | Where-Object { $_.originalFormulaId -eq $formulaId }
   Write-Host "Current assignedBagPerPig: $($oldAssignment.assignedBagPerPig)"
   ```

### Execution
3. Update the pig pen's `PigQty`:
   ```powershell
   $newQty = $pen.pigQty + 1
   $updateBody = @{ pigQty = $newQty } | ConvertTo-Json
   $updated = Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens/$($pen.id)" `
       -Method PUT `
       -Headers @{"X-Api-Key"=$apiKey; "Content-Type"="application/json"} `
       -Body $updateBody
   ```

### Verification
4. Check that `assignedBagPerPig` was synced:
   ```powershell
   $updatedAssignment = $updated.formulaAssignments | Where-Object { $_.originalFormulaId -eq $formulaId }
   
   # Fetch the current formula ConsumeRate
   # (This requires formula endpoint - adapt to your API)
   Write-Host "Current assignedBagPerPig: $($updatedAssignment.assignedBagPerPig)"
   Write-Host "✅ Verify manually that this matches the formula's ConsumeRate in the database"
   ```

**Expected Result**: `assignedBagPerPig` matches the formula's latest `ConsumeRate` value.

---

## Scenario 6: Logging Verification

**Goal**: Verify that change events are logged (FR-005).

### Setup
1. Ensure logging is visible (check server console or log file).

### Execution
2. Update a pig pen's `PigQty`:
   ```powershell
   $pen = Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens" -Headers @{"X-Api-Key"=$apiKey} |
       Where-Object { -not $_.isCalculationLocked } | Select-Object -First 1
   
   $oldQty = $pen.pigQty
   $newQty = $oldQty + 3
   $updateBody = @{ pigQty = $newQty } | ConvertTo-Json
   
   Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens/$($pen.id)" `
       -Method PUT `
       -Headers @{"X-Api-Key"=$apiKey; "Content-Type"="application/json"} `
       -Body $updateBody | Out-Null
   ```

### Verification
3. Check server logs for structured log entry:
   ```
   Expected log format:
   Updated pig pen {PigPenId} quantity from {OldQty} to {NewQty} by user {UserId}, recalculated {AssignmentCount} assignments
   
   Example:
   Updated pig pen 123e4567-e89b-12d3-a456-426614174000 quantity from 10 to 13 by user admin@company.com, recalculated 3 assignments
   ```

4. Verify log entry contains:
   - ✅ Pig pen ID
   - ✅ Old quantity value
   - ✅ New quantity value
   - ✅ User ID
   - ✅ Number of assignments updated

**Expected Result**: Log entry appears with all required fields (FR-005 compliance).

---

## Summary Checklist

After running all scenarios, verify:

- [ ] **Scenario 1**: Basic recalculation works (happy path)
- [ ] **Scenario 2**: Locked pens reject updates
- [ ] **Scenario 3**: Out-of-range quantities rejected (0 and 101)
- [ ] **Scenario 4**: Ceiling rounding applied correctly
- [ ] **Scenario 5**: Formula `ConsumeRate` synced to `assignedBagPerPig`
- [ ] **Scenario 6**: Change events logged with all required fields

**If all scenarios pass**: Feature is ready for production.

**If any scenario fails**: Review implementation against data-model.md and contracts/UpdatePigPenEndpoint.yml.

---

## Troubleshooting

### Common Issues

1. **"Unauthorized" error**:
   - Verify API key is valid: `Invoke-RestMethod -Uri "http://localhost:5000/api/pigpens" -Headers @{"X-Api-Key"=$apiKey}`
   - Check server logs for authentication failures

2. **No locked pen found** (Scenario 2):
   - Create one manually: `POST /api/pigpens/{id}/force-close`
   - Or use database: `UPDATE PigPens SET IsCalculationLocked = 1 WHERE Id = '...'`

3. **No decimal assignedBagPerPig** (Scenario 4):
   - Update a formula: `UPDATE FeedFormulas SET ConsumeRate = 1.3 WHERE Id = '...'`
   - Regenerate assignments: `POST /api/pigpens/{id}/regenerate-assignments`

4. **Logs not visible** (Scenario 6):
   - Check console output where server is running
   - Or check log file (if configured): `logs/pigfarm-{date}.log`

---

**End of Quickstart**
