# Manual Testing Guide: POSPOS Product Import Button

## ✅ Implementation Complete

**Feature:** Import POSPOS products via UI button  
**Location:** Feed Formula Management Page  
**Date:** October 2, 2025

---

## 🎯 What Was Added

### 1. **Client Service** (`FeedFormulaService.cs`)
- ✅ Added `ImportResultResponse` DTO
- ✅ Added `ImportFromPosposAsync()` method to interface
- ✅ Implemented method to call `POST /api/feed-formulas/import`

### 2. **UI Button** (`FeedFormulaManagement.razor`)
- ✅ Added "Import from POSPOS" button (green, with cloud download icon)
- ✅ Shows loading spinner during import
- ✅ Button disabled during import operation
- ✅ Positioned next to "Add New Feed Formula" button

### 3. **Import Logic** (`FeedFormulaManagement.razor @code`)
- ✅ `ImportFromPospos()` method added
- ✅ Progress indicator (`importingFromPospos` state)
- ✅ Detailed success/error notifications
- ✅ Automatic page refresh after import

---

## 📋 Manual Testing Steps

### Prerequisites
1. **POSPOS API Configuration**
   - Check `launchSettings.json` or `appsettings.json`
   - Required: `Pospos__ApiBase` or `Pospos__StockApiBase`
   - Required: `Pospos__ApiKey`

2. **Database**
   - SQLite database ready: `src/server/PigFarmManagement.Server/pigfarm.db`
   - Migration applied: `20251002044944_AddPOSPOSFieldsToFeedFormula`

### Test Case 1: Start the Application

**Step 1: Start Server**
```powershell
cd "d:\dz Projects\PigFarmManagement"
dotnet run --project src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj
```

**Expected:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:61009
```

**Step 2: Start Client (if not using Blazor WebAssembly)**
```powershell
# If separate client needed
dotnet run --project src/client/PigFarmManagement.Client/PigFarmManagement.Client.csproj
```

**Step 3: Open Browser**
- Navigate to: `http://localhost:61009` (or the port shown in console)
- Or: `http://localhost:7000` (if client runs separately)

---

### Test Case 2: Navigate to Feed Formula Page

**Steps:**
1. Open the application in browser
2. Click on **"Feed Formula Management"** in navigation menu
3. Look for the page title: **"Feed Formula Management"**

**Expected:**
- ✅ Page loads successfully
- ✅ Shows existing feed formulas (if any)
- ✅ Shows statistics cards (Total, เจ็ท, เพียว)
- ✅ Shows "Add New Feed Formula" button
- ✅ Shows **"Import from POSPOS"** button (green, with cloud icon)

**Screenshot Location:**
```
Look for:
- Statistics Cards at top
- Button row with:
  - "Add New Feed Formula" (blue)
  - "Import from POSPOS" (green) ← NEW BUTTON
```

---

### Test Case 3: Click Import Button (First Import)

**Steps:**
1. Click the **"Import from POSPOS"** button (green button with cloud icon)
2. Observe the button state
3. Wait for completion

**Expected Behavior:**

**During Import:**
- ✅ Button shows "Importing..." text
- ✅ Loading spinner appears on button
- ✅ Button is disabled (cannot click again)
- ✅ Notification appears: "Starting import from POSPOS... This may take a few minutes."

**After Import (Success):**
- ✅ Green notification: "✅ Successfully imported X products"
- ✅ Summary notification: "Import complete: X imported, 0 skipped, 0 errors"
- ✅ Page automatically refreshes
- ✅ Feed formulas list updates with new products
- ✅ Statistics cards update (Total count increases)
- ✅ Button returns to normal "Import from POSPOS" state

**Expected Duration:**
- **Small catalog (< 50 products):** 30 seconds - 2 minutes
- **Large catalog (100+ products):** 5-10 minutes
- Rate limited: 10 requests/minute (6 seconds per product)

---

### Test Case 4: Click Import Button (Second Import - Duplicates)

**Steps:**
1. Wait for first import to complete
2. Click **"Import from POSPOS"** button again
3. Wait for completion

**Expected Behavior:**

**After Import (Duplicates):**
- ✅ Orange notification: "⚠️ Skipped X duplicate products"
- ✅ Summary: "Import complete: 0 imported, X skipped, 0 errors"
- ✅ No duplicate products added to database
- ✅ Statistics remain the same

**Purpose:**
- Verifies duplicate detection works
- Products with existing `Code` are skipped

---

### Test Case 5: Verify Imported Data

**Step 1: Check UI Table**
1. Scroll through feed formulas table
2. Look for new products

