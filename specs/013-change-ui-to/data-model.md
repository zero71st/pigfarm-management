# Data Model: Thai Language UI Conversion

**Date**: 2025-11-30 | **Feature**: 013-change-ui-to

## Overview
This feature has **NO database schema changes** - it is a pure UI presentation layer modification. This document describes the configuration and cultural data setup required for Thai language display.

---

## Cultural Configuration

### CultureInfo Setup (Global)

**Location**: `src/client/PigFarmManagement.Client/Program.cs`

**Configuration**:
```csharp
// Set default culture for the application
var thaiCulture = new CultureInfo("th-TH");
CultureInfo.DefaultThreadCurrentCulture = thaiCulture;
CultureInfo.DefaultThreadCurrentUICulture = thaiCulture;
```

**Impact**:
- **Date Formatting**: Automatic ISO format (yyyy-MM-dd) via th-TH culture
- **Number Formatting**: Thousands separator (,) and decimal point (.)
- **Currency Formatting**: Thai Baht symbol (฿) prefix with format ฿1,234.56
- **MudBlazor Components**: Date pickers, numeric fields inherit culture automatically

### Font Configuration

**Location**: `src/client/PigFarmManagement.Client/wwwroot/index.html` and global CSS

**Google Fonts Import** (in `<head>`):
```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Prompt:wght@300;400;500;600;700&display=swap" rel="stylesheet">
```

**Global CSS Override** (in `wwwroot/css/app.css` or component styles):
```css
body, .mud-typography {
    font-family: "Prompt", "Leelawadee UI", Tahoma, sans-serif;
}
```

**Impact**:
- **Primary Font**: "Prompt" (modern, readable Thai font)
- **Fallback**: System fonts for Thai (Leelawadee UI, Tahoma)
- **Weight Support**: 300-700 for proper heading/body hierarchy

---

## Translation Mapping Strategy

### Approach: Hardcoded Strings (No Resource Files)

**Rationale**: 
- User requirement: "don't make feature to switch language just hard code the ui only Thai language"
- Simplicity principle: No i18n framework overhead
- Single language target: No need for resource file abstraction

**Implementation Pattern**:

| Source Type | Location | Translation Method |
|-------------|----------|-------------------|
| Razor Markup | `*.razor` files | Direct string replacement in markup |
| Component Labels | `MudTextField.Label`, `MudButton` content | Thai string in property/content |
| Validation Messages | DataAnnotations attributes | `ErrorMessage = "Thai text"` |
| Snackbar Notifications | `_snackbar.Add()` calls | Thai string argument |
| Dialog Titles | `<TitleContent>` blocks | Thai text in MudText |
| Table Headers | `PropertyColumn.Title` | Thai string property |

**Example Mapping**:
```
English             → Thai
--------------------|------------------
"Save"              → "บันทึก"
"Cancel"            → "ยกเลิก"
"Delete"            → "ลบ"
"Customer Name"     → "ชื่อลูกค้า"
"Are you sure?"     → "คุณแน่ใจหรือไม่?"
"Required field"    → "กรุณาระบุข้อมูล"
```

---

## Component-Level Data Flow

### Before Translation (English)
```
User View (Browser)
    ↓ (renders)
Blazor Component
    - <MudButton>Save</MudButton>
    - Label="Customer Name"
    - ErrorMessage="Required"
    ↓ (binds to)
DTO Model
    - [Required(ErrorMessage = "Required")]
```

### After Translation (Thai)
```
User View (Browser)
    ↓ (renders)
Blazor Component
    - <MudButton>บันทึก</MudButton>
    - Label="ชื่อลูกค้า"
    - ErrorMessage="กรุณาระบุข้อมูล"
    ↓ (binds to)
DTO Model
    - [Required(ErrorMessage = "กรุณาระบุข้อมูล")]
```

**Key Point**: Data model entities (CustomerEntity, PigPenEntity, etc.) are **unchanged**. Only presentation strings modified.

---

## Files Modified (No New Files)

### Category 1: Razor Components (~30 files)
**Location**: `src/client/Features/**/Pages/*.razor`, `src/client/Features/**/Components/*.razor`

**Modified Elements**:
- Button text: `<MudButton>Thai text</MudButton>`
- Text field labels: `Label="Thai text"`
- Dialog titles: `<MudText>Thai text</MudText>`
- Table column headers: `Title="Thai text"`
- Alert messages: `_snackbar.Add("Thai text")`

### Category 2: DTOs with Validation (~15 files)
**Location**: `src/shared/DTOs/*.cs`

**Modified Elements**:
- DataAnnotations error messages:
  ```csharp
  [Required(ErrorMessage = "Thai error message")]
  [StringLength(100, ErrorMessage = "Thai length message")]
  [Range(1, 1000, ErrorMessage = "Thai range message")]
  ```

### Category 3: Client Services (~20 files)
**Location**: `src/client/Features/**/Services/*.cs`

**Modified Elements**:
- User-facing exception messages
- Confirmation dialog prompts
- Success/error notification text

**Excluded from Translation**:
- Backend API responses (technical, keep English)
- Logging messages (developer-facing, keep English)
- Configuration files, code comments
- System-generated documents (PDFs, exports, emails)

---

## State Management

**No State Changes**: Translation is static at compile-time (hardcoded strings), not runtime switching.

**No Localization State**:
- No language selection dropdown
- No user preference storage
- No cookie/local storage for language
- No API calls to fetch translations

**Cultural State** (Read-Only):
- `CultureInfo.CurrentCulture` set to `th-TH` at application startup
- Persists for entire session (no language switching)

---

## Validation Rules (Unchanged)

All validation logic remains identical to English version:
- Field length constraints (e.g., 100 characters)
- Required field checks
- Range validations
- Format validations (email, phone)

**Only Change**: Error message text from English to Thai.

**Example**:
```csharp
// Before
[StringLength(100, ErrorMessage = "Name must be less than 100 characters")]

// After
[StringLength(100, ErrorMessage = "ชื่อต้องมีความยาวไม่เกิน 100 ตัวอักษร")]

// Validation logic: UNCHANGED (still checks 100 character limit)
```

---

## Data Persistence (None)

**No Database Changes**:
- No new tables
- No new columns
- No migrations
- No schema updates

**No Translation Storage**:
- No resource files (.resx)
- No JSON language files
- No database translation tables
- No API translation endpoints

**Rationale**: All translations embedded directly in Razor components and DTOs at compile-time.

---

## Summary

| Aspect | Status |
|--------|--------|
| New Entities | None |
| Schema Changes | None |
| Configuration Changes | CultureInfo setup in Program.cs |
| Resource Files | None (hardcoded approach) |
| State Management | None (static translation) |
| Validation Logic | Unchanged (only messages translated) |
| Data Flow | Unchanged (presentation layer only) |

This is a **presentation-only feature** with zero impact on data model, business logic, or API contracts.
