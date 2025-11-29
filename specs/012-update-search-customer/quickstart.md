# Customer Search Feature - Quickstart Validation Guide

**Status**: Implementation planning complete; ready for development  
**Last Updated**: 2025-11-29  
**Feature Ticket**: 012-update-search-customer  
**Branch**: `012-update-search-customer`

## Overview

This document provides step-by-step manual validation scenarios for the POSPOS customer search enhancement feature. Use this checklist **after code deployment** to verify that the implementation meets all requirements.

---

## Pre-Validation Setup

### Prerequisites
- Feature branch `012-update-search-customer` has been merged to main
- Backend server running: `http://localhost:5000` (dev) or production URL
- Frontend client running: `http://localhost:7000` (dev) or production URL
- Admin user with valid API key
- POSPOS API credentials configured (dev or sandbox)
- At least 3 customers in database (2 local + 1 POSPOS-synced)

### Test Data Preparation

**Local Database Customers** (created before any POSPOS sync):
1. **Customer A**: Code=`LOCAL_001`, FirstName=`John`, Status=`1` (Active)
   - CreatedAt: `2025-11-01T08:00:00Z` (oldest)
2. **Customer B**: Code=`LOCAL_002`, FirstName=`Jane`, Status=`1` (Active)
   - CreatedAt: `2025-11-15T10:00:00Z` (middle)

**POSPOS-Synced Customer** (imported most recently):
3. **Customer C**: Code=`POSPOS_001`, ExternalId=`ext_12345`, FirstName=`Michael`, Status=`1` (Active)
   - CreatedAt: `2025-11-25T14:30:00Z` (newest)

**Inactive Customer** (for negative test):
4. **Customer D**: Code=`INACTIVE_001`, FirstName=`Inactive`, Status=`0` (Inactive)
   - CreatedAt: `2025-11-20T12:00:00Z`

---

## Validation Scenarios

### Scenario 1: POSPOS Source Search - Returns Single Latest Customer

**Objective**: Verify that source=`pospos` returns exactly 1 customer (most recently created)

**Steps**:
1. Open frontend at `http://localhost:7000/customers/search`
2. Ensure logged in with valid admin credentials
3. Click "Search POSPOS Customers" button or select `source: pospos` filter
4. Observe table results

**Expected Outcome**:
- ✅ Table displays **exactly 1 row** (Customer C: Michael, POSPOS_001)
- ✅ No other customers visible (Local_001, Local_002, Inactive_001 NOT shown)
- ✅ Customer C shows:
  - Code: `POSPOS_001`
  - FirstName: `Michael`
  - Status: `Active` (visual indicator)
  - CreatedAt: `2025-11-25T14:30:00Z` (or formatted date)
  - ExternalId: `ext_12345` (if column visible)

**API Call Verification** (via Postman/curl):
```powershell
# Using curl
curl -X GET "http://localhost:5000/api/customers/search?source=pospos" \
  -H "X-Api-Key: <YOUR_API_KEY>" \
  -H "Content-Type: application/json"

# Expected response:
# {
#   "customers": [
#     {
#       "id": "...",
#       "code": "POSPOS_001",
#       "firstName": "Michael",
#       "status": 1,
#       "createdAt": "2025-11-25T14:30:00Z",
#       "externalId": "ext_12345"
#     }
#   ],
#   "error": null
# }
```

**Pass/Fail**: ☐ PASS / ☐ FAIL (Notes: _______________)

---

### Scenario 2: All Source Search - Returns Multiple Customers (3)

**Objective**: Verify that source=`all` returns all active customers, excluding inactive

**Steps**:
1. From the same search page, select `source: all` filter
2. Click "Search All Customers" or change source dropdown to "All"
3. Observe table results

**Expected Outcome**:
- ✅ Table displays **exactly 3 rows** (Customers A, B, C)
- ✅ Customer D (Inactive) is NOT shown
- ✅ Rows appear in any order (no sort requirement for this scenario)
- ✅ Rows show:
  - Customer A: Code=`LOCAL_001`, FirstName=`John`, Status=`Active`
  - Customer B: Code=`LOCAL_002`, FirstName=`Jane`, Status=`Active`
  - Customer C: Code=`POSPOS_001`, FirstName=`Michael`, Status=`Active`

**API Call Verification**:
```powershell
curl -X GET "http://localhost:5000/api/customers/search?source=all" \
  -H "X-Api-Key: <YOUR_API_KEY>"

# Expected: Array with 3 customers (A, B, C), no error
```

**Pass/Fail**: ☐ PASS / ☐ FAIL (Notes: _______________)

---

### Scenario 3: Select All Checkbox is DISABLED

**Objective**: Verify that the "Select All" checkbox in the search table header is disabled/hidden

