# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]`  
**Created**: [DATE]  
**Status**: Draft  
**Input**: User description: "$ARGUMENTS"

## Execution Flow (main)
# Feature Specification: Fix incorrect feed item mapping between PigPen Feed Item and POSPOS Feed Item

**Feature Branch**: `004-fix-incorrect-feed`  
**Created**: 2025-09-28  
**Status**: Draft  
**Input**: User description: "fix incorrect feed item mapping with between pig frame feed item and pospos feed item"

## Clarifications

### Session 2025-09-28
- Q: How should the importer handle POSPOS transactions whose buyer/customer cannot be mapped to an internal Customer/PigPen? → A: A
 - Q: Which idempotency/deduplication strategy should the importer use to avoid creating duplicate Feed records from repeated POSPOS transactions? → A: A
 - Q: How should POSPOS order items be matched to internal feed products when codes/names differ or are missing? → A: A
 - Q: Which buyer identifier should be preferred when mapping POSPOS transactions to internal Customer/PigPen? → A: D
 - Q: If a POSPOS order item has no matching internal product, should the importer still create a `Feed` record using the POSPOS `code`/`name` (no internal product link)? → A: A
 - Q: If a POSPOS transaction contains a mix of mapped and unmapped order items, should the importer import mapped items and create flagged `Feed` records for unmapped items? → A: A

## User Scenarios & Testing *(mandatory)*

### Primary User Story
- As a farm manager or data operator, when importing transactions from POSPOS into PigFarmManagement, the imported feed records MUST reflect the correct product (code and name), quantity (converted correctly from bags to kg), and price so that inventory and feed expense reporting are correct.
 - As a farm manager or data operator, when importing transactions from POSPOS into PigFarmManagement, the imported feed records MUST reflect the correct product (code and name), quantity (mapped from bags as a count), and price so that inventory and feed expense reporting are correct.

### Acceptance Scenarios
1. Given a POSPOS transaction that includes an order_list with an item having `stock`, `code`, `name`, `price`, and `total_price_include_discount`, when the import runs, then a `Feed` record MUST be created with:
   - `ProductCode` equal to POSPOS item `code`
   - `ProductName` equal to POSPOS item `name`
   - `Quantity` equal to POSPOS item `stock` (number of bags); map bags one-to-one as integer quantity
   - `UnitPrice` equal to POSPOS item `price` (price per bag)
   - `TotalPrice` equal to POSPOS `total_price_include_discount`

2. Given a POSPOS order item where `stock` is a decimal or string numeric, when parsing, then the system MUST accept integer, decimal, and numeric-string representations and convert them correctly to integer (number of bags) before mapping to `Feed.Quantity`.

3. Given a POSPOS transaction whose buyer detail includes a valid `code`, when the import runs, then the system MUST find a matching internal `Customer`/`PigPen` using mapping files by matching `buyer_detail.code` only. Do NOT use `key_card_id` or buyer `name` for mapping. If no match exists the transaction MUST be marked as unmapped for the buyer (no pigpen assigned) but the import SHOULD still create `Feed` records for order items. For order items that have an internal product match, create normal `Feed` records linked to the internal product and pigpen (if buyer mapped). For order items that do not match an internal product, create `Feed` records using POSPOS `code` and `name`, set `UnmappedProduct = true`, populate `ExternalProductCode` and `ExternalProductName`, and include these in the import result for manual review.

### Edge Cases
- If `stock` is missing or zero, the import should skip the order item and record a warning for the transaction.
- If `price` is zero or missing, create the Feed with `UnitPrice = 0` but flag the feed item as needing review.
- If POSPOS `code` is not present, use `key_card_id` or fallback to buyer name for logging; do not create ambiguous product codes.
- If mapping causes totalFeed quantity mismatch with POSPOS totals (e.g., due to unexpected fractional stock), log a reconciliation note and include diagnostics in the import result.
 - If an order contains both mapped and unmapped items, import mapped items normally and create `Feed` records for unmapped items flagged as `UnmappedProduct`.
 - If an order item has no internal product match, create a `Feed` record using POSPOS `code` and `name`, mark it `UnmappedProduct`, and include a note in the import result indicating that manual mapping is required before analytics.

## Requirements *(mandatory)*

