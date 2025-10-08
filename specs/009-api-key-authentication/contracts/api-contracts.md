# API Contracts: API-Key Authentication System

**Feature**: 009-api-key-authentication  
**Created**: October 8, 2025  
**Status**: Draft

## Overview

This document defines the complete API contract for the API-Key Authentication system, including request/response schemas, HTTP status codes, error handling, and integration patterns.

## Base Configuration

### Base URLs
- **Development**: `https://localhost:5000/api`
- **Production**: `https://your-domain.com/api`

### Common Headers
```http
Content-Type: application/json
Accept: application/json
X-Api-Key: {api-key} # For authenticated requests
```

### Authentication
All protected endpoints require a valid API key in the `X-Api-Key` header. The middleware will validate the key and populate the security context.

## Data Transfer Objects

### Core DTOs

#### LoginRequest
```csharp
public record LoginRequest
{
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public string? KeyLabel { get; init; }
    public int? ExpirationDays { get; init; } = 30;
}
```

#### LoginResponse
```csharp
public record LoginResponse
{
    public string ApiKey { get; init; } = "";
    public DateTime ExpiresAt { get; init; }
    public UserInfo User { get; init; } = null!;
}
```

#### UserInfo
```csharp
public record UserInfo
{
    public Guid Id { get; init; }
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public string[] Roles { get; init; } = Array.Empty<string>();
    public bool IsActive { get; init; }
    public DateTime? LastLoginAt { get; init; }
}
```

#### CreateUserRequest
```csharp
public record CreateUserRequest
{
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public string Password { get; init; } = "";
    public string[] Roles { get; init; } = Array.Empty<string>();
    public bool IsActive { get; init; } = true;
}
```

#### UpdateUserRequest
```csharp
public record UpdateUserRequest
{
    public string? Email { get; init; }
    public string[] Roles { get; init; } = Array.Empty<string>();
    public bool IsActive { get; init; }
}
```

#### ApiKeyInfo
```csharp
public record ApiKeyInfo
{
    public Guid Id { get; init; }
    public string? Label { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public bool IsActive { get; init; }
    public int UsageCount { get; init; }
}
```

#### CreateApiKeyRequest
```csharp
public record CreateApiKeyRequest
{
    public string? Label { get; init; }
    public int? ExpirationDays { get; init; } = 30;
}
```

#### CreateApiKeyResponse
```csharp
public record CreateApiKeyResponse
{
    public string ApiKey { get; init; } = "";
    public Guid KeyId { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string? Label { get; init; }
}
```

#### PagedResult<T>
```csharp
public record PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public PaginationInfo Pagination { get; init; } = null!;
}

public record PaginationInfo
{
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
}
```

#### ErrorResponse
```csharp
public record ErrorResponse
{
    public string Error { get; init; } = "";
    public string? Detail { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? TraceId { get; init; }
    public Dictionary<string, string[]>? ValidationErrors { get; init; }
}
```

## Authentication Endpoints

### POST /api/auth/login
Authenticate user and generate new API key.

**Security**: Public endpoint (no authentication required)

**Request Body**:
```json
{
  "username": "admin",
  "password": "securePassword123",
  "keyLabel": "Desktop App",
  "expirationDays": 30
}
```

**Success Response (200 OK)**:
```json
{
  "apiKey": "Abc123XyzSecureRandomKey456...",
  "expiresAt": "2025-11-08T10:30:00Z",
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "username": "admin",
    "email": "admin@pigfarm.com",
    "roles": ["Admin", "Manager"],
    "isActive": true,
    "lastLoginAt": "2025-10-08T10:30:00Z"
  }
}
```

**Error Responses**:
```json
// 401 Unauthorized - Invalid credentials
{
  "error": "Invalid credentials",
  "timestamp": "2025-10-08T10:30:00Z",
  "traceId": "abc123"
}

// 400 Bad Request - Validation errors
{
  "error": "Validation failed",
  "validationErrors": {
    "Username": ["Username is required"],
    "Password": ["Password must be at least 8 characters"]
  },
  "timestamp": "2025-10-08T10:30:00Z"
}

// 423 Locked - Account disabled
{
  "error": "Account is disabled",
  "detail": "Contact administrator to reactivate account",
  "timestamp": "2025-10-08T10:30:00Z"
}
```

