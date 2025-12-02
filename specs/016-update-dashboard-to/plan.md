# Implementation Plan: Dashboard Thai Translation and Business Metrics Enhancement

**Branch**: `016-update-dashboard-to` | **Date**: 2025-12-02 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/016-update-dashboard-to/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path ✅
2. Fill Technical Context (no NEEDS CLARIFICATION detected) ✅
3. Fill Constitution Check section ✅
4. Evaluate Constitution Check → PASS ✅
5. Execute Phase 0 → research.md ✅
6. Execute Phase 1 → data-model.md, quickstart.md ✅
7. Re-evaluate Constitution Check → PASS ✅
8. Plan Phase 2 → Task generation approach documented ✅
9. STOP - Ready for /tasks command ✅
```

## Summary
Translate existing dashboard UI to Thai language and enhance business metrics visibility by:
1. **UI Translation**: Convert all dashboard labels, headers, and controls from English to Thai
2. **Active-Only Filtering**: Display only active pig pens (Status != "Closed") across all metrics
3. **Metric Reorganization**: Add 4th financial metric card (Customer's Capital), expand Cash/Project sections to 6 metrics each
4. **Customer Table Enhancement**: Add Customer's Capital column with 10-items-per-page pagination
5. **Formula Updates**: Implement new calculations (Owner's Capital = TotalCost - TotalDeposit, Investment = Owner's Capital + Customer's Capital + Profit)

**Technical Approach**: Frontend-only changes to existing Blazor components (Dashboard.razor, DashboardOverviewCards.razor, CustomerStatsTable) and backend service/repository updates for new filtering and calculation logic. No database schema changes required.

## Technical Context
**Language/Version**: C# .NET 8, Blazor WebAssembly  
**Primary Dependencies**: MudBlazor UI components, Entity Framework Core 8  
**Storage**: Existing SQLite (dev) / PostgreSQL (prod) - no schema changes  
**Testing**: Manual validation via quickstart.md (no automated test generation per user request)  
**Target Platform**: Web browsers (Chrome/Edge/Safari)  
**Project Type**: Web application (Blazor WASM client + ASP.NET Core server)  
**Performance Goals**: <500ms page load, <1s dashboard data fetch for 100 active pens  
**Constraints**: <500ms pagination navigation, Thai locale number formatting (฿1,234.56)  
**Scale/Scope**: 3 Razor components, 2 service classes, 2 repository methods, ~300-400 LOC changes  
**User Request**: No test plan or test documentation artifacts

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Data Integrity
- ✅ **PASS**: No changes to historical data (feed assignments, harvests, invoices)
- ✅ **PASS**: Active-only filtering does not modify or delete closed pig pen data
- ✅ **PASS**: New calculations use existing fields (TotalCost, TotalDeposit, TotalPriceIncludeDiscount)

### Simplicity & Minimalism
- ✅ **PASS**: UI-only changes (translation strings, layout reorganization)
- ✅ **PASS**: Calculation logic added to existing service/repository pattern (no new abstractions)
- ✅ **PASS**: Pagination uses existing MudTable built-in features (no custom component)

### Feature-Based Architecture
- ✅ **PASS**: Changes contained within existing Dashboard feature folder (`src/client/Features/Dashboard/`, `src/server/Features/Analytics/`)
- ✅ **PASS**: No new feature boundaries introduced

### Backwards Compatibility
- ✅ **PASS**: Existing database schema unchanged (Status field already exists)
- ✅ **PASS**: Existing API contracts unchanged (DashboardOverview/CustomerPigPenStats DTOs extended with new calculated fields)
- ⚠️ **ADVISORY**: Closed pig pens will no longer appear in dashboard metrics (intentional behavior change per requirements)

**Gate Status**: PASS - No constitutional violations. Proceed to Phase 0.

## Project Structure

### Documentation (this feature)
```
specs/016-update-dashboard-to/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
src/
├── client/PigFarmManagement.Client/
│   └── Features/Dashboard/
│       ├── Pages/
│       │   └── Dashboard.razor                    # Add pagination, Thai labels
│       ├── Components/
│       │   ├── DashboardOverviewCards.razor      # Add 4th card, update formulas, Thai labels
│       │   └── CustomerStatsTable.razor          # Add Customer's Capital column, pagination
│       └── Services/
│           └── IDashboardService.cs              # Interface (no changes)
│
├── server/PigFarmManagement.Server/
│   └── Features/Analytics/
│       ├── Analytics.cs                          # Update DashboardOverview/CustomerPigPenStats records
│       ├── DashboardService.cs                   # Implement active-only filtering, new calculations
│       └── DashboardRepository.cs                # Add LINQ queries for active pen filtering
│
└── shared/PigFarmManagement.Shared/
    └── DTOs/
        └── DashboardDtos.cs                      # Extend with TotalCustomerCapital fields
