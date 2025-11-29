# Update Summary: Existing Component Modification (NOT New Search Page)

**Date**: 2025-11-29  
**Feature**: 012-update-search-customer  
**Change**: Plan updated to reflect modification of existing ImportCandidatesDialog component and API (vs. creating new search infrastructure)

---

## What Changed

### Before (Original Plan)
- ❌ Create new search page/component for customer search
- ❌ Create new API endpoint `/api/customers/search`
- ❌ Modify CustomerRepository with new `GetLatestPosposCustomerAsync()` method
- ❌ Create new DTOs: CustomerSearchRequest, CustomerSearchResponse

### After (Updated Plan)
- ✅ **Modify existing ImportCandidatesDialog.razor component**
- ✅ **Enhance existing `/api/customers/import/candidates` endpoint** (add source parameter)
- ✅ **No new components or pages needed**
- ✅ **No new DTOs needed** (reuse existing CandidateMember)
- ✅ **Minimal code changes** (focused modifications only)

---

## Modified Components

### 1. Backend: `CustomerImportEndpoints.GetCandidates()`

**Location**: `src/server/PigFarmManagement.Server/Features/Customers/CustomerImportEndpoints.cs`

**Changes**:
- Add `[FromQuery] string source = "all"` parameter
- Validate source value (must be "pospos" or "all")
- Filter members when source="pospos": `OrderByDescending(m => m.CreatedAt).Take(1)`
- Keep existing behavior when source="all"
- Enhance error handling: distinguish 503 (POSPOS unavailable) from 500

**Effort**: 30-45 minutes

---

### 2. Frontend: `ImportCandidatesDialog.razor` Component

**Location**: `src/client/PigFarmManagement.Client/Features/Customers/Components/ImportCandidatesDialog.razor`

**Changes**:
- Add `_source` field: `private string _source = "all";`
- Modify `LoadCandidates()`: Include source in API URL
- Conditionally render select-all checkbox: `@if (_source != "pospos")`
- Enhance error handling: Catch HttpRequestException for service unavailable

**Effort**: 45-60 minutes

---

## Files Updated/Created

| File | Status | Change |
|------|--------|--------|
| `research.md` | ✅ Updated | Now focuses on existing component modification |
| `data-model.md` | ✅ Updated | Documents existing models and API modifications |
| `plan.md` | ✅ Updated | Reflects modification scope (existing components) |
| `PHASE_2_COMPLETION.md` | ✅ Updated | Updated artifact summaries |
| `contracts/import-candidates-api.openapi.json` | ✅ Created | OpenAPI spec for enhanced endpoint |
| `quickstart.md` | ✅ Existing | Validation scenarios adapted for existing component |

---

## API Enhancement Details

### Endpoint: GET /api/customers/import/candidates

**New Query Parameter**: `source`
- `source=pospos` → Returns 1 member (latest by CreatedAt)
- `source=all` → Returns all members (existing behavior)
- Omitted → Returns all (backward compatible)

**Backward Compatibility**: ✅ YES
- Existing code that calls `/api/customers/import/candidates` (without source param) still works
- Default behavior unchanged

**Error Handling**:
- 400: Invalid source value
- 503: POSPOS service unavailable (distinct message: "POSPOS service unavailable. Please try again later.")

---

## Component Enhancement Details

### ImportCandidatesDialog.razor

**New Behavior**:
1. When source="all" (default): Shows all members, select-all checkbox enabled (existing behavior)
2. When source="pospos": Shows 1 latest member, select-all checkbox DISABLED

**Selection State**: Session-scoped (unchanged)
- Clears on page reload
- Clears on dialog close
- No persistence

**Error Messages**:
- "POSPOS service unavailable. Please try again later." (new, distinct)
- Other errors: Existing messages

---

## Implementation Effort Summary

| Task | Effort | Priority |
|------|--------|----------|
| Backend API: Add source parameter | 30-45 min | High |
| Backend API: Add validation | 15-20 min | High |
| Backend API: Enhanced error handling | 15 min | Medium |
| Frontend: Add source field | 5 min | High |
| Frontend: Modify LoadCandidates() | 15-20 min | High |
| Frontend: Conditional select-all render | 10-15 min | High |
| Frontend: Enhance error handling | 10-15 min | Medium |
| Testing: Backend unit tests | 30-45 min | Medium |
| Testing: Frontend component tests | 30-45 min | Medium |
| Testing: Integration validation | 30-60 min | Medium |
| **Total Estimated** | **3-4 hours** | - |

---

## Why This Approach

✅ **Simpler** - Modify existing components vs. creating new infrastructure  
✅ **Lower Risk** - Focused changes to proven components  
✅ **Backward Compatible** - Existing import workflow unchanged  
✅ **Reuses Existing Code** - No new DTOs or patterns needed  
✅ **Faster Development** - Less code to write and test  
✅ **Aligned with Feature** - ImportCandidatesDialog is the right place for this feature  

---

## Next Steps

1. **Run `/tasks` command** to generate task list for implementation
2. **Review generated tasks** in `tasks.md`
3. **Execute tasks** following dependency order (backend first, then frontend)
4. **Run validation scenarios** from `quickstart.md`
5. **Deploy to environment**
6. **Collect sign-offs**

---

## Key Files to Reference During Implementation

- **Backend Changes**: `src/server/.../Features/Customers/CustomerImportEndpoints.cs` (method: GetCandidates)
- **Frontend Changes**: `src/client/.../Features/Customers/Components/ImportCandidatesDialog.razor` (methods: LoadCandidates, OnSelectAllChanged, render logic)
- **API Contract**: `specs/012-update-search-customer/contracts/import-candidates-api.openapi.json`
- **Validation Guide**: `specs/012-update-search-customer/quickstart.md`
- **Data Model Details**: `specs/012-update-search-customer/data-model.md`
- **Technical Decisions**: `specs/012-update-search-customer/research.md`

---

**Document Date**: 2025-11-29  
**Status**: Plan Update Complete - Ready for Task Generation  
**Next Command**: `/tasks` (Phase 3)
