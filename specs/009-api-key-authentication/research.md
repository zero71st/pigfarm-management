# Research: API-Key Authentication Implementation

**Feature**: 009-api-key-authentication  
**Created**: October 8, 2025  
**Status**: Complete

## Research Summary

This document consolidates research findings for implementing API-key based authentication in a Blazor WebAssembly + ASP.NET Core application, addressing security patterns, framework integration, and operational requirements.

## Security Implementation Patterns

### Decision: BCrypt for Password Hashing
**Rationale**: Industry standard with proven security record, built-in salt generation, configurable work factor
**Implementation**: Microsoft.AspNetCore.Identity.PasswordHasher<T> wrapper around BCrypt
**Configuration**: Work factor 12+ for production, balances security vs. performance

**Alternatives Considered**:
- Argon2id: More secure but requires additional NuGet packages
- PBKDF2: Built into .NET but BCrypt preferred by security community
- Custom implementation: Rejected due to security risks

### Decision: SHA-256 for API Key Hashing
**Rationale**: Fast lookup performance for middleware, sufficient security for this use case
**Implementation**: Convert.ToBase64String(SHA256.ComputeHash(utf8Bytes))
**Security**: Raw keys never stored, only hashes. Keys generated with cryptographically secure RNG

**Alternatives Considered**:
- HMAC-SHA256: Overkill for this use case, requires key management
- BCrypt for API keys: Too slow for per-request validation
- Plain text storage: Rejected for obvious security reasons

### Decision: X-Api-Key Header Authentication
**Rationale**: Simple, widely adopted, works well with HTTP clients and middleware
**Implementation**: Custom middleware validates header, populates ClaimsPrincipal
**Client Integration**: DelegatingHandler automatically adds header to requests

**Alternatives Considered**:
- Authorization: Bearer token: Standard but API keys aren't JWT tokens
- Custom Authorization header: Non-standard, harder for API consumers
- Query parameter: Rejected due to logging/caching security risks

## ASP.NET Core Integration Patterns

### Decision: Custom Middleware + Claims-based Authorization
**Rationale**: Integrates with existing ASP.NET Core authorization, supports role-based access
**Implementation**: ApiKeyMiddleware runs before authorization, populates HttpContext.User
**Role Support**: Claims populated from API key's role snapshot

**Alternatives Considered**:
- ASP.NET Core Identity: Overkill for API-key only authentication
- Custom authorization filters: Less reusable than middleware approach
- JWT tokens: Adds complexity for stateless requirement

### Decision: Repository Pattern for Data Access
**Rationale**: Testable, follows existing project patterns, separates concerns
**Implementation**: IAuthRepository with async methods, EF Core implementation
**Performance**: Batch queries (GetByExternalIdsAsync pattern) for efficiency

**Alternatives Considered**:
- Direct DbContext usage in services: Less testable, tighter coupling
- Generic repository: Too abstract for specific auth needs
- CQRS pattern: Overkill for this feature scope

## Blazor WebAssembly Client Patterns

### Decision: DelegatingHandler for Automatic Header Injection
**Rationale**: Transparent to application code, handles all HTTP requests automatically
**Implementation**: ApiKeyHandler retrieves key from secure storage, adds to headers
**Storage**: Blazored.LocalStorage for development, can upgrade to secure storage

**Alternatives Considered**:
- Manual header addition: Error-prone, requires remembering in every API call
- Base HTTP service class: Centralized but requires inheritance everywhere
- Interceptors: More complex setup than DelegatingHandler

### Decision: Blazored.LocalStorage for API Key Storage
**Rationale**: Simple development setup, can be upgraded to secure storage later
**Security**: Adequate for development, production should consider secure alternatives
**Implementation**: Store raw key after login, retrieve in DelegatingHandler

**Alternatives Considered**:
- Session storage: Lost on browser close, poor UX
- Memory only: Lost on page refresh, poor UX
- IndexedDB: More complex setup for minimal security gain in browser context

## Entity Framework Configuration

### Decision: Entity Framework Configurations in Separate Files
**Rationale**: Follows existing project patterns, keeps entities clean
**Implementation**: UserEntityConfiguration, ApiKeyEntityConfiguration classes
**Conventions**: IEntityTypeConfiguration<T> pattern with fluent API

**Alternatives Considered**:
- Data annotations only: Less flexible for complex constraints
- Embedded configuration: Makes entity classes harder to read
- Code-first migrations: Already established in project

### Decision: Soft Delete for Users
**Rationale**: Preserves audit trail, allows historical data integrity
**Implementation**: IsDeleted flag with filtered indexes
**Cascade**: API keys automatically revoked when user soft deleted

