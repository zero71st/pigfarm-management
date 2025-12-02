# Research: Dashboard Thai Translation and Business Metrics Enhancement

**Feature**: 016-update-dashboard-to  
**Date**: 2025-12-02  
**Status**: Complete

## Research Topics

### 1. Thai Locale Formatting in Blazor WebAssembly

**Question**: How to implement Thai language number and currency formatting in Blazor WASM without external libraries?

**Research Findings**:
- .NET `CultureInfo` class provides built-in Thai locale support (`th-TH`)
- Currency formatting: `value.ToString("C", new CultureInfo("th-TH"))` produces ฿1,234.56
- Number formatting: `value.ToString("N", new CultureInfo("th-TH"))` produces 1,234.56
- Percentage formatting: `value.ToString("P1", new CultureInfo("th-TH"))` produces 12.5%

**Decision**: Use .NET built-in globalization (`CultureInfo("th-TH")`)

**Rationale**:
- Zero additional dependencies or package installations
- Consistent with existing project patterns (already using C# ToString patterns)
- Server-side and client-side compatible (Blazor WASM supports CultureInfo)
- Automatic Baht symbol (฿), comma separators, and decimal precision

**Alternatives Considered**:
1. JavaScript `Intl.NumberFormat` via JSInterop
   - Rejected: Adds complexity with C#/JS bridging, harder to test
2. Custom string formatting helper methods
   - Rejected: Reinventing the wheel, violates simplicity principle

**Implementation Pattern**:
```razor
@using System.Globalization

<MudText>@totalInvestment.ToString("C", new CultureInfo("th-TH"))</MudText>
<!-- Output: ฿1,234,567.89 -->
```

---

### 2. MudBlazor Table Pagination

**Question**: Best approach to implement 10-items-per-page pagination with conditional hide for <10 items?

**Research Findings**:
- MudTable component has built-in pagination via `<MudTablePager>`
- Properties: `RowsPerPage`, `@bind-Page`, `PageSize`, `HidePageSizeDropdown`
- Conditional rendering: Use `@if (totalItems > 10)` wrapper around pager component
- Automatic page calculation and navigation buttons provided

**Decision**: Use MudTable built-in `<MudTablePager>` with conditional rendering

**Rationale**:
- Zero custom code for pagination logic (buttons, page tracking, etc.)
- Consistent with existing CustomerStatsTable implementation
- Automatic accessibility (keyboard navigation, aria labels)
- Responsive design out of the box

**Alternatives Considered**:
1. Custom pagination component
   - Rejected: Violates simplicity principle, duplicates MudBlazor functionality
2. Third-party pagination library
   - Rejected: Adds dependency, inconsistent with existing UI

**Implementation Pattern**:
```razor
<MudTable Items="@customerStats" RowsPerPage="10">
    <HeaderContent>...</HeaderContent>
    <RowTemplate>...</RowTemplate>
    @if (customerStats.Count > 10)
    {
        <PagerContent>
            <MudTablePager />
        </PagerContent>
    }
</MudTable>
```

---

### 3. Active Pig Pen Filtering Strategy

**Question**: Where to implement "Status == 'Active'" filtering - repository, service, or client?

**Research Findings**:
- Repository layer filtering: LINQ `.Where()` translates to SQL WHERE clause
- Service layer filtering: In-memory filtering after database fetch
- Client-side filtering: Blazor component filters after API call
- Performance implications: 100 active + 50 closed pens scenario
  - Repository: Fetches 100 rows (optimal)
  - Service: Fetches 150 rows, filters to 100 (inefficient)
  - Client: Fetches 150 rows over network (worst)

**Decision**: Repository layer filtering with LINQ `.Where(p => p.Status == "Active")`

**Rationale**:
- Minimum data transfer (only active pens fetched from database)
- Single source of truth for filtering logic (reusable across endpoints)
- Testable in isolation (unit tests can verify LINQ expression)
- EF Core query optimization (translates to efficient SQL)

**Alternatives Considered**:
1. Service layer filtering
   - Rejected: Fetches unnecessary closed pen data from database
2. Client-side filtering
   - Rejected: Network overhead, violates performance constraint (<1s dashboard load)

**Implementation Pattern**:
```csharp
// DashboardRepository.cs
public async Task<DashboardOverview> GetDashboardOverviewAsync()
{
    var activePens = await _context.PigPens
        .Where(p => p.Status == "Active") // Filter at database level
        .Include(p => p.Customer)
        .Include(p => p.Deposits)
        .ToListAsync();
        
    // Aggregate calculations on filtered data
    return new DashboardOverview { ... };
}
```

---

### 4. Financial Formula Implementation

**Question**: Where to implement new formulas (Owner's Capital, Customer's Capital, Investment) - database, DTO, or component?

**Research Findings**:
- Database computed columns: Requires migration, breaks SQLite/PostgreSQL compatibility
- DTO calculated properties: Computed in C# code when DTO is created
- Component-level calculations: Computed in Razor templates during render
- Performance: All options O(1) per dashboard load

**Decision**: DTO calculated properties in `DashboardOverview` and `CustomerPigPenStats` records

**Rationale**:
- Encapsulates business logic in data model (single source of truth)
- Reusable across Cash/Project sections without duplication
- Unit testable (can verify formulas independently)
- No database schema changes (preserves cross-database compatibility)

**Alternatives Considered**:
1. Database computed columns
   - Rejected: Requires migration, SQLite has limited support for computed columns
2. Component-level calculations
   - Rejected: Formula duplication across 3 components (DashboardOverviewCards, Cash, Project sections)

**Implementation Pattern**:
```csharp
// DashboardDtos.cs
public record DashboardOverview
{
    public decimal TotalCost { get; init; }
    public decimal TotalDeposit { get; init; }
    public decimal TotalPriceIncludeDiscount { get; init; }
    
    // Calculated properties (formulas encapsulated here)
    public decimal TotalCustomerCapital => TotalDeposit;
    public decimal TotalOwnerCapital => TotalCost - TotalDeposit;
    public decimal TotalProfit => TotalPriceIncludeDiscount - TotalCost;
    public decimal TotalInvestment => TotalOwnerCapital + TotalCustomerCapital + TotalProfit;
}
```

---

## Summary of Decisions

| Topic | Decision | Key Benefit |
|-------|----------|-------------|
| Thai Formatting | CultureInfo("th-TH") | Zero dependencies, built-in .NET |
| Pagination | MudTable built-in pager | Zero custom code, consistent UI |
| Active Filtering | Repository LINQ Where | Minimal data transfer, testable |
| Formulas | DTO calculated properties | Reusable, no DB migration |

**No additional research required** - all technical unknowns resolved with existing technology stack.