**Expected:**
- ✅ New rows appear in table
- ✅ Each row shows:
  - Product Code (e.g., "PK64000158")
  - Product Name (e.g., "เจ็ท 105 หมูเล็ก 6-15 กก.")
  - Brand/Category
  - Updated timestamp

**Step 2: Check Database (Optional)**
```powershell
# In PowerShell
sqlite3 src/server/PigFarmManagement.Server/pigfarm.db
```

```sql
-- Count imported products
SELECT COUNT(*) FROM FeedFormulas WHERE ExternalId IS NOT NULL;

-- View imported products
SELECT 
    Code, 
    Name, 
    Cost, 
    CategoryName, 
    UnitName, 
    LastUpdate 
FROM FeedFormulas 
WHERE ExternalId IS NOT NULL
LIMIT 10;

-- Exit
.quit
```

**Expected:**
- ✅ Products have `ExternalId` (not NULL)
- ✅ Products have POSPOS fields: Code, Name, Cost, CategoryName, UnitName
- ✅ LastUpdate timestamp matches POSPOS data

---

### Test Case 6: Error Handling

**Test 6.1: API Unreachable**
1. Stop the server (Ctrl+C)
2. In browser, click "Import from POSPOS"
3. Observe error

**Expected:**
- ✅ Red notification: "Network error: ... Please check your connection and try again."
- ✅ Button returns to normal state
- ✅ No crash

**Test 6.2: Invalid API Key**
1. Edit `launchSettings.json` - change `Pospos__ApiKey` to invalid value
2. Restart server
3. Click "Import from POSPOS"

**Expected:**
- ✅ Error notification shows
- ✅ May show: "Import failed: ..." or HTTP error
- ✅ Button returns to normal state

**Test 6.3: Network Timeout**
1. Disconnect internet
2. Click "Import from POSPOS"

**Expected:**
- ✅ Timeout error after ~30 seconds
- ✅ Clear error message
- ✅ Button returns to normal state

---

## ✅ Success Criteria Checklist

After manual testing, verify:

- [ ] **Button Visibility**
  - [ ] "Import from POSPOS" button appears on Feed Formula page
  - [ ] Button has green color and cloud download icon
  - [ ] Button positioned next to "Add New Feed Formula"

- [ ] **Button Behavior**
  - [ ] Clicking button triggers import
  - [ ] Button shows "Importing..." text during import
  - [ ] Loading spinner appears during import
  - [ ] Button is disabled during import
  - [ ] Button returns to normal after completion

- [ ] **Import Functionality**
  - [ ] First import successfully adds products
  - [ ] Products appear in feed formulas table
  - [ ] Statistics cards update with new counts
  - [ ] Second import skips duplicates (no new products added)

- [ ] **Notifications**
  - [ ] "Starting import..." notification appears
  - [ ] Success notification shows count: "Successfully imported X products"
  - [ ] Duplicate notification shows: "Skipped X duplicate products"
  - [ ] Summary notification appears with totals
  - [ ] Error notifications appear on failure

- [ ] **Data Validation**
  - [ ] Imported products have correct data (Code, Name, Cost, etc.)
  - [ ] Products have ExternalId field populated
  - [ ] Products have CategoryName (from POSPOS category.name)
  - [ ] Products have UnitName (from POSPOS unit.name)
  - [ ] LastUpdate timestamp is recent

- [ ] **Error Handling**
  - [ ] Network errors show user-friendly message
  - [ ] API errors show appropriate notification
  - [ ] Button returns to normal state after error
  - [ ] Application doesn't crash on error

- [ ] **Page Refresh**
  - [ ] Feed formulas list refreshes after import
  - [ ] Statistics update automatically
  - [ ] No manual page refresh needed

---

## 🐛 Troubleshooting

### Button Not Visible
**Issue:** Can't find "Import from POSPOS" button  
**Solution:**
1. Check you're on Feed Formula Management page (`/feed-formulas`)
2. Look in button row below statistics cards
3. Button is green with cloud icon, next to "Add New Feed Formula"

### Button Doesn't Work
**Issue:** Clicking button does nothing  
**Solution:**
1. Open browser DevTools (F12)
2. Check Console tab for JavaScript errors
3. Check Network tab - look for POST to `/api/feed-formulas/import`
4. Verify server is running

### Import Takes Too Long
**Issue:** Import running for 10+ minutes  
**Expected:** This is normal for large catalogs (100+ products)  
**Reason:** Rate limiting (10 requests/minute = 6 seconds per product)  
**Solution:** Wait patiently, or check server logs for progress

### "Network error" Message
**Issue:** Import fails with network error  
**Solutions:**
1. Check server is running: `dotnet run --project src/server/...`
2. Check server port matches client API base URL
3. Check firewall settings
4. Check internet connection (needed for POSPOS API)

