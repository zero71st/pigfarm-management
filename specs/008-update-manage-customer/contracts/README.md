# Customer Management API Contracts

## Overview
Enhanced customer management endpoints supporting deletion, location management, POS sync, and view preferences.

## Base URL
```
Backend: https://api.pigfarm.local/customers
Frontend: Blazor WebAssembly client components
```

## Authentication
- **Development**: No authentication required
- **Production**: Bearer token authentication (future enhancement)

## Error Responses
All endpoints return consistent error format:
```json
{
  "error": "string",
  "message": "string", 
  "details": "string",
  "timestamp": "2025-10-05T10:30:00Z"
}
```

## Status Codes
- `200 OK`: Successful operation
- `201 Created`: Resource created
- `400 Bad Request`: Invalid request data
- `404 Not Found`: Resource not found
- `409 Conflict`: Business rule violation
- `500 Internal Server Error`: Server error