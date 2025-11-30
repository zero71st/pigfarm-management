# Integration Checklist: Full Import Workflow

**Feature**: 012-update-search-customer  
**Date**: 2025-11-29  
**Purpose**: Verify that all related import endpoints and workflows continue functioning after modifications

---

## Endpoint Backward Compatibility

### GET /api/customers/import/candidates (ENHANCED)

- [ ] Endpoint exists and is accessible
- [ ] Calling without source parameter returns all members (existing behavior)
- [ ] Calling with source=all returns all members (explicit)
- [ ] Calling with source=pospos returns 1 member (latest)
- [ ] Response format unchanged (same member object structure)
- [ ] Performance acceptable (< 100ms response time)

### POST /api/customers/import/selected (UNCHANGED)

- [ ] Endpoint accepts array of member IDs in request body
- [ ] Endpoint creates new Customer records or updates existing
- [ ] Endpoint returns success response with import count
- [ ] Selected members properly imported to database
- [ ] Works with single member (source=pospos workflow)
- [ ] Works with multiple members (source=all workflow)
- [ ] ExternalId field correctly maps POSPOS member ID to Customer

### POST /api/customers/import/all (UNCHANGED)

- [ ] Endpoint imports all available POSPOS members
- [ ] Existing behavior preserved
- [ ] No modification required (not impacted by feature)

### POST /api/customers/import/sync (UNCHANGED)

- [ ] Endpoint syncs POSPOS members with database
- [ ] Existing behavior preserved
- [ ] No modification required (not impacted by feature)

---

## Authentication & Authorization

- [ ] X-Api-Key header required for all import endpoints
- [ ] Invalid/missing API key returns 401 Unauthorized
- [ ] Valid API key allows access
- [ ] No changes to auth scheme or requirement
- [ ] User context preserved (Creator/LastModifiedBy fields set correctly)

---

## Data Integrity

- [ ] No duplicate customers created on repeated imports
- [ ] ExternalId properly mapped to prevent duplicates
- [ ] IsDeleted soft-delete flag respected (existing records not re-imported)
- [ ] Timestamps (CreatedAt, LastModifiedAt) set correctly
- [ ] Customer relationships (PigPen links) maintained

---

## Selection State

- [ ] Selection state is session-scoped (not persisted to database)
- [ ] Selection clears on page reload
- [ ] Selection clears on dialog close/reopen
- [ ] Multiple browser tabs have independent selection state
- [ ] No session cookies or database records created for selection

---

## Error Handling

- [ ] API returns 400 for invalid source parameter
- [ ] API returns 401 for auth failures
- [ ] API returns 500 for unexpected server errors
- [ ] API returns 503 specifically for POSPOS service unavailability
- [ ] Error messages are user-friendly and distinct
- [ ] Error doesn't corrupt state or leave partial records

### POSPOS Service Unavailable (503)

- [ ] When POSPOS API fails: Returns 503 status
- [ ] Error message: "POSPOS service unavailable. Please try again later."
- [ ] Different from other error types (e.g., database connection error = 500)
- [ ] UI shows distinct snackbar message to user
- [ ] User can retry without data loss

### Other Errors (500)

- [ ] Database connection errors handled gracefully
- [ ] Null reference exceptions don't crash endpoint
- [ ] Invalid POSPOS response handled (e.g., malformed JSON)
- [ ] Error logged for debugging

---

## Performance Metrics

- [ ] GetCandidates(source=pospos) response: < 100ms
- [ ] GetCandidates(source=all) response: < 100ms
- [ ] ImportSelected() response: < 500ms for 1 member
- [ ] ImportSelected() response: < 1s for 10 members
- [ ] No memory leaks (memory stable after repeated calls)
- [ ] No connection pool exhaustion

---

## Regression Testing

### Non-POSPOS Searches Unaffected

- [ ] Customer search by code/name works unchanged
- [ ] Database customer records unaffected by POSPOS import changes
- [ ] Existing customer bulk operations work (if any)
- [ ] Reports and dashboards show correct data

### Existing Import Workflows

- [ ] CSV import (if exists) unaffected
- [ ] Manual customer creation unaffected
- [ ] Customer edit/update operations unaffected
- [ ] Bulk customer operations work unchanged

---

## Database State

- [ ] No schema changes required (tables, columns identical)
- [ ] No migration needed on deployment
- [ ] Existing customer records not modified
- [ ] POSPOS API integration unchanged
- [ ] Database indexes sufficient for performance

---

## UI Integration

- [ ] ImportCandidatesDialog renders without errors
- [ ] Dialog opens/closes smoothly
- [ ] Table pagination works (if applicable)
- [ ] Sorting by CreatedAt works for displayed members
- [ ] No console JavaScript errors
- [ ] MudBlazor components render correctly

---

## Sign-Off

**Integration Testing Complete**:
- [ ] All checks performed and recorded (date: ____________)
- [ ] Tested by (QA/Tech Lead): ____________
- [ ] Environment: _____ (dev/staging/prod)

**Issues Found**:
```

```

**Integration Status**: _____ (Pass / Fail)  
**Approved for Merge**: [ ] Yes / [ ] No (if No, explain above)

---

**Document**: Integration checklist for T003  
**Task**: T003 (Prepare integration test checklist)  
**Status**: Created 2025-11-29
