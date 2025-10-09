# Data Model: Server-Side Endpoint Security Without Database Tables

**Date**: October 9, 2025  
**Feature**: 010-secure-all-the  
**Phase**: 1 - Data Design

## Entity Analysis from Feature Specification

### Security Configuration
**Purpose**: Defines authentication methods, user credentials, permissions, and security policies stored in configuration files  
**Storage**: JSON/XML configuration files with IOptions pattern integration  
**Lifecycle**: Application startup with hot reload support for policy updates

**Fields**:
- `ApiKeySettings`: Authentication configuration (header name, cache timeout)
- `RoleSettings`: Role definitions and hierarchy (Admin, User, ReadOnly)  
- `EndpointGroups`: Role-to-endpoint mapping for authorization rules
- `RateLimitPolicies`: Request threshold configurations per role/endpoint
- `LoggingSettings`: Security event logging configuration (retention, file paths)
- `SessionSettings`: Session timeout and cleanup intervals

**Relationships**: None (configuration-based, no database persistence)

**Validation Rules**:
- Role names must be non-empty and unique
- Rate limit values must be positive integers
- File paths must be valid and writable
- Timeout values must be positive TimeSpan values

### Rate Limit Policy  
**Purpose**: Specifies request thresholds, time windows, and enforcement rules for different endpoint categories  
**Storage**: In-memory cache (IMemoryCache) with sliding window algorithm  
**Lifecycle**: Runtime state with automatic cleanup via cache expiration

**Fields**:
- `UserId`: Unique identifier for the user (from API key)
- `Endpoint`: API endpoint pattern or group identifier
- `RequestCount`: Current request count within time window
- `WindowStart`: Start time of current sliding window
- `WindowDuration`: Length of rate limiting window (configurable)
- `ThresholdLimit`: Maximum requests allowed in window
- `IsBlocked`: Current enforcement status
- `BlockedUntil`: Timestamp when access will be restored

**Relationships**: Links to user via existing API Keys table (read-only)

**State Transitions**:
- Normal → Approaching Limit (80% threshold)
- Approaching Limit → Blocked (100% threshold exceeded)
- Blocked → Normal (window expiry + cleanup)

### Security Event
**Purpose**: Represents logged security activities with timestamp, user, action, result, and context information  
**Storage**: Structured JSON log files with Serilog file sink  
**Lifecycle**: Write-only with configurable 30-day retention and automatic cleanup

**Fields**:
- `Timestamp`: Event occurrence time (ISO 8601 format)
- `EventId`: Unique identifier for correlation and deduplication
- `UserId`: User identifier from authenticated context (nullable for anonymous)
- `UserRole`: Role of the user during event (if authenticated)
- `Action`: Security action type (Authentication, Authorization, RateLimit, Validation)
- `Result`: Event outcome (Success, Failure, Blocked, Warning)
- `Endpoint`: Target API endpoint or resource
- `IpAddress`: Client IP address for geographic analysis
- `UserAgent`: Client user agent for device/browser tracking
- `Details`: Additional context data (error messages, validation failures)
- `RequestId`: Correlation ID for tracing across middleware pipeline

**Relationships**: References user context but not enforced (log data isolation)

**Validation Rules**:
- Timestamp must be valid and recent (within 1 hour for clock skew)
- Action and Result must be from predefined enums
- Endpoint must match valid API route patterns
- Details field limited to 1KB to prevent log bloat

### Session Token
**Purpose**: Manages user authentication sessions with expiration, validation, and cleanup stored in memory  
**Storage**: Distributed memory cache (IDistributedCache) with Redis support for scale  
**Lifecycle**: Created on authentication, refreshed on activity, expired after idle timeout

**Fields**:
- `TokenId`: Unique session identifier (GUID-based)
- `UserId`: Associated user from API Keys table
- `UserRole`: Cached role for authorization decisions
- `CreatedAt`: Session creation timestamp
- `LastAccessed`: Most recent activity timestamp for idle calculation
- `ExpiresAt`: Absolute expiration time (created + max session duration)
- `IdleTimeout`: Configurable idle timeout (2 hours default)
- `IsActive`: Session validity flag
- `RefreshToken`: Optional token for manual session refresh
- `IpAddress`: Client IP for session hijacking detection
- `UserAgent`: Client identification for security validation

**Relationships**: Links to existing API Keys table user records

**State Transitions**:
- Created → Active (successful authentication)
- Active → Refreshed (activity within idle window)
- Active → Idle (no activity beyond timeout)
- Idle → Expired (cleanup process)
- Active → Revoked (manual logout or security breach)

### Input Validation Rule
**Purpose**: Defines data format requirements, security checks, and sanitization rules per endpoint  
**Storage**: Configuration-based rules with runtime caching for performance  
**Lifecycle**: Loaded at startup, cached for request processing, updated via configuration

