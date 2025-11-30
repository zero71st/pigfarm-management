# Tasks: Invoice Management Tab in Feed History Section

**Input**: Design documents from `/specs/014-add-invoice-list/`
**Prerequisites**: plan.md (complete), contracts/ (complete), quickstart.md (complete)

## Execution Flow
```
1. Load plan.md from feature directory ✅
   → Tech stack: C# .NET 8, Blazor WebAssembly, MudBlazor 7.x, EF Core 8.x
   → Structure: Web app (client/server/shared)
2. Load design documents:
   → contracts/delete-invoice-endpoint.md ✅
   → quickstart.md (6 validation scenarios) ✅
   → data-model.md (skipped - no new entities)
3. Generate tasks by category:
   → Setup: No new dependencies, no project init needed
   → Validation: Manual validation via quickstart.md (per user requirement)
   → Core: Backend (endpoint, repository), Shared (DTO), Frontend (components, service)
   → Integration: Tab refresh logic, dialog integration
   → Polish: Performance check, documentation
4. Apply task rules:
   → Different files = [P] for parallel
   → Same file = sequential (no [P])
   → Backend before frontend (endpoint enables UI)
5. Number tasks sequentially (T001-T015)
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Exact file paths included in descriptions

## Path Conventions (Web Application)
- **Backend**: `src/server/PigFarmManagement.Server/`
- **Frontend**: `src/client/PigFarmManagement.Client/`
- **Shared**: `src/shared/PigFarmManagement.Shared/`

---

## Phase 3.1: Setup
*No setup tasks needed - all dependencies already in project*

---

## Phase 3.2: Validation Preparation
Manual validation scenarios documented in quickstart.md. No automated tests per user requirement.

- [ ] **T001** [P] Review `contracts/delete-invoice-endpoint.md` and confirm DELETE endpoint specification (path params, response schema, business rules)
- [ ] **T002** [P] Review `quickstart.md` scenarios 1-6 and prepare test data (pig pen with 10+ invoices, various item counts)

---

## Phase 3.3: Core Implementation

### Backend (Server)
- [ ] **T003** [P] Add `DeleteByInvoiceReferenceAsync()` method in `src/server/PigFarmManagement.Server/Infrastructure/Data/Repositories/FeedRepository.cs`
  - Parameters: `Guid pigPenId, string invoiceReferenceCode`
  - Returns: `Task<int>` (deleted count)
  - Logic: `DELETE FROM Feeds WHERE PigPenId = @pigPenId AND InvoiceReferenceCode = @invoiceReferenceCode`
  - Transaction: Use atomic EF Core `SaveChangesAsync()`

- [ ] **T004** Add DELETE endpoint in `src/server/PigFarmManagement.Server/Features/PigPens/PigPenEndpoints.cs`
  - Route: `DELETE /api/pigpens/{pigPenId}/invoices/{invoiceReferenceCode}`
  - Authorization: `[Authorize]` attribute
  - Validation: Null/empty check on invoiceReferenceCode → 400
  - Repository call: `FeedRepository.DeleteByInvoiceReferenceAsync()`
  - Response: `DeleteInvoiceResponse` record (deletedCount, invoiceReferenceCode, message)
  - Error handling: Return 404 if deletedCount == 0
  - Logging: Log deletion with count and invoice reference
  - **Dependency**: Blocked by T003 (needs repository method)

### Shared (DTOs)
- [ ] **T005** [P] Create `InvoiceGroupDto.cs` in `src/shared/PigFarmManagement.Shared/DTOs/`
  - Record definition:
    ```csharp
    public record InvoiceGroupDto(
        string InvoiceReferenceCode,
        string TransactionCode,      // From first feed item
        decimal TotalAmount,          // Sum of all items
        DateTime InvoiceDate,         // From first feed item
        int ItemCount                 // Count of grouped items
    );
    ```

### Frontend (Client)

#### Components (New)
- [ ] **T006** [P] Create `InvoiceListTab.razor` in `src/client/PigFarmManagement.Client/Features/PigPens/Components/`
  - Props: `Guid PigPenId, List<FeedDto> Feeds`
  - Grouping logic: LINQ `GroupBy(f => f.InvoiceReferenceCode)` → filter null/empty
  - UI: MudTable with columns (InvoiceReferenceCode, TransactionCode, TotalAmount ฿, InvoiceDate yyyy-MM-dd, ItemCount)
  - Sort: `OrderBy(g => g.First().InvoiceDate)` ascending
  - Actions: Delete button per row → opens DeleteInvoiceConfirmDialog
  - Callback: `EventCallback OnInvoiceDeleted` to parent for refresh

- [ ] **T007** [P] Create `FeedHistoryTab.razor` in `src/client/PigFarmManagement.Client/Features/PigPens/Components/`
  - Extract existing feed history table code from `PigPenDetailPage.razor`
  - Props: `List<FeedDto> Feeds`
  - UI: Preserve existing MudTable layout (all feed detail columns)
  - No business logic changes - pure code extraction

- [ ] **T008** [P] Create `DeleteInvoiceConfirmDialog.razor` in `src/client/PigFarmManagement.Client/Features/PigPens/Components/`
  - Props: `string InvoiceReferenceCode, decimal TotalAmount, int ItemCount`
  - UI: MudDialog with:
    - Title: "ยืนยันการลบใบแจ้งหนี้" (Confirm Delete Invoice)
    - Content: Display invoice reference, item count, total amount
    - Warning: "การดำเนินการนี้ไม่สามารถย้อนกลับได้" (Cannot be undone)
    - Actions: "ยกเลิก" (Cancel) + "ลบ" (Delete) buttons
  - Callbacks: `EventCallback OnConfirm, EventCallback OnCancel`

#### Services (Modify)
- [ ] **T009** Add `DeleteInvoiceByReferenceAsync()` method in `src/client/PigFarmManagement.Client/Features/PigPens/Services/PigPenService.cs`
  - Parameters: `Guid pigPenId, string invoiceReferenceCode`
  - HTTP call: `DELETE /api/pigpens/{pigPenId}/invoices/{invoiceReferenceCode}`
  - Returns: `Task<DeleteInvoiceResponse>`
  - Error handling: Throw on non-success status codes
  - **Dependency**: Blocked by T004 (needs backend endpoint)

#### Pages (Modify)
- [ ] **T010** Wrap feed history section in MudTabs in `src/client/PigFarmManagement.Client/Features/PigPens/Pages/PigPenDetailPage.razor`
  - Replace existing feed history `<div>` with `<MudTabs>` component
  - Tab 1 (default): "การจัดการใบแจ้งหนี้" (Invoice Management) → `<InvoiceListTab>`
  - Tab 2: "ประวัติการให้อาหาร" (Feed History) → `<FeedHistoryTab>`
  - Tab switch event: Reload feeds on `ActivePanelIndexChanged` (recalculate both tabs)
  - Props passing: Pass `PigPenId` and `Feeds` to both tab components
  - **Dependency**: Blocked by T006, T007 (needs tab components)

---

## Phase 3.4: Integration

- [ ] **T011** Add dialog integration in `InvoiceListTab.razor`
  - Wire delete button click → show `DeleteInvoiceConfirmDialog`
  - On confirm → call `PigPenService.DeleteInvoiceByReferenceAsync()`
  - On success → show MudSnackbar success message
  - On success → trigger `OnInvoiceDeleted` callback to parent
  - On error → show MudSnackbar error message
  - **Dependency**: Blocked by T006, T008, T009 (needs tab, dialog, service)

- [ ] **T012** Add tab refresh logic in `PigPenDetailPage.razor`
  - Handle `OnInvoiceDeleted` event from `InvoiceListTab`
  - Reload feeds: Call existing `LoadFeedsAsync()` method
  - Update both tabs: Feed list refresh automatically updates both tab components
  - **Dependency**: Blocked by T010, T011 (needs tab wrapper and dialog integration)

---

## Phase 3.5: Polish

- [ ] **T013** [P] Performance verification
  - Test tab switch with 50+ invoices: Verify <500ms response
  - Test invoice list load: Verify <1s for 50-100 invoices
  - Test delete operation: Verify <1s including confirmation
  - Document any performance issues in CHANGELOG.md

- [ ] **T014** [P] Execute `quickstart.md` manual validation scenarios
  - Scenario 1: View invoice list grouped by reference code
  - Scenario 2: Switch between tabs
  - Scenario 3: Delete invoice with confirmation
  - Scenario 4: Import feeds and verify both tabs update
  - Scenario 5: Handle empty state (no invoices)
  - Scenario 6: Performance check (optional)
  - Document validation results in `specs/014-add-invoice-list/validation-results.md`

- [ ] **T015** [P] Update documentation
  - Add Feature 014 to `CHANGELOG.md` (Invoice Management tab)
  - Update `.github/copilot-instructions.md` if not auto-updated
  - Verify `docs/QUICKSTART.md` mentions invoice management (if applicable)

---

## Dependencies

### Critical Path (Sequential)
```
T003 (Repository method)
  → T004 (DELETE endpoint)
    → T009 (Service method)
      → T011 (Dialog integration)
        → T012 (Tab refresh)
