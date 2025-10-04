# PigFarmManagement

Initial scaffold based on PRD. Contains:

* Server: Minimal API (.NET 8) with POSPOS integration for live feed imports
* Shared: DTO / record models
* Client: Blazor WebAssembly basic UI for Pig Pens list + detail (feeds, deposits, harvest)
* Shared: DTO / record models
* Client: Blazor WebAssembly basic UI for Pig Pens list + detail (feeds, deposits, harvest)

## Features

### POSPOS Feed Import Enhancement (Feature 006)

Enhanced feed import functionality that calculates consumption and expense per pig pen using POSPOS transaction data:

- **Feed Consumption Calculation**: Rounds stock to integer bags, calculates bags per pig and coverage
- **Expense Attribution**: Uses `TotalPriceIncludeDiscount` from POSPOS as authoritative expense source
- **Formula Cost Integration**: Automatically looks up and applies feed formula costs when product codes match
- **Discount Processing**: Records POSPOS discount amounts and computes actual pricing after discounts
- **Pricing Reconciliation**: Compares system-calculated totals vs POS-provided totals for validation

### New UI Features

The Pig Pen detail page now displays enhanced feed information:
- **INV Date**: Invoice date from POSPOS transactions  
- **Cost**: Feed formula cost (when available)
- **Price+Discount**: Actual unit price after applying discounts
- **Sys Total**: System-calculated total (Price+Discount × Quantity)
- **POS Total**: Total provided by POSPOS for comparison

## Database Schema Changes

**Migration**: `AddPosTotalPriceIncludeDiscount`

New columns added to `Feeds` table:
- `Cost` (decimal?) - From FeedFormula.Cost lookup
- `CostDiscountPrice` (decimal?) - From POSPOS DiscountAmount  
- `PriceIncludeDiscount` (decimal?) - UnitPrice - CostDiscountPrice
- `Sys_TotalPriceIncludeDiscount` (decimal?) - System calculated total
- `TotalPriceIncludeDiscount` (decimal) - Renamed from TotalPrice for clarity
- `Pos_TotalPriceIncludeDiscount` (decimal?) - POS-provided total for comparison

### Migration Commands

Apply the migration:
```bash
cd src/server/PigFarmManagement.Server
dotnet ef database update
```

**Rollback Instructions** (if needed):
```bash
# List migrations to find the previous one
dotnet ef migrations list

# Rollback to previous migration (replace with actual previous migration name)
dotnet ef database update <PreviousMigrationName>

# Remove the migration file
dotnet ef migrations remove
```

## Testing Enhanced Feed Import

### Sample Import Data

Use this sample POSPOS JSON to test the new functionality:

```json
[
  {
    "no": "INV001",
    "date": "2024-10-04T10:00:00Z",
    "buyer_detail": {
      "code": "M000001",
      "key_card_id": "12345"
    },
    "order_list": [
      {
        "product_code": "PK64000158",
        "product_name": "เจ็ท 105 หมูเล็ก 6-15 กก.",
        "stock": "20.0",
        "price": "755.00",
        "special_price": "750.00", 
        "discount_amount": "10.00",
        "total_price_include_discount": "14900.00"
      }
    ]
  }
]
```

### API Testing

Import via JSON endpoint:
```bash
curl -X POST "http://localhost:5000/api/feeds/import/pospos/json" \
  -H "Content-Type: application/json" \
  -d '{"jsonContent": "[{\"no\":\"INV001\",\"date\":\"2024-10-04T10:00:00Z\",\"buyer_detail\":{\"code\":\"M000001\"},\"order_list\":[{\"product_code\":\"PK64000158\",\"product_name\":\"เจ็ท 105 หมูเล็ก 6-15 กก.\",\"stock\":\"20.0\",\"price\":\"755.00\",\"special_price\":\"750.00\",\"discount_amount\":\"10.00\",\"total_price_include_discount\":\"14900.00\"}]}]"}'
```

### Expected Results

For the sample data:
- **Bags**: 20 (rounded from stock)
- **Unit Price**: 750.00 (special_price preferred over price)
- **Cost Discount**: 10.00 (from discount_amount)
- **Price+Discount**: 740.00 (750 - 10)
- **Sys Total**: 14800.00 (740 × 20)
- **POS Total**: 14900.00 (from POSPOS)

**Difference Analysis**: The 100.00 difference (14900 - 14800) represents POS-specific pricing logic or rounding that differs from the system calculation.

## Run (Dev)

1. Open solution folder `src` in VS Code / terminal
2. Run server:
```
dotnet run --project .\server\PigFarmManagement.Server\PigFarmManagement.Server.csproj
```
(Default: http://localhost:5000 if you set ASPNETCORE_URLS; otherwise shown in output.)
3. Run client:
```
dotnet run --project .\client\PigFarmManagement.Client\PigFarmManagement.Client.csproj
```
4. Open the client dev URL printed (typically https://localhost:7xxx) and ensure the server base address in `Program.cs` matches the server port.

## Next Steps

* Add proper solution file & projects referencing (generate via `dotnet new sln && dotnet sln add ...`)
* Implement authentication & roles
* Persist data to Supabase (PostgreSQL)
* Integrate POSPOS API sync
* Add reporting & printing endpoints
* Refactor into layered architecture (Domain, Application, Infrastructure)

> Project constitution: .specify/memory/constitution.md — contains governance, ownership, and template propagation rules.

---
Note: legacy mock data endpoints were removed. Use the POSPOS integration and the JSON import flow for testing and replay.
