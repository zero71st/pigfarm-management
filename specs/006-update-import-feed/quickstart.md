# Quickstart: 006-update-import-feed

This guide demonstrates how to test the enhanced feed import functionality that calculates feed consumption and expense using POSPOS transaction data.

## Overview

The enhanced import feature now:
- Calculates feed consumption per pig pen using POSPOS `TotalPriceIncludeDiscount`
- Records formula costs, discount pricing, and computed totals
- Displays new pricing fields in the UI (INV Date, Cost, Price+Discount, Sys Total, POS Total)

## Prerequisites

1. Server running locally on port 5000
2. Client running locally on port 7000  
3. Database schema updated with the latest migration (AddPosTotalPriceIncludeDiscount)

## Testing the Feature

### Step 1: Sample POSPOS Data

Use this sample JSON to test import functionality:

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

### Step 2: Import via API

**Option A: Import from JSON**
```bash
curl -X POST "http://localhost:5000/api/feeds/import/pospos/json" \
  -H "Content-Type: application/json" \
  -d '{"jsonContent": "[{\"no\":\"INV001\",\"date\":\"2024-10-04T10:00:00Z\",\"buyer_detail\":{\"code\":\"M000001\",\"key_card_id\":\"12345\"},\"order_list\":[{\"product_code\":\"PK64000158\",\"product_name\":\"เจ็ท 105 หมูเล็ก 6-15 กก.\",\"stock\":\"20.0\",\"price\":\"755.00\",\"special_price\":\"750.00\",\"discount_amount\":\"10.00\",\"total_price_include_discount\":\"14900.00\"}]}]"}'
```

**Option B: Direct API Import**
```bash
curl -X POST "http://localhost:5000/api/feeds/import/pospos" \
  -H "Content-Type: application/json" \
  -d @sample-pospos.json
```

### Step 3: Verify Import Results

Check the API response for:
- `TotalTransactions`: 1
- `SuccessfulImports`: 1
- `ImportedFeeds`: Contains the processed feed with pricing calculations

### Step 4: View in UI

1. Navigate to `http://localhost:7000/pigpens`
2. Find the pig pen for customer M000001
3. Click "View Detail" 
4. Scroll to the feed comparison table

**Expected UI Display:**
- **INV Date**: Shows 10/04/2024 (formatted feed date)
- **Cost**: Shows formula cost if available, or "-" 
- **Price+Discount**: Shows computed PriceIncludeDiscount (UnitPrice - CostDiscountPrice)
- **Sys Total**: Shows system-calculated total (PriceIncludeDiscount × Quantity)  
- **POS Total**: Shows 14900.00 (from POSPOS TotalPriceIncludeDiscount)

### Step 5: Verify Calculations

For the sample data above:
- Bags imported: 20 (rounded from Stock)
- UnitPrice: 750.00 (special_price if available, else price)
- CostDiscountPrice: 10.00 (from discount_amount)
- PriceIncludeDiscount: 740.00 (750 - 10)
- Sys_TotalPriceIncludeDiscount: 14800.00 (740 × 20)
- Pos_TotalPriceIncludeDiscount: 14900.00 (from POSPOS)

## Test Scenarios

### 1. Formula Cost Lookup
- Import feeds with product codes that match FeedFormulas (e.g., PK64000158)
- Verify `Cost` column shows formula cost value

### 2. Discount Calculations  
- Import with various `discount_amount` values
- Verify `Price+Discount` reflects UnitPrice minus discount

### 3. Total Comparison
- Compare `Sys Total` vs `POS Total` columns
- Values should be close but may differ due to rounding or POS-specific logic

### 4. Missing Data Handling
- Import with missing `total_price_include_discount`
- Verify fallback to calculated `UnitPrice × Quantity`

## Common Issues

**Import Fails:**
- Check customer exists (buyer_detail.code matches a Customer.Code)
- Verify POSPOS JSON structure matches expected format

**Fields Show "-":**
- Normal for manually-added feeds (they don't have POSPOS pricing)
- Formula cost only appears if product code matches FeedFormula

**UI Not Updating:**
- Hard refresh the page (Ctrl+F5)
- Check browser developer console for client errors

## API Endpoints

- `POST /api/feeds/import/pospos/json` - Import from JSON string
- `POST /api/feeds/import/pospos` - Import from POSPOS transaction array
- `GET /api/pigpens/{id}/feeds` - Get feeds for pig pen (includes new fields)

## Database Changes

New columns added to `Feeds` table:
- `Cost` (decimal?) - From FeedFormula.Cost
- `CostDiscountPrice` (decimal?) - From PosPosFeedItem.DiscountAmount  
- `PriceIncludeDiscount` (decimal?) - UnitPrice - CostDiscountPrice
- `Sys_TotalPriceIncludeDiscount` (decimal?) - PriceIncludeDiscount × Quantity
- `TotalPriceIncludeDiscount` (decimal) - Renamed from TotalPrice
- `Pos_TotalPriceIncludeDiscount` (decimal?) - POS-provided total for comparison

Migration: `AddPosTotalPriceIncludeDiscount`