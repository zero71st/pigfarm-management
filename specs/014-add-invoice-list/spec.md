# Feature Specification: Invoice Management Tab in Feed History Section

**Feature Branch**: `014-add-invoice-list`  
**Created**: 2025-11-30  
**Status**: Draft  
**Input**: User description: "add invoice list tab (include import, delete) in feed history section, I mean first tab is manage import invoide history from pospos and second is existing feed history tab, this requirement use exsiting db table"

## Execution Flow (main)
```
1. Parse user description from Input
   → Feature adds tabbed interface to feed history section
2. Extract key concepts from description
   → Actors: Farm operators managing pig pen feeds
   → Actions: View invoices, import invoices from POSPOS, delete invoices
   → Data: Feed invoices (using existing FeedEntity table with InvoiceReferenceCode)
   → Constraints: Must use existing database table
3. For each unclear aspect:
   → RESOLVED: Group invoices by InvoiceReferenceCode
   → RESOLVED: Delete all feed items matching InvoiceReferenceCode
   → RESOLVED: Reuse existing POSPOS import functionality (no new import)
4. Fill User Scenarios & Testing section
   → User flow: Navigate to pig pen detail → switch to invoice tab → view/import/delete invoices
5. Generate Functional Requirements
   → Tab switching, invoice listing, POSPOS import, invoice deletion
6. Identify Key Entities
   → Invoice (logical grouping of FeedEntity records)
7. Run Review Checklist
   → All clarifications resolved
8. Return: SUCCESS (spec ready for planning)
```

---

## ⚡ Quick Guidelines
- ✅ Focus on WHAT users need and WHY
- ❌ Avoid HOW to implement (no tech stack, APIs, code structure)
- 👥 Written for business stakeholders, not developers

---

## User Scenarios & Testing

### Primary User Story
As a farm operator managing a pig pen, I want to view and manage feed invoices separately from individual feed records so that I can track invoice-level information from POSPOS, import new invoices, and remove incorrect or duplicate invoices while maintaining detailed feed history.

### Acceptance Scenarios

#### Scenario 1: View Invoice List
1. **Given** I am viewing a pig pen detail page with existing feed records
2. **When** I navigate to the feed history section
3. **Then** I see two tabs: "Invoice Management" (first tab) and "Feed History" (second tab)
4. **And** the Invoice Management tab displays a list of all invoices grouped by invoice reference
5. **And** each invoice shows: invoice reference code, transaction code, total amount, date, and item count

#### Scenario 2: Import Invoices from POSPOS
1. **Given** I am on the Invoice Management tab or Feed History tab
2. **When** I click the existing "Import Feeds" button
3. **Then** the system opens the existing import dialog (same as current feed import)
4. **When** I complete the import process
5. **Then** the system imports feed data from POSPOS (with InvoiceReferenceCode populated)
6. **And** displays success/error summary as per existing functionality
7. **And** both Invoice Management and Feed History tabs automatically recalculate and refresh

#### Scenario 3: Delete Invoice
1. **Given** I am viewing the invoice list with multiple invoices
2. **When** I click the delete button on an invoice with InvoiceReferenceCode "INV-001"
3. **Then** the system displays a confirmation dialog showing invoice details and affected feed items
4. **When** I confirm deletion
5. **Then** the system removes ALL feed items where InvoiceReferenceCode = "INV-001"
6. **And** displays a success message
7. **And** refreshes both the invoice list and feed history tab with recalculated data

#### Scenario 4: Switch Between Tabs
1. **Given** I am on either the Invoice Management or Feed History tab
2. **When** I click the other tab
3. **Then** the view switches to show the corresponding data
4. **And** any filters or selections are maintained within each tab independently

### Edge Cases
- What happens when an invoice has no InvoiceReferenceCode (manually added feeds)?
  - Do not show in Invoice Management tab (only show feeds with InvoiceReferenceCode)
- How does system handle when POSPOS import returns duplicate invoices that already exist?
  - Uses existing import logic idempotency (skips based on TransactionCode)
- What happens when all feed items of an invoice are deleted?
  - Invoice automatically disappears from the list
- How does system handle partial invoice imports (some items fail)?
  - Uses existing import error handling (detailed messages, import successful items only)
- What happens when switching tabs during operations?
  - Tab switching triggers automatic recalculation for both tabs

---

## Requirements

### Functional Requirements

#### Tab Interface
- **FR-001**: System MUST display feed history section with two tabs: "Invoice Management" (first/default tab) and "Feed History" (second tab)
- **FR-002**: System MUST allow users to switch between tabs without losing current pig pen context
- **FR-003**: System MUST maintain independent state for each tab (filters, sorting, selection)

#### Invoice List Display
- **FR-004**: System MUST display invoices grouped by InvoiceReferenceCode (one row per unique InvoiceReferenceCode)
- **FR-005**: Each invoice entry MUST show:
  - Invoice reference code (InvoiceReferenceCode)
  - Transaction code (TransactionCode from first feed item)
  - Total amount (sum of TotalPriceIncludeDiscount for all feed items)
  - Invoice date (FeedDate from feed items)
  - Number of feed items in the invoice
