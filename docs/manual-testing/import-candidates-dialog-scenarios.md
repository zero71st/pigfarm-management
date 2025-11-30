# Manual Validation Scenarios: ImportCandidatesDialog Component

**Feature**: 012-update-search-customer  
**Date**: 2025-11-29  
**Purpose**: Manual test scenarios for ImportCandidatesDialog UI enhancements

---

## Scenario A: Dialog Opens with Default (All Members)

**Precondition**: 3+ POSPOS members exist in system

**Test Steps**:
1. Navigate to admin dashboard (or relevant page containing ImportCandidatesDialog)
2. Trigger ImportCandidatesDialog to open (via button/menu)
3. Observe initial load

**Verify**:
- [ ] Dialog renders without errors
- [ ] Members table displays and is loading
- [ ] Select-all checkbox is visible in table header
- [ ] Select-all checkbox is clickable/enabled
- [ ] Members display in table rows
- [ ] Individual row checkboxes are visible
- [ ] API called with default source (or source=all)

**Result**: _____ (Pass/Fail)  
**Notes**:
```

```

---

## Scenario B: Load All Members (source=all)

**Precondition**: 3+ POSPOS members exist in system; dialog is open

**Test Steps**:
1. If UI has "All Members" button, click it (otherwise this is default state)
2. Observe API call and table refresh

**Verify**:
- [ ] API called with `?source=all` or no source parameter
- [ ] Table displays all POSPOS members (all 3+)
- [ ] Members sorted correctly (if visible: newest first by CreatedAt)
- [ ] Select-all checkbox visible in table header
- [ ] Select-all checkbox enabled and clickable
- [ ] No error messages displayed

**Result**: _____ (Pass/Fail)  
**Notes**:
```

```

---

## Scenario C: Load Latest Member (source=pospos)

**Precondition**: 3+ POSPOS members exist in system; dialog is open; newest member is identifiable

**Test Steps**:
1. If UI has "Latest Member" button, click it (or if implemented in menu/dropdown)
2. Observe API call and table refresh
3. Verify only newest member displays

**Verify**:
- [ ] API called with `?source=pospos`
- [ ] Table displays exactly 1 member (the most recently created)
- [ ] Displayed member is the newest one (by CreatedAt timestamp)
- [ ] Select-all checkbox is hidden/not visible in header
- [ ] Individual row checkbox still visible for the single member
- [ ] No error messages displayed

**Result**: _____ (Pass/Fail)  
**Notes**:
```

```

---

## Scenario D: Individual Selection Works (Both Modes)

**Precondition**: Dialog open with members displayed (either all or latest)

**Test Steps**:
1. Click checkbox for 1 member row (anywhere in table)
2. Observe row highlights/changes visual state
3. Click checkbox for another member (if available)

**Verify**:
- [ ] Clicked row checkbox toggles selected state (visual feedback)
- [ ] Row highlighting/styling indicates selection
- [ ] Multiple members can be selected independently
- [ ] Individual selection works in source=all mode
- [ ] Individual selection works in source=pospos mode
- [ ] "Import Selected" button becomes enabled when ≥1 member selected

**Result**: _____ (Pass/Fail)  
**Notes**:
```

```

---

## Scenario E: Selection State Clears on Close/Reopen

**Precondition**: Dialog open; 1-2 members selected

**Test Steps**:
1. Select 1-2 members (checkboxes checked, rows highlighted)
2. Note the selected members
3. Close dialog (via X button or Cancel button)
4. Reopen dialog immediately

**Verify**:
- [ ] Dialog closes without error
- [ ] Dialog reopens successfully
- [ ] No members are pre-selected (all checkboxes unchecked)
- [ ] Previously selected members are no longer highlighted
- [ ] Selection state is cleared (session-scoped as designed)

**Result**: _____ (Pass/Fail)  
**Notes**:
```

```

---

## Scenario F: POSPOS API Failure Shows Distinct Error

**Precondition**: Dialog open; POSPOS service can be simulated as down/unavailable

**Setup** (if manual):
- Stop POSPOS API or simulate network failure
- Or use mock/test endpoint returning 503

**Test Steps**:
1. Attempt to load members (click "Load" or reload dialog)
2. Observe error response from POSPOS

**Verify**:
- [ ] Error message snackbar appears
- [ ] Message text is exactly: "POSPOS service unavailable. Please try again later."
- [ ] Message is distinct from other error types (not generic "Failed to load")
- [ ] Message guides user to retry (implies temporary issue)
- [ ] Table remains empty or shows previous results
- [ ] User can dismiss error and retry

**Result**: _____ (Pass/Fail)  
**Notes**:
```

```

---

## Scenario G: Empty Results UX (No POSPOS Members)

**Precondition**: POSPOS has no members OR members filtered to empty result

**Test Steps**:
1. Trigger LoadCandidates with no members available
2. Observe empty state rendering

**Verify**:
- [ ] Table displays empty state (no error, no confusion)
- [ ] Message shown: "No candidates found" or similar
- [ ] Select-all checkbox hidden (no false affordance)
- [ ] No error messages (empty is valid state)
- [ ] Individual row checkboxes not shown (no members to check)
- [ ] "Import Selected" button disabled

**Result**: _____ (Pass/Fail)  
**Notes**:
```

```

---

## Scenario H: Invalid Source Parameter (API Test)

**Precondition**: Manual API testing capability (Postman, curl, or browser DevTools)

**Test Steps**:
1. Call endpoint directly: `GET /api/customers/import/candidates?source=invalid`
2. Include valid X-Api-Key header
3. Observe response

**Verify**:
- [ ] Response HTTP status: 400 Bad Request
- [ ] Response body contains error field
- [ ] Error message: "Invalid source. Must be 'pospos' or 'all'."
- [ ] Response is valid JSON
- [ ] No server error logs (expected client error)

**Result**: _____ (Pass/Fail)  
**Notes**:
```

```

---

## Additional Observations

**Visual/UX Notes**:
- [ ] Checkbox alignment correct and visible
- [ ] Column widths appropriate for data
- [ ] Table responsive on different screen sizes
- [ ] No console errors in browser DevTools
- [ ] No JavaScript exceptions thrown

**Performance**:
- [ ] API response time acceptable (< 1 second)
- [ ] UI responsive (no freezing on table render)
- [ ] Selection/deselection instant (no lag)

**Accessibility**:
- [ ] Keyboard navigation works (Tab, Enter)
- [ ] Screen reader announces table structure
- [ ] Color not only indicator of state (visual + other cue)

---

## Sign-Off

**Manual Scenarios Testing**:
- [ ] All scenarios A-H tested and results recorded (date: ____________)
- [ ] Tested by (QA/Implementer): ____________
- [ ] Overall result: _____ (All Pass / Some Fail / All Fail)

**Issues Found**:
```

```

**Approved for Deployment**: [ ] Yes / [ ] No (if No, explain above)

---

**Document**: Manual validation scenarios for T002  
**Task**: T002 (Create manual validation scenarios)  
**Status**: Created 2025-11-29
