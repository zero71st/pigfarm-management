# POSPOS Product Import - Testing Guide

## ✅ Implementation Status
All tasks (T001-T005) are **COMPLETE**. The feature is ready for testing.

---

## 🎯 What to Test

### 1. Import POSPOS Products
**Endpoint:** `POST /api/feed-formulas/import`  
**Method:** No request body needed - automatically fetches from POSPOS  
**Expected:** Products imported into FeedFormula table

---

## 📋 Pre-Test Checklist

### Configuration Required
The system needs POSPOS API credentials. Check your current configuration:

**Option A: Environment Variables (launchSettings.json)**
```json
{
  "profiles": {
    "PigFarmManagement.Server": {
      "environmentVariables": {
        "Pospos__ApiBase": "https://go.pospos.co/developer/api/stock",
        "Pospos__ApiKey": "your-actual-api-key"
      }
    }
  }
}
```

**Option B: appsettings.json**
```json
{
  "Pospos": {
    "ApiBase": "https://go.pospos.co/developer/api/stock",
    "ApiKey": "your-actual-api-key"
  }
}
```

**Option C: Environment Variables (System/Terminal)**
```powershell
$env:POSPOS_API_BASE = "https://go.pospos.co/developer/api/stock"
$env:POSPOS_API_KEY = "your-actual-api-key"
```

---

## 🚀 How to Test

### Step 1: Start the Server
```powershell
cd "d:\dz Projects\PigFarmManagement"
dotnet run --project src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:61009
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Step 2: Call the Import Endpoint

**Using PowerShell:**
```powershell
Invoke-RestMethod -Method POST -Uri "http://localhost:61009/api/feed-formulas/import" -ContentType "application/json"
```

**Using curl:**
```powershell
curl -X POST http://localhost:61009/api/feed-formulas/import -H "Content-Type: application/json"
```

**Using Swagger UI:**
1. Open browser: `http://localhost:61009/swagger`
2. Find: `POST /api/feed-formulas/import`
3. Click "Try it out"
4. Click "Execute"

### Step 3: Check the Response

**Success Response (200 OK):**
```json
{
  "successCount": 42,
  "errorCount": 0,
  "skippedCount": 5,
  "errors": [],
  "importedCodes": [
    "PK64000158",
    "PK64000160",
    "..."
  ]
}
```

**Partial Success (200 OK):**
```json
{
  "successCount": 38,
  "errorCount": 4,
  "skippedCount": 5,
  "errors": [
    "Product X failed: Invalid data"
  ],
  "importedCodes": ["..."]
}
```

**Complete Failure (400 BadRequest):**
```json
{
  "successCount": 0,
  "errorCount": 42,
  "skippedCount": 0,
  "errors": [
    "Product A: Network timeout",
    "Product B: Invalid format"
  ],
  "importedCodes": []
}
```

### Step 4: Verify Database

**Option A: Check via API**
```powershell
Invoke-RestMethod -Method GET -Uri "http://localhost:61009/api/feed-formulas"
```

**Option B: Check SQLite Database**
```powershell
# Install SQLite tools if needed
# https://www.sqlite.org/download.html

sqlite3 src/server/PigFarmManagement.Server/pigfarm.db
```

```sql
-- Count imported products
SELECT COUNT(*) FROM FeedFormulas WHERE ExternalId IS NOT NULL;

-- View imported products
SELECT Id, Code, Name, Cost, CategoryName, UnitName, LastUpdate 
FROM FeedFormulas 
WHERE ExternalId IS NOT NULL
LIMIT 10;

-- Exit
.quit
```

---

## 🔍 Troubleshooting

### Issue: "POSPOS API base URL not configured"
**Cause:** Missing `Pospos:ApiBase` configuration  
**Solution:** 
1. Check `launchSettings.json` has `Pospos__ApiBase` environment variable
2. OR add to `appsettings.json`:
   ```json
   {
     "Pospos": {
       "ApiBase": "https://go.pospos.co/developer/api/stock"
     }
   }
   ```
3. OR set environment variable: `$env:POSPOS_API_BASE = "https://go.pospos.co/developer/api/stock"`

