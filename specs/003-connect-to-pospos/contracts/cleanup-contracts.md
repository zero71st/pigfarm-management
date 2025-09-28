# Contracts: POSPOS Invoice Import

This folder will contain contract tests and API schema expectations for the POSPOS transactions endpoint.

## Candidate contract: GET /developer/api/transactions
- Query parameters: start (YYYY-MM-DD), end (YYYY-MM-DD), page (int), limit (int)
- Authorization: Bearer token in `Authorization` header (POSPOS_API_KEY)

### Example expected response shape (to be validated against real sample):
```json
{
  "data": [
    {
      "invoice_id": "string",
      "date": "2025-09-03T12:34:56Z",
      "customer": {
        "id": "string",
        "name": "string"
      },
      "lines": [
        {
          "sku": "string",
          "description": "string",
          "qty": 1,
          "unit_price": 100.0,
          "total": 100.0
        }
      ],
      "subtotal": 100.0,
      "tax": 0.0,
      "total": 100.0,
      "status": "PAID"
    }
  ],
  "meta": {
    "page": 1,
    "limit": 200,
    "total": 1
  }
}
```

### Contract tests (outline)
- Test 1: Call the endpoint with a small date range; assert HTTP 200 and `data` present.
- Test 2: Validate each invoice object has `invoice_id` and `total` fields.
- Test 3: Validate pagination meta fields when more results expected.

> NOTE: These tests are skeletons; concrete assertions will be refined once real sample JSON is available.