### GET /api/auth/me
Get current user information based on API key.

**Security**: Requires valid API key

**Headers**:
```http
X-Api-Key: Abc123XyzSecureRandomKey456...
```

**Success Response (200 OK)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "username": "admin",
  "email": "admin@pigfarm.com",
  "roles": ["Admin", "Manager"],
  "isActive": true,
  "lastLoginAt": "2025-10-08T10:30:00Z"
}
```

**Error Responses**:
```json
// 401 Unauthorized - Invalid or missing API key
{
  "error": "Invalid or missing API key",
  "timestamp": "2025-10-08T10:30:00Z"
}

// 401 Unauthorized - Expired API key
{
  "error": "API key has expired",
  "timestamp": "2025-10-08T10:30:00Z"
}
```

## Admin User Management Endpoints

### POST /api/admin/users
Create new user account.

**Security**: Requires Admin role

**Request Body**:
```json
{
  "username": "farmmanager1",
  "email": "manager@pigfarm.com",
  "password": "TempPassword123!",
  "roles": ["Manager", "Worker"],
  "isActive": true
}
```

**Success Response (201 Created)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "username": "farmmanager1",
  "email": "manager@pigfarm.com",
  "roles": ["Manager", "Worker"],
  "isActive": true,
  "lastLoginAt": null
}
```

**Error Responses**:
```json
// 400 Bad Request - Validation errors
{
  "error": "Validation failed",
  "validationErrors": {
    "Username": ["Username 'farmmanager1' is already taken"],
    "Email": ["Email format is invalid"],
    "Password": ["Password must contain at least one uppercase letter"]
  },
  "timestamp": "2025-10-08T10:30:00Z"
}

// 403 Forbidden - Insufficient permissions
{
  "error": "Insufficient permissions",
  "detail": "Admin role required for user management",
  "timestamp": "2025-10-08T10:30:00Z"
}
```

### GET /api/admin/users
List all users with pagination and filtering.

**Security**: Requires Admin role

**Query Parameters**:
- `page` (int, default: 1): Page number
- `pageSize` (int, default: 20, max: 100): Items per page
- `isActive` (bool, optional): Filter by active status
- `role` (string, optional): Filter by role
- `search` (string, optional): Search in username or email

**Example Request**:
```http
GET /api/admin/users?page=1&pageSize=10&isActive=true&role=Manager&search=farm
```

**Success Response (200 OK)**:
```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "username": "farmmanager1",
      "email": "manager@pigfarm.com",
      "roles": ["Manager", "Worker"],
      "isActive": true,
      "lastLoginAt": "2025-10-08T09:15:00Z"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalPages": 1,
    "totalCount": 1
  }
}
```

### GET /api/admin/users/{userId}
Get specific user details.

**Security**: Requires Admin role

**Path Parameters**:
- `userId` (Guid): User identifier

**Success Response (200 OK)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "username": "farmmanager1",
  "email": "manager@pigfarm.com",
  "roles": ["Manager", "Worker"],
  "isActive": true,
  "lastLoginAt": "2025-10-08T09:15:00Z"
}
```

**Error Responses**:
```json
// 404 Not Found - User not found
{
  "error": "User not found",
  "detail": "User with ID '550e8400-e29b-41d4-a716-446655440001' does not exist",
  "timestamp": "2025-10-08T10:30:00Z"
}
```

### PUT /api/admin/users/{userId}
Update user information.

**Security**: Requires Admin role

**Path Parameters**:
- `userId` (Guid): User identifier

**Request Body**:
```json
{
  "email": "newemail@pigfarm.com",
  "roles": ["Manager"],
  "isActive": false
}
```

**Success Response (200 OK)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "username": "farmmanager1",
  "email": "newemail@pigfarm.com",
  "roles": ["Manager"],
  "isActive": false,
  "lastLoginAt": "2025-10-08T09:15:00Z"
}
```

### DELETE /api/admin/users/{userId}
Soft delete user account (sets IsDeleted flag).

**Security**: Requires Admin role

**Path Parameters**:
- `userId` (Guid): User identifier

**Success Response (204 No Content)**

**Error Responses**:
```json
// 400 Bad Request - Cannot delete self
{
  "error": "Cannot delete own account",
  "detail": "Administrators cannot delete their own account",
  "timestamp": "2025-10-08T10:30:00Z"
}

// 409 Conflict - User has active sessions
{
  "error": "User has active API keys",
  "detail": "Revoke all API keys before deleting user",
  "timestamp": "2025-10-08T10:30:00Z"
}
```

