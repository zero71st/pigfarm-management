# Quick Start: Invoice Management Tab Manual Validation

**Feature**: Invoice Management Tab in Feed History Section  
**Branch**: `014-add-invoice-list`  
**Date**: 2025-11-30

## Prerequisites

1. **Development Environment Running**:
   ```powershell
   # Terminal 1: Start server
   cd src/server/PigFarmManagement.Server
   dotnet run --urls http://localhost:5000

   # Terminal 2: Start client
   cd src/client/PigFarmManagement.Client
   dotnet run --urls http://localhost:7000
   ```

2. **Test Data Setup**:
   - At least one pig pen with feed items imported from POSPOS
   - Feed items should have `InvoiceReferenceCode` populated
   - Verify via database: `SELECT DISTINCT InvoiceReferenceCode FROM Feeds WHERE PigPenId = '{somePigPenId}'`

3. **Browser**: Chrome, Firefox, Safari, or Edge

## Validation Scenarios

### Scenario 1: View Invoice List Grouped by Reference Code

**Objective**: Verify invoices are correctly grouped and displayed

**Steps**:
1. Navigate to `http://localhost:7000`
2. Login with valid credentials
3. Navigate to Customers page
4. Click on a customer with pig pens
5. Click on a pig pen that has feed imports from POSPOS
6. Locate the "Feed History" section

**Expected Results**:
- ✅ Section shows TWO tabs: "Invoice Management" (first/default) and "Feed History" (second)
- ✅ Invoice Management tab is active by default
- ✅ Invoice list displays with columns:
  - Invoice Reference Code
  - Transaction Code (from first item)
  - Total Amount (sum with ฿ symbol)
  - Invoice Date (formatted as yyyy-MM-dd)
  - Item Count
- ✅ Invoices are sorted by date in ASCENDING order (oldest first)
- ✅ Only invoices with non-null/non-empty `InvoiceReferenceCode` are shown
- ✅ Each unique `InvoiceReferenceCode` appears exactly once

**Acceptance Criteria**:
- [ ] Tab interface renders correctly
- [ ] Invoice grouping is accurate (verify count matches database)
- [ ] All required columns display with correct data
- [ ] Sort order is ascending by date
- [ ] No invoices with null/empty reference codes appear

---

### Scenario 2: Switch Between Invoice and Feed History Tabs

**Objective**: Verify tab switching works and maintains independent state

**Steps**:
1. From Scenario 1, ensure you're on Invoice Management tab
2. Click the "Feed History" tab
3. Observe the feed history table (existing functionality)
4. Click back to "Invoice Management" tab

**Expected Results**:
- ✅ Feed History tab shows existing feed detail view (no changes to existing functionality)
- ✅ Tab switching is instant (<500ms)
- ✅ Each tab maintains its own state (scrolling position, sorting, filters if any)
- ✅ Data is consistent across both views
- ✅ Total feed count in Feed History matches sum of item counts across all invoices

**Acceptance Criteria**:
- [ ] Tab switching works smoothly
- [ ] No visual glitches during switch
- [ ] Feed History tab preserves existing functionality
- [ ] Data consistency between tabs verified

---

### Scenario 3: Delete Invoice with Confirmation

**Objective**: Verify invoice deletion removes all associated feed items

**Steps**:
1. From Invoice Management tab, note the current total number of invoices
2. Select an invoice with known item count (e.g., 5 items)
3. Click the "Delete" button for that invoice
4. Observe the confirmation dialog

**Expected Dialog Content**:
- ✅ Invoice reference code displayed
- ✅ Number of feed items to be deleted shown (e.g., "5 items")
- ✅ Total amount displayed
- ✅ Warning message: "This action cannot be undone"
- ✅ Two buttons: "Cancel" and "Delete" (or "Confirm")

**Steps (continued)**:
5. Click "Cancel" → dialog closes, no changes
6. Click "Delete" again, then click "Confirm"

**Expected Results After Deletion**:
- ✅ Success message appears (e.g., "Successfully deleted 5 feed items for invoice INV-001")
- ✅ Invoice disappears from the Invoice Management list
- ✅ Invoice count decreases by 1
- ✅ Switch to Feed History tab → feed items for that invoice are gone
- ✅ Total feed count decreases by the deleted item count

**Database Verification** (optional):
```sql
-- Should return 0 rows
SELECT * FROM Feeds 
WHERE InvoiceReferenceCode = '{deleted-invoice-ref-code}';
```

