# Research: Thai Language UI Conversion

**Date**: 2025-11-30 | **Feature**: 013-change-ui-to

## Research Areas

### 1. Thai Unicode Rendering

**Requirement**: Ensure all Thai text displays correctly across target browsers

**Findings**:
- **Unicode Range**: Thai script uses Unicode block U+0E00 to U+0E7F (128 characters)
- **Browser Support**: Modern browsers (Chrome 90+, Firefox 88+, Edge 90+, Safari 14+) have native Thai rendering support
- **Font Requirements**: 
  - **Primary Font**: "Prompt" (Google Fonts - modern, readable Thai font)
  - System font fallback: Leelawadee UI (Windows), Thonburi (macOS), Tahoma
  - Web-safe fallback chain: `font-family: "Prompt", "Leelawadee UI", Tahoma, sans-serif;`
  - Requires Google Fonts import in index.html or App.razor
- **Character Width**: Thai characters are typically narrower than Latin characters, but some vowel/tone combinations can extend character width
- **Layout Impact**: Minimal overflow risk compared to English (Thai tends to be more compact)

**Decision**: Import "Prompt" font from Google Fonts as primary Thai font. Add CSS font-family override in global styles.

---

### 2. MudBlazor Localization Patterns

**Requirement**: Override MudBlazor built-in English text (e.g., date picker labels, validation messages)

**Findings**:
- **MudBlazor 7.x Localization**: Supports `IMudLocalizer` interface for global localization
- **Date Picker Component**: 
  - Properties: `DateFormat`, `FirstDayOfWeek`, month/day names customizable via `Culture` parameter
  - Default culture from `CultureInfo.CurrentCulture`
- **Validation Messages**:
  - MudBlazor uses DataAnnotations attribute messages (e.g., `[Required(ErrorMessage = "...")]`)
  - Can override via custom `ErrorMessage` parameters in Thai
- **Component Labels**: No global translation system - must set per-component properties (e.g., `MudButton.Text`, `MudTextField.Label`)

**Strategy**:
1. **Global Culture**: Set `CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("th-TH");` in `Program.cs` for date/number formatting
2. **Component Text**: Hardcode Thai strings in Razor markup: `<MudButton>บันทึก</MudButton>`
3. **Validation**: Update DataAnnotations messages: `[Required(ErrorMessage = "กรุณาระบุชื่อ")]`
4. **Date Pickers**: Use `Culture="@(new CultureInfo("th-TH"))"` parameter for Thai month names

**No Resource Files Needed**: Direct string replacement in Razor files per user requirement (no language switcher).

---

### 3. Blazor Hardcoded Translation Strategy

**Requirement**: Replace English UI text with Thai without i18n framework

**Pattern**:

**Before (English)**:
```razor
<MudButton Color="Color.Primary" Variant="Variant.Filled">
    Save Customer
</MudButton>
<MudTextField @bind-Value="model.Name" Label="Customer Name" />
```

**After (Thai)**:
```razor
<MudButton Color="Color.Primary" Variant="Variant.Filled">
    บันทึกลูกค้า
</MudButton>
<MudTextField @bind-Value="model.Name" Label="ชื่อลูกค้า" />
```

**C# Code-Behind (Notifications/Alerts)**:
```csharp
// Before
_snackbar.Add("Customer deleted successfully", Severity.Success);

// After
_snackbar.Add("ลบลูกค้าสำเร็จ", Severity.Success);
```

**Validation Messages**:
```csharp
// Before
[Required(ErrorMessage = "Name is required")]
[StringLength(100, ErrorMessage = "Name must be less than 100 characters")]

// After
[Required(ErrorMessage = "กรุณาระบุชื่อ")]
[StringLength(100, ErrorMessage = "ชื่อต้องมีความยาวไม่เกิน 100 ตัวอักษร")]
```

**File Scope**: 
- Razor components: `src/client/Features/**/*.razor`
- C# models with DataAnnotations: `src/shared/DTOs/*.cs`
- Service layer user-facing messages: `src/client/Features/**/Services/*.cs`

**Excluded**: 
- Backend API error responses (technical, keep English)
- Logging messages (developer-facing, keep English)
- Configuration files, code comments

---

### 4. Number/Currency Formatting

**Requirement**: Display numbers with Thai separators and Baht symbol (฿)

**Findings**:
- **Thai Number Format (th-TH)**:
  - Decimal separator: `.` (same as English)
  - Thousands separator: `,` (same as English)
  - Numerals: Arabic numerals 0-9 (NOT Thai numerals ๐-๙) per user clarification
