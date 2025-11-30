# Quickstart: Thai Language UI Conversion

**Date**: 2025-11-30 | **Feature**: 013-change-ui-to | **Branch**: `013-change-ui-to`

## Purpose
This quickstart provides step-by-step manual validation scenarios to verify Thai language UI conversion is complete and correct across all user-facing components.

---

## Prerequisites

### Environment Setup
1. **Local Development**:
   ```powershell
   # Start server
   cd src/server/PigFarmManagement.Server
   dotnet run --urls http://localhost:5000
   
   # Start client (separate terminal)
   cd src/client/PigFarmManagement.Client
   dotnet run --urls http://localhost:7000
   ```

2. **Browser**: Chrome 90+, Firefox 88+, Edge 90+, or Safari 14+ (Google Fonts support required)

3. **Font**: Verify "Prompt" font loads from Google Fonts (check Network tab in DevTools)

4. **Test Data**: At least one customer, pig pen, and feed item in database

---

## Validation Scenarios

### Scenario 1: Customer Management Pages

**Objective**: Verify all customer-related UI displays Thai text correctly

**Steps**:
1. Navigate to `http://localhost:7000/customers`
2. **Check page header**: Should display "ลูกค้า" or "จัดการลูกค้า" (not "Customers" or "Manage Customers")
3. **Check table headers**:
   - Column names in Thai (e.g., "ชื่อลูกค้า", "ชื่อผู้ติดต่อ", "เบอร์โทร")
   - Action column shows Thai labels (e.g., "แก้ไข", "ลบ")
4. **Click "Create New" button**: 
   - Button text should be Thai (e.g., "สร้างใหม่", "เพิ่มลูกค้า")
5. **In create dialog**:
   - Dialog title in Thai (e.g., "เพิ่มลูกค้าใหม่")
   - All field labels in Thai ("ชื่อลูกค้า", "ชื่อผู้ติดต่อ", "เบอร์โทร", etc.)
   - Action buttons in Thai ("บันทึก", "ยกเลิก")
6. **Test validation**:
   - Leave required field empty, click save
   - Error message should be Thai (e.g., "กรุณาระบุชื่อลูกค้า")
7. **Fill valid data and save**:
   - Success notification should be Thai (e.g., "บันทึกลูกค้าสำเร็จ")
8. **Edit existing customer**:
   - Edit dialog title/labels/buttons all in Thai
9. **Delete customer**:
   - Confirmation dialog text in Thai (e.g., "คุณแน่ใจหรือไม่ที่จะลบลูกค้านี้?")
   - Confirm/cancel buttons in Thai

**Expected Results**:
- ✅ All labels, buttons, headers in Thai
- ✅ Validation messages in Thai
- ✅ Notifications/alerts in Thai
- ✅ No English text visible except in browser console logs (developer tools)

---

### Scenario 2: Pig Pen Management with Date/Number Formatting

**Objective**: Verify pig pen pages display Thai text AND correct Thai date/number formats

**Steps**:
1. Navigate to pig pen management page
2. **Check page layout**: All UI text in Thai
3. **Check date displays**:
   - Date created/updated should use ISO format: `2025-11-30` (YYYY-MM-DD)
   - Date picker labels in Thai when editing
4. **Check number displays**:
   - Pig count displayed with thousands separator if applicable (e.g., `1,234`)
   - Numerals should be Arabic (0-9), NOT Thai numerals (๐-๙)
5. **Create new pig pen**:
   - All form fields labeled in Thai
   - Date picker shows Thai month names if applicable (depends on MudBlazor culture setting)
   - Number input fields accept Thai-formatted numbers
6. **Validate date input**:
   - Enter invalid date → error message in Thai
7. **Save and verify**:
   - Success message in Thai
   - Saved record displays dates in ISO format

**Expected Results**:
- ✅ Dates in ISO format (yyyy-MM-dd)
- ✅ Numbers use Arabic numerals with comma separators
- ✅ Date picker UI elements in Thai
- ✅ All text content in Thai

---

### Scenario 3: Feed Management Dialogs

**Objective**: Verify feed-related dialogs show Thai validation messages and labels

**Steps**:
1. Navigate to feed management page
2. **Open product selection dialog**:
   - Dialog title in Thai
   - Search placeholder text in Thai
   - Product name column header in Thai
   - Select/cancel buttons in Thai