## API Key Management Endpoints

### POST /api/admin/users/{userId}/apikeys
Generate new API key for specific user.

**Security**: Requires Admin role

**Path Parameters**:
- `userId` (Guid): User identifier

**Request Body**:
```json
{
  "label": "Mobile App",
  "expirationDays": 90
}
```

**Success Response (201 Created)**:
```json
{
  "apiKey": "Xyz789NewSecureRandomKey123...",
  "keyId": "550e8400-e29b-41d4-a716-446655440010",
  "expiresAt": "2026-01-08T10:30:00Z",
  "label": "Mobile App"
}
```

### GET /api/admin/users/{userId}/apikeys
List API keys for specific user.

**Security**: Requires Admin role

**Path Parameters**:
- `userId` (Guid): User identifier

**Success Response (200 OK)**:
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440010",
    "label": "Mobile App",
    "createdAt": "2025-10-08T10:30:00Z",
    "expiresAt": "2026-01-08T10:30:00Z",
    "lastUsedAt": "2025-10-08T15:45:00Z",
    "isActive": true,
    "usageCount": 42
  },
  {
    "id": "550e8400-e29b-41d4-a716-446655440011",
    "label": "Desktop Client",
    "createdAt": "2025-10-01T08:00:00Z",
    "expiresAt": "2025-11-01T08:00:00Z",
    "lastUsedAt": "2025-10-07T16:30:00Z",
    "isActive": true,
    "usageCount": 156
  }
]
```

### DELETE /api/admin/apikeys/{keyId}
Revoke specific API key.

**Security**: Requires Admin role

**Path Parameters**:
- `keyId` (Guid): API key identifier

**Success Response (204 No Content)**

**Error Responses**:
```json
// 404 Not Found - API key not found
{
  "error": "API key not found",
  "detail": "API key with ID '550e8400-e29b-41d4-a716-446655440010' does not exist",
  "timestamp": "2025-10-08T10:30:00Z"
}
```

### POST /api/admin/users/{userId}/revoke-all-keys
Revoke all API keys for specific user.

**Security**: Requires Admin role

**Path Parameters**:
- `userId` (Guid): User identifier

**Success Response (200 OK)**:
```json
{
  "message": "All API keys revoked successfully",
  "revokedCount": 3,
  "timestamp": "2025-10-08T10:30:00Z"
}
```

## Password Management Endpoints

### POST /api/admin/users/{userId}/reset-password
Reset user password (admin-initiated).

**Security**: Requires Admin role

**Path Parameters**:
- `userId` (Guid): User identifier

**Request Body**:
```json
{
  "newPassword": "NewSecurePassword123!",
  "revokeApiKeys": true
}
```

**Success Response (200 OK)**:
```json
{
  "message": "Password reset successfully",
  "revokedApiKeys": 2,
  "timestamp": "2025-10-08T10:30:00Z"
}
```

## Error Handling

### Standard HTTP Status Codes

| Status Code | Description | Usage |
|-------------|-------------|--------|
| 200 OK | Success | Successful GET, PUT operations |
| 201 Created | Resource created | Successful POST operations |
| 204 No Content | Success, no response body | Successful DELETE operations |
| 400 Bad Request | Validation errors | Invalid request data |
| 401 Unauthorized | Authentication failed | Invalid/missing API key |
| 403 Forbidden | Authorization failed | Insufficient permissions |
| 404 Not Found | Resource not found | Invalid resource ID |
| 409 Conflict | Business rule violation | Duplicate username/email |
| 423 Locked | Account disabled | Inactive user account |
| 429 Too Many Requests | Rate limit exceeded | Too many auth attempts |
| 500 Internal Server Error | Server error | Unexpected server issues |

### Error Response Format

All error responses follow a consistent format:

```json
{
  "error": "Brief error description",
  "detail": "Optional detailed explanation",
  "timestamp": "2025-10-08T10:30:00Z",
  "traceId": "abc123def456",
  "validationErrors": {
    "FieldName": ["Error message 1", "Error message 2"]
  }
}
```

### Validation Error Examples

```json
// Username validation errors
{
  "error": "Validation failed",
  "validationErrors": {
    "Username": [
      "Username is required",
      "Username must be between 3 and 50 characters",
      "Username can only contain letters, numbers, underscore, and hyphen"
    ]
  },
  "timestamp": "2025-10-08T10:30:00Z"
}