```

### Parallel Groups
```
Group 1 (Backend + Shared): T003, T005 can run together
Group 2 (Frontend components): T006, T007, T008 can run together (all new files)
Group 3 (Validation prep): T001, T002 can run together
Group 4 (Polish): T013, T014, T015 can run together
```

### Blocking Relationships
- T004 blocked by T003 (needs repository)
- T009 blocked by T004 (needs endpoint)
- T010 blocked by T006, T007 (needs tab components)
- T011 blocked by T006, T008, T009 (needs tab, dialog, service)
- T012 blocked by T010, T011 (needs tab wrapper and dialog)
- T013-T015 blocked by T012 (needs complete implementation)

---

## Parallel Execution Examples

### Example 1: Backend + Shared (Early Phase)
```powershell
# Launch T003 and T005 together (different files):
# Terminal 1
Task: "Add DeleteByInvoiceReferenceAsync method in FeedRepository.cs"

# Terminal 2
Task: "Create InvoiceGroupDto.cs record in shared/DTOs/"
```

### Example 2: Frontend Components (Mid Phase)
```powershell
# Launch T006, T007, T008 together (all new files):
# Terminal 1
Task: "Create InvoiceListTab.razor component with grouping logic"

# Terminal 2
Task: "Create FeedHistoryTab.razor by extracting existing code"

