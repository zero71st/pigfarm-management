# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]`  
**Created**: [DATE]  
**Status**: Draft  
**Input**: User description: "$ARGUMENTS"

## Execution Flow (main)
# Feature Specification: Update import — compute feed consumption & expense from POSPOS

**Feature Branch**: `006-update-import-feed`  
**Created**: 2025-10-04  
**Status**: Draft  
**Input**: Update import feed consume from pos transaction; calculate feed consume per pig pen and calculate expense per pig pen using `PosPosFeedItem.TotalPriceIncludeDiscount`

---

## Summary
Import POSPOS transactions and use line-item fields (notably `Stock` and `TotalPriceIncludeDiscount`) to compute feed consumption (bags, bags per pig, coverage) and monetary expense per pig pen. Provide per-item and per-invoice per-pig-pen aggregates in the import result and expose via API for UI consumption. Calculations are transient by default; persistence of computed fields is optional and listed as a next step.

## Clarifications

### Session 2025-10-04

- Q: When BagPerPig is unknown, how should CoveragePct be shown? → A: Leave CoveragePct empty when BagPerPig unknown (show as "—" in UI)

## User Scenarios & Testing *(mandatory)*

### Primary User Story
As a farm manager I want POSPOS transaction data imported into the system and translated into feed consumption and expense per pig pen, so I can understand how many bags were consumed, the bags-per-pig rate, and the expense distributed across the pen.

### Acceptance Scenarios
1. Given a POSPOS transaction with order_list items, when importing for a pig pen, then for each order_list item the system creates a `Feed` record with:
   - `Quantity` = round(`stock`) (MidpointRounding.AwayFromZero)
   - `UnitPrice` = `special_price` if < `price`, else `price`
   - `TotalPrice` = `total_price_include_discount` (fallback to `UnitPrice * Quantity` if missing)
2. Given N pigs in the pig pen, when items are imported, the system computes per-item:
   - `BagsImported` (int), `BagsPerPig` = BagsImported / N (decimal), `Expense` = `TotalPriceIncludeDiscount` (decimal)
3. Given multiple items imported for the same invoice and pig pen, when summarizing, the system returns per-invoice, per-pig-pen aggregates:
   - `TotalBags`, `TotalExpense`, `AvgBagsPerPig`, `ExpensePerPig`, `CoveragePct` (where CoveragePct = TotalBags / (BagPerPig * N) when BagPerPig is known)
4. Given invalid inputs (negative stock, missing totals), the import includes warnings in `FeedImportResult.Errors` and uses defensible fallbacks (zero or computed fallback) as documented.

### Edge Cases
- Negative or zero `stock`: treat as 0, add warning.
- Missing or zero `total_price_include_discount`: fallback to `UnitPrice * Quantity` and flag the item.
- Pig pen `PigQty` == 0: set per-pig metrics to 0 and flag for manual review.
- Duplicate invoice: if invoice code already imported, skip and report in errors unless force-import is requested.

## Functional Requirements

- FR-001: Import POSPOS `PosPosFeedTransaction` objects and create `Feed` entries per pig pen using `PosPosFeedItem.Stock` rounded to integer bags.
- FR-002: Choose `UnitPrice` as `SpecialPrice` when less than `Price`, otherwise `Price`.
- FR-003: Use `TotalPriceIncludeDiscount` as authoritative line expense; fallback to `UnitPrice * Quantity` if absent.
- FR-004: For each imported feed item, compute and return transient metrics: `BagsImported`, `BagsPerPig`, and `Expense`.
- FR-005: Aggregate per-invoice per-pig-pen metrics: `TotalBags`, `TotalExpense`, `AvgBagsPerPig`, `ExpensePerPig`, `CoveragePct` (if BagPerPig known).
- FR-006: Record validation warnings and errors in `FeedImportResult.Errors` for edge cases.

## Key Entities & Data Model Impact

- PosPosFeedItem (external): use `Stock`, `Price`, `SpecialPrice`, `TotalPriceIncludeDiscount`.
- Feed (existing): ensure `Quantity`, `UnitPrice`, `TotalPrice`, `FeedDate`, `InvoiceNumber`, `InvoiceReferenceCode` are populated from POSPOS data. Optionally add transient fields in API mapping: `BagsPerPig`, `Expense`.
- FeedImportResult (existing): add `ImportedFeedItems` (per-item details) and `ImportedFeedSummaries` (per invoice / pig pen aggregates).

## Non-Functional Requirements

- Data volume: small (<100 products, <100 transactions/day) — import latency target: <10s for typical import.
- Reliability: network timeouts and 5xx responses handled with retries/backoff and clear error reporting.

## Implementation Notes

- Use `PosPosFeedItem.Stock` rounded using MidpointRounding.AwayFromZero for bag counts.
- Prefer `TotalPriceIncludeDiscount` for expense attribution. Do not recompute expense except as fallback.
- If FeedFormula data is available for the pig pen, use `BagPerPig` from the active assignment to compute `CoveragePct` accurately; otherwise the import result should still provide `AvgBagsPerPig` (from imported data) and leave `CoveragePct` empty or approximate with a default.
- Computed metrics are returned in `FeedImportResult` and are not persisted to DB by default. Persisting them is a follow-up task.

## Out-of-Scope / Next Steps

- Persisting computed per-item metrics to `FeedEntity` (requires DB migration) — can be added if needed.
- Automated reconciliation against historical imports (reporting refinement) — future work.

## Review & Acceptance Checklist

- [x] Functional requirements clear and testable
- [x] Edge cases enumerated
- [x] Data sources and authoritative fields identified (`TotalPriceIncludeDiscount`)
- [x] Ready for planning

---

## Execution Status

- [x] Spec created and populated
- [ ] Clarifications complete (none outstanding)
- [ ] Ready for planning
