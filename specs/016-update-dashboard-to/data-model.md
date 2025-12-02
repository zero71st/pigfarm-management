# Data Model: Dashboard Thai Translation and Business Metrics Enhancement

**Feature**: 016-update-dashboard-to  
**Date**: 2025-12-02  
**Status**: Complete

## Overview

This feature extends existing DTOs with new calculated fields and updates filtering logic. **No database schema changes required** - all changes are in-memory calculations and query filters.

---

## Updated Entities

### 1. DashboardOverview (DTO)

**Type**: Data Transfer Object (no database table)  
**Purpose**: Aggregated business metrics for dashboard display

**Existing Fields** (unchanged):
```csharp
public int TotalActivePigPens { get; init; }        // Count of active pens
public int TotalActivePigs { get; init; }           // Sum of pigs in active pens
public int TotalActiveCustomers { get; init; }      // Count of customers with active pens
public decimal TotalCost { get; init; }             // Sum of all costs (feed, materials)
public decimal TotalPriceIncludeDiscount { get; init; } // Sum of sale prices
public decimal TotalDeposit { get; init; }          // Sum of customer deposits
```

**New Fields**:
```csharp
public decimal TotalCustomerCapital { get; init; }  // = TotalDeposit
```

**Updated Calculated Properties** (formulas changed):
```csharp
// OLD: TotalOwnerCapital = TotalCost
// NEW:
public decimal TotalOwnerCapital => TotalCost - TotalDeposit;

// OLD: TotalInvestment = TotalCost + TotalProfit
// NEW:
public decimal TotalInvestment => TotalOwnerCapital + TotalCustomerCapital + TotalProfit;

// NEW explicit formula (was implicit):
public decimal TotalProfit => TotalPriceIncludeDiscount - TotalCost;
```

**Nested Objects** (formulas updated):
```csharp
public DashboardSection CashOperations { get; init; }    // Cash pen metrics
public DashboardSection ProjectOperations { get; init; } // Project pen metrics
public List<CustomerPigPenStats> CustomerStats { get; init; } // Per-customer data
```

**Filtering Rule**:
- All aggregations filter where `PigPen.Status == "Active"`
- Closed pens excluded from all calculations

**Validation Rules**:
- `TotalActivePigPens >= 0` (can be 0 if all pens closed)
- `TotalOwnerCapital >= 0` (deposits should not exceed costs)
- `TotalProfit` can be negative (loss scenario)

---

### 2. DashboardSection (DTO)

**Type**: Data Transfer Object (nested in DashboardOverview)  
**Purpose**: Cash vs Project breakdown metrics

**Existing Fields** (unchanged):
```csharp
public int ActivePigPens { get; init; }
public int TotalPigs { get; init; }
public decimal TotalCost { get; init; }
public decimal TotalDeposit { get; init; }
public decimal TotalPriceIncludeDiscount { get; init; }
```

**New Fields**:
```csharp
public decimal TotalCustomerCapital { get; init; }  // = TotalDeposit (for this type)
```

**Updated Calculated Properties**:
```csharp
public decimal TotalOwnerCapital => TotalCost - TotalDeposit;
public decimal TotalProfit => TotalPriceIncludeDiscount - TotalCost;
public decimal TotalInvestment => TotalOwnerCapital + TotalCustomerCapital + TotalProfit;
```

**Filtering Rule**:
- Cash section: `PigPen.Type == "Cash" AND PigPen.Status == "Active"`
- Project section: `PigPen.Type == "Project" AND PigPen.Status == "Active"`

---

### 3. CustomerPigPenStats (DTO)

**Type**: Data Transfer Object (item in DashboardOverview.CustomerStats list)  
**Purpose**: Per-customer performance metrics

**Existing Fields** (unchanged):
```csharp
public string CustomerName { get; init; }
public int PigPenCount { get; init; }               // Count of active pens for this customer
public int TotalPigs { get; init; }
public decimal TotalCost { get; init; }
public decimal TotalDeposit { get; init; }
public decimal TotalPriceIncludeDiscount { get; init; }
```

**New Fields**:
```csharp
public decimal TotalCustomerCapital { get; init; }  // = TotalDeposit (for this customer)
```

**Updated Calculated Properties**:
```csharp
public decimal TotalOwnerCapital => TotalCost - TotalDeposit;
public decimal TotalProfitLoss => TotalPriceIncludeDiscount - TotalCost;
public decimal TotalInvestment => TotalOwnerCapital + TotalCustomerCapital + TotalProfitLoss;
```

