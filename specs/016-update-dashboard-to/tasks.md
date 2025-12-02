# Tasks: Dashboard Thai Translation and Business Metrics Enhancement

**Feature**: 016-update-dashboard-to  
**Input**: Design documents from `/specs/016-update-dashboard-to/`  
**Prerequisites**: plan.md ✅, research.md ✅, data-model.md ✅, quickstart.md ✅

## Execution Flow
```
1. Load plan.md ✅
   → Tech stack: C# .NET 8, Blazor WebAssembly, MudBlazor, EF Core 8
   → Structure: Feature-based (Dashboard/Analytics)
2. Load design documents ✅
   → data-model.md: 3 DTOs extended (DashboardOverview, DashboardSection, CustomerPigPenStats)
   → quickstart.md: 8 validation scenarios
   → No contracts/: API response shape extended (backward compatible)
3. Generate tasks by category:
   → Backend: DTOs → Repository → Service
   → Frontend: Cards → Sections → Table
   → Validation: Manual quickstart scenarios
4. Apply task rules:
   → Backend before frontend (data flow dependency)
   → Components in parallel (different files)
   → Validation last (requires complete implementation)
5. Number tasks T001-T010
6. Parallel execution: T005-T009 (independent components)
```

---

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- All paths relative to repository root: `d:\dz Projects\PigFarmManagement\`

---

## Phase 3.1: Setup (Prerequisites)
*No setup tasks required - existing project structure, dependencies already in place*

---

## Phase 3.2: Backend Implementation (Foundation)

### T001: Update DTOs with TotalCustomerCapital field and formulas ✅
**File**: `src/shared/PigFarmManagement.Shared/DTOs/DashboardDtos.cs`

**Changes**:
1. **DashboardOverview** record:
   - Add field: `public decimal TotalCustomerCapital { get; init; }`
   - Update property: `public decimal TotalOwnerCapital => TotalCost - TotalDeposit;`
   - Update property: `public decimal TotalInvestment => TotalOwnerCapital + TotalCustomerCapital + TotalProfit;`
   - Update property: `public decimal TotalProfit => TotalPriceIncludeDiscount - TotalCost;`

2. **DashboardSection** record:
   - Add field: `public decimal TotalCustomerCapital { get; init; }`
   - Add property: `public decimal TotalOwnerCapital => TotalCost - TotalDeposit;`
   - Add property: `public decimal TotalProfit => TotalPriceIncludeDiscount - TotalCost;`
   - Add property: `public decimal TotalInvestment => TotalOwnerCapital + TotalCustomerCapital + TotalProfit;`

3. **CustomerPigPenStats** record:
   - Add field: `public decimal TotalCustomerCapital { get; init; }`
   - Update property: `public decimal TotalOwnerCapital => TotalCost - TotalDeposit;`
   - Update property: `public decimal TotalInvestment => TotalOwnerCapital + TotalCustomerCapital + TotalProfitLoss;`

**Acceptance Criteria**:
- Formulas match data-model.md specifications
- DTOs compile without errors
- Calculated properties use correct field names

---

### T002: Add active-only filtering to DashboardRepository ✅
**File**: `src/server/PigFarmManagement.Server/Features/Dashboard/DashboardService.cs` (no separate repository)

**Changes**:
1. Locate `GetDashboardOverviewAsync()` method
2. Add LINQ filter: `.Where(p => p.Status == "Active")` to all PigPen queries
3. Update aggregate calculations to use filtered data
4. Populate `TotalCustomerCapital = activePens.Sum(p => p.Deposits.Sum(d => d.Amount))`

**Example Pattern** (from research.md):
```csharp
var activePens = await _context.PigPens
    .Where(p => p.Status == "Active") // NEW: Active-only filter
    .Include(p => p.Customer)
    .Include(p => p.Deposits)
    .ToListAsync();
