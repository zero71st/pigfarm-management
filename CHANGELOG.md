# CHANGELOG

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### [Feature 014] Invoice Management Tab in Feed History (2025-11-30)

**Feature**: Add tabbed interface to pig pen detail page separating invoice management from feed history, with delete-by-invoice functionality

**Changes**:

#### Backend (`PigPenEndpoints.cs`, `FeedRepository.cs`, `IRepositories.cs`)
- Added `DELETE /api/pigpens/{pigPenId}/invoices/{invoiceReferenceCode}` endpoint
  - Deletes all feed items associated with a specific invoice reference code
  - Returns `DeleteInvoiceResponse` with deleted count, invoice reference, and message
  - Error handling: 400 (invalid reference), 404 (not found), 500 (internal error)
  - Logging: Records deletion count and invoice reference code
- Added `DeleteByInvoiceReferenceAsync()` method to FeedRepository
  - Atomic operation using `RemoveRange()` and `SaveChangesAsync()`
  - Returns count of deleted items

#### Shared DTOs (`InvoiceGroupDto.cs`, `PigPenDtos.cs`)
- Created `InvoiceGroupDto` record for invoice list display
  - Fields: InvoiceReferenceCode, TransactionCode, TotalAmount, InvoiceDate, ItemCount
- Added `DeleteInvoiceResponse` record for delete operation response
  - Fields: DeletedCount, InvoiceReferenceCode, Message

#### Frontend Components (`InvoiceListTab.razor`, `FeedHistoryTab.razor`, `DeleteInvoiceConfirmDialog.razor`)
- Created `InvoiceListTab` component
  - Client-side LINQ GroupBy on InvoiceReferenceCode
  - MudTable with 6 columns (Thai labels): reference code, transaction code, total amount, date, item count, actions
  - Delete button triggers confirmation dialog
  - EventCallback pattern for parent notification on deletion
  - Empty state: Shows info alert with import button instruction
- Created `DeleteInvoiceConfirmDialog` component
  - Thai UI text: "ยืนยันการลบใบแจ้งหนี้"
  - Displays: invoice reference, item count (MudChip), formatted total amount
  - Warning: "การดำเนินการนี้ไม่สามารถย้อนกลับได้"
  - Actions: "ยกเลิก" (Cancel) and "ลบใบแจ้งหนี้" (Delete)
- Created `FeedHistoryTab` component placeholder for existing feed history

#### Frontend Services (`PigPenService.cs`)
- Added `DeleteInvoiceByReferenceAsync()` method
  - HTTP DELETE call with EnsureSuccessStatusCode
  - Returns `DeleteInvoiceResponse` DTO
  - Throws InvalidOperationException on response parsing failure

#### Frontend Pages (`PigPenDetailPage.razor`)
- Replaced "Section 3: Feed History" with tabbed interface using MudTabs
  - Tab 1 (default): "การจัดการใบแจ้งหนี้" (Invoice Management) with InvoiceListTab
  - Tab 2: "ประวัติการให้อาหาร" (Feed History) with FeedHistoryTabContent RenderFragment
- Added tab state management
  - `_activeTabIndex` field for tracking active tab
  - `OnTabChanged()` event handler - refreshes feeds on tab switch
  - `HandleInvoiceDeleted()` event handler - refreshes feeds after deletion

**Performance Targets**:
- Tab switch: <500ms
- Invoice list load: <1s for 50-100 invoices
- Delete operation: <50ms (typical 5-10 items)

**Testing**: Manual validation via `specs/014-add-invoice-list/quickstart.md` (6 scenarios)

---

### [Feature 012] POSPOS Import Enhancement - Latest Member Display (2025-11-29)

**Feature**: Enhance POSPOS customer import workflow to display only the latest customer and disable bulk select-all operation

**Changes**:

#### Backend (`CustomerImportEndpoints.cs`)
- Enhanced `GET /api/customers/import/candidates` endpoint with optional `source` query parameter
  - `source=pospos`: Returns only the latest POSPOS member (1 item max, ordered by CreatedAt DESC, Id DESC)
  - `source=all`: Returns all available members (default, backward compatible)
  - Parameter omitted: Defaults to `all` (fully backward compatible)
- Added validation: Invalid source parameter returns 400 Bad Request with descriptive message
- Enhanced error handling:
  - Returns 503 Service Unavailable with message "POSPOS service unavailable. Please try again later." for POSPOS API failures
  - Distinguishes from other 500 errors for better user experience and debugging

#### Frontend (`ImportCandidatesDialog.razor`)
- Added `_source` field to track import context (pospos vs all)
- Modified `LoadCandidates()` method to include source parameter in API URL
- Conditionally hidden select-all checkbox when viewing latest member only (`source=pospos`)
- Individual member row selection remains enabled for both `source` modes
- Enhanced error handling: Shows distinct snackbar message for POSPOS service unavailable vs. other errors
- Selection state remains session-scoped (ephemeral, no database persistence)

#### Documentation (`docs/IMPORT_API.md`)
- Updated `/import/customers/candidates` endpoint documentation with:
  - New `source` query parameter description
  - Usage examples for all query parameter values
  - Response status codes (200, 400, 401, 500, 503)
  - Error response formats with examples

#### Development Reference (`.github/copilot-instructions.md`)
- Added Feature 012 section documenting:
  - Scope and files modified
  - Key API and component changes
  - Pattern used (modifying existing infrastructure)
  - Error handling behavior
  - Selection state lifecycle
  - Reference to validation scenarios

**API Changes**:
- `GET /api/customers/import/candidates?source=pospos` - Returns 1 latest member
- `GET /api/customers/import/candidates?source=all` - Returns all members (new explicit parameter)
- `GET /api/customers/import/candidates` - Returns all members (default, unchanged behavior)

**Breaking Changes**: None (fully backward compatible)

**Migration Required**: No database migrations required

**Testing**:
- Contract validation: All query parameters, response formats, error codes verified
- Manual validation scenarios: 8 user workflow scenarios documented and tested
- Integration tests: Backward compatibility, error handling, performance verified
- See `specs/012-update-search-customer/quickstart.md` for complete validation scenarios

**Implementation Details**:
- No new database tables or schema changes
- No new migrations required
- No new DTOs or models (existing CandidateMember class used)
- Server-side filtering (more efficient than client-side)
- Session-scoped selection state (no persistence)

---

## How to Contribute

When adding new features or changes:
1. Add entry under `[Unreleased]` section
2. Follow the format of existing entries
3. Include: Backend changes, Frontend changes, API changes, Breaking changes, Testing
4. Reference feature number (e.g., Feature 012) and file locations
5. Include date in YYYY-MM-DD format

---

**Last Updated**: 2025-11-29  
**Changelog Maintained By**: Development Team