```

**Structure Decision**: Existing web application structure (client/server/shared). Changes isolated to Dashboard/Analytics features per feature-based architecture principle.

## Phase 0: Outline & Research
*Research findings consolidated in research.md*

### Research Completed
1. **Thai Locale Formatting in Blazor**
   - Decision: Use `@("value".ToString("C", new CultureInfo("th-TH")))` for currency
   - Rationale: Built-in .NET globalization support, no external libraries needed
   - Alternatives: JavaScript Intl API (rejected - adds client-side complexity)

2. **MudBlazor Pagination**
   - Decision: Use `<MudTable>` built-in pagination with `RowsPerPage="10"` and `@bind-Page`
   - Rationale: Zero additional code, consistent with existing table patterns
   - Alternatives: Custom pagination component (rejected - violates simplicity)

3. **Active Pig Pen Filtering**
   - Decision: LINQ `.Where(p => p.Status == "Active")` in repository layer
   - Rationale: Single source of truth for filtering, testable in isolation
   - Alternatives: Client-side filtering (rejected - performance for 100+ pens)

4. **Financial Formula Implementation**
   - Decision: Calculated properties in DashboardOverview record
   - Rationale: Encapsulates business logic, reusable across Cash/Project sections
   - Alternatives: SQL computed columns (rejected - cross-database compatibility)

**Output**: ✅ research.md created with detailed findings

## Phase 1: Design & Contracts
*Prerequisites: research.md complete ✅*

### Data Model (data-model.md)
**Entities Updated**:

1. **DashboardOverview** (DTO - no database table)
   - **New Fields**:
     - `decimal TotalCustomerCapital` (= TotalDeposit)
   - **Updated Calculations**:
     - `TotalOwnerCapital` = `TotalCost - TotalDeposit`
     - `TotalInvestment` = `TotalOwnerCapital + TotalCustomerCapital + TotalProfit`
     - `TotalProfit` = `TotalPriceIncludeDiscount - TotalCost`
   - **Filtering**: All aggregations filter where `Status == "Active"`

2. **CustomerPigPenStats** (DTO - no database table)
   - **New Fields**:
     - `decimal TotalCustomerCapital` (= customer's total deposits)
   - **Updated Calculations**:
     - `TotalOwnerCapital` = `TotalCost - TotalDeposit`
     - `TotalInvestment` = `TotalOwnerCapital + TotalCustomerCapital + TotalProfitLoss`
     - `TotalProfitLoss` = `TotalPriceIncludeDiscount - TotalCost`
   - **Filtering**: Customer shown only if has at least one active pig pen

3. **No Database Changes**: Existing PigPen, Deposit, and related tables unchanged

### API Contracts
**Endpoint**: `GET /api/dashboard/overview` (existing - response shape extended)

**Response DTO Extension**:
```csharp
public record DashboardOverview
{
    // Existing fields (unchanged)
    public int TotalActivePigPens { get; init; }
    public int TotalActivePigs { get; init; }
    public int TotalActiveCustomers { get; init; }
    public decimal TotalCost { get; init; }
    public decimal TotalPriceIncludeDiscount { get; init; }
    
    // NEW field
    public decimal TotalCustomerCapital { get; init; } // = TotalDeposit
    
    // UPDATED calculations (formulas changed)
    public decimal TotalOwnerCapital { get; init; } // = TotalCost - TotalDeposit
    public decimal TotalInvestment { get; init; }   // = TotalOwnerCapital + TotalCustomerCapital + TotalProfit
    public decimal TotalProfit { get; init; }       // = TotalPriceIncludeDiscount - TotalCost
    
    // Breakdown sections (formulas updated)
    public DashboardSection CashOperations { get; init; }
    public DashboardSection ProjectOperations { get; init; }
    
