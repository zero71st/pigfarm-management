# Tasks: Thai Language UI Conversion

**Input**: Design documents from `/specs/013-change-ui-to/`
**Prerequisites**: plan.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

## Execution Flow
```
1. Load plan.md from feature directory ✅
   → Tech stack: C# .NET 8, Blazor WebAssembly, MudBlazor 7.x
   → Structure: Web app (frontend + backend)
   → No automated tests per user requirement
2. Load design documents:
   → data-model.md: Font + culture configuration (no DB entities)
   → contracts/: No API changes (UI-only)
   → research.md: Prompt font, hardcoded translation strategy
   → quickstart.md: 6 manual validation scenarios
3. Generate tasks by category:
   → Setup: Font import, culture configuration
   → Translation: Feature-by-feature UI text replacement
   → Validation: Manual testing via quickstart.md
4. Apply task rules:
   → Different feature areas = mark [P] for parallel
   → Same file = sequential (no [P])
   → No TDD (no automated tests per user request)
5. Number tasks sequentially (T001, T002...)
6. Create parallel execution groups
7. Validate completeness:
   → All features covered? ✅ (Customers, PigPens, Feeds, Shared)
   → Font configured? ✅
   → Culture configured? ✅
   → Validation plan? ✅ (quickstart.md)
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files/features, no dependencies)
- Include exact file paths in descriptions

## Path Conventions
- **Client**: `src/client/PigFarmManagement.Client/`
- **Shared**: `src/shared/PigFarmManagement.Shared/`
- **Server**: `src/server/PigFarmManagement.Server/` (minimal changes)

---

## Phase 3.1: Font & Culture Setup

- [x] **T001** Import Prompt font from Google Fonts in `src/client/PigFarmManagement.Client/wwwroot/index.html`
  - Add `<link>` tags in `<head>` section for Prompt font (weights: 300, 400, 500, 600, 700)
  - Verify font loads correctly in browser DevTools Network tab

- [x] **T002** Add global CSS font override in `src/client/PigFarmManagement.Client/wwwroot/css/app.css`
  - Set `font-family: "Prompt", "Leelawadee UI", Tahoma, sans-serif;` for body and .mud-typography
  - Test font rendering on sample page

- [x] **T003** Configure Thai culture (th-TH) in `src/client/PigFarmManagement.Client/Program.cs`
  - Set `CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("th-TH")`
  - Set `CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("th-TH")`
  - Verify date/number/currency formatting uses Thai locale

---

## Phase 3.2: Customer Feature Translation

- [x] **T004** [P] Translate Customer pages in `src/client/Features/Customers/Pages/`
  - `CustomersPage.razor`: Page title, table headers, buttons ("เพิ่มลูกค้า", "แก้ไข", "ลบ")
  - All dialog titles and confirmation messages to Thai
  - Search placeholder text to Thai

- [x] **T005** [P] Translate Customer components in `src/client/Features/Customers/Components/`
  - `ImportCandidatesDialog.razor`: Dialog title, buttons, labels, status messages
  - `CustomerFormDialog.razor` (if exists): Form field labels to Thai
  - All MudBlazor component properties (Label, Title, Text) to Thai

- [x] **T006** [P] Translate Customer DTOs in `src/shared/DTOs/`
  - `CustomerCreateDto.cs`, `CustomerUpdateDto.cs`: DataAnnotations ErrorMessage to Thai
  - Example: `[Required(ErrorMessage = "กรุณาระบุชื่อลูกค้า")]`
  - `[StringLength(100, ErrorMessage = "ชื่อต้องมีความยาวไม่เกิน 100 ตัวอักษร")]`

- [x] **T007** [P] Translate Customer services in `src/client/Features/Customers/Services/`
  - `CustomerService.cs`: User-facing exception messages, Snackbar notifications to Thai
  - Success messages: "บันทึกลูกค้าสำเร็จ", "ลบลูกค้าสำเร็จ"
  - Error messages: "ไม่สามารถบันทึกลูกค้าได้", etc.

---

## Phase 3.3: Pig Pen Feature Translation

- [x] **T008** [P] Translate Pig Pen pages in `src/client/Features/PigPens/Pages/`
  - `PigPensPage.razor` (or equivalent): Page title, table headers, action buttons
  - Date displays to use ISO format (yyyy-MM-dd) - verify culture setting applies
  - Number displays with comma separators

- [x] **T009** [P] Translate Pig Pen components in `src/client/Features/PigPens/Components/`
  - `PigPenFormDialog.razor` (if exists): Form labels to Thai
  - `PigPenPosImportDialog.razor`: Import instructions, progress messages to Thai
  - All confirmation dialogs to Thai

- [x] **T010** [P] Translate Pig Pen DTOs in `src/shared/DTOs/`
  - Pig pen-related DTOs: DataAnnotations ErrorMessage to Thai
  - Validation messages for required fields, ranges, formats

- [x] **T011** [P] Translate Pig Pen services in `src/client/Features/PigPens/Services/`
  - User-facing messages and notifications to Thai
  - Import success/error messages to Thai

---

## Phase 3.4: Feed Feature Translation

- [x] **T012** [P] Translate Feed pages in `src/client/Features/Feeds/Pages/`
  - Feed management page: Title, headers, buttons to Thai
  - Formula creation/edit page: All labels and instructions to Thai
  - Status labels: "ใช้งาน" (Active), "ไม่ใช้งาน" (Inactive)

- [x] **T013** [P] Translate Feed components in `src/client/Features/Feeds/Components/`
  - `FeedImportDialog.razor`: Import flow messages, file upload labels to Thai
  - `ProductSelectionDialog.razor`: Search placeholder, product column headers to Thai
  - All feed-related dialogs and forms to Thai

- [x] **T014** [P] Translate Feed DTOs in `src/shared/DTOs/`
  - Feed formula DTOs: Validation messages to Thai
  - Quantity/weight validation error messages
  - Required field messages

- [x] **T015** [P] Translate Feed services in `src/client/Features/Feeds/Services/`
  - Feed service notifications to Thai
  - Import/export status messages to Thai
  - Error handling messages to Thai

---

## Phase 3.5: Shared Components Translation

- [x] **T016** [P] Translate navigation in `src/client/Shared/`
  - `NavMenu.razor`: Menu item labels to Thai
  - "ลูกค้า" (Customers), "คอกสุกร" (Pig Pens), "อาหาร" (Feeds)
  - Dashboard/Home menu items to Thai

- [x] **T017** [P] Translate layout components in `src/client/Shared/`
  - `MainLayout.razor`: Header, footer text to Thai (if any)
  - User profile dropdown labels (if applicable)
  - Logout/login labels to Thai

- [x] **T018** [P] Translate shared dialogs and components in `src/client/Shared/Components/`
  - Common confirmation dialogs: "คุณแน่ใจหรือไม่?", "ยืนยัน", "ยกเลิก"
  - Loading indicators: "กำลังโหลด..."
  - Empty state messages: "ไม่มีข้อมูล"

---

## Phase 3.6: Currency Formatting

- [x] **T019** Add currency formatting with Baht symbol (฿) in all relevant components
  - Search for price/cost displays across all features
  - Apply `.ToString("C", new CultureInfo("th-TH"))` format
  - Verify displays as ฿1,234.56 (prefix, 2 decimals, comma separator)
  - Files affected: Currency already formatted with ฿ symbol using N2 format throughout the application

---

## Phase 3.7: Manual Validation (No Automated Tests)

- [ ] **T020** Execute Scenario 1 from `quickstart.md`: Customer Management Pages
  - Verify all customer page text in Thai
  - Check table headers, buttons, form labels
  - Test validation messages appear in Thai
  - Document any issues in validation log

- [ ] **T021** Execute Scenario 2 from `quickstart.md`: Pig Pen with Date/Number Formatting
  - Verify date displays use ISO format (yyyy-MM-dd)
  - Check number formatting (Arabic numerals, comma separators)
  - Test date picker UI in Thai
  - Document formatting correctness

- [ ] **T022** Execute Scenario 3 from `quickstart.md`: Feed Management Dialogs
  - Verify all feed dialog text in Thai
  - Check import/export flow messages
  - Test product selection dialog labels
  - Document dialog completeness

- [ ] **T023** Execute Scenario 4 from `quickstart.md`: Currency Display with Baht Symbol
  - Verify all currency displays show ฿ prefix
  - Check format: ฿1,234.56 (comma separator, 2 decimals)
  - Test small, medium, large amounts
  - Document currency formatting correctness

- [ ] **T024** Execute Scenario 5 from `quickstart.md`: Layout Overflow Check
  - Test desktop view (1920x1080): No text overflow
  - Test tablet view (768x1024): Responsive behavior correct
  - Test mobile view (375x667): Dialogs fit viewport
  - Document any layout issues

- [ ] **T025** Execute Scenario 6 from `quickstart.md`: Technical Text Remains English
  - Verify browser console logs in English
  - Check API error responses in English
  - Confirm user-facing errors in Thai, technical errors in English
  - Document separation correctness

- [ ] **T026** Complete full validation checklist from `quickstart.md`
  - Check all pages listed in checklist (customer, pig pen, feed, dashboard)
  - Verify all UI element categories (headers, buttons, labels, messages)
  - Confirm all formatting rules (dates, numbers, currency)
  - Mark success criteria as met or document blockers

---

## Dependencies

```
Setup Phase (Sequential):
  T001 (Font import) → T002 (CSS override) → T003 (Culture config)
  ↓
  All translation tasks can proceed