### Functional Requirements
- **FR-001**: System MUST parse POSPOS transaction JSON robustly accepting numeric types (int, decimal, numeric string) for `order_list[*].stock` and `order_list[*].price`.
 - **FR-002**: System MUST map POSPOS order items to internal `Feed` records such that:
   - **FR-002a**: `Feed.ProductCode` = POSPOS `order_list[*].code`. The importer MUST match existing products by exact `code` first. If a matching internal product is found, use it.
   - **FR-002b**: If POSPOS `code` is missing or no internal product exists with that code, the importer MUST fallback to an exact `name` match (`order_list[*].name`) against internal product names. If exact name match is found, use it.
   - **FR-002c**: If neither exact code nor exact name matches an internal product, the item MUST be treated as unmapped (do not create a Feed for that item) and the transaction should be reported as unmapped per Acceptance Scenario #3.
      - **FR-002d**: If neither exact code nor exact name matches an internal product, create a `Feed` record with `ExternalProductCode`/`ExternalProductName`, set `UnmappedProduct = true`, and include it in the import result for manual mapping.
      - **FR-002e**: `Feed.Quantity` = POSPOS `order_list[*].stock` (integer number of bags). Do not convert to other units.
      - **FR-002f**: `Feed.UnitPrice` = POSPOS `order_list[*].price` (price per bag).
      - **FR-002g**: `Feed.TotalPrice` = `order_list[*].total_price_include_discount`.
   - **FR-002e**: `Feed.UnitPrice` = POSPOS `order_list[*].price` (price per bag).
   - **FR-002f**: `Feed.TotalPrice` = `order_list[*].total_price_include_discount`.
- **FR-003**: System MUST provide clear import result metadata describing for each transaction whether it was imported, skipped, or failed and the reason.
- **FR-004**: System MUST not create duplicate `Feed` entries if the same POSPOS transaction is re-imported. Use the POSPOS transaction `code` (invoice id) as the unique key; if a `Feed` (or import record) already exists for that code the importer MUST skip creating new records and report the import as a duplicate/skip.
-- **FR-005**: System MUST support importing from saved raw JSON files (ImportFromJsonAsync) for replay.

### Key Entities *(include if feature involves data)*
- **POSPOSTransaction**: External transaction containing `code`, `timestamp`, `buyer_detail`, and `order_list`.
- **POSPOSOrderItem**: External order item with attributes: `stock`, `name`, `price`, `code`, `total_price_include_discount`.
- **Feed**: Internal entity representing a feed purchase with attributes: `ProductCode`, `ProductName`, `Quantity`, `UnitPrice`, `TotalPrice`, `InvoiceNumber` (POSPOS transaction code), `ExternalReference`.

### Implementation Notes (for planning, not a substitute for dev tasks)
 - Use `decimal` for intermediate numeric calculations (price). For `stock`, parse numeric strings and convert to integer number of bags.
 - Normalize POSPOS item codes and names by trimming and uppercasing product codes when matching.
 - Add a tolerant JSON deserialization approach to accept numeric strings (custom JsonConverter or use `decimal` properties in DTOs), then coerce `stock` to integer.
 - Ensure `Feed.Quantity` represents count of bags (integer). Map `stock` one-to-one to `Feed.Quantity`.
 - Matching rule: perform exact `code` match first, then exact `name` match as fallback. Do NOT apply fuzzy matching or auto-create product mappings without a separate explicit opt-in workflow.
 - Buyer mapping rule: use `buyer_detail.code` only to map transactions to internal `Customer`/`PigPen` records. Do NOT use `key_card_id` or buyer `name` for automated mapping.
 - Matching rule: perform exact `code` match first, then exact `name` match as fallback for product matching. Do NOT apply fuzzy matching or auto-create product mappings without a separate explicit opt-in workflow.
 - When creating `Feed` records for unmapped products, persist `ExternalProductCode` and `ExternalProductName` on the `Feed` entity and set `UnmappedProduct` = true.

## Review & Acceptance Checklist

### Content Quality
- [x] No low-level implementation details beyond numeric type choices and conversions
- [x] Focused on user value and business needs
- [x] All mandatory sections completed

### Requirement Completeness
- [ ] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

---

## Execution Status

- [x] User description parsed
- [x] Key concepts extracted
- [x] Ambiguities marked
- [x] User scenarios defined
- [x] Requirements generated
- [x] Entities identified
- [ ] Review checklist passed

Additional note: This spec intentionally marks the idempotency/migration task as separate (see TODO list). The next phase is implementation: update DTO numeric types, adjust mapping logic in `FeedImportService.ProcessTransactionForPigPenAsync`, add tolerant JSON parsing, and validate import results with sample POSPOS JSON.
- [ ] No implementation details (languages, frameworks, APIs)