**Steps**:
1. Navigate to customer search page with any source filter active
2. Look at table header (first row with column headers)
3. Observe checkbox column in header

**Expected Outcome**:
- ✅ "Select All" checkbox is **NOT visible** OR **disabled** (grayed out)
- ✅ User cannot click to select all customers at once
- ✅ Individual row checkboxes ARE visible and clickable (next scenario)

**Visual Check**:
- No checkbox in the header row OR checkbox present but disabled with cursor showing "not-allowed" style

**Pass/Fail**: ☐ PASS / ☐ FAIL (Notes: _______________)

---

### Scenario 4: Individual Customer Selection Works (Single Row Select)

**Objective**: Verify that users can select individual customers by clicking row checkboxes

**Steps**:
1. With source=`all` active (showing 3 customers)
2. Click checkbox next to Customer A (LOCAL_001)
3. Observe selection state change
4. Click checkbox next to Customer B (LOCAL_002)
5. Verify both can be selected independently

**Expected Outcome**:
- ✅ Customer A checkbox shows as checked after clicking
- ✅ Customer A row highlights or shows visual selection indicator
- ✅ Customer B checkbox can be independently checked/unchecked
- ✅ Multiple customers can be selected (no single-select limitation)
- ✅ Unchecking a customer removes selection

**JavaScript Console Check** (open DevTools):
- No errors related to selection state
- Component state updates logged (if debug mode enabled)

**Pass/Fail**: ☐ PASS / ☐ FAIL (Notes: _______________)

---

### Scenario 5: Selection State is Session-Scoped (Clears on Page Reload)

**Objective**: Verify that selected customers are NOT persisted when page reloads

**Steps**:
1. With source=`all` active
2. Select Customer A and Customer B (check both checkboxes)
3. Verify both show as selected
4. Press F5 or click browser back/forward
5. Return to customer search page
6. Observe selection state

**Expected Outcome**:
- ✅ After reload, **all checkboxes are unchecked** (selection cleared)
- ✅ No selection persists in browser session storage or local storage
- ✅ Page refreshes with clean (unselected) state
- ✅ Repeat selection flow works normally

**Storage Check** (DevTools → Application → Storage):
- No entries in `localStorage` related to customer selection
- SessionStorage may be empty or contain only UI state (not selections)

**Pass/Fail**: ☐ PASS / ☐ FAIL (Notes: _______________)

---

### Scenario 6: Empty Results - Shows Appropriate Message

**Objective**: Verify that search with no matching customers displays a user-friendly empty state

**Steps**:
1. Navigate to search page with source=`all`
2. Enter search text (e.g., "XXXNOTFOUND") that doesn't match any customer
3. Click search button
4. Observe result

**Expected Outcome**:
- ✅ Table body shows **empty state message** (e.g., "No customers found")
- ✅ Message is clearly visible and not an error
- ✅ "Select All" checkbox remains disabled
- ✅ Page does not break or show error toast

**Alternative Check**: POSPOS source with no matches
- Same behavior: Empty message, no error

**Pass/Fail**: ☐ PASS / ☐ FAIL (Notes: _______________)

---

### Scenario 7: POSPOS API Failure - Shows Distinct Error Message

**Objective**: Verify that when POSPOS API is unavailable, user sees specific error message

**Precondition**: Simulate API failure
- Disable POSPOS network connectivity (disconnect VPN, block API endpoint, or temporarily remove POSPOS_API_URL env var)

**Steps**:
1. With POSPOS connectivity disabled
2. Navigate to search page with source=`pospos`
3. Click search or trigger POSPOS search request
4. Observe error message

**Expected Outcome**:
- ✅ Error message appears: **"POSPOS service unavailable. Please try again later."**
- ✅ Message is NOT generic (not "An error occurred")
- ✅ Message is clearly distinguished from empty results
- ✅ Error appears as toast notification OR inline message
- ✅ Table shows no customers (empty state, not error list)

**API Response Check** (via Network tab in DevTools):
```json
{
  "customers": [],
  "error": "POSPOS service unavailable. Please try again later."
}
```

**Post-Test**: Restore POSPOS connectivity

**Pass/Fail**: ☐ PASS / ☐ FAIL (Notes: _______________)

---

### Scenario 8: Database Query Performance - Latest Customer Index Used

**Objective**: Verify that server efficiently retrieves latest customer via index

**Precondition**: Database has 1000+ customers

**Steps**:
1. With database populated (or run performance test script)
2. Execute source=`pospos` search via API
3. Monitor query execution time
4. Check database query logs or profiling tools

**Expected Outcome**:
- ✅ API response time < 100ms (with index)
- ✅ Database query uses index `idx_customers_created_at_id`
- ✅ Query execution plan shows "Index Scan" (not "Table Scan")

