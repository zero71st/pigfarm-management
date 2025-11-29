# Research Document: POSPOS Import Enhancement (Component Modification)

**Feature**: 012-update-search-customer  
**Phase**: 0 Research  
**Date**: 2025-11-29  
**Status**: Complete (Updated for existing component modification)  
**Scope**: Enhance existing import infrastructure to show only latest POSPOS customer and disable select-all bulk operation

---

## Overview

This feature modifies **existing import infrastructure** (backend API + frontend component) rather than creating new search functionality:

- **Backend**: Enhance `CustomerImportEndpoints.GetCandidates()` to accept optional `source` query parameter
- **Frontend**: Enhance `ImportCandidatesDialog.razor` to disable select-all checkbox
- **No new migrations or tables**: Filtering applied at API query time using existing CreatedAt field from POSPOS members

---

## Decision 1: API Enhancement Strategy - POSPOS Member Filtering

**Choice**: Add `source` query parameter to existing `/api/customers/import/candidates` endpoint

**Rationale**:
- Minimizes code changes (single endpoint enhanced vs. creating new endpoint)
- Reuses existing POSPOS integration (IPosposMemberClient already fetches members)
- Backward compatible: omitted parameter returns all (existing behavior)
- Server-side filtering reduces payload and client complexity
- Leverages existing member sorting by CreatedAt

**Implementation approach**:
```csharp
// CustomerImportEndpoints.GetCandidates() - MODIFIED
public static async Task<IResult> GetCandidates(
    IPosposMemberClient posposClient,
    [FromQuery] string source = "all")  // NEW parameter
{
    try
    {
        var members = await posposClient.GetMembersAsync();
        
        // NEW: Filter by source
        if (source.Equals("pospos", StringComparison.OrdinalIgnoreCase))
        {
            // Return latest member only
            members = members
                .OrderByDescending(m => m.CreatedAt)
                .ThenByDescending(m => m.Id)
                .Take(1)
                .ToList();
        }
        else if (source.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            // Return all (existing behavior)
        }
        else
        {
            return Results.BadRequest(new { error = "Invalid source. Must be 'pospos' or 'all'." });
        }

        // Project and return (existing code remains unchanged)
        var projected = members.Select(m => new
        {
            Id = m.Id,
            Code = string.IsNullOrWhiteSpace(m.Code) ? m.Id : m.Code,
            FirstName = m.FirstName,
            LastName = m.LastName,
            Phone = string.IsNullOrWhiteSpace(m.Phone) ? m.PhoneNumber : m.Phone,
            Email = m.Email,
            Address = m.Address,
            KeyCardId = m.KeyCardId,
            ExternalId = m.Id,
            Sex = m.Sex,
            Zipcode = m.Zipcode,
            CreatedAt = m.CreatedAt
        });

        return Results.Ok(projected);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to get import candidates: {ex.Message}");
    }
}
```

**Alternatives considered**:
- Create new `/api/customers/import/latest` endpoint (rejected: redundant, violates DRY principle)
- Client-side filtering in ImportCandidatesDialog (rejected: less efficient, duplicates logic)
- Cache latest member in memory (rejected: adds complexity without reducing API calls)

---

## Decision 2: Component Enhancement - Select-All Checkbox Behavior

**Choice**: Disable/hide select-all checkbox in ImportCandidatesDialog header

**Rationale**:
- Prevents accidental bulk import operations when viewing latest customer only
- Simplifies UX for POSPOS import workflow (typically single customer imports)
- Individual row selection remains enabled (users can still select specific customers when importing all)
- Session-scoped state maintained (existing component pattern)
- Minimal UI changes required

**Implementation approach**:
```razor
<!-- EXISTING header in ImportCandidatesDialog.razor -->
<MudTh>
    <!-- OPTION 1: Conditionally hide when source=pospos -->
    @if (_source != "pospos")
    {
        <MudCheckBox T="bool" Value="_selectAll" ValueChanged="OnSelectAllChanged" />
    }
</MudTh>

@code {
    private string _source = "all";  // NEW: Track source context
    
    // MODIFIED: Call LoadCandidates with source parameter
    private async Task LoadCandidates()
    {
        _isLoading = true;
        try
        {
            var url = $"/api/customers/import/candidates?source={_source}";
            var list = await Http.GetFromJsonAsync<List<CandidateMember>>(url);
            if (list != null)
                _candidates = list;
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
    
    // OPTIONAL: Add UI to toggle source
    private async Task SetSourcePospos()
    {
        _source = "pospos";
        await LoadCandidates();
    }
    
    private async Task SetSourceAll()
    {
        _source = "all";
        await LoadCandidates();
    }
}
```