- **FR-006**: System MUST sort invoices by date in ascending order (oldest first)
- **FR-007**: System MUST exclude feed items that have NULL or empty InvoiceReferenceCode from the invoice list

#### Invoice Import from POSPOS
- **FR-008**: System MUST use existing "Import Feeds" button functionality (no new import button needed)
- **FR-009**: System MUST leverage existing POSPOS import dialog and workflow
- **FR-010**: System MUST populate InvoiceReferenceCode field during existing import process
- **FR-011**: System MUST maintain existing import idempotency logic (skip duplicates by TransactionCode)
- **FR-012**: System MUST recalculate both Invoice Management and Feed History tabs after import
- **FR-013**: System MUST display import results using existing import result dialog

#### Invoice Deletion
- **FR-014**: System MUST provide delete button for each invoice in the list
- **FR-015**: System MUST display confirmation dialog before deleting an invoice
- **FR-016**: Confirmation dialog MUST show:
  - Invoice reference code
  - Number of feed items that will be deleted
  - Total amount
  - Warning that action cannot be undone
- **FR-017**: System MUST delete ALL FeedEntity records where InvoiceReferenceCode matches the selected invoice when user confirms deletion
- **FR-018**: System MUST recalculate both Invoice Management and Feed History tabs after successful deletion
- **FR-019**: System MUST display success message after deletion
- **FR-020**: System MUST handle errors during deletion and display appropriate error messages

#### Data Integrity and Tab Synchronization
- **FR-021**: System MUST use existing FeedEntity database table for all invoice operations (no schema changes)
- **FR-022**: System MUST preserve existing feed history functionality when invoice tab is added
- **FR-023**: System MUST maintain referential integrity with pig pen when deleting invoices
- **FR-024**: System MUST automatically recalculate feed summary data when switching between tabs
- **FR-025**: System MUST refresh both tabs after any import or delete operation
- **FR-026**: System MUST reload data from server when tab becomes active to ensure data consistency

### Key Entities

- **Invoice (Logical Grouping)**: Represents a collection of feed items imported from POSPOS as a single transaction
  - Attributes derived from FeedEntity records:
    - InvoiceReferenceCode: Identifier from POSPOS invoice reference
    - TransactionCode: Unique transaction identifier
    - TotalAmount: Sum of TotalPriceIncludeDiscount from all items
    - InvoiceDate: FeedDate from feed items
    - ItemCount: Number of feed items in the invoice
  - Relationships: Grouped from multiple FeedEntity records with same InvoiceReferenceCode
  - Note: Not a separate database table, but a logical view/grouping

- **Feed Item (Existing FeedEntity)**: Individual feed transaction record
  - Already exists in database with InvoiceReferenceCode field
  - Attributes used for invoicing:
    - InvoiceReferenceCode: Links to invoice group
    - TransactionCode: Unique identifier
    - ProductCode, ProductName: Feed product details
    - Quantity, UnitPrice, TotalPriceIncludeDiscount: Pricing data
    - FeedDate: Date of transaction
    - ExternalReference: POSPOS reference

---

## Review & Acceptance Checklist

### Content Quality
- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

### Requirement Completeness
- [x] No [NEEDS CLARIFICATION] markers remain
  - All clarifications resolved:
    1. ✅ Invoice grouping by InvoiceReferenceCode
    2. ✅ Delete all feed items matching InvoiceReferenceCode
    3. ✅ Use existing import functionality (no new import)
    4. ✅ Exclude feeds without InvoiceReferenceCode
    5. ✅ Auto-recalculate both tabs on switch
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Scope is clearly bounded (existing feed history section only)
- [x] Dependencies identified (existing FeedEntity table, existing POSPOS import)

---

## Execution Status

- [x] User description parsed
- [x] Key concepts extracted
- [x] Ambiguities marked
- [x] User scenarios defined
- [x] Requirements generated
- [x] Entities identified
- [x] Review checklist passed

---

## Implementation Notes for Planning Phase

All requirements clarified and ready for implementation planning:

1. **Invoice Grouping**: Group by `InvoiceReferenceCode` field - one row per unique value
2. **Deletion Scope**: Delete ALL `FeedEntity` records matching the `InvoiceReferenceCode`
3. **Import Functionality**: Reuse existing "Import Feeds" button and POSPOS import workflow (no new import UI needed)
4. **Uncategorized Feeds**: Only show invoices with non-null/non-empty `InvoiceReferenceCode` in invoice tab
5. **Tab Synchronization**: Automatically reload and recalculate data when switching tabs or after operations
6. **Sort Order**: Display invoices in ascending order by date (oldest first)

**Key Design Decision**: This feature adds a new view/grouping of existing data without requiring database schema changes or new import infrastructure.