**Manual Check** (PostgreSQL):
```sql
-- Check index exists
SELECT * FROM pg_stat_user_indexes WHERE schemaname='public' AND indexname='idx_customers_created_at_id';

-- Verify query uses index
EXPLAIN (ANALYZE, BUFFERS) 
SELECT * FROM "Customers" 
WHERE "IsDeleted" = false AND "Status" = 1 
ORDER BY "CreatedAt" DESC, "Id" DESC 
LIMIT 1;
-- Should show "Index Scan" on idx_customers_created_at_id
```

**Pass/Fail**: ☐ PASS / ☐ FAIL (Notes: _______________)

---

### Scenario 9: Authorization - API Key Required

**Objective**: Verify that search endpoint enforces authentication

**Steps**:
1. Call search endpoint without X-Api-Key header
   ```powershell
   curl -X GET "http://localhost:5000/api/customers/search?source=all"
   ```
2. Observe response

**Expected Outcome**:
- ✅ Returns HTTP 401 Unauthorized
- ✅ Response body: `{ "error": "Unauthorized" }` or similar
- ✅ No customer data exposed

**Pass**:
3. Call endpoint with valid API key
   ```powershell
   curl -X GET "http://localhost:5000/api/customers/search?source=all" \
     -H "X-Api-Key: <VALID_KEY>"
   ```
4. Verify HTTP 200 and customer data returned

**Pass/Fail**: ☐ PASS / ☐ FAIL (Notes: _______________)

---

### Scenario 10: Invalid Source Parameter - Returns Error

**Objective**: Verify that invalid source values are rejected with clear error

**Steps**:
1. Call search with invalid source:
   ```powershell
   curl -X GET "http://localhost:5000/api/customers/search?source=invalid" \
     -H "X-Api-Key: <YOUR_API_KEY>"
   ```
2. Observe response

**Expected Outcome**:
- ✅ Returns HTTP 400 Bad Request
- ✅ Error message: "Invalid search source" or "Source must be 'pospos' or 'all'"
- ✅ No customer data returned

**Pass/Fail**: ☐ PASS / ☐ FAIL (Notes: _______________)

---

## Regression Tests

### Regression 1: Existing "All Customers" Search Still Works

**Objective**: Ensure backward compatibility with existing customer list feature

**Steps**:
1. Navigate to main Customers page (not search page)
2. Verify existing customer list/grid displays all active customers
3. Verify existing filters (status, search text) still work

**Expected Outcome**:
- ✅ All active customers displayed (no breaking changes)
- ✅ Existing search/filter UI functions normally
- ✅ No performance degradation

**Pass/Fail**: ☐ PASS / ☐ FAIL (Notes: _______________)

---

### Regression 2: Customer CRUD Operations Unaffected

**Objective**: Ensure create/update/delete operations work normally

**Steps**:
1. Create new customer (test manual entry)
2. Update existing customer (modify name)
3. Soft-delete a customer
4. Verify search excludes deleted customers

**Expected Outcome**:
- ✅ Create/Update/Delete work without errors
- ✅ Deleted customers not shown in search results
- ✅ Search results update immediately after CRUD operations

**Pass/Fail**: ☐ PASS / ☐ FAIL (Notes: _______________)

---

## Deployment Verification Checklist

- [ ] Code merged to main branch
- [ ] Database migration executed (index `idx_customers_created_at_id` created)
- [ ] Backend server deployed and running
- [ ] Frontend client deployed and running
- [ ] POSPOS API credentials configured in production environment
- [ ] Admin user created with valid API key
- [ ] All 10 validation scenarios executed and passed
- [ ] All 2 regression tests executed and passed
- [ ] Performance test completed (query < 100ms)
- [ ] Production logs monitored for errors during validation period
- [ ] Rollback plan documented (in case of critical issues)

---

## Issues Found During Validation

| Scenario | Issue | Severity | Resolution |
|----------|-------|----------|-----------|
| (Number) | Description | High/Med/Low | Action taken |
| 1 | (Example: Select-All checkbox still visible) | High | (Re-check implementation) |

---

## Sign-Off

| Role | Name | Date | Status |
|------|------|------|--------|
| QA Lead | __________ | __________ | ☐ Approved / ☐ Failed |
| Product Owner | __________ | __________ | ☐ Approved / ☐ Failed |
| Tech Lead | __________ | __________ | ☐ Approved / ☐ Failed |

---

## Notes & Observations

```
[Space for general notes, observations, or recommendations from validation team]



```

---

**Document Version**: 1.0  
**Last Updated**: 2025-11-29  
**Maintained By**: Development Team  
**Next Review Date**: After deployment to production