```

**Acceptance Criteria**:
- All queries filter by `Status == "Active"`
- Closed pens excluded from counts and sums
- Customer shown only if has >= 1 active pen
- Repository builds successfully

---

### T003: Update DashboardSection breakdown logic for 6 metrics ✅
**File**: `src/server/PigFarmManagement.Server/Features/Dashboard/DashboardService.cs`

**Changes**:
1. Locate Cash/Project section aggregation code
2. For each section (Cash, Project):
   - Filter by `Type == "Cash"` or `Type == "Project"` AND `Status == "Active"`
   - Calculate and populate `TotalCustomerCapital` field
3. Verify all 6 metrics calculated:
   - ActivePigPens (count)
   - TotalPigs (sum)
   - TotalCost (sum)
   - TotalCustomerCapital (sum of deposits)
   - TotalOwnerCapital (calculated property)
   - TotalProfit (calculated property)

**Acceptance Criteria**:
- CashOperations and ProjectOperations sections each have TotalCustomerCapital
- Filtering combines Type AND Status checks
- All calculated properties return correct values

---

### T004: Update DashboardService to pass through new DTO fields ✅
**File**: `src/server/PigFarmManagement.Server/Features/Dashboard/DashboardService.cs`

**Changes**:
1. Review service layer data flow
2. Ensure `TotalCustomerCapital` field is passed from repository to API response
3. No business logic changes needed (calculations in DTOs)
4. Verify CustomerStats list includes only customers with active pens

**Acceptance Criteria**:
- Service compiles successfully
- All new DTO fields accessible in response
- No breaking changes to existing consumers

---

## Phase 3.3: Frontend Implementation (UI Layer)

### T005 [P]: Add 4th card and update labels in DashboardOverviewCards ✅
**File**: `src/client/PigFarmManagement.Client/Features/Dashboard/Components/DashboardOverviewCards.razor`

**Changes**:
1. **Activity Metrics Row** (3 cards):
   - Card 1 label: `"คอกสุกรที่ใช้งานทั้งหมด"` (Total Active Pig Pens)
   - Card 2 label: `"จำนวนสุกรที่ใช้งานทั้งหมด"` (Total Active Pigs)
   - Card 3 label: `"จำนวนลูกค้าที่ใช้งานทั้งหมด"` (Total Active Customers)

2. **Financial Metrics Row** (4 cards - ADD 1 NEW):
   - Card 1 label: `"เงินลงทุนทั้งหมด"` (Total Investment)
     - Value: `@overview.TotalInvestment.ToString("C", new CultureInfo("th-TH"))`
   - Card 2 label: `"เงินลงทุนส่วนเจ้าของรวม"` (Total Owner's Capital)
     - Value: `@overview.TotalOwnerCapital.ToString("C", new CultureInfo("th-TH"))`
   - **Card 3 [NEW]** label: `"เงินลงทุนส่วนลูกค้า"` (Total Customer's Capital)
     - Value: `@overview.TotalCustomerCapital.ToString("C", new CultureInfo("th-TH"))`
   - Card 4 label: `"กำไร/ขาดทุนทั้งหมด"` (Total Profit/Loss)
     - Value: `@overview.TotalProfit.ToString("C", new CultureInfo("th-TH"))`
     - Color: Green if positive, red if negative

3. Add namespace: `@using System.Globalization`

**Acceptance Criteria**:
- 3 activity cards + 4 financial cards visible
- All labels in Thai
- Currency formatted as ฿X,XXX.XX
- Profit/Loss color-coded correctly

---

### T006 [P]: Update Cash section with 6 metrics and Thai labels ✅
**File**: `src/client/PigFarmManagement.Client/Features/Dashboard/Components/DashboardOverviewCards.razor` (integrated in same file)

**Changes**:
1. Update section header: `"การดำเนินงานเงินสด"` (Cash Operations)
2. Display 6 metrics with Thai labels:
   - `"คอกสุกรที่ใช้งาน"` → `@overview.CashOperations.ActivePigPens`
   - `"จำนวนสุกร"` → `@overview.CashOperations.TotalPigs`
   - `"เงินลงทุน"` → `@overview.CashOperations.TotalInvestment.ToString("C", ...)`
   - `"เงินลงทุนส่วนเจ้าของ"` → `@overview.CashOperations.TotalOwnerCapital.ToString("C", ...)`
   - `"เงินลงทุนส่วนลูกค้า"` [NEW] → `@overview.CashOperations.TotalCustomerCapital.ToString("C", ...)`
   - `"กำไร/ขาดทุน"` → `@overview.CashOperations.TotalProfit.ToString("C", ...)`
3. Add namespace: `@using System.Globalization`

**Acceptance Criteria**:
- Section header in Thai
- Exactly 6 metrics displayed
- Currency values formatted as ฿X,XXX.XX
- All labels in Thai

---

### T007 [P]: Update Project section with 6 metrics and Thai labels ✅
**File**: `src/client/PigFarmManagement.Client/Features/Dashboard/Components/DashboardOverviewCards.razor` (integrated in same file)

**Changes**:
1. Update section header: `"การดำเนินงานโครงการ"` (Project Operations)
2. Display 6 metrics with Thai labels (same structure as Cash section):
   - `"คอกสุกรที่ใช้งาน"` → `@overview.ProjectOperations.ActivePigPens`
   - `"จำนวนสุกร"` → `@overview.ProjectOperations.TotalPigs`
   - `"เงินลงทุน"` → `@overview.ProjectOperations.TotalInvestment.ToString("C", ...)`
   - `"เงินลงทุนส่วนเจ้าของ"` → `@overview.ProjectOperations.TotalOwnerCapital.ToString("C", ...)`
   - `"เงินลงทุนส่วนลูกค้า"` [NEW] → `@overview.ProjectOperations.TotalCustomerCapital.ToString("C", ...)`
   - `"กำไร/ขาดทุน"` → `@overview.ProjectOperations.TotalProfit.ToString("C", ...)`
3. Add namespace: `@using System.Globalization`

**Acceptance Criteria**:
- Section header in Thai
- Exactly 6 metrics displayed
- Currency values formatted as ฿X,XXX.XX
- All labels in Thai

---

### T008 [P]: Reorder sections (Project before Cash) ✅
**File**: `src/client/PigFarmManagement.Client/Features/Dashboard/Components/DashboardOverviewCards.razor` (Project rendered first)

**Changes**:
1. Locate section rendering code
2. Ensure render order:
   - DashboardOverviewCards (activity + financial metrics)
   - ProjectOperationsSection component **[FIRST]**
   - CashOperationsSection component **[SECOND]**
   - CustomerStatsTable component
3. Verify layout visually: Project section appears above Cash section

**Acceptance Criteria**:
- Project section renders before Cash section
- Visual order matches spec requirement (FR-005)
- No layout shift or flickering

---

### T009 [P]: Add Customer's Capital column and pagination to CustomerStatsTable ✅
**File**: `src/client/PigFarmManagement.Client/Features/Dashboard/Components/CustomerStatsTable.razor`

**Changes**:
1. Update `<MudTable>` component:
   - Set `RowsPerPage="10"`
   - Add `@bind-Page="_currentPage"` (create field if needed)

2. Update column headers (7 columns total):
   1. `"ชื่อลูกค้า"` (Customer Name)
   2. `"จำนวนคอก"` (Pig Pen Count)
   3. `"จำนวนสุกร"` (Total Pigs)
   4. `"เงินลงทุนทั้งหมด"` (Total Investment)
   5. `"เงินลงทุนส่วนเจ้าของรวม"` (Total Owner's Capital)
   6. **`"รวมเงินมัดจำ"` [NEW]** (Total Customer's Capital)
   7. `"กำไร/ขาดทุนรวม"` (Total Profit/Loss)

3. Add column for `TotalCustomerCapital`:
   ```razor
   <MudTd>@customer.TotalCustomerCapital.ToString("C", new CultureInfo("th-TH"))</MudTd>
   ```

4. Add conditional pagination:
   ```razor
   @if (customerStats?.Count > 10)
   {
       <PagerContent>
           <MudTablePager />
       </PagerContent>
   }
   ```

5. Add namespace: `@using System.Globalization`

**Acceptance Criteria**:
- All 7 column headers in Thai
- Customer's Capital column displays formatted values
- Pagination visible when > 10 customers
- Pagination hidden when <= 10 customers
- 10 rows per page maximum

---

## Phase 3.4: Validation (Manual Quickstart Execution)

### T010: Execute quickstart.md validation scenarios
**Reference**: `specs/016-update-dashboard-to/quickstart.md`

**Steps**:
1. Start server: `dotnet run --project src/server/PigFarmManagement.Server --urls http://localhost:5000`
2. Start client: `dotnet run --project src/client/PigFarmManagement.Client --urls http://localhost:7000`
3. Execute each scenario from quickstart.md:
   - ✅ Scenario 1: Activity metrics row (3 Thai labels, active pens only)
   - ✅ Scenario 2: Financial metrics row (4 Thai labels, correct formulas)
   - ✅ Scenario 3: Section order (Project before Cash)
   - ✅ Scenario 4: Cash/Project sections (6 metrics each, Thai labels)
   - ✅ Scenario 5: Customer table (7 columns, Thai headers, pagination)
   - ✅ Scenario 6: Pagination navigation (< 500ms)
   - ✅ Scenario 7: Empty state (all closed pens → 0 values, no errors)
   - ✅ Scenario 8: Pagination hidden (< 10 customers)