**Fields**:
- `EndpointPattern`: API route pattern for rule application
- `HttpMethod`: HTTP verb for specific endpoint variant
- `FieldName`: Target field or parameter name
- `DataType`: Expected data type (string, int, email, etc.)
- `ValidationPattern`: Regex pattern for format validation
- `SanitizationRules`: XSS and injection prevention rules
- `MaxLength`: Maximum allowed input length
- `IsRequired`: Field requirement flag
- `AllowedValues`: Whitelist of acceptable values (for enums)
- `RejectedPatterns`: Blacklist patterns for security filtering

**Relationships**: Applied to all endpoints, no direct database relationships

**Validation Rules**:
- Endpoint patterns must be valid regex or route templates
- Validation patterns must be compilable regex expressions
- Max length values must be positive and reasonable (< 10MB)
- Sanitization rules must not break legitimate data

### Audit Record
**Purpose**: Captures data modification operations with before/after states and user context for compliance  
**Storage**: JSON log files separate from security events for regulatory compliance  
**Lifecycle**: Created on data changes, immutable, long-term retention per compliance requirements

**Fields**:
- `RecordId`: Unique audit identifier for compliance tracking
- `Timestamp`: Operation timestamp (ISO 8601 with timezone)
- `UserId`: User performing the operation
- `UserRole`: Role context for permission analysis
- `Operation`: Type of change (Create, Update, Delete, Import)
- `EntityType`: Type of data being modified (Customer, PigPen, Feed)
- `EntityId`: Primary key of affected record
- `BeforeState`: JSON serialization of pre-change state
- `AfterState`: JSON serialization of post-change state
- `ChangeReason`: User-provided reason for audit trail
- `IpAddress`: Client IP for geographic compliance
- `RequestId`: Correlation with security events

**Relationships**: References existing domain entities but maintains separate lifecycle

**Compliance Requirements**:
- Immutable once written (no updates or deletes)
- Encrypted sensitive fields in JSON state
- Minimum 7-year retention for regulatory compliance
- Geographic data residency for international users

## Configuration Schema

### Security Policy Configuration
```json
{
  "$schema": "security-config-schema.json",
  "Authentication": {
    "ApiKeyHeader": "X-Api-Key",
    "CacheTimeout": "00:05:00",
    "AllowAnonymousEndpoints": ["/api/health", "/api/version"]
  },
  "Authorization": {
    "Roles": [
      { "Name": "Admin", "Level": 100, "Inherits": [] },
      { "Name": "User", "Level": 50, "Inherits": ["ReadOnly"] },
      { "Name": "ReadOnly", "Level": 10, "Inherits": [] }
    ],
    "EndpointGroups": {
      "Admin": ["/api/admin/*", "/api/users/*", "/api/system/*"],
      "User": ["/api/customers/*", "/api/pigpens/*", "/api/feeds/import"],
      "ReadOnly": ["/api/feeds/read", "/api/reports/*"]
    }
  },
  "RateLimiting": {
    "Policies": [
      { "Name": "General", "RequestsPerHour": 500, "WindowMinutes": 60, "AppliesTo": ["User", "ReadOnly"] },
      { "Name": "Admin", "RequestsPerHour": 200, "WindowMinutes": 60, "AppliesTo": ["Admin"] }
    ],
    "BlockDuration": "00:15:00",
    "CleanupInterval": "00:05:00"
  },
  "Logging": {
    "SecurityEvents": {
      "RetentionDays": 30,
      "FilePath": "logs/security/events-{Date}.json",
      "MaxFileSize": "100MB"
    },
    "AuditTrail": {
      "RetentionDays": 2555,
      "FilePath": "logs/audit/trail-{Date}.json",
      "MaxFileSize": "500MB"
    }
  },
  "Sessions": {
    "IdleTimeoutHours": 2,
    "MaxSessionHours": 24,
    "CleanupIntervalMinutes": 30,
    "RequireRefresh": true
  }
}
```

## Integration with Existing Schema

### API Keys Table (Existing)
**Current Usage**: Authentication for external integrations  
**New Usage**: Foundation for comprehensive endpoint security  
**Required Changes**: None - existing table provides sufficient user identification

**Relationship Mapping**:
- SecurityEvent.UserId → ApiKeys.UserId
- SessionToken.UserId → ApiKeys.UserId  
- AuditRecord.UserId → ApiKeys.UserId

### User Roles (Existing)
**Current Usage**: Basic role assignment in authentication feature  
**New Usage**: Authorization hierarchy for endpoint access control  
**Required Changes**: None - existing role system supports Admin/User/ReadOnly hierarchy

## Performance Considerations

### Memory Usage
- **Rate Limiting Cache**: ~100 bytes per active user-endpoint combination
- **Session Storage**: ~500 bytes per active session
- **Configuration Cache**: ~10KB for all security policies
- **Total estimated**: ~5-10MB for 1000 concurrent users

### Request Processing
- **Authentication lookup**: O(1) via memory cache
- **Authorization check**: O(1) via role hierarchy cache
- **Rate limit enforcement**: O(1) via sliding window algorithm
- **Input validation**: O(n) where n = input field count

### Storage I/O
- **Security events**: Async file writes, ~1KB per event
- **Audit records**: Async file writes, ~5KB per data operation
- **Configuration reload**: File system watcher for hot updates

---

*Data model design complete - ready for contract generation*