# POSPOS Product Import - Quick Reference Guide

## Overview
Import feed formula products from POSPOS API into the PigFarmManagement system.

---

## Configuration

### Option 1: appsettings.json
```json
{
  "Pospos": {
    "ApiBase": "https://api.pospos.com/products",
    "ApiKey": "your-api-key-here"
  }
}
```

### Option 2: Environment Variables
```bash
POSPOS_API_BASE=https://api.pospos.com/products
POSPOS_API_KEY=your-api-key-here
```

---

## API Endpoint

### Import All Products
```http
POST /api/feed-formulas/import
Content-Type: application/json
```

**No request body required** - automatically fetches all products from POSPOS.

### Response Format
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

### HTTP Status Codes
- `200 OK` - Success (full or partial)
- `400 Bad Request` - All imports failed
- `500 Internal Server Error` - Unexpected error

---

## Usage Examples

### cURL
```bash
# Import all products
curl -X POST http://localhost:5000/api/feed-formulas/import

# With output formatting
curl -X POST http://localhost:5000/api/feed-formulas/import | jq
```

### PowerShell
```powershell
# Import all products
Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/feed-formulas/import"

# Save response to variable
$result = Invoke-RestMethod -Method POST -Uri "http://localhost:5000/api/feed-formulas/import"
Write-Host "Imported: $($result.successCount)"
Write-Host "Skipped: $($result.skippedCount)"
Write-Host "Errors: $($result.errorCount)"
```

### C# HttpClient
```csharp
var client = new HttpClient();
var response = await client.PostAsync(
    "http://localhost:5000/api/feed-formulas/import", 
    null);

var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>();
Console.WriteLine($"Imported: {result.SuccessCount}");
Console.WriteLine($"Skipped: {result.SkippedCount}");
```

---

## Features

### Duplicate Detection
- Products with existing `Code` are automatically skipped
- Case-insensitive comparison
- Reports skipped count in response

### Rate Limiting
- Maximum 10 requests per minute to POSPOS API
- Automatic throttling (6 seconds between requests)
- Prevents API rate limit violations

### Error Handling
- Per-product error tracking
- Import continues even if individual products fail
- Detailed error messages in response

### Field Mapping
| POSPOS Field | FeedFormula Field | Notes |
|--------------|-------------------|-------|
| `_id` | `ExternalId` | Converted to Guid via MD5 hash |
| `code` | `Code` | Primary identifier |
| `name` | `Name` | Product name |
| `cost` | `Cost` | Product cost |
| `category.name` | `CategoryName` | Product category |
| `unit.name` | `UnitName` | Unit of measurement |
| `lastupdate` | `LastUpdate` | Last modified date |
| - | `ConsumeRate` | Set to null (user input) |

---

## Typical Workflow

### Initial Import
1. Configure POSPOS API credentials
2. Call import endpoint: `POST /api/feed-formulas/import`
3. Review import statistics
4. Check for any errors in response
5. Verify imported products in database

### Subsequent Imports
1. Call import endpoint again: `POST /api/feed-formulas/import`
2. Existing products are automatically skipped
3. Only new products are imported
4. Review `skippedCount` to see how many already existed

---

## Troubleshooting

### Issue: "POSPOS API base URL not configured"
**Solution:** Set `Pospos:ApiBase` in appsettings.json or `POSPOS_API_BASE` environment variable

### Issue: "Failed to connect to POSPOS API"
**Causes:**
- Invalid API base URL
- Network connectivity issues
- POSPOS API is down

**Solution:** 
- Verify API base URL is correct
- Check network connectivity
- Test POSPOS API directly

### Issue: "POSPOS API rate limit exceeded"
**Solution:** 
- Wait 1 minute before retrying
- Rate limiter automatically prevents this, but may occur if multiple instances are running

### Issue: All imports show as "skipped"
**Cause:** Products already exist in database

**Solution:** This is expected behavior - duplicates are prevented by design

### Issue: Some products fail to import
**Check:**
- Review `errors` array in response for specific error messages
- Verify POSPOS product data has required fields (especially `code`)
- Check logs for detailed error information

---

## Monitoring

### Log Messages
```
[Information] Starting POSPOS product import
[Information] Fetched {Count} products from POSPOS
[Debug] Product {Code} already exists, skipping
[Debug] Imported product {Code}: {Name}
[Information] POSPOS import completed: {Success} imported, {Skipped} skipped, {Errors} errors
[Error] Error importing product {Code}: {Message}
```

### Database Queries
```sql
-- Count imported products
SELECT COUNT(*) FROM FeedFormulas WHERE ExternalId IS NOT NULL;

-- View recently imported products
SELECT Code, Name, CategoryName, Cost, CreatedAt 
FROM FeedFormulas 
WHERE ExternalId IS NOT NULL 
ORDER BY CreatedAt DESC;

-- Check for duplicates
SELECT Code, COUNT(*) 
FROM FeedFormulas 
GROUP BY Code 
HAVING COUNT(*) > 1;
```

---

## Performance

### Estimated Time
- **100 products:** ~1-2 minutes (rate limiting: 10 requests/min)
- **1000 products:** ~10-20 minutes
- **Rate:** Approximately 10 products per minute (due to API rate limit)

### Optimization
- Import runs as single database transaction
- Duplicate detection uses HashSet (O(1) lookups)
- Pagination minimizes memory usage

---

## Security

### API Key Storage
- ✅ **GOOD:** appsettings.json or environment variables
- ❌ **BAD:** Hardcoded in source code
- ❌ **BAD:** Committed to version control

### Recommendations
- Use environment variables in production
- Rotate API keys regularly
- Monitor API usage
- Use HTTPS for POSPOS API calls

---

## Support

### Common Questions

**Q: Can I import a specific product by code?**  
A: Not currently. The endpoint imports all products. Use `GetProductByCodeAsync` in PosposProductClient for single product lookup (for future implementation).

**Q: Can I update existing products?**  
A: Not currently. Existing products are skipped. Product updates are planned for future release.

**Q: How often should I run imports?**  
A: Depends on how frequently POSPOS products change. Daily or weekly imports are typical.

**Q: Can I schedule automatic imports?**  
A: Not built-in. Use external scheduler (cron, Task Scheduler, Azure Functions) to call the endpoint periodically.

**Q: What happens if import fails halfway?**  
A: Database transaction ensures all-or-nothing for the batch. Either all successfully imported products are saved, or none are.

---

## Related Endpoints

### Get All Feed Formulas
```http
GET /api/feed-formulas
```

### Get Feed Formula by ID
```http
GET /api/feed-formulas/{id}
```

### Check if Feed Formula Exists
```http
GET /api/feed-formulas/exists/{code}
```

---

## Version History

### v1.0.0 (October 2, 2025)
- Initial implementation
- Import all products from POSPOS
- Duplicate detection
- Rate limiting
- Error handling

### Planned Features
- Incremental import (only new/updated products)
- Product update logic
- Import from JSON file
- Webhook support
- Import history tracking

---

## Contact & Support

For issues or questions:
1. Check this guide
2. Review VALIDATION_REPORT.md
3. Check application logs
4. Review tasks.md and plan.md in specs/005-i-want-to/
