# API Contracts: Thai Language UI Conversion

**Date**: 2025-11-30 | **Feature**: 013-change-ui-to

## Summary

**No API contract changes required.**

This feature is a **client-side UI translation only**. All backend endpoints remain unchanged:
- Same request/response DTOs
- Same HTTP methods and routes
- Same status codes
- Same validation rules (only error messages change client-side)

---

## Backend API Behavior

### Unchanged Endpoints

All existing endpoints in `src/server/Features/` continue to:
1. Accept requests in the same format
2. Return responses in the same format
3. Use English for technical error messages (per user requirement)
4. Log in English (per user requirement)

**Example**: Customer Create Endpoint

**Before and After (Identical)**:
```
POST /api/customers
Content-Type: application/json

Request:
{
  "displayName": "วัดสมาน ฟาร์ม",
  "contactName": "นายสมชาย",
  "phone": "081-234-5678"
}

Response (200 OK):
{
  "id": "guid",
  "displayName": "วัดสมาน ฟาร์ม",
  "contactName": "นายสมชาย",
  "phone": "081-234-5678",
  "createdAt": "2025-11-30T10:30:00Z"
}
```

**Note**: 
- Request/response data can contain Thai text (customer names, addresses, etc.)
- This was ALREADY supported (Unicode strings)
- No code changes needed for Thai data handling

---

## Client-Side Changes

### Modified: Validation Error Display

**Backend Response** (Unchanged):
```json
// 400 Bad Request
{
  "errors": {
    "DisplayName": ["The DisplayName field is required."]
  }
}
```

**Client Handling** (Modified):
- Blazor components display validation from DataAnnotations attributes
- Attributes now have Thai error messages: `[Required(ErrorMessage = "กรุณาระบุชื่อ")]`
- Server-side errors still in English (as designed)
- Client-side validation shows Thai before submission

**Result**: User sees Thai validation messages for client-side checks, English for server-side technical errors.

---

## Contract Testing

**No new contract tests required** because:
1. No endpoint changes
2. No request/response schema changes
3. Existing contract tests remain valid

**Existing tests** (in `tests/contract/` or similar) still validate:
- Endpoint routes
- HTTP methods
- Request/response formats
- Status codes
- Authorization requirements

---

## Integration Points

### External APIs (POSPOS, etc.)

**Status**: Unchanged

- POSPOS API calls remain in English
- External API responses processed identically
- No translation layer needed (backend handles integration, not UI)

### Frontend-Backend Communication

**Status**: Unchanged

- HttpClient calls use same endpoints
- JSON serialization/deserialization identical
- Error handling preserves English technical messages for logging

---

## Documentation

**API Documentation** (Swagger/OpenAPI):
- Remains in English (developer-facing documentation)
- No schema changes to document
- Endpoint descriptions unchanged

**User-Facing Documentation**:
- Out of scope per user requirement ("keep documents in English")

---

## Contract Artifacts

**No contract files generated** for this feature because:
- Zero API surface area changes
- Pure UI/presentation modification
- Backend contracts already documented in other features

**See instead**:
- Existing feature specs (`001-title-import-customer/`, `008-update-manage-customer/`, etc.)
- Those features contain the API contracts for customer, pig pen, feed endpoints
- This feature only changes how those APIs are consumed visually

---

## Summary Table

| Contract Type | Status | Location |
|--------------|--------|----------|
| REST Endpoints | Unchanged | See existing feature specs |
| Request DTOs | Unchanged | `src/shared/DTOs/*.cs` |
| Response DTOs | Unchanged | `src/shared/DTOs/*.cs` |
| Error Responses | Unchanged | Backend returns English technical errors |
| WebSocket/SignalR | N/A | Not used in this project |
| GraphQL Schema | N/A | Not used in this project |

**Conclusion**: No contract documentation needed beyond this note explaining "no changes".