### Issue: "No API key found in request"
**Cause:** Missing API key configuration  
**Solution:**
1. Check `launchSettings.json` has `Pospos__ApiKey` environment variable
2. OR add to `appsettings.json`:
   ```json
   {
     "Pospos": {
       "ApiKey": "your-actual-api-key"
     }
   }
   ```
3. OR set environment variable: `$env:POSPOS_API_KEY = "your-api-key"`

### Issue: "Connection refused" or "Could not connect"
**Cause:** Server not running or wrong port  
**Solution:**
1. Check server is running: `dotnet run --project src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj`
2. Check the port from server output (e.g., `http://localhost:61009`)
3. Use the correct port in curl/Invoke-RestMethod

### Issue: "401 Unauthorized" or "403 Forbidden"
**Cause:** Invalid API key  
**Solution:**
1. Verify your POSPOS API key is correct
2. Check the API key has permission to access the Stock API
3. Try accessing POSPOS API directly to verify credentials

### Issue: "404 Not Found"
**Cause:** Wrong endpoint URL  
**Solution:**
1. Verify endpoint: `POST /api/feed-formulas/import` (not `/import` alone)
2. Check Swagger UI to see all available endpoints: `http://localhost:61009/swagger`

### Issue: "Rate limit exceeded"
**Note:** This is expected behavior - the client implements rate limiting (10 requests/minute)  
**Expected:** Import may take several minutes for large product catalogs  
**Behavior:** 6 seconds delay between POSPOS API calls

### Issue: All products skipped (skippedCount = total)
**Cause:** Products already imported (duplicate detection)  
**Solution:** This is expected behavior on subsequent imports - products with existing `Code` are skipped

---

## 📊 Expected Behavior

### First Import
- **successCount:** Number of new products imported
- **errorCount:** 0 (if POSPOS API is healthy)
- **skippedCount:** 0 (no duplicates yet)
- **importedCodes:** List of product codes imported

### Subsequent Imports
- **successCount:** Number of new products (if any)
- **errorCount:** 0
- **skippedCount:** Number of existing products (duplicate detection)
- **importedCodes:** Only new products

### Import with Errors
- **successCount:** Products successfully imported
- **errorCount:** Number of products that failed
- **skippedCount:** Duplicates
- **errors:** List of error messages
- **importedCodes:** Successfully imported products

---

## 📝 Validation Checklist

After testing, verify:

- [ ] Server starts without errors
- [ ] Endpoint `/api/feed-formulas/import` exists in Swagger UI
- [ ] Import returns 200 OK with valid response
- [ ] Database contains imported products (`SELECT * FROM FeedFormulas WHERE ExternalId IS NOT NULL`)
- [ ] Products have expected fields: Code, Name, Cost, CategoryName, UnitName, LastUpdate
- [ ] Duplicate products are skipped on re-import
- [ ] Rate limiting works (6 seconds between POSPOS API calls)
- [ ] Errors are logged to console/log file

---

## 🎉 Success Criteria

The feature is working correctly if:

1. ✅ Server starts successfully
2. ✅ POST `/api/feed-formulas/import` returns 200 OK
3. ✅ Response shows `successCount > 0`
4. ✅ Database contains products with POSPOS data
5. ✅ Re-importing skips duplicates (shows `skippedCount`)
6. ✅ Products visible in database with correct data mapping

---

## 📚 Additional Resources

- **VALIDATION_REPORT.md** - Technical validation details
- **QUICK_REFERENCE.md** - API usage guide
- **plan.md** - Implementation plan
- **spec.md** - Feature specification
- **data-model.md** - Data structure details

---

## 🔗 API Documentation

Once server is running, access:
- **Swagger UI:** `http://localhost:61009/swagger`
- **OpenAPI JSON:** `http://localhost:61009/swagger/v1/swagger.json`

---

## Next Steps After Successful Test

1. ✅ Verify products imported correctly
2. Document any issues found during testing
3. Consider implementing remaining tasks:
   - T006-T008: Transaction integration
   - T009: UI updates
   - T010-T011: Performance and documentation

---

**Last Updated:** October 2, 2025  
**Status:** Ready for Testing
