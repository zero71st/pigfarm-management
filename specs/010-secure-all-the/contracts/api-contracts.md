# API Contracts: Server-Side Endpoint Security Without Database Tables

**Date**: October 9, 2025  
**Feature**: 010-secure-all-the  
**Phase**: 1 - Contract Design

## Security API Endpoints

### Authentication Endpoints

#### Validate API Key
```http
POST /api/security/auth/validate
Content-Type: application/json
X-Api-Key: {api_key}

Request Body:
{
  "includePermissions": true
}

Response 200 (Success):
{
  "isValid": true,
  "userId": "12345",
  "role": "User",
  "permissions": ["customers:read", "customers:write", "pigpens:read"],
  "sessionToken": "sess_abc123",
  "expiresAt": "2025-10-09T16:00:00Z"
}

Response 401 (Invalid API Key):
{
  "error": "authentication_failed",
  "message": "Invalid or expired API key",
  "details": null
}

Response 429 (Rate Limited):
{
  "error": "rate_limit_exceeded",
  "message": "Too many authentication attempts",
  "retryAfter": "00:15:00"
}
```

#### Refresh Session
```http
POST /api/security/auth/refresh
Content-Type: application/json
X-Api-Key: {api_key}
Authorization: Bearer {session_token}

Request Body:
{
  "extendBy": "02:00:00"
}

Response 200 (Success):
{
  "sessionToken": "sess_def456",
  "expiresAt": "2025-10-09T18:00:00Z",
  "previousToken": "sess_abc123"
}

Response 401 (Invalid Session):
{
  "error": "session_invalid",
  "message": "Session expired or invalid",
  "details": null
}
```

### Authorization Endpoints

#### Check Permissions
```http
GET /api/security/auth/permissions?endpoint=/api/customers&method=POST
X-Api-Key: {api_key}
Authorization: Bearer {session_token}

Response 200 (Authorized):
{
  "hasAccess": true,
  "requiredRole": "User",
  "userRole": "User",
  "permissions": ["customers:write"]
}

Response 403 (Forbidden):
{
  "hasAccess": false,
  "requiredRole": "Admin",
  "userRole": "ReadOnly",
  "message": "Insufficient permissions for this operation"
}
```

### Rate Limiting Endpoints

#### Get Rate Limit Status
```http
GET /api/security/ratelimit/status
X-Api-Key: {api_key}

Response 200 (Success):
{
  "policies": [
    {
      "name": "General",
      "requestsRemaining": 487,
      "requestsLimit": 500,
      "windowReset": "2025-10-09T17:00:00Z",
      "isBlocked": false
    }
  ],
  "globalStatus": "normal"
}

Response 429 (Rate Limited):
{
  "policies": [
    {
      "name": "General", 
      "requestsRemaining": 0,
      "requestsLimit": 500,
      "windowReset": "2025-10-09T17:00:00Z",
      "isBlocked": true,
      "blockedUntil": "2025-10-09T16:15:00Z"
    }
  ],
  "globalStatus": "blocked"
}
```

### Security Monitoring Endpoints

#### Get Security Events
```http
GET /api/security/events?from=2025-10-09T00:00:00Z&to=2025-10-09T23:59:59Z&userId=12345
X-Api-Key: {api_key}
Authorization: Bearer {session_token}

Response 200 (Success):
{
  "events": [
    {
      "eventId": "evt_abc123",
      "timestamp": "2025-10-09T14:30:00Z",
      "userId": "12345",
      "userRole": "User",
      "action": "Authentication",
      "result": "Success",
      "endpoint": "/api/customers",
      "ipAddress": "192.168.1.100",
      "userAgent": "Mozilla/5.0...",
      "details": null
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 50
}

Response 403 (Forbidden - Admin Only):
{
  "error": "access_denied",
  "message": "Security monitoring requires Admin role",
  "details": null
}
```

## Enhanced Existing Endpoints

### Standard Response Headers
All secured endpoints will include these additional headers:

```http
X-RateLimit-Remaining: 487
X-RateLimit-Limit: 500
X-RateLimit-Reset: 1728489600
X-Request-ID: req_abc123
X-Security-Version: 1.0
```