**Acceptance Criteria**:
- [ ] Confirmation dialog displays correctly
- [ ] Cancel works (no deletion occurs)
- [ ] Delete removes ALL feed items for that invoice
- [ ] Both tabs update automatically
- [ ] Success message appears
- [ ] Database reflects deletion

---

### Scenario 4: Import Feeds and Verify Tab Updates

**Objective**: Verify existing import functionality populates both tabs correctly

**Steps**:
1. From pig pen detail page, click "Import Feeds" button (existing button)
2. Complete the existing import dialog:
   - Select date range
   - Click import
   - Wait for import to complete
3. Observe the import result dialog (existing functionality)
4. Close the dialog

**Expected Results**:
- ✅ Import dialog is the SAME as existing feed import (no new UI)
- ✅ Import completes successfully
- ✅ Import result shows count of imported items
- ✅ After closing dialog, BOTH tabs automatically refresh:
  - Invoice Management tab shows new/updated invoices
  - Feed History tab shows new feed items
- ✅ New invoices appear with correct grouping by `InvoiceReferenceCode`
- ✅ Invoice count increases by number of unique invoice references imported

**Acceptance Criteria**:
- [ ] Existing import dialog unchanged
- [ ] Import populates InvoiceReferenceCode correctly
- [ ] Both tabs refresh automatically post-import
- [ ] New invoices appear in Invoice Management
- [ ] New feeds appear in Feed History
- [ ] Counts are consistent

---

### Scenario 5: Handle Empty State (No Invoices)

**Objective**: Verify UI handles pig pen with no invoiced feeds

**Steps**:
1. Navigate to a pig pen that has either:
   - No feed items at all, OR
   - Only manually added feeds (no `InvoiceReferenceCode`)
2. View the Invoice Management tab

**Expected Results**:
- ✅ Tab renders without errors
- ✅ Shows empty state message (e.g., "No invoices found" or "No invoice data available")
- ✅ No delete buttons or invoice rows displayed
- ✅ Feed History tab still shows manually added feeds (if any)

**Acceptance Criteria**:
- [ ] Empty state renders correctly
- [ ] No JavaScript errors in console
- [ ] Feed History tab unaffected
- [ ] Can still import feeds via existing button

---

### Scenario 6: Performance Check (Optional)

**Objective**: Verify performance meets expectations

**Steps**:
1. Navigate to a pig pen with 50-100 feed items grouped into 10-20 invoices
2. Switch between tabs multiple times
3. Delete an invoice
4. Measure approximate response times

**Expected Performance**:
- ✅ Tab switch: <500ms
- ✅ Invoice list load: <1s
- ✅ Delete operation: <1s (including confirmation)
- ✅ Both tabs refresh: <2s total

**Acceptance Criteria**:
- [ ] UI remains responsive
- [ ] No noticeable lag during tab switch
- [ ] Delete operation completes quickly
- [ ] No browser console errors

---

## Regression Checks

**Verify Existing Functionality Still Works**:
- [ ] Feed History tab displays all feed items correctly
- [ ] Existing "Import Feeds" button works
- [ ] Feed formula comparison section unaffected
- [ ] Pig pen summary calculations correct
- [ ] No visual regressions in pig pen detail page layout

---

## Rollback Procedure

If critical issues found:

1. **Database**: No schema changes made - no rollback needed
2. **Code**: Revert branch merge or PR
   ```powershell
   git checkout main
   git pull origin main
   ```
3. **Deployment**: Redeploy previous version

---

## Success Criteria Summary

All scenarios must pass for feature acceptance:
- ✅ Invoice Management tab displays correctly with grouped invoices
- ✅ Tab switching works smoothly
- ✅ Invoice deletion removes all associated feed items
- ✅ Existing import functionality works and populates both tabs
- ✅ Empty state handled gracefully
- ✅ Performance meets targets
- ✅ No regressions to existing feed history functionality

---

## Known Limitations

1. **Invoice Reference Format**: Depends on POSPOS API format - no validation on format
2. **Concurrent Deletions**: Single-user application - no optimistic locking needed
3. **Pagination**: Not implemented for initial release - acceptable for typical pig pen scale (10-50 invoices)
4. **Undo Delete**: No undo functionality - deletion is permanent (per spec)

---

## Support Information

**Developer**: Review implementation in `PigPenDetailPage.razor`, `InvoiceListTab.razor`, `PigPenEndpoints.cs`
**Logs**: Check browser console and server logs for errors
**Database**: SQLite (dev) or PostgreSQL (prod) - query `Feeds` table for verification
