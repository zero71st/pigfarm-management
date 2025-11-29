# Contract Validation Checklist: GET /api/customers/import/candidates Enhancement

**Feature**: 012-update-search-customer  
**Date**: 2025-11-29  
**Purpose**: Validate API contract compliance for enhanced endpoint

---

## API Endpoint Validation

**Endpoint**: `GET /api/customers/import/candidates`

### Basic Functionality

- [ ] Endpoint exists and is accessible
- [ ] Endpoint requires `X-Api-Key` header for authentication (401 if missing)
- [ ] Endpoint accepts optional `source` query parameter

### Parameter Validation

- [ ] Parameter `source=pospos` accepted and processed
- [ ] Parameter `source=all` accepted and processed
- [ ] Parameter omitted (defaults to "all") works correctly
- [ ] Parameter `source=invalid` returns 400 Bad Request with message: "Invalid source. Must be 'pospos' or 'all'."
- [ ] Parameter `source=POSPOS` (uppercase) treated as "pospos" (case-insensitive)
- [ ] Parameter `source=ALL` (uppercase) treated as "all" (case-insensitive)

### Response Format

- [ ] Response HTTP 200 returned on success
- [ ] Response body is JSON array of member objects
- [ ] Each member object contains: id, code, firstName, lastName, phone, email, address, keyCardId, externalId, sex, zipcode, createdAt
- [ ] All fields present in response (no missing properties)

### source=pospos Behavior (Latest Member Only)

- [ ] Returns array with 0-1 items (never more than 1)
- [ ] When members exist: Returns the member with latest CreatedAt timestamp
- [ ] When multiple members have same CreatedAt: Uses Id DESC as tiebreaker (deterministic)
- [ ] When no members exist: Returns empty array []
- [ ] Members ordered correctly: CreatedAt DESC, then Id DESC

### source=all Behavior (All Members)

- [ ] Returns array with 0-N items (all available members)
- [ ] When members exist: Returns all members from POSPOS
- [ ] When no members exist: Returns empty array []
- [ ] Same member set returned regardless of repetition (idempotent)

### Error Handling

- [ ] HTTP 400 Bad Request for invalid source parameter
  - [ ] Response body: `{ "error": "Invalid source. Must be 'pospos' or 'all'." }`
- [ ] HTTP 401 Unauthorized for missing/invalid API key
  - [ ] Proper authentication challenge sent
- [ ] HTTP 500 Internal Server Error for unexpected server failures
  - [ ] Response contains error message
- [ ] HTTP 503 Service Unavailable for POSPOS API failures
  - [ ] Response body contains: "POSPOS service unavailable. Please try again later."
  - [ ] Different from other errors (distinct status code)

### Performance

- [ ] API responds in < 100ms for typical member lists
- [ ] No memory leaks or connection exhaustion from repeated calls
- [ ] Handles concurrent requests correctly

### Backward Compatibility

- [ ] Existing code calling endpoint without source parameter still works
- [ ] Response format unchanged (same member object structure)
- [ ] Default behavior (no source parameter) equals source=all

---

## Test Matrix

| source param | Expected Result | Status Code | Notes |
|--------------|-----------------|-------------|-------|
| (omitted) | All members | 200 | Default/backward compatible |
| `all` | All members | 200 | Explicit all |
| `pospos` | Latest member only (0-1 items) | 200 | New feature |
| `ALL` (uppercase) | All members | 200 | Case-insensitive |
| `POSPOS` (uppercase) | Latest member only | 200 | Case-insensitive |
| `invalid` | Error message | 400 | Invalid value |
| (invalid API key) | Unauthorized | 401 | Auth required |

---

## Sign-Off

**Contract Validation**: 
- [ ] All checks pass (date: ____________)
- [ ] Checked by: ____________
- [ ] Issues/Deviations: (if any)
  ```
  
  ```

**Ready for Implementation**: [ ] Yes / [ ] No

---

**Document**: contract validation checklist for T001  
**Task**: T001 (Create contract validation checklist)  
**Status**: Created 2025-11-29