### Standard Error Responses

#### Authentication Required (401)
```json
{
  "error": "authentication_required",
  "message": "Valid API key required for this endpoint",
  "details": {
    "requiredHeader": "X-Api-Key",
    "authEndpoint": "/api/security/auth/validate"
  }
}
```

#### Insufficient Permissions (403)
```json
{
  "error": "insufficient_permissions", 
  "message": "Your role does not permit access to this resource",
  "details": {
    "requiredRole": "Admin",
    "userRole": "User",
    "permissionsEndpoint": "/api/security/auth/permissions"
  }
}
```

#### Rate Limit Exceeded (429)
```json
{
  "error": "rate_limit_exceeded",
  "message": "Request rate limit exceeded for your role",
  "details": {
    "policy": "General",
    "limit": 500,
    "window": "1 hour",
    "retryAfter": "00:15:00",
    "statusEndpoint": "/api/security/ratelimit/status"
  }
}
```

#### Input Validation Failed (422)
```json
{
  "error": "validation_failed",
  "message": "Request contains invalid or malicious data",
  "details": {
    "field": "customerName",
    "violation": "contains_script_tags",
    "sanitized": false,
    "pattern": "^[a-zA-Z0-9\\s\\-\\.]+$"
  }
}
```

## Security Middleware Integration

### Request Processing Pipeline
```
1. Exception Handling Middleware
   ↓
2. Security Event Logging Middleware  
   ↓
3. API Key Authentication Middleware
   ↓ 
4. Role-Based Authorization Middleware
   ↓
5. Rate Limiting Middleware
   ↓
6. Input Validation Middleware
   ↓
7. Existing Application Middleware
```

### Middleware Configuration
```json
{
  "SecurityMiddleware": {
    "Order": [
      "SecurityEventLogging",
      "ApiKeyAuthentication", 
      "RoleBasedAuthorization",
      "RateLimiting",
      "InputValidation"
    ],
    "BypassEndpoints": [
      "/health",
      "/metrics", 
      "/api/security/auth/validate"
    ]
  }
}
```

## OpenAPI/Swagger Integration

### Security Scheme Definition
```yaml
components:
  securitySchemes:
    ApiKeyAuth:
      type: apiKey
      in: header
      name: X-Api-Key
      description: API key for authentication
    SessionAuth:
      type: http
      scheme: bearer
      bearerFormat: session_token
      description: Session token from authentication

security:
  - ApiKeyAuth: []
  - SessionAuth: []

paths:
  /api/customers:
    get:
      security:
        - ApiKeyAuth: []
      parameters:
        - name: X-Api-Key
          in: header
          required: true
          schema:
            type: string
      responses:
        200:
          description: Success
          headers:
            X-RateLimit-Remaining:
              schema:
                type: integer
            X-RateLimit-Limit:
              schema:
                type: integer
        401:
          $ref: '#/components/responses/AuthenticationRequired'
        403:
          $ref: '#/components/responses/InsufficientPermissions'
        429:
          $ref: '#/components/responses/RateLimitExceeded'
```

## Contract Testing Requirements

### Authentication Tests
- Valid API key returns successful validation
- Invalid API key returns 401 with proper error structure
- Expired API key returns 401 with expiration details
- Missing API key header returns 401 with required header info

### Authorization Tests  
- Admin role can access all endpoints
- User role cannot access admin endpoints
- ReadOnly role cannot access write operations
- Role hierarchy properly enforced

### Rate Limiting Tests
- Requests within limit process normally
- Requests exceeding limit return 429 with retry information
- Rate limit counters reset after window expiration
- Different roles have different rate limits

### Input Validation Tests
- Valid input passes through unchanged
- XSS attempts are blocked with 422 response
- SQL injection patterns are blocked
- Oversized payloads are rejected
- Required fields validation works correctly

### Session Management Tests
- Session tokens expire after idle timeout
- Session refresh extends expiration correctly
- Invalid session tokens return 401
- Session cleanup removes expired tokens

---

*API contracts complete - ready for quickstart guide*