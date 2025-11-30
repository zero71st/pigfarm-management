# Import API Reference

This document describes the server import endpoints used to import customers from the external POSPOS API.

Base path: `/import/customers`

---

## GET /import/customers/candidates
Fetch a list of candidate members from the POSPOS API and return as a simplified candidate DTO.

**Query Parameters** (Optional):
- `source` (string): Filter context for candidates
  - `pospos`: Return only the latest POSPOS member (newest by CreatedAt, then Id)
  - `all`: Return all available POSPOS members (default if omitted)
  - Default: `all` (backward compatible)

**Response**: 200 OK  
**Content-Type**: application/json

**Sample item**:
```json
{
  "id": "600d4ad69412705f72ae7b92",
  "code": "M000123",
  "firstName": "Somchai",
  "lastName": "Srisuk",
  "phone": "0812345678",
  "email": "somchai@example.com",
  "address": "123 Moo 4",
  "externalId": "600d4ad69412705f72ae7b92",
  "createdAt": "2021-01-24T10:24:22.000Z"
}
```

**Examples**:
```bash
# Get all candidates (default)
curl "https://pigfarm.example.com/import/customers/candidates" \
  -H "X-Api-Key: YOUR_API_KEY"

# Get all candidates (explicit)
curl "https://pigfarm.example.com/import/customers/candidates?source=all" \
  -H "X-Api-Key: YOUR_API_KEY"

# Get latest candidate only
curl "https://pigfarm.example.com/import/customers/candidates?source=pospos" \
  -H "X-Api-Key: YOUR_API_KEY"
```

**Response Status Codes**:
- `200 OK`: Success (returns array, may be empty)
- `400 Bad Request`: Invalid source parameter (e.g., `?source=invalid`)
- `401 Unauthorized`: Missing or invalid X-Api-Key header
- `500 Internal Server Error`: Unexpected server error
- `503 Service Unavailable`: POSPOS API unreachable (distinct from other errors)

**Error Response** (invalid source):
```json
{
  "message": "Invalid source. Must be 'pospos' or 'all'."
}
```

**Error Response** (POSPOS service down):
```json
{
  "message": "POSPOS service unavailable. Please try again later."
}
```

**Notes**:
- Server maps POSPOS response fields to `CandidateMember` DTO.
- When `source=pospos`: Returns 0-1 items (latest member only, ordered by CreatedAt DESC then Id DESC)
- When `source=all` or omitted: Returns 0-N items (all available members, existing behavior)
- If POSPOS API is unreachable, returns 503 with distinct error message
- If other server errors occur, returns 500 with explanation

---

## POST /import/customers/selected?persist=true
Import a list of selected POSPOS member ids.

Request body: JSON array of POSPOS `id` strings.

Response: 200 OK
Body: JSON summary

Sample response:
```json
{
  "timestamp": "2025-09-28T12:34:56Z",
  "created": 12,
  "updated": 3,
  "skipped": 0,
  "errors": []
}
```

Behavior:
- Server will fetch matching POSPOS records and map them to `Customer` entities.
- If `persist=true` (default for POSPOS imports) the externalId->internalId mapping is persisted to `customer_id_mapping.json`.
- Import is idempotent: existing mapped customers will be updated, not duplicated.

---

## POST /import/customers?persist=true
Trigger an import for all candidates (server will query POSPOS and import all returned candidates).

Body: none
Response: same summary format as above.

---

## POST /import/customers/fix-codes
Backfill or fix `Customer.Code` and `Customer.ExternalId` by reconciling POSPOS codes.

Request: none
Response: JSON with audit information, e.g.:
```json
{
  "updated": 5,
  "audit": [
    {"customerId":"...","action":"updated_code","old":"A123","new":"M000123"}
  ]
}
```

Notes:
- This endpoint is administrative; back up DB and mapping file before running.

---

## Error format
Server returns a JSON error object on 4xx/5xx responses, with `message` and optionally `details`.

```json
{ "message": "POSPOS API unreachable", "details": "socket error" }
```

---

## Mapping file
- Location: `src/server/PigFarmManagement.Server/customer_id_mapping.json`
- Format: `{ "posposExternalId": "internalGuid" }`
- Updated when imports run with `persist=true`.