**Alternatives considered**:
- Always hide select-all checkbox (rejected: removes bulk import capability entirely)
- Disable but keep visible (acceptable alternative: shows affordance but prevents interaction)
- Keyboard shortcut for select-all (rejected: feature scope doesn't include shortcuts)

---

## Decision 3: Error Handling Strategy

**Choice**: Distinguish POSPOS service failures from empty results via existing snackbar messaging

**Rationale**:
- Existing component has error handling via snackbar (see LoadCandidates() catch block)
- Clarification specifies: distinct error message "POSPOS service unavailable. Please try again later."
- Empty result has separate UX: "No candidates found" message (existing component shows this)
- Leverages existing exception handling pattern

**Implementation approach**:
```csharp
// CustomerImportEndpoints.GetCandidates() - error handling ENHANCED
catch (HttpRequestException ex)
{
    // POSPOS API failure (network, timeout, etc.)
    return Results.StatusCode(503, new 
    { 
        error = "POSPOS service unavailable. Please try again later." 
    });
}
catch (JsonException ex)
{
    // Invalid response format from POSPOS
    return Results.StatusCode(502, new 
    { 
        error = "POSPOS service returned invalid data. Please try again later." 
    });
}
catch (Exception ex)
{
    // Other unexpected failures
    return Results.Problem($"Failed to get import candidates: {ex.Message}");
}

// Frontend handles different status codes
// LoadCandidates() already has catch block - enhance it:
private async Task LoadCandidates()
{
    _isLoading = true;
    try
    {
        var url = $"/api/customers/import/candidates?source={_source}";
        var list = await Http.GetFromJsonAsync<List<CandidateMember>>(url);
        if (list != null)
            _candidates = list;
    }
    catch (HttpRequestException ex)
    {
        // POSPOS API failure
        Snackbar.Add("POSPOS service unavailable. Please try again later.", Severity.Error);
    }
    catch (Exception ex)
    {
        // Other failures
        Snackbar.Add($"Failed to load candidates: {ex.Message}", Severity.Error);
    }
    finally
    {
        _isLoading = false;
    }
}
```

**Alternatives considered**:
- Generic error for all failures (rejected: hides POSPOS-specific issues)
- Fallback to cached data (rejected: masks real problems, doesn't help user)
- Retry mechanism (rejected: out of feature scope)

---

## Decision 4: Selection State Scope

**Choice**: Session-scoped state via component `_candidates` list (existing pattern - no changes)

**Rationale**:
- Clarification specifies: session-scoped (user selected "clears on reload")
- Existing component already uses session state via `_candidates` list with `IsSelected` field
- No persistence needed; simplifies backend
- Aligns with import workflow: temporary selection before import action
- Zero additional implementation effort (existing pattern)

**Implementation approach** (no changes needed):
```razor
@code {
    private List<CandidateMember> _candidates = new();  // Session-scoped: cleared on reload
    
    // Selection is automatically cleared when:
    // 1. Component reinitialized (page reload/navigation)
    // 2. New search performed (LoadCandidates() called, replaces list)
    // 3. Import completed and dialog closed
    // 4. Browser tab closed
    
    // Existing CandidateMember class
    private class CandidateMember
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DisplayName => /* ... */;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string KeyCardId { get; set; } = string.Empty;
        public string ExternalId { get; set; } = string.Empty;
        public string Sex { get; set; } = string.Empty;
        public string Zipcode { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public bool IsSelected { get; set; }  // EXISTING: tracks selection state
    }
}
```

**Alternatives considered**:
- LocalStorage persistence (rejected: implies data is saved, contradicts requirement)
- Database persistence (rejected: overkill for ephemeral import workflow)
- SessionStorage (rejected: equivalent to component state, adds unnecessary complexity)

---

## Decision 5: Ordering Definition - Latest POSPOS Member

**Choice**: `OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)`

**Rationale**:
- CreatedAt field available on POSPOS member (used for sorting in IPosposMemberClient)
- Secondary sort by Id ensures deterministic ordering (edge case: identical CreatedAt)
- Aligns with customer database patterns (existing CreatedAt field on Customer entity)
- No additional API calls or POSPOS integration changes needed

**Ordering specifics**:
- Primary: `CreatedAt DESC` (newest first)
- Secondary: `Id DESC` (if timestamps equal, higher ID sorts first for determinism)

**Alternatives considered**:
- CreatedAt only (rejected: non-deterministic if timestamps identical)
- UpdatedAt (rejected: may be null or unreliable on POSPOS side)
- Random selection from "latest day" (rejected: unpredictable UX, doesn't meet requirement)

---

## Implementation Summary Table

| Aspect | Decision | Effort |
|--------|----------|--------|
| **API Enhancement** | Add source parameter to existing GetCandidates() | 1-2 hours |
| **Component UI** | Conditionally hide/disable select-all checkbox | 30 mins |
| **Error Handling** | Enhance existing error messages in component | 30 mins |
| **Selection State** | No changes (session-scoped already exists) | None |
| **Database** | No changes (existing CreatedAt field used) | None |
| **New Endpoints** | None (existing endpoint enhanced) | None |
| **New Migrations** | None required | None |
| **Breaking Changes** | None (backward compatible) | None |

---

## Risk Assessment

| Risk | Probability | Mitigation |
|------|-------------|------------|
| POSPOS API returns members in non-deterministic order | Low | Use explicit sorting: CreatedAt DESC, Id DESC |
| Existing bulk import workflows broken | Low | Backward compatible: default source=all returns all members |
| Select-all hidden from admins who need bulk import | Low | Individual selection still works; can revert if needed |
| Error message not clear enough | Very Low | Follows clarification exactly: "POSPOS service unavailable..." |
| Performance degradation with large POSPOS datasets | Very Low | Server-side filtering more efficient than client-side |

---

## Testing Considerations (for /tasks phase)

**Backend Tests**:
- GetCandidates with source=pospos → returns 1 member (latest by CreatedAt)
- GetCandidates with source=all → returns all members (existing behavior)
- GetCandidates with source=invalid → returns 400 error
- Error handling: Catch and return 503 for POSPOS API failures

**Frontend Tests**:
- ImportCandidatesDialog renders without select-all when loading with source=pospos
- Select-all visible when source=all
- Individual row selection works for both sources
- Error message "POSPOS service unavailable..." displays on API failure

**Integration Tests**:
- Full import flow with single member (source=pospos)
- Full import flow with multiple members (source=all)
- Selection state clears on page reload

---

**Document Version**: 1.1 (Updated for existing component modification)  
**Next Phase**: Phase 1 Design (data-model.md, contracts, quickstart.md)  
**Maintained By**: Development Team  
**Last Updated**: 2025-11-29
