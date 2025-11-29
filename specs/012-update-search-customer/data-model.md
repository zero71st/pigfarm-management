# Data Model: POSPOS Import Enhancement (Existing Component Modification)

**Feature**: `012-update-search-customer`  
**Date**: November 29, 2025  
**Status**: Complete (Updated for existing component modification)  
**Purpose**: Document data structures and API modifications for ImportCandidatesDialog enhancement

---

## Overview

This feature enhances the existing import infrastructure by:
- Modifying `CustomerImportEndpoints.GetCandidates()` API to accept source parameter
- Updating `ImportCandidatesDialog.razor` component to disable select-all and call API with source
- No new entities or database migrations required
- Leveraging existing POSPOS member data structures

---

## Existing Components (Modified)

### 1. POSPOS Member Model (from IPosposMemberClient)

**Purpose**: Represents a member from POSPOS system

**Existing fields** (used for import):
```csharp
public class PosposMember
{
    public string Id { get; set; }                    // POSPOS member ID
    public string Code { get; set; }                  // Member code
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Phone { get; set; }                 // Primary phone
    public string PhoneNumber { get; set; }           // Alternative phone field
    public string Email { get; set; }
    public string Address { get; set; }
    public string KeyCardId { get; set; }
    public string Sex { get; set; }
    public string Zipcode { get; set; }
    
    public DateTime CreatedAt { get; set; }           // **USED FOR ORDERING LATEST**
    public DateTime UpdatedAt { get; set; }
}
```

**For this feature**: `CreatedAt` field used to order members and return latest when source=pospos

---

### 2. CandidateMember Model (Client-Side DTO - Existing)

**Purpose**: Blazor component model for displaying import candidates in ImportCandidatesDialog

**Existing definition** (in ImportCandidatesDialog.razor):
```csharp
private class CandidateMember
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName =>
        string.IsNullOrWhiteSpace($"{FirstName} {LastName}".Trim()) 
            ? (string.IsNullOrWhiteSpace(Code) ? Id : Code) 
            : $"{FirstName} {LastName}".Trim();
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string KeyCardId { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string Sex { get; set; } = string.Empty;
    public string Zipcode { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    
    public bool IsSelected { get; set; }              // **USED FOR SELECTION STATE**
}
```

**No changes needed**: Model sufficient for feature; existing IsSelected field handles session-scoped state

---

## API Modifications

### GET /api/customers/import/candidates (ENHANCED)

**Current behavior** (existing endpoint):
- Returns all POSPOS members available for import
- Called by: `ImportCandidatesDialog.razor` → `LoadCandidates()`
- Response: Array of member objects (projected from PosposMember)

**Enhancement**: Add optional `source` query parameter

**Endpoint URL Examples**:
```
GET /api/customers/import/candidates                    # All members (existing)
GET /api/customers/import/candidates?source=all         # All members (explicit)
GET /api/customers/import/candidates?source=pospos      # Latest member only
```

**Query Parameters**:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `source` | string | "all" | Filter context: "pospos" returns 1 latest; "all" returns all members |

**Response Structure** (unchanged):
```json
[
  {
    "id": "pospos-member-123",
    "code": "CUST001",
    "firstName": "John",
    "lastName": "Doe",
    "phone": "+1-555-0123",
    "email": "john@example.com",
    "address": "123 Farm Road",
    "keyCardId": "KC001",
    "externalId": "pospos-member-123",
    "sex": "M",
    "zipcode": "12345",
    "createdAt": "2025-11-25T14:30:00Z"
  }
]
```

**For source=pospos**: Array contains 0-1 items (latest member only)  
**For source=all**: Array contains 0-N items (all available members)

**Response Status Codes**:

| Code | Condition | Body |
|------|-----------|------|
| 200 | Success | Array of members (0 or more items) |
| 400 | Invalid source parameter | `{ "error": "Invalid source. Must be 'pospos' or 'all'." }` |
| 500 | POSPOS API error | `{ "error": "Failed to get import candidates: ..." }` |
| 503 | POSPOS service unavailable | `{ "error": "POSPOS service unavailable. Please try again later." }` |