// Password validation errors
{
  "error": "Validation failed",
  "validationErrors": {
    "Password": [
      "Password must be at least 8 characters long",
      "Password must contain at least one uppercase letter",
      "Password must contain at least one lowercase letter",
      "Password must contain at least one digit"
    ]
  },
  "timestamp": "2025-10-08T10:30:00Z"
}

// Role validation errors
{
  "error": "Validation failed",
  "validationErrors": {
    "Roles": [
      "Invalid role 'InvalidRole'. Valid roles are: Admin, Manager, Worker, Viewer"
    ]
  },
  "timestamp": "2025-10-08T10:30:00Z"
}
```

## Rate Limiting

### Authentication Endpoints
- **POST /api/auth/login**: 5 attempts per minute per IP
- **POST /api/admin/users/{userId}/reset-password**: 3 attempts per hour per admin

### Rate Limit Headers
```http
X-RateLimit-Limit: 5
X-RateLimit-Remaining: 3
X-RateLimit-Reset: 1633024800
```

### Rate Limit Exceeded Response
```json
{
  "error": "Rate limit exceeded",
  "detail": "Too many login attempts. Please try again in 60 seconds.",
  "timestamp": "2025-10-08T10:30:00Z"
}
```

## Security Headers

### Required Request Headers
```http
Content-Type: application/json
Accept: application/json
X-Api-Key: {api-key}  # For authenticated endpoints
```

### Response Security Headers
```http
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Strict-Transport-Security: max-age=31536000; includeSubDomains
```

## Client Integration Examples

### C# HttpClient Example
```csharp
public class ApiKeyAuthClient
{
    private readonly HttpClient _httpClient;
    private string? _apiKey;

    public ApiKeyAuthClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var request = new LoginRequest
        {
            Username = username,
            Password = password,
            KeyLabel = "Desktop Client"
        };

        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
        
        if (response.IsSuccessStatusCode)
        {
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            _apiKey = loginResponse?.ApiKey;
            
            // Store API key securely and add to default headers
            _httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
            
            return true;
        }
        
        return false;
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        var response = await _httpClient.GetAsync("/api/auth/me");
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UserInfo>();
        }
        
        return null;
    }
}
```

### JavaScript/TypeScript Example
```typescript
class ApiKeyAuthClient {
    private apiKey: string | null = null;
    private baseUrl: string;

    constructor(baseUrl: string) {
        this.baseUrl = baseUrl;
        this.apiKey = localStorage.getItem('apiKey');
    }

    async login(username: string, password: string): Promise<boolean> {
        const response = await fetch(`${this.baseUrl}/api/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                username,
                password,
                keyLabel: 'Web App'
            })
        });

        if (response.ok) {
            const loginResponse = await response.json();
            this.apiKey = loginResponse.apiKey;
            localStorage.setItem('apiKey', this.apiKey);
            return true;
        }

        return false;
    }

    async getCurrentUser(): Promise<UserInfo | null> {
        if (!this.apiKey) return null;

        const response = await fetch(`${this.baseUrl}/api/auth/me`, {
            headers: {
                'X-Api-Key': this.apiKey
            }
        });

        if (response.ok) {
            return await response.json();
        }

        return null;
    }

    private getAuthHeaders(): HeadersInit {
        return this.apiKey ? { 'X-Api-Key': this.apiKey } : {};
    }
}
```

## Testing Considerations

### Unit Test Scenarios
- Valid login with correct credentials
- Invalid login with wrong credentials
- API key generation and validation
- Role-based authorization checks
- User management operations
- Error handling for various scenarios

### Integration Test Scenarios
- End-to-end authentication flow
- API key middleware functionality
- Admin user management workflows
- Security header validation
- Rate limiting enforcement

### Security Test Scenarios
- SQL injection attempts
- Cross-site scripting (XSS) prevention
- API key brute force attacks
- Privilege escalation attempts
- Session fixation attacks

---

This API contract specification provides comprehensive documentation for implementing and integrating with the API-Key Authentication system, ensuring consistent behavior across all client and server implementations.