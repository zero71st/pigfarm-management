# Import API Reference

This document describes the server import endpoints used to import customers from the external POSPOS API.

Base path: `/import/customers`

---

## GET /import/customers/candidates
Fetch a list of candidate members from the POSPOS API and return as a simplified candidate DTO.

Response: 200 OK
Content-Type: application/json

Sample item:
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

Notes:
- Server maps POSPOS response fields to `CandidateMember` DTO.
- If POSPOS API is unreachable the server returns 5xx with an explanation.

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
