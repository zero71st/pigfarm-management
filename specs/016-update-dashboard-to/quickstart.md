# Quickstart: Dashboard Thai Translation and Business Metrics Enhancement

**Feature**: 016-update-dashboard-to  
**Date**: 2025-12-02  
**Purpose**: Manual validation checklist for verifying dashboard UI translation and metrics calculations

---

## Prerequisites

**Before Testing**:
1. Server running on `http://localhost:5000`
2. Client running on `http://localhost:7000`
3. Database seeded with test data:
   - At least 15 customers with active pig pens (for pagination testing)
   - Mix of Cash and Project pen types
   - At least 5 closed pig pens (to verify filtering)
   - Customers with deposits paid (for Customer's Capital testing)

**Test User**: Admin account with dashboard access

---

## Validation Scenarios

### Scenario 1: Activity Metrics Row (FR-002)

**Expected Behavior**: First row shows 3 cards with Thai labels, active pens only

**Steps**:
1. Navigate to Dashboard page (`/dashboard`)
2. Locate the first row of metric cards (top of page)
3. Verify card 1 label: **"คอกสุกรที่ใช้งานทั้งหมด"** (Total Active Pig Pens)
4. Verify card 2 label: **"จำนวนสุกรที่ใช้งานทั้งหมด"** (Total Active Pigs)
5. Verify card 3 label: **"จำนวนลูกค้าที่ใช้งานทั้งหมด"** (Total Active Customers)

**Validation Checks**:
- [ ] All 3 labels displayed in Thai
- [ ] Card values are positive integers (>= 0)
- [ ] Card values exclude closed pig pens (verify against database count)

**Acceptance Criteria**: ✅ PASS if all 3 Thai labels visible and counts match active pens only

---

### Scenario 2: Financial Metrics Row (FR-003)

**Expected Behavior**: Second row shows 4 cards with Thai labels and correct formulas

**Steps**:
1. Scroll down to second row of metric cards
2. Verify card 1 label: **"เงินลงทุนทั้งหมด"** (Total Investment)
   - Formula: Owner's Capital + Customer's Capital + Profit/Loss
3. Verify card 2 label: **"เงินลงทุนส่วนเจ้าของรวม"** (Total Owner's Capital)
   - Formula: TotalCost - TotalDeposit
4. Verify card 3 label: **"เงินลงทุนส่วนลูกค้า"** (Total Customer's Capital)
   - Formula: TotalDeposit
5. Verify card 4 label: **"กำไร/ขาดทุนทั้งหมด"** (Total Profit/Loss)
   - Formula: TotalPriceIncludeDiscount - TotalCost

**Validation Checks**:
- [ ] All 4 labels displayed in Thai
- [ ] Currency formatted as **฿X,XXX.XX** (Thai Baht with commas, 2 decimals)
- [ ] Profit/Loss shows **green** for positive, **red** for negative
- [ ] Manual formula verification:
  - Owner's Capital = Cost - Deposit (use calculator)
  - Investment = Owner + Customer + Profit (verify sum)

**Acceptance Criteria**: ✅ PASS if all 4 Thai labels, correct formatting, and manual formula checks match

---

### Scenario 3: Section Order (FR-005)

**Expected Behavior**: Project Operations section appears before Cash Operations

**Steps**:
1. Scroll down past metric cards to breakdown sections
2. Identify first section header
3. Identify second section header

**Validation Checks**:
- [ ] First section header: **"การดำเนินงานโครงการ"** (Project Operations)
- [ ] Second section header: **"การดำเนินงานเงินสด"** (Cash Operations)
- [ ] Project section positioned visually above Cash section

**Acceptance Criteria**: ✅ PASS if Project section appears before Cash section

---

### Scenario 4: Cash/Project Sections - 6 Metrics (FR-005)

**Expected Behavior**: Both sections display 6 metrics each with Thai labels

**Steps**:
1. In **Project Operations** section, count metrics displayed
2. Verify metric labels (Thai):
   - **"คอกสุกรที่ใช้งาน"** (Active Pig Pens)
   - **"จำนวนสุกร"** (Total Pigs)
   - **"เงินลงทุน"** (Total Investment)
   - **"เงินลงทุนส่วนเจ้าของ"** (Owner's Capital)
   - **"เงินลงทุนส่วนลูกค้า"** (Customer's Capital)
   - **"กำไร/ขาดทุน"** (Profit/Loss)
3. Repeat for **Cash Operations** section

**Validation Checks**:
- [ ] Project section shows exactly 6 metrics
- [ ] Cash section shows exactly 6 metrics
- [ ] All 12 labels (6 per section) in Thai
- [ ] Values include only active pens of respective type (Project or Cash)

**Acceptance Criteria**: ✅ PASS if both sections show 6 Thai metrics with active-only data

---

### Scenario 5: Customer Table - 7 Columns with Thai Headers (FR-009)

**Expected Behavior**: Table displays 7 columns with Thai headers, 10 customers per page

**Steps**:
1. Scroll down to Customer Performance Summary table
2. Verify column headers (Thai, left to right):
   1. **"ชื่อลูกค้า"** (Customer Name)
   2. **"จำนวนคอก"** (Pig Pen Count)
   3. **"จำนวนสุกร"** (Total Pigs)
   4. **"เงินลงทุนทั้งหมด"** (Total Investment)
   5. **"เงินลงทุนส่วนเจ้าของรวม"** (Total Owner's Capital)
   6. **"รวมเงินมัดจำ"** (Total Customer's Capital)
   7. **"กำไร/ขาดทุนรวม"** (Total Profit/Loss)
3. Verify table shows exactly 10 rows (if >= 10 customers exist)
4. Verify pagination controls visible at bottom

**Validation Checks**:
- [ ] All 7 column headers in Thai
- [ ] Table shows maximum 10 customers per page
- [ ] Pagination controls visible (if total customers > 10)
- [ ] Currency columns formatted as **฿X,XXX.XX**
- [ ] Customers with only closed pens NOT shown in table

**Acceptance Criteria**: ✅ PASS if 7 Thai headers, 10 rows/page, pagination visible

---

### Scenario 6: Pagination Navigation (FR-006)

**Expected Behavior**: Pagination allows navigation between pages, <500ms response

**Prerequisites**: Database has > 10 customers with active pens

**Steps**:
1. In Customer Performance table, locate pagination controls at bottom
2. Note current page indicator (e.g., "หน้า 1 จาก 2" = Page 1 of 2)
3. Click "Next Page" button (right arrow)
4. Observe page transition time
5. Verify different customers displayed on page 2
6. Click "Previous Page" button (left arrow)

**Validation Checks**:
- [ ] Page indicator shows **"หน้า X จาก Y"** in Thai
- [ ] Next/Previous buttons functional
- [ ] Page transition < 500ms (use browser DevTools Network tab)
- [ ] Different customer rows on each page (no duplicates)

**Acceptance Criteria**: ✅ PASS if pagination works smoothly and < 500ms transitions

---

### Scenario 7: Empty State - All Closed Pens (Edge Case)

**Expected Behavior**: Dashboard shows 0 values with Thai labels, no errors

**Prerequisites**: All pig pens in database set to Status = "Closed" (temporarily for testing)

**Steps**:
1. Set all pig pens to closed status via database or admin tool
2. Refresh dashboard page
3. Verify activity metrics row shows:
   - คอกสุกรที่ใช้งานทั้งหมด: **0**
   - จำนวนสุกรที่ใช้งานทั้งหมด: **0**
   - จำนวนลูกค้าที่ใช้งานทั้งหมด: **0**
4. Verify financial metrics row shows:
   - All values: **฿0.00**
5. Verify Cash/Project sections show **0** for all metrics
6. Verify Customer Performance table is empty

**Validation Checks**:
- [ ] No JavaScript errors in browser console
- [ ] No server errors in logs
- [ ] All Thai labels still visible
- [ ] All metric values display as 0 or ฿0.00
- [ ] Customer table shows "No records found" message (if applicable)
- [ ] Pagination controls hidden (0 customers)

**Acceptance Criteria**: ✅ PASS if dashboard gracefully displays empty state with Thai labels

---

### Scenario 8: Pagination Hidden for < 10 Customers (Edge Case)

**Expected Behavior**: Pagination controls hidden when < 10 active customers

**Prerequisites**: Database has exactly 5-9 customers with active pens

**Steps**:
1. Ensure only 5-9 customers have active pens (close others or use test database)
2. Navigate to Dashboard page
3. Scroll to Customer Performance table
4. Verify all customers displayed on single page
5. Look for pagination controls at bottom of table

**Validation Checks**:
- [ ] All customers (< 10) visible on single page
- [ ] Pagination controls NOT visible (no pager component)
- [ ] No page indicator text displayed
- [ ] Table still shows all 7 Thai column headers

**Acceptance Criteria**: ✅ PASS if all customers shown on one page with no pagination UI

---

## Summary Checklist

**Before Production Deployment**, verify:
- [ ] All 8 scenarios PASS
- [ ] No console errors during testing
- [ ] No server errors in logs
- [ ] Performance < 500ms for dashboard load
- [ ] Performance < 1s for data fetch (100 active pens)
- [ ] Thai formatting correct: ฿1,234.56 (currency), 1,234 (numbers)
- [ ] Color coding: green for profit, red for loss

---

## Rollback Instructions

**If validation fails**:
1. Check browser console for JavaScript errors
2. Check server logs for backend exceptions
3. Verify database has mixed active/closed pens
4. Verify database has customers with deposits
5. Compare actual vs. expected values using database queries:
   ```sql
   SELECT COUNT(*) FROM PigPens WHERE Status = 'Active';
   SELECT SUM(Deposit) FROM Deposits WHERE PigPenId IN (SELECT Id FROM PigPens WHERE Status = 'Active');
   ```

**Revert deployment**:
1. Checkout previous commit: `git checkout <previous-commit-hash>`
2. Restart server: `dotnet run --project src/server/...`
3. Restart client: `dotnet run --project src/client/...`

---

## Test Data Setup Commands

**PowerShell commands to seed test data** (run from repo root):

```powershell
# Ensure at least 15 active customers
# (Manual: Use existing customer creation UI or SQL INSERT)

# Set some pens to closed status
# (Manual: Use pig pen detail page "Close Pen" button or SQL UPDATE)

# Example SQL for testing:
# UPDATE PigPens SET Status = 'Closed' WHERE Id IN (SELECT TOP 5 Id FROM PigPens);
```

---

**End of Quickstart**

**Status After Completion**: All scenarios validated → Feature ready for production deployment