    // Customer stats (updated)
    public List<CustomerPigPenStats> CustomerStats { get; init; }
}

public record CustomerPigPenStats
{
    public string CustomerName { get; init; }
    public int PigPenCount { get; init; }
    public int TotalPigs { get; init; }
    public decimal TotalCost { get; init; }
    public decimal TotalDeposit { get; init; }
    public decimal TotalPriceIncludeDiscount { get; init; }
    
    // NEW field
    public decimal TotalCustomerCapital { get; init; } // = TotalDeposit
    
    // UPDATED calculations
    public decimal TotalOwnerCapital { get; init; }    // = TotalCost - TotalDeposit
    public decimal TotalInvestment { get; init; }      // = TotalOwnerCapital + TotalCustomerCapital + TotalProfitLoss
    public decimal TotalProfitLoss { get; init; }      // = TotalPriceIncludeDiscount - TotalCost
}
```

**No OpenAPI contract generation**: Existing endpoint, backward-compatible extension (new fields additive only).

### Quickstart Manual Validation
**Scenarios** (from spec acceptance criteria):
1. View activity metrics row → verify 3 Thai labels, active pens only
2. View financial metrics row → verify 4 Thai labels, correct calculations
3. Verify Project section before Cash section
4. Verify Cash/Project sections show 6 metrics each in Thai
5. Verify customer table shows 7 columns in Thai with pagination (10/page)
6. Test pagination with >10 customers
7. Test empty state (all closed pens) → 0 values, no errors
8. Test <10 customers → pagination hidden

**Output**: ✅ quickstart.md created with step-by-step validation procedures

### Agent Context Update
**Command**: `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot`
**Updates**:
- Feature 016: Dashboard Thai translation + metrics enhancement
- Thai locale formatting patterns (`CultureInfo("th-TH")`)
- New financial formulas (Owner's Capital, Customer's Capital, Investment)
- Active-only filtering pattern

**Output**: ✅ `.github/copilot-instructions.md` updated

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
1. Load `.specify/templates/tasks-template.md` as base
2. Generate tasks from Phase 1 design docs (data-model.md, quickstart.md)
3. Order by dependency: Backend → Frontend → Validation

**Task Categories**:

**Backend Changes** (Foundation):
- [ ] Task 1: Update DashboardOverview/CustomerPigPenStats DTOs with TotalCustomerCapital field
- [ ] Task 2: Update DashboardRepository with active-only filtering LINQ queries
- [ ] Task 3: Update DashboardService with new calculation formulas
- [ ] Task 4: Update Cash/Project breakdown logic to return 6 metrics

**Frontend Changes** (UI Layer):
- [ ] Task 5: Update DashboardOverviewCards component - add 4th card, Thai labels
- [ ] Task 6: Update DashboardOverviewCards - implement new formula displays
- [ ] Task 7: Update Cash/Project section components - 6 metrics, Thai labels, reorder (Project first)
- [ ] Task 8: Update CustomerStatsTable - add Customer's Capital column, Thai headers
- [ ] Task 9: Add MudTable pagination (RowsPerPage=10, conditional hide)

**Validation** (Quickstart Execution):
- [ ] Task 10: Execute quickstart.md scenarios 1-8, document results

**Ordering Strategy**:
- **Dependency order**: Tasks 1-4 (backend) before Tasks 5-9 (frontend)
- **Parallel execution**: Tasks 5-9 can be done in parallel (independent components)
- **Validation last**: Task 10 after all implementation complete

**Estimated Output**: 10 numbered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan. No tasks.md file is created during /plan execution.

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md with 10 tasks)  
**Phase 4**: Implementation (execute tasks 1-10 following constitutional principles)  
**Phase 5**: Validation (run quickstart.md scenarios, performance validation <500ms targets)

## Complexity Tracking
*No constitutional violations detected - table empty*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none)    | N/A        | N/A |

## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command) ✅
- [x] Phase 1: Design complete (/plan command) ✅
- [x] Phase 2: Task planning complete (/plan command - describe approach only) ✅
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS ✅
- [x] Post-Design Constitution Check: PASS ✅
- [x] All NEEDS CLARIFICATION resolved ✅ (clarifications in spec)
- [x] Complexity deviations documented ✅ (none)

---
*Based on Constitution v2.1.1 - See `.specify/memory/constitution.md`*