4. Document results in `specs/016-update-dashboard-to/validation-results.md`:
   - Each scenario: PASS/FAIL
   - Screenshots of Thai UI
   - Performance measurements (DevTools Network tab)
   - Formula verification (manual calculator check)
   - Any bugs or issues found

**Acceptance Criteria**:
- All 8 scenarios PASS
- No console errors
- No server errors in logs
- Performance < 500ms dashboard load
- Performance < 1s data fetch (100 active pens)
- Thai formatting: ฿1,234.56 (currency), 1,234 (numbers)
- Color coding: green profit, red loss

---

## Dependencies

### Sequential Dependencies
1. **T001 blocks T002-T004** (DTOs must exist before repository/service can use them)
2. **T002 blocks T003** (repository filtering before section breakdowns)
3. **T003 blocks T004** (sections calculated before service passes them)
4. **T004 blocks T005-T009** (backend API must return data before frontend can consume)
5. **T005-T009 block T010** (all UI changes complete before validation)

### Parallel Opportunities
- **T005, T006, T007, T008, T009** can run in parallel (different .razor files, independent components)

---

## Parallel Execution Example

**After T004 complete**, launch frontend tasks together:

```bash
# Terminal 1: Activity/Financial cards
Task T005: "Update DashboardOverviewCards.razor - add 4th card, Thai labels"

# Terminal 2: Cash section
Task T006: "Update CashOperationsSection.razor - 6 metrics, Thai labels"

# Terminal 3: Project section
Task T007: "Update ProjectOperationsSection.razor - 6 metrics, Thai labels"

# Terminal 4: Section ordering
Task T008: "Reorder Dashboard.razor - Project before Cash"

# Terminal 5: Customer table
Task T009: "Update CustomerStatsTable.razor - add column, pagination"
```

