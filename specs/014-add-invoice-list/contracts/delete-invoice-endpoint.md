# API Contract: Delete Invoice by Reference Code

## Endpoint
```
DELETE /api/pigpens/{pigPenId}/invoices/{invoiceReferenceCode}
```

## Purpose
Delete all feed items associated with a specific invoice reference code for a given pig pen.

## Request Parameters

### Path Parameters
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `pigPenId` | `Guid` | Yes | The unique identifier of the pig pen |
| `invoiceReferenceCode` | `string` | Yes | The invoice reference code to delete (from InvoiceReferenceCode field) |

### Headers
| Header | Value | Required | Description |
|--------|-------|----------|-------------|
| `X-Api-Key` | `string` | Yes | API key for authentication |

## Response

### Success Response (200 OK)
```json
{
  "deletedCount": 5,
  "invoiceReferenceCode": "INV-001",
  "message": "Successfully deleted 5 feed items for invoice INV-001"
}
```

**Schema**:
```csharp
public record DeleteInvoiceResponse(
    int DeletedCount,
    string InvoiceReferenceCode,
    string Message
);
```

### Error Responses

#### 404 Not Found
```json
{
  "error": "No feed items found with invoice reference code 'INV-001' for pig pen '{pigPenId}'"
}
```

#### 400 Bad Request
```json
{
  "error": "Invoice reference code cannot be null or empty"
}
```

#### 401 Unauthorized
```json
{
  "error": "Unauthorized"
}
```

## Business Rules

1. **Authorization**: Requires valid API key in `X-Api-Key` header
2. **Pig Pen Validation**: Pig pen must exist (checked implicitly by foreign key constraint)
3. **Invoice Reference Validation**: Must not be null, empty, or whitespace
4. **Deletion Scope**: Deletes ALL FeedEntity records where:
   - `PigPenId` = `{pigPenId}` AND
   - `InvoiceReferenceCode` = `{invoiceReferenceCode}`
5. **Idempotency**: Returns 404 if no items found (already deleted or never existed)
6. **Transaction**: All items deleted in a single database transaction (atomic operation)

## Side Effects

1. **Feed Summary Recalculation**: Deleting feed items affects pig pen feed totals
2. **Feed Formula Comparison**: May impact feed formula comparison data
3. **Client State**: Both Invoice Management and Feed History tabs must refresh after deletion

## Implementation Notes

- Uses `FeedRepository.DeleteByInvoiceReferenceAsync()` method
- Follows existing minimal API pattern in `PigPenEndpoints.cs`
- Should log deletion with count and invoice reference for audit trail
- No soft delete - permanent removal from database

## Example Usage

### Request
```http
DELETE /api/pigpens/123e4567-e89b-12d3-a456-426614174000/invoices/INV-20231115-001 HTTP/1.1
Host: localhost:5000
X-Api-Key: your-api-key-here
```

### Response
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "deletedCount": 8,
  "invoiceReferenceCode": "INV-20231115-001",
  "message": "Successfully deleted 8 feed items for invoice INV-20231115-001"
}
```

## Database Impact

**Query Pattern**:
```sql
DELETE FROM Feeds 
WHERE PigPenId = @pigPenId 
  AND InvoiceReferenceCode = @invoiceReferenceCode
```

**Expected Performance**: <50ms for typical invoice (5-10 items)

## Related Contracts

- GET `/api/pigpens/{id}/feeds` - Returns all feed items (includes InvoiceReferenceCode)
- POST `/api/feeds/import` - Existing import endpoint that populates InvoiceReferenceCode
