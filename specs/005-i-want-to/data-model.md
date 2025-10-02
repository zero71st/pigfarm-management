# Data Model: Import POSPOS Product to Feed Formula

## POSPOS Product JSON Structure
```json
{
    "success": 1,
    "data": [
        {
            "_id": "600c45c64bd0c8591b75ce5c",
            "code": "PK64000158",
            "name": "เจ็ท 105 หมูเล็ก 6-15 กก.",
            "cost": 737,
            "category": {
                "name": "อาหารสัตว์"
            },
            "unit": {
                "name": "กระสอบ"
            },
            "lastupdate": "2025-10-01T05:04:59.197Z"
        }
    ]
}
```

## Field Mapping
- POSPOS `_id` → FeedFormula `ExternalId`
- POSPOS `code` → FeedFormula `Code`
- POSPOS `name` → FeedFormula `Name`
- POSPOS `cost` → FeedFormula `Cost`
- POSPOS `category.name` → FeedFormula `CategoryName`
- POSPOS `unit.name` → FeedFormula `UnitName`
- POSPOS `lastupdate` → FeedFormula `LastUpdate`

## POSPOS Transaction Models (from PosApiModels.cs)
Uses existing `PosPosFeedTransaction` and `PosPosFeedItem` models for transaction data:

- **PosPosFeedTransaction**: Contains transaction details with `OrderList` of feed items
- **PosPosFeedItem**: Individual items with `Stock` (quantity), `SpecialPrice`, `Code`, etc.

## Entities

### FeedFormula
- **Purpose**: Represents feed products imported from POSPOS with product data.
- **Fields**:
  - Id: Guid (primary key)
  - ExternalId: Guid? (from POSPOS _id)
  - Code: string? (POSPOS code, primary product code)
  - Name: string? (POSPOS name)
  - Cost: decimal? (POSPOS cost, used for profit calculations)
  - ConsumeRate: decimal? (user input, e.g., 0.5 per pig)
  - CategoryName: string? (POSPOS category.name)
  - UnitName: string? (POSPOS unit.name)
  - LastUpdate: DateTime? (POSPOS lastupdate)
- **Relationships**: None direct, used in feed history display.
- **Validation**: Code required, Cost >= 0 if present.

### POSPOSTransaction (uses PosPosFeedTransaction)
- **Purpose**: External transaction data from POSPOS using existing `PosPosFeedTransaction` model.
- **Fields**: Uses `PosPosFeedTransaction` with `OrderList` containing `PosPosFeedItem`s (quantities, SpecialPrice, etc.)
- **Relationships**: One-to-many with FeedFormula 1 : 1 invoice has many feed formulas.

## State Transitions
- FeedFormula: Created from POSPOS import, immutable after creation.

## Data Integrity Rules
- Historical FeedFormula records preserve imported data.
- No edits allowed to imported fields to maintain audit trail.