# Terminal 3
Task: "Create DeleteInvoiceConfirmDialog.razor with Thai UI text"
```

### Example 3: Polish Phase (Final)
```powershell
# Launch T013, T014, T015 together:
# Terminal 1
Task: "Performance verification - test tab switch and delete with 50+ invoices"

# Terminal 2
Task: "Execute all 6 quickstart.md validation scenarios and document results"

# Terminal 3
Task: "Update CHANGELOG.md and verify documentation completeness"
```

---

## Notes

- **[P] marking**: Only for truly independent tasks (different files, no data dependencies)
- **Thai UI text**: All user-facing labels, buttons, messages in Thai (per Feature 013)
- **No automated tests**: Per user requirement - rely on manual validation via quickstart.md
- **Commit frequency**: Commit after each task or logical checkpoint (T003-T004 together, T006-T008 together, etc.)
- **Error handling**: All service calls must handle errors and show user-friendly Thai messages via MudSnackbar
- **Code reuse**: Existing POSPOS import dialog unchanged, existing feed history logic extracted to component

---

## Validation Checklist
*GATE: Verify before merging to main*

- [ ] DELETE endpoint contract implemented per `contracts/delete-invoice-endpoint.md`
- [ ] InvoiceGroupDto record matches specification (5 fields)
- [ ] All 6 quickstart.md scenarios pass manual validation
- [ ] Tab switching works smoothly (<500ms)
- [ ] Invoice deletion removes all feed items atomically
- [ ] Both tabs refresh automatically after import/delete
- [ ] Thai UI text used throughout (labels, buttons, messages)
- [ ] No regressions to existing feed history functionality
- [ ] Performance goals met (documented in validation results)
- [ ] CHANGELOG.md updated with Feature 014 entry

---

## Task Generation Rules Applied

1. **From Contracts** ✅
   - `delete-invoice-endpoint.md` → T003 (repository), T004 (endpoint), T009 (service)

2. **From Plan** ✅
   - 8 files to create/modify → T003-T012 (implementation tasks)
   - No new entities → No data model tasks

3. **From Quickstart** ✅
   - 6 validation scenarios → T014 (manual validation execution)
   - Performance expectations → T013 (performance check)

4. **Ordering** ✅
   - Setup (skipped - no new dependencies)
   - Validation prep (T001-T002)
   - Backend (T003-T004)
   - Shared (T005)
   - Frontend (T006-T012)
   - Polish (T013-T015)

5. **Parallel Marking** ✅
   - Different files + no dependencies = [P]
   - Same file or data dependency = no [P]
   - Examples: T003 [P] T005, T006 [P] T007 [P] T008, T013 [P] T014 [P] T015

---

**Total Tasks**: 15  
**Estimated Implementation Time**: 6-8 hours (excluding validation)  
**Ready for Phase 4 execution**: ✅