3. **Open feed import dialog**:
   - Import instructions/labels in Thai
   - File upload button text in Thai
   - Progress messages in Thai
4. **Test formula creation**:
   - Form field labels in Thai
   - Quantity/weight input labels in Thai
   - Validation for required fields shows Thai messages
5. **Check feed history table**:
   - Column headers in Thai
   - Status labels in Thai (e.g., "ใช้งาน", "ไม่ใช้งาน" for active/inactive)

**Expected Results**:
- ✅ All dialog content in Thai
- ✅ Import/export flow messages in Thai
- ✅ Form validation in Thai
- ✅ Table data labels in Thai

---

### Scenario 4: Currency Display with Baht Symbol

**Objective**: Verify all currency values display with Thai Baht symbol (฿) in correct format

**Steps**:
1. Navigate to pages displaying prices/costs (feeds, invoices, transactions)
2. **Check currency format**:
   - Symbol position: Prefix (e.g., `฿1,234.56`, NOT `1,234.56 THB` or `1234.56฿`)
   - Decimal separator: `.` (period)
   - Thousands separator: `,` (comma)
   - Decimal places: 2 digits (e.g., `฿1,000.00`, not `฿1000` or `฿1,000.0`)
3. **Check different value ranges**:
   - Small amounts: `฿0.50`, `฿10.00`
   - Medium amounts: `฿1,234.56`
   - Large amounts: `฿12,345,678.90`
4. **Input currency values**:
   - Numeric input fields should accept Thai-formatted numbers
   - Auto-format on blur if applicable

**Expected Results**:
- ✅ All currency values show ฿ symbol prefix
- ✅ Correct thousands/decimal separators
- ✅ Consistent 2 decimal places
- ✅ No English currency labels (no "Baht" or "THB" text)

---

### Scenario 5: Layout Overflow Check (Responsive)

**Objective**: Verify Thai text does not break UI layouts on different screen sizes

**Steps**:
1. **Desktop view (1920x1080)**:
   - Open all major pages (customers, pig pens, feeds)
   - Check for text overflow in:
     - Dialog titles
     - Button labels (especially multi-word buttons)
     - Table column headers
     - Form field labels
   - Verify no horizontal scrolling in dialogs
   - Verify table columns auto-size correctly

2. **Tablet view (768x1024)**:
   - Resize browser window or use DevTools responsive mode
   - Check same elements as desktop
   - Verify MudBlazor responsive breakpoints work with Thai text

3. **Mobile view (375x667 - iPhone SE)**:
   - Check same elements
   - Verify hamburger menu labels in Thai
   - Verify dialog buttons stack vertically if needed (not cut off)

**Expected Results**:
- ✅ No text overflow/truncation on any screen size
- ✅ Buttons auto-size to fit Thai text
- ✅ Table headers wrap or abbreviate gracefully
- ✅ Dialogs remain within viewport
- ✅ No horizontal scroll bars on dialogs/forms

---

### Scenario 6: Technical Text Remains English

**Objective**: Verify technical/developer-facing text remains in English per user requirement

**Steps**:
1. **Open browser developer console** (F12)
2. **Trigger validation error** (e.g., submit empty form):
   - User-facing message in UI: Thai
   - Console log message: English (if any)
3. **Check API error responses**:
   - Use Network tab in DevTools
   - Trigger server error (e.g., delete customer with pig pens)
   - Response JSON should have English error messages
   - UI displays Thai user-friendly message
4. **Check application logs** (if accessible):
   - Server logs should be English
   - Exception messages should be English
5. **Check code comments** (developer view):
   - Code comments remain English (not user-facing)

**Expected Results**:
- ✅ User-facing validation: Thai
- ✅ Console logs: English
- ✅ API error responses: English
- ✅ Server logs: English
- ✅ Code comments: English
- ✅ Clear separation between user-facing (Thai) and technical (English) text

---

## Validation Checklist

Use this checklist during manual testing:

### Pages
- [ ] Customer list page
- [ ] Customer create dialog
- [ ] Customer edit dialog
- [ ] Customer delete confirmation
- [ ] Pig pen list page
- [ ] Pig pen create/edit forms
- [ ] Feed management page
- [ ] Feed import dialog
- [ ] Product selection dialog
- [ ] Feed formula creation
- [ ] Dashboard (if applicable)
- [ ] Login page (if applicable)