**Implementation Location**:
- File: `src/server/PigFarmManagement.Server/Features/Customers/CustomerImportEndpoints.cs`
- Method: `GetCandidates()` (static method in CustomerImportEndpoints class)
- Changes:
  1. Add `[FromQuery] string source = "all"` parameter
  2. Add validation: source must be "pospos" or "all"
  3. Filter members by source (if source=pospos, `.OrderByDescending(m => m.CreatedAt).Take(1)`)
  4. Enhanced error handling: Distinguish 503 (service unavailable) from 500 (other errors)

---

## Component Modifications

### ImportCandidatesDialog.razor (Frontend Component)

**Current implementation** (existing):
- Displays list of POSPOS members available for import
- Allows bulk select (via select-all checkbox) and individual selection
- Calls `/api/customers/import/candidates` to fetch members
- Session-scoped selection state via `_candidates` list

**Changes required**:

#### 1. Add source tracking field
```csharp
@code {
    private string _source = "all";  // NEW: Track source context (default=all for backward compat)
    
    // ... rest of component code
}
```

#### 2. Modify LoadCandidates() method
```csharp
private async Task LoadCandidates()
{
    _isLoading = true;
    try
    {
        // MODIFIED: Include source parameter in URL
        var url = $"/api/customers/import/candidates?source={_source}";
        var list = await Http.GetFromJsonAsync<List<CandidateMember>>(url);
        if (list != null)
            _candidates = list;
    }
    catch (HttpRequestException ex)
    {
        // NEW: Distinguish POSPOS service failures
        if (ex.InnerException?.Message.Contains("503") ?? false)
        {
            Snackbar.Add("POSPOS service unavailable. Please try again later.", Severity.Error);
        }
        else
        {
            Snackbar.Add($"Failed to load candidates: {ex.Message}", Severity.Error);
        }
    }
    catch (Exception ex)
    {
        Snackbar.Add($"Failed to load candidates: {ex.Message}", Severity.Error);
    }
    finally
    {
        _isLoading = false;
    }
}
```

#### 3. Modify table header to conditionally show select-all
```razor
<!-- MODIFIED: Hide select-all checkbox when source=pospos -->
<MudTh>
    @if (_source != "pospos")
    {
        <MudCheckBox T="bool" Value="_selectAll" ValueChanged="OnSelectAllChanged" />
    }
</MudTh>
```

#### 4. Add optional UI controls for source selection (future enhancement)
```razor
<!-- OPTIONAL: Add buttons/dropdown to toggle source -->
<MudStack Row AlignItems="AlignItems.Center" Spacing="2">
    <MudButton Variant="@(_source == "all" ? Variant.Filled : Variant.Outlined)" 
               Color="Color.Primary" 
               OnClick="async () => { _source = 'all'; await LoadCandidates(); }">
        All Members
    </MudButton>
    <MudButton Variant="@(_source == \"pospos\" ? Variant.Filled : Variant.Outlined)" 
               Color="Color.Primary" 
               OnClick="async () => { _source = 'pospos'; await LoadCandidates(); }">
        Latest Member
    </MudButton>
</MudStack>
```

**Session-scoped state** (no changes needed):
- Selection state remains session-scoped via `_candidates.IsSelected` field
- Cleared automatically on page reload, dialog close, or new LoadCandidates() call

---

## Database Impact

**No schema changes required**:
- Existing Customer entity used (no modifications)
- Existing CreatedAt field on Customer entity (used for ordering)
- No new tables or migrations
- No index creation required (filtering happens on POSPOS member objects in memory, not database query)

**Performance considerations**:
- POSPOS API returns all members; filtering (for latest) happens in-memory in API endpoint
- For small member lists (<10K): Negligible performance impact
- For large lists: Consider future optimization of POSPOS client to support server-side filtering if available

---

## Error Handling Data Structures

### Existing Error Messages (Enhanced)

**LoadCandidates() error handling** (in ImportCandidatesDialog.razor):