- **Currency**:
  - Symbol: ฿ (Thai Baht)
  - Position: Prefix (e.g., ฿1,234.56)
  - Format string: `{0:C}` with `CultureInfo("th-TH")` produces "฿1,234.56"

**Implementation**:

**Automatic Formatting (via Culture)**:
```csharp
// In Program.cs
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("th-TH");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("th-TH");
```

**Manual Formatting (for specific values)**:
```razor
@* Currency *@
<MudText>@model.TotalCost.ToString("C", new CultureInfo("th-TH"))</MudText>

@* Number with thousands separator *@
<MudText>@model.Quantity.ToString("N0", new CultureInfo("th-TH"))</MudText>
```

**MudBlazor Numeric Fields**:
```razor
<MudNumericField @bind-Value="model.Price" 
                 Label="ราคา" 
                 Format="N2" 
                 Culture="@(new CultureInfo("th-TH"))" />
```

**Decision**: Set global culture to `th-TH` in `Program.cs` for consistency. No manual formatting needed unless overriding global culture.

---

### 5. Layout Overflow Prevention

**Requirement**: Prevent Thai text from breaking UI layouts designed for English

**Findings**:
- **Thai Text Length**: Generally equal to or shorter than English equivalents
  - Example: "Delete Customer" (15 chars) → "ลบลูกค้า" (7 chars)
  - Exception: Technical terms transliterated from English may be longer
- **MudBlazor Responsive Behavior**: 
  - `MudButton` auto-expands horizontally based on content
  - `MudDataGrid` columns auto-size or use fixed widths with ellipsis overflow
  - `MudTextField.Label` has built-in wrapping for long labels
- **Potential Issues**:
  - Fixed-width buttons in dialogs (e.g., "Cancel" vs "ยกเลิก")
  - Table column headers with long Thai labels
  - Dialog titles exceeding max-width

**Mitigation Strategies**:

1. **Buttons**: Use MudBlazor default sizing (no fixed widths)
   ```razor
   <!-- Auto-sizing (recommended) -->
   <MudButton>บันทึกและปิด</MudButton>
   
   <!-- If fixed width needed, increase to accommodate Thai -->
   <MudButton Style="min-width: 120px;">บันทึก</MudButton>
   ```

2. **Table Headers**: Enable text wrapping or use abbreviations
   ```razor
   <MudDataGrid>
       <Columns>
           <PropertyColumn Property="x => x.Name" Title="ชื่อลูกค้า" />
           <!-- If overflow occurs, add HeaderStyle -->
           <PropertyColumn Property="x => x.Description" 
                         Title="รายละเอียด" 
                         HeaderStyle="white-space: normal;" />
       </Columns>
   </MudDataGrid>
   ```

3. **Dialog Titles**: Keep titles concise, use subtitle for additional context
   ```razor
   <MudDialog>
       <TitleContent>
           <MudText Typo="Typo.h6">เพิ่มลูกค้าใหม่</MudText>
       </TitleContent>
   </MudDialog>
   ```

4. **Testing**: Manual visual inspection during quickstart validation (check all dialogs, tables, forms on desktop and mobile viewports)

**Decision**: No preemptive CSS changes. Address overflow reactively during quickstart validation if issues found.

---

## Implementation Approach Summary

1. **Font Setup**: Import "Prompt" font from Google Fonts in `wwwroot/index.html` and add global CSS font-family override
2. **Global Culture Setup**: Add `CultureInfo` configuration in `Program.cs` for automatic date/number/currency formatting
3. **Direct Text Replacement**: Scan all Razor files in `src/client/Features/` and replace English strings with Thai equivalents
4. **Validation Messages**: Update DataAnnotations in DTOs with Thai error messages
5. **MudBlazor Components**: Override component-specific labels, button text, dialog titles with Thai
6. **Manual Testing**: Follow quickstart scenarios to validate text display, layout integrity, and formatting correctness

**New Files**: Global CSS for font override (or add to existing global styles)

**Estimated Scope**: 
- ~30 Razor pages/components in `src/client/Features/`
- ~15 DTO files with validation attributes in `src/shared/DTOs/`
- ~20 service files with user-facing messages in `src/client/Features/*/Services/`

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Thai text overflow in fixed-width components | Low | Medium | Manual testing in quickstart, use auto-sizing |
| Browser font fallback failure | Very Low | Low | System fonts universally supported |
| Date/number formatting inconsistency | Low | Low | Global CultureInfo ensures consistency |
| Translation accuracy issues | Medium | Medium | User review during quickstart validation |
| Missing translation spots | Medium | Medium | Systematic file scan, grep for English patterns |

**Overall Risk**: Low - UI-only change with no data model or API contract modifications.