### UI Elements
- [ ] Page headers/titles
- [ ] Navigation menu items
- [ ] Button labels (primary, secondary, cancel, delete)
- [ ] Form field labels
- [ ] Table column headers
- [ ] Search/filter placeholders
- [ ] Pagination controls
- [ ] Tab labels
- [ ] Breadcrumbs
- [ ] Tooltips (if any)

### Messages
- [ ] Success notifications (Snackbar)
- [ ] Error notifications
- [ ] Warning messages
- [ ] Confirmation dialogs
- [ ] Validation error messages (client-side)
- [ ] Empty state messages (e.g., "ไม่มีข้อมูล")
- [ ] Loading indicators (e.g., "กำลังโหลด...")

### Formatting
- [ ] Dates use ISO format (yyyy-MM-dd)
- [ ] Numbers use Arabic numerals (0-9)
- [ ] Thousands separator is comma (,)
- [ ] Currency shows ฿ prefix
- [ ] Currency has 2 decimal places
- [ ] No Thai numerals (๐-๙) used

### Responsive Layout
- [ ] Desktop (1920x1080) - no overflow
- [ ] Tablet (768x1024) - no overflow
- [ ] Mobile (375x667) - no overflow
- [ ] Dialogs fit viewport on all sizes
- [ ] Buttons stack/resize appropriately

### Technical Boundaries
- [ ] Browser console logs in English
- [ ] API error responses in English
- [ ] Developer comments in English
- [ ] Configuration files in English

---

## Common Issues & Fixes

### Issue 1: Text Overflow in Buttons
**Symptom**: Thai text cut off or wrapping awkwardly in buttons  
**Fix**: Remove fixed width constraints, use MudBlazor auto-sizing:
```razor
<!-- Before -->
<MudButton Style="width: 100px;">บันทึกและปิด</MudButton>

<!-- After -->
<MudButton Style="min-width: 120px;">บันทึกและปิด</MudButton>
<!-- Or remove Style entirely for auto-sizing -->
```

### Issue 2: Date Picker Shows English Month Names
**Symptom**: Date picker displays "January" instead of Thai equivalent  
**Fix**: Ensure global culture set in Program.cs:
```csharp
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("th-TH");
```

### Issue 3: Currency Missing ฿ Symbol
**Symptom**: Numbers display as `1234.56` instead of `฿1,234.56`  
**Fix**: Use currency format string:
```razor
@model.Price.ToString("C", new CultureInfo("th-TH"))
```

### Issue 4: English Text Still Visible
**Symptom**: Some labels/buttons remain in English  
**Fix**: 
1. Search codebase for English strings: `grep -r "Customer" src/client/`
2. Replace with Thai equivalent in Razor component
3. Check DataAnnotations in DTOs for English error messages

### Issue 5: Validation Messages in English
**Symptom**: Client-side validation shows English errors  
**Fix**: Update DataAnnotations in DTO classes:
```csharp
[Required(ErrorMessage = "กรุณาระบุชื่อ")]
```

### Issue 6: Thai Text Not Rendering with Prompt Font
**Symptom**: Thai text displays with system fonts instead of Prompt  
**Fix**: 
1. Verify Google Fonts link in `wwwroot/index.html` `<head>` section
2. Check Network tab in DevTools for successful font download
3. Ensure global CSS override is applied: `font-family: "Prompt", ...`
4. Clear browser cache and hard reload (Ctrl+F5)

---

## Success Criteria

Feature is ready for deployment when:
1. ✅ All 6 validation scenarios pass
2. ✅ All items in Validation Checklist checked
3. ✅ No English text visible in user-facing UI
4. ✅ All dates/numbers/currency formatted correctly
5. ✅ No layout overflow on desktop/tablet/mobile
6. ✅ Technical text (logs, API errors) remains English
7. ✅ User can complete full workflow (create → edit → delete) in Thai across all features

---

## Next Steps

After quickstart validation:
1. If issues found → Document in GitHub issue → Fix → Re-validate
2. If all scenarios pass → Update `.github/copilot-instructions.md` with feature summary
3. If all scenarios pass → Ready for `/tasks` command to generate task breakdown
4. Deploy to staging environment for final user acceptance testing

---

## Notes

- **No automated tests**: Per user requirement "generate plan without test plan, test docs"
- **Manual validation only**: This quickstart is the primary testing document
- **Translation accuracy**: Assumes Thai translations provided by user or bilingual developer
- **Font support**: Uses "Prompt" font from Google Fonts for consistent, modern Thai typography