```csharp
catch (HttpRequestException ex)
{
    // NEW: Handle service unavailable
    if (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
    {
        Snackbar.Add("POSPOS service unavailable. Please try again later.", Severity.Error);
    }
    else if (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
    {
        Snackbar.Add("Invalid search parameters.", Severity.Error);
    }
    else
    {
        Snackbar.Add($"Failed to load candidates: {ex.Message}", Severity.Error);
    }
}
catch (Exception ex)
{
    Snackbar.Add($"Failed to load candidates: {ex.Message}", Severity.Error);
}
```

---

## Validation Rules

### API Input Validation

```csharp
// CustomerImportEndpoints.GetCandidates() validation
if (!string.IsNullOrWhiteSpace(source) && 
    !source.Equals("pospos", StringComparison.OrdinalIgnoreCase) && 
    !source.Equals("all", StringComparison.OrdinalIgnoreCase))
{
    return Results.BadRequest(new { error = "Invalid source. Must be 'pospos' or 'all'." });
}
```

### Component Validation (ImportCandidatesDialog)

```csharp
// Ensure source is valid before calling API
_source = _source?.ToLowerInvariant() ?? "all";
if (_source != "pospos" && _source != "all")
{
    _source = "all";  // Fallback to safe default
}
```

---

## Selection State Lifecycle

**Session-scoped behavior** (existing pattern):

1. **Component initialized**: `OnInitializedAsync()` → `LoadCandidates()`
   - _candidates = [ ] (empty)
   - _selectAll = false
   - _source = "all" (default)

2. **Members loaded**: List populated from API
   - Each member has IsSelected = false (unselected by default)

3. **User interaction**: Individual row checkbox clicked
   - Member.IsSelected toggled
   - State stored in component (memory)

4. **Import action**: User clicks "Import selected"
   - Selected members imported via POST /api/customers/import/selected
   - Dialog closes

5. **Dialog reopened or page reloaded**:
   - Component reinitialized
   - _candidates = [ ] (empty, previous selection lost)
   - LoadCandidates() called again
   - Selection state fully cleared

**No persistence**: Selection does NOT survive:
- Page reload
- Dialog reopen
- Tab close
- Browser restart

---

## Data Flow Diagram

```
ImportCandidatesDialog.razor
    ↓
    LoadCandidates() [with source parameter]
    ↓
    HTTP GET /api/customers/import/candidates?source={pospos|all}
    ↓
    CustomerImportEndpoints.GetCandidates()
    ↓
    IPosposMemberClient.GetMembersAsync()
    ↓
    POSPOS API (external)
    ↓
    [Filter if source=pospos: Latest only]
    ↓
    Project to client DTO (CandidateMember)
    ↓
    HTTP 200 [Array of members]
    ↓
    ImportCandidatesDialog.razor
    ↓
    Render table with members
    ↓
    User selection (session-scoped)
    ↓
    User clicks "Import selected"
    ↓
    POST /api/customers/import/selected with selected IDs
    ↓
    CustomerImportService.ImportSelectedCustomersAsync()
    ↓
    Create/update Customer entities
    ↓
    Success response
```

---

## Related Entities (Unchanged)

### Customer Entity (existing)
- Used to store imported members as customers
- Not modified by this feature
- ExternalId field maps POSPOS member ID to Customer

### PigPen Entity (existing)
- Related to Customer via CustomerId foreign key
- Not affected by this feature

---

## File Locations

| Component | File Location |
|-----------|---------------|
| API Endpoint | `src/server/PigFarmManagement.Server/Features/Customers/CustomerImportEndpoints.cs` |
| Component | `src/client/PigFarmManagement.Client/Features/Customers/Components/ImportCandidatesDialog.razor` |
| POSPOS Client | `src/server/PigFarmManagement.Server/Services/ExternalServices/IPosposMemberClient.cs` |
| Tests (Backend) | `src/server/PigFarmManagement.Server.Tests/Features/Customers/` |
| Tests (Frontend) | `src/client/PigFarmManagement.Client.Tests/Features/Customers/` |

---

**Document Version**: 1.1 (Updated for existing component modification)  
**Next Phase**: Phase 2 Contracts (OpenAPI spec, quickstart validation)  
**Maintained By**: Development Team  
**Last Updated**: 2025-11-29