Translation Phase (Parallel Groups):
  Group 1 [P]: T004, T005, T006, T007 (Customer feature - independent)
  Group 2 [P]: T008, T009, T010, T011 (Pig Pen feature - independent)
  Group 3 [P]: T012, T013, T014, T015 (Feed feature - independent)
  Group 4 [P]: T016, T017, T018 (Shared components - independent)
  Group 5: T019 (Currency formatting - touches multiple features)
  ↓
  All translation must complete before validation

Validation Phase (Sequential):
  T020 → T021 → T022 → T023 → T024 → T025 → T026
```

---

## Parallel Execution Examples

### Setup Phase (Sequential - Must Run in Order)
```bash
# T001: Font import
# Edit: src/client/PigFarmManagement.Client/wwwroot/index.html

# T002: CSS override (depends on T001)
# Edit: src/client/PigFarmManagement.Client/wwwroot/css/app.css

# T003: Culture config (depends on T001-T002 for testing)
# Edit: src/client/PigFarmManagement.Client/Program.cs
```

### Translation Phase (Parallel Groups - Can Run Simultaneously)
```bash
# Group 1: Customer Feature [P]
Task T004: src/client/Features/Customers/Pages/CustomersPage.razor
Task T005: src/client/Features/Customers/Components/*.razor
Task T006: src/shared/DTOs/Customer*.cs
Task T007: src/client/Features/Customers/Services/CustomerService.cs

# Group 2: Pig Pen Feature [P] (parallel with Group 1)
Task T008: src/client/Features/PigPens/Pages/*.razor
Task T009: src/client/Features/PigPens/Components/*.razor
Task T010: src/shared/DTOs/PigPen*.cs
Task T011: src/client/Features/PigPens/Services/*.cs

# Group 3: Feed Feature [P] (parallel with Groups 1-2)
Task T012: src/client/Features/Feeds/Pages/*.razor
Task T013: src/client/Features/Feeds/Components/*.razor
Task T014: src/shared/DTOs/Feed*.cs
Task T015: src/client/Features/Feeds/Services/*.cs

# Group 4: Shared Components [P] (parallel with Groups 1-3)
Task T016: src/client/Shared/NavMenu.razor
Task T017: src/client/Shared/MainLayout.razor
Task T018: src/client/Shared/Components/*.razor
```

### Currency Formatting (After Parallel Translation)
```bash
# T019: Search and update currency displays
# Files: Multiple across features (depends on T004-T018 complete)
```

### Validation Phase (Sequential Manual Testing)
```bash
# Execute in order (each scenario builds on previous)
T020 → Customer pages validation
T021 → Date/number formatting validation
T022 → Feed dialogs validation
T023 → Currency display validation
T024 → Responsive layout validation
T025 → Technical text boundaries validation
T026 → Complete checklist review
```

---

## Notes

- **No Automated Tests**: Per user requirement "without test plan, test docs"
- **Manual Validation Only**: Tasks T020-T026 are human-executed scenarios from quickstart.md
- **Parallel Translation**: Tasks T004-T018 can be executed simultaneously (different files/features)
- **Font First**: T001-T003 must complete before testing any translations
- **Commit Strategy**: Commit after each parallel group completes (e.g., all Customer feature tasks done → commit)
- **Translation Accuracy**: Assumes Thai translations provided by bilingual developer or reviewed by native speaker
- **Technical Text**: Do NOT translate: backend API errors, logging, code comments, configuration files
- **Estimated Time**: ~8-12 hours total (2-3h setup, 4-6h translation, 2-3h validation)

---

## Task Checklist Summary

**Setup**: 3 tasks (T001-T003) - Sequential  
**Translation**: 16 tasks (T004-T019) - Mostly parallel  
**Validation**: 7 tasks (T020-T026) - Sequential manual testing  
**Total**: 26 tasks

**Critical Path**:
```
T001 → T002 → T003 → [Parallel Translation T004-T018] → T019 → T020-T026
```

**Success Criteria**:
- ✅ All user-facing text in Thai (buttons, labels, messages, dialogs)
- ✅ Dates in ISO format (yyyy-MM-dd)
- ✅ Numbers use Arabic numerals with comma separators
- ✅ Currency displays ฿ prefix with correct format
- ✅ No layout overflow on desktop/tablet/mobile
- ✅ Technical text (logs, API errors) remains English
- ✅ All 6 quickstart scenarios pass validation
- ✅ Prompt font loads and renders correctly
