# Data Model: Import POSPOS Stock to Feed Formula

## Entities

### FeedFormula
- **Purpose**: Represents feed products imported from POSPOS with stock data.
- **Fields**:
  - Id: Guid (primary key)
  - ExternalId: Guid? (from POSPOS _id)
  - Code: string? (POSPOS code, primary product code)
  - Name: string? (POSPOS name)
  - Brand: string? (POSPOS brand)
  - SpecialPrice: decimal? (POSPOS special_price)
  - Cost: decimal? (POSPOS cost, used for profit calculations)
  - CostDiscountPrice: decimal? (POSPOS cost_discount_price)
  - ConsumeRate: decimal? (user input, e.g., 0.5 per pig)
  - CategoryName: string? (POSPOS category.name)
  - UnitName: string? (POSPOS unit.name)
  - LastUpdate: DateTime? (POSPOS lastupdate)
- **Relationships**: None direct, used in feed history display.
- **Validation**: Code required, Cost >= 0 if present.

### POSPOSTransaction
- **Purpose**: External transaction data from POSPOS.
- **Fields**: As per existing DTOs (code, order_list with stock, etc.)
- **Relationships**: One-to-many with FeedFormula (per invoice).

## State Transitions
- FeedFormula: Created from POSPOS import, immutable after creation.

## Data Integrity Rules
- Historical FeedFormula records preserve imported data.
- No edits allowed to imported fields to maintain audit trail.