**Alternatives Considered**:
- Hard delete: Breaks audit trail and historical references
- Archive table: More complex with minimal benefit
- Status flags: Soft delete is clearer intent

## Performance and Scalability Patterns

### Decision: API Key Usage Tracking
**Rationale**: Enables security monitoring and usage analytics
**Implementation**: Asynchronous update of LastUsedAt and UsageCount
**Performance**: Non-blocking updates to avoid latency impact

**Alternatives Considered**:
- Synchronous updates: Would add latency to every request
- No usage tracking: Limits security monitoring capabilities
- Separate analytics service: Overkill for current requirements

### Decision: Role Snapshot in API Keys
**Rationale**: Avoids user lookup on every request, improves performance
**Implementation**: Copy user roles to API key at generation time
**Trade-off**: Role changes require new API key generation

**Alternatives Considered**:
- Live role lookup: Too slow for per-request middleware
- Role caching: Complex invalidation logic required
- JWT tokens with role claims: Adds token validation complexity

## Testing Strategy

### Decision: Contract-First Testing
**Rationale**: Ensures API compatibility, enables parallel development
**Implementation**: OpenAPI schema validation, contract tests for all endpoints
**Coverage**: Authentication flow, admin operations, error scenarios

**Alternatives Considered**:
- Unit tests only: Misses integration issues
- Manual testing: Not repeatable or scalable
- End-to-end only: Too slow for rapid feedback

### Decision: Test Authentication Helper
**Rationale**: Reduces test setup boilerplate, consistent test users
**Implementation**: TestAuthHelper creates users and API keys for tests
**Security**: Test keys clearly marked, never used in production

**Alternatives Considered**:
- Mock authentication: Doesn't test real auth flow
- Shared test database: Race conditions and cleanup issues
- Production-like setup: Too complex for unit tests

## Operational Considerations

### Decision: Admin Seed Data
**Rationale**: Ensures initial admin access, enables deployment automation
**Implementation**: Database seed creates default admin if none exists
**Security**: Default credentials documented but must be changed immediately

**Alternatives Considered**:
- Manual admin creation: Blocks automated deployment
- Configuration-based admin: Credentials in config files risky
- Emergency access endpoints: Security risk if not properly protected

### Decision: Audit Logging
**Rationale**: Security compliance, debugging support, user activity tracking
**Implementation**: Structured logging with correlation IDs
**Storage**: Application logs with separate audit log table for critical events

**Alternatives Considered**:
- No audit logging: Fails security compliance requirements
- File-based logging: Harder to query and analyze
- External audit service: Adds complexity and dependencies

## Security Review Checklist

### Password Security
- ✅ BCrypt hashing with appropriate work factor
- ✅ Password complexity requirements enforced
- ✅ No plain text password storage anywhere
- ✅ Secure password reset process (admin-only)

### API Key Security
- ✅ Cryptographically secure key generation
- ✅ SHA-256 hashing for storage
- ✅ Raw keys displayed only once
- ✅ Configurable expiration dates
- ✅ Immediate revocation capability

### Transport Security
- ✅ HTTPS-only for all authentication endpoints
- ✅ Secure headers (HSTS, CSP, etc.)
- ✅ No sensitive data in URLs or logs
- ✅ Proper error handling without information disclosure

### Authorization Security
- ✅ Role-based access control
- ✅ Principle of least privilege
- ✅ No privilege escalation paths
- ✅ Session isolation

### Audit and Monitoring
- ✅ All authentication attempts logged
- ✅ Administrative actions audited
- ✅ Failed authentication monitoring
- ✅ Unusual activity detection

## Implementation Dependencies

### Required NuGet Packages
- Microsoft.AspNetCore.Identity (password hashing)
- Microsoft.EntityFrameworkCore (data access)
- Microsoft.AspNetCore.Authentication.JwtBearer (if adding JWT later)
- Blazored.LocalStorage (client storage)
- Microsoft.AspNetCore.Components.Authorization (Blazor auth)

### Development Tools
- Entity Framework Core CLI tools (migrations)
- OpenAPI/Swagger (API documentation)
- xUnit (testing framework)
- Microsoft.AspNetCore.Mvc.Testing (integration tests)

### Configuration Requirements
- Connection strings (development SQLite, production PostgreSQL)
- API key generation settings (length, expiration defaults)
- Rate limiting configuration
- CORS settings for Blazor client

## Next Steps

1. **Phase 1**: Design data models and API contracts based on research findings
2. **Implementation**: Follow TDD approach with contract tests first
3. **Security Review**: Independent security review of authentication flow
4. **Performance Testing**: Load testing of authentication middleware
5. **Documentation**: Operational runbooks for user management

---

*Research complete. All technical decisions documented with rationale. Ready for Phase 1 design.*