**Filtering Rule**:
- Customer shown only if `COUNT(active pens) > 0`
- If all customer's pens are closed, customer NOT included in list

**Validation Rules**:
- `CustomerName != null && CustomerName != ""`
- `PigPenCount > 0` (guaranteed by filter)

---

## Database Entities (No Changes)

### PigPen (existing table - no modifications)

**Relevant Fields** (used for filtering):
```sql
Status VARCHAR(20)  -- Values: "Active" or "Closed"
Type VARCHAR(20)    -- Values: "Cash" or "Project"
CustomerId GUID     -- FK to Customer table
PigQty INT          -- Number of pigs in pen
```

**Filtering Query Pattern**:
```csharp
var activePens = await _context.PigPens
    .Where(p => p.Status == "Active")
    .Include(p => p.Customer)
    .Include(p => p.Deposits)
    .ToListAsync();
```

---

## Calculation Formulas Reference

### Financial Metrics (All Entities)

| Metric | Formula | Example |
|--------|---------|---------|
| **Total Customer's Capital** | `TotalDeposit` | ฿50,000 |
| **Total Owner's Capital** | `TotalCost - TotalDeposit` | ฿200,000 - ฿50,000 = ฿150,000 |
| **Total Profit/Loss** | `TotalPriceIncludeDiscount - TotalCost` | ฿250,000 - ฿200,000 = ฿50,000 |
| **Total Investment** | `OwnerCapital + CustomerCapital + Profit` | ฿150,000 + ฿50,000 + ฿50,000 = ฿250,000 |

### Filtering Rules

| Scope | Filter Condition | Example |
|-------|------------------|---------|
| **Dashboard Overview** | `Status == "Active"` | 80 active + 20 closed → show 80 |
| **Cash Section** | `Status == "Active" AND Type == "Cash"` | 50 cash (40 active) → show 40 |
| **Project Section** | `Status == "Active" AND Type == "Project"` | 50 project (40 active) → show 40 |
| **Customer Stats** | Customer has `>= 1` active pen | Customer with 2 closed pens → hidden |

---

## State Transitions

**Pig Pen Status** (existing, not modified):
```
Active → Closed (when harvest complete, pen sold)
Closed → [terminal state, no return]
```

**Impact on Dashboard**:
- When pen transitions to "Closed" → excluded from all dashboard metrics
- Historical data preserved in database (data integrity principle)
- No data deletion or modification required

---

## Relationships

```
DashboardOverview (1)
├── CashOperations (1) : DashboardSection
├── ProjectOperations (1) : DashboardSection
└── CustomerStats (*) : List<CustomerPigPenStats>
    └── Each customer aggregates their active pens
```

**No changes to database foreign keys or relationships** - all aggregation logic is in application code.

---

## Validation & Constraints

### Business Rules
1. `TotalCustomerCapital <= TotalCost` (deposits should not exceed total costs)
2. `TotalActivePigPens == CashOperations.ActivePigPens + ProjectOperations.ActivePigPens`
3. Sum of customer TotalInvestment should approximately equal dashboard TotalInvestment

### Data Integrity
- Filtering does NOT delete or modify closed pens (read-only queries)
- All formulas use immutable DTO properties (no side effects)

---

## Implementation Notes

1. **No Migration Required**: All changes are calculated fields in DTOs
2. **Backward Compatible**: Existing fields unchanged, new fields additive only
3. **Performance**: Filtering at database level (LINQ Where translates to SQL WHERE)
4. **Testability**: Formulas in DTOs can be unit tested independently of database

---

## Thai Translation Mapping (Reference)

**Dashboard Metrics** (not part of data model, for frontend reference):

| English | Thai | Field Name |
|---------|------|------------|
| Total Active Pig Pens | คอกสุกรที่ใช้งานทั้งหมด | TotalActivePigPens |
| Total Active Pigs | จำนวนสุกรที่ใช้งานทั้งหมด | TotalActivePigs |
| Total Active Customers | จำนวนลูกค้าที่ใช้งานทั้งหมด | TotalActiveCustomers |
| Total Investment | เงินลงทุนทั้งหมด | TotalInvestment |
| Total Owner's Capital | เงินลงทุนส่วนเจ้าของรวม | TotalOwnerCapital |
| Total Customer's Capital | เงินลงทุนส่วนลูกค้า | TotalCustomerCapital |
| Total Profit/Loss | กำไร/ขาดทุนทั้งหมด | TotalProfit |