**Wait for all 5 to complete** → Then execute T010 (validation)

---

## Task Summary

| Phase | Task Count | Parallel | Sequential |
|-------|------------|----------|------------|
| Backend | T001-T004 | 0 | 4 |
| Frontend | T005-T009 | 5 | 0 |
| Validation | T010 | 0 | 1 |
| **Total** | **10 tasks** | **5** | **5** |

**Estimated Duration**:
- Backend: 2-3 hours (sequential)
- Frontend: 1-2 hours (parallel execution = fastest task duration)
- Validation: 1 hour (manual testing)
- **Total**: 4-6 hours (with parallelization)

---

## Notes

1. **Thai Locale**: Add `@using System.Globalization` to all Razor components using `CultureInfo("th-TH")`
2. **Formula Verification**: During T010, manually verify formulas match data-model.md specifications
3. **Performance**: Use browser DevTools Network tab to measure < 500ms page load
4. **Git Commits**: Commit after each task completion:
   - T001: "feat: add TotalCustomerCapital to DTOs and update formulas"
   - T002: "feat: add active-only filtering to DashboardRepository"
   - T005: "feat: translate dashboard cards to Thai, add 4th financial card"
   - T010: "docs: add validation results for quickstart scenarios"

5. **Constitution Compliance**:
   - ✅ Data Integrity: No historical data modified (closed pens preserved)
   - ✅ Simplicity: Built-in .NET/MudBlazor features (no new dependencies)
   - ✅ Feature-Based: Changes isolated to Dashboard/Analytics features
   - ✅ Backwards Compatibility: DTO extensions (additive only)

---

## Validation Checklist

*GATE: All checks must PASS before merging to main*

- [ ] All 10 tasks completed
- [ ] All 8 quickstart scenarios PASS
- [ ] Backend compiles without errors (`dotnet build src/server/`)
- [ ] Frontend compiles without errors (`dotnet build src/client/`)
- [ ] No console errors during manual testing
- [ ] No server errors in logs during testing
- [ ] Thai formatting verified: ฿1,234.56 format
- [ ] Formulas manually verified (calculator check)
- [ ] Performance < 500ms dashboard load
- [ ] Performance < 1s data fetch (100 active pens scenario)
- [ ] Pagination works (visible when > 10 customers)
- [ ] Pagination hidden (when <= 10 customers)
- [ ] Empty state graceful (all closed pens → 0 values, no crashes)
- [ ] Git commits clean (meaningful messages, logical grouping)

---

**Status**: Ready for execution  
**Branch**: `016-update-dashboard-to`  
**Next Command**: Begin T001 implementation