### No Products Imported
**Issue:** Import succeeds but no new products  
**Possible Causes:**
1. **Duplicates:** Products already imported (check "skipped" count)
2. **API Config:** POSPOS API key invalid or endpoint wrong
3. **API Response:** POSPOS API returning empty result

**Debug Steps:**
1. Check notification for "skipped" count
2. Check server logs for POSPOS API calls
3. Verify `Pospos__ApiBase` or `Pospos__StockApiBase` is correct
4. Verify `Pospos__ApiKey` is valid

### Imported Data Looks Wrong
**Issue:** Products have missing or incorrect data  
**Solutions:**
1. Check POSPOS API response format matches expected structure
2. Verify field mapping in server `FeedFormulaService.cs`
3. Check database schema matches entity model

---

## 📸 Expected UI Screenshots

### Before Import
```
┌─────────────────────────────────────────────────────────┐
│ 🧪 Feed Formula Management                              │
├─────────────────────────────────────────────────────────┤
│ [Statistics Cards: Total, เจ็ท, เพียว]                 │
├─────────────────────────────────────────────────────────┤
│ [Filter] [Quick Filters] [Clear] [Refresh]             │
│ [➕ Add New Feed Formula] [☁️ Import from POSPOS]       │ ← NEW
├─────────────────────────────────────────────────────────┤
│ Table: (Existing feed formulas)                         │
└─────────────────────────────────────────────────────────┘
```

### During Import
```
┌─────────────────────────────────────────────────────────┐
│ [➕ Add New Feed Formula] [⏳ Importing... ]             │ ← Loading
└─────────────────────────────────────────────────────────┘
```

### After Import (Success)
```
┌─────────────────────────────────────────────────────────┐
│ ✅ Successfully imported 42 products                     │
│ Import complete: 42 imported, 0 skipped, 0 errors       │
├─────────────────────────────────────────────────────────┤
│ Table: (42 new products added)                          │
└─────────────────────────────────────────────────────────┘
```

---

## 📝 Test Results Template

Copy this template to record your test results:

```markdown
# POSPOS Import Button - Test Results

**Tester:** [Your Name]
**Date:** [Date]
**Environment:** Development / Staging / Production

## Test Results

### Button Visibility
- [ ] Button appears on Feed Formula page: YES / NO
- [ ] Button has correct styling (green, cloud icon): YES / NO
- [ ] Button positioned correctly: YES / NO

### Import Functionality
- [ ] First import successful: YES / NO
  - Products imported: _____ 
  - Duration: _____ seconds
- [ ] Second import skips duplicates: YES / NO
  - Products skipped: _____

### User Experience
- [ ] Loading indicator works: YES / NO
- [ ] Notifications clear and helpful: YES / NO
- [ ] Page refreshes automatically: YES / NO

### Errors Encountered
- [ ] Error 1: [Description]
  - Severity: Critical / Major / Minor
  - Resolved: YES / NO
- [ ] Error 2: [Description]

### Overall Assessment
- [ ] PASS - Ready for production
- [ ] PASS WITH ISSUES - Note issues below
- [ ] FAIL - Needs fixes

**Notes:**
[Add any additional observations or issues]
```

---

## 🎉 Completion Criteria

The feature is **complete and ready** when:

1. ✅ Button visible and properly styled
2. ✅ Import successfully fetches products from POSPOS
3. ✅ Products saved to database with correct data
4. ✅ Duplicate detection works (second import skips existing)
5. ✅ User receives clear feedback (notifications)
6. ✅ Errors handled gracefully
7. ✅ Page refreshes automatically after import
8. ✅ No console errors or warnings

---

**Next Steps After Testing:**
1. Document any issues found
2. Fix any critical bugs
3. Consider adding:
   - Import progress bar (X of Y products)
   - Import history log
   - Ability to import specific products by code
   - Scheduled automatic imports

---

**Implementation Files:**
- `/src/client/.../Services/FeedFormulaService.cs` - Client service
- `/src/client/.../Pages/FeedFormulaManagement.razor` - UI button and logic
- `/src/server/.../Features/FeedFormulas/FeedFormulaEndpoints.cs` - API endpoint (already implemented)
- `/src/server/.../Features/FeedFormulas/FeedFormulaService.cs` - Business logic (already implemented)

**Documentation:**
- `/specs/005-i-want-to/TESTING_GUIDE.md` - API testing guide
- `/specs/005-i-want-to/VALIDATION_REPORT.md` - Technical validation
- `/specs/005-i-want-to/QUICK_REFERENCE.md` - API reference

---

**Last Updated:** October 2, 2025  
**Status:** ✅ Ready for Manual Testing
