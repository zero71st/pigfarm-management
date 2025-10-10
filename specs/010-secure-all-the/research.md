# Research: Server-Side Endpoint Security Without Database Tables

**Date**: October 9, 2025  
**Feature**: 010-secure-all-the  
**Phase**: 0 - Research & Technology Decisions

## Technical Context Analysis

No NEEDS CLARIFICATION markers remain in the feature specification - all ambiguities were resolved during the clarification session (Session 2025-10-09). All technology choices are well-defined within the existing ASP.NET Core 8.0 stack.

## Research Findings

### ASP.NET Core Security Middleware Pipeline
**Decision**: Use ASP.NET Core built-in middleware pipeline with custom security middleware components  
**Rationale**: Provides established patterns for authentication, authorization, and request processing without framework modifications. Integrates seamlessly with existing endpoint structure.  
**Alternatives considered**: Custom security filters (more complex), third-party security frameworks (dependency overhead), API gateway (infrastructure complexity)

### Authentication Strategy Using Existing API Keys
**Decision**: Leverage existing API Keys table with custom authentication handler implementing IAuthenticationHandler  
**Rationale**: Maintains existing authentication mechanism while adding comprehensive endpoint protection. No database schema changes required.  
**Alternatives considered**: JWT tokens (requires new token management), OAuth2 (infrastructure complexity), Basic Auth (less secure)

### Role-Based Authorization Implementation
**Decision**: Use ASP.NET Core Policy-Based Authorization with custom IAuthorizationHandler implementations  
**Rationale**: Native framework support for role hierarchies (Admin > User > ReadOnly), declarative endpoint protection via [Authorize] attributes.  
**Alternatives considered**: Custom authorization filters (reinventing wheel), attribute-based permissions (too granular), resource-based authorization (overkill)

### In-Memory Rate Limiting Architecture
**Decision**: Custom middleware using IMemoryCache with sliding window algorithm and user/endpoint key combinations  
**Rationale**: High performance with 500/200 req/hour thresholds, no database persistence overhead, automatic cleanup via cache expiration.  
**Alternatives considered**: Redis (infrastructure dependency), database persistence (performance overhead), token bucket (complexity vs. requirements)

### File-Based Security Logging
**Decision**: Structured logging with Serilog file sink, JSON format with 30-day rolling retention  
**Rationale**: Searchable audit trail without database performance impact, configurable retention policies, standard .NET logging integration.  
**Alternatives considered**: Database logging (performance overhead), Windows Event Log (platform dependency), Cloud logging (infrastructure dependency)

### Configuration-Based Security Policies
**Decision**: ASP.NET Core IConfiguration with JSON/XML settings files and IOptions pattern for hot reload capability  
**Rationale**: Runtime policy updates without code deployment, environment-specific security configurations, validation support.  
**Alternatives considered**: Database configuration (violates constraint), hardcoded policies (inflexible), environment variables (limited structure)

### Session Management Without Database
**Decision**: ASP.NET Core distributed memory cache with custom session provider implementing IDistributedCache  
**Rationale**: 2-hour idle timeout support, scalable to multiple instances via Redis in production, no database persistence.  
**Alternatives considered**: In-memory sessions (single instance), JWT stateless (requires token refresh), database sessions (constraint violation)

### Input Validation and Sanitization
**Decision**: Data Annotations with custom ValidationAttributes and middleware-based request sanitization  
**Rationale**: Declarative validation rules, automatic model binding validation, XSS/injection prevention at framework level.  
**Alternatives considered**: Manual validation (error-prone), third-party validators (dependency), FluentValidation (complexity vs. needs)

## Performance and Security Considerations

### Request Processing Overhead
- Authentication middleware: ~1-2ms per request (cache-optimized API key lookup)
- Authorization checks: ~0.5ms per request (in-memory role resolution)
- Rate limiting: ~0.1ms per request (memory cache operations)
- Input validation: ~0.5-2ms per request (depends on payload size)
- **Total overhead**: ~2-5ms per request (well within 200ms p95 target)

### Memory Usage Estimates
- Rate limiting cache: ~1MB per 10,000 active users (sliding windows)
- Session storage: ~2MB per 1,000 active sessions (user context)
- Configuration cache: ~10KB (security policies)
- **Total memory footprint**: ~5-10MB for target scale

### Security Attack Vectors Addressed
- **Authentication bypass**: API key validation middleware prevents unauthorized access
- **Privilege escalation**: Role-based authorization with hierarchy enforcement
- **Brute force attacks**: Rate limiting with exponential backoff patterns
- **Injection attacks**: Input validation and output sanitization at middleware level
- **Information disclosure**: Consistent error responses without internal details
- **Session hijacking**: Secure session tokens with timeout enforcement

## Implementation Architecture

### Middleware Registration Order
1. Exception handling middleware (catch security exceptions)
2. Request logging middleware (audit trail)
3. Authentication middleware (API key validation)
4. Authorization middleware (role-based permissions)
5. Rate limiting middleware (request throttling)
6. Input validation middleware (sanitization)
7. Existing application middleware pipeline

### Configuration Structure
```json
{
  "Security": {
    "Authentication": {
      "ApiKeyHeader": "X-Api-Key",
      "CacheTimeout": "00:05:00"
    },
    "Authorization": {
      "Roles": ["Admin", "User", "ReadOnly"],
      "EndpointGroups": {
        "Admin": ["/api/admin/*"],
        "User": ["/api/customers/*", "/api/pigpens/*"],
        "ReadOnly": ["/api/feeds/read"]
      }
    },
    "RateLimiting": {
      "General": { "RequestsPerHour": 500, "WindowMinutes": 60 },
      "Admin": { "RequestsPerHour": 200, "WindowMinutes": 60 }
    },
    "Logging": {
      "RetentionDays": 30,
      "FilePath": "logs/security-{Date}.json"
    },
    "Sessions": {
      "IdleTimeoutHours": 2,
      "CleanupIntervalMinutes": 30
    }
  }
}
```

## Risk Mitigation

### Performance Risks
- **Mitigation**: Async middleware implementation with efficient cache patterns
- **Monitoring**: Response time metrics for security middleware pipeline
- **Fallback**: Circuit breaker pattern for external dependencies

### Security Risks
- **Mitigation**: Defense in depth with multiple validation layers
- **Monitoring**: Real-time security event dashboards and alerting
- **Fallback**: Graceful degradation for non-critical security features

### Operational Risks
- **Mitigation**: Comprehensive logging and configuration validation
- **Monitoring**: Health checks for security subsystem components
- **Fallback**: Rollback procedures for security configuration changes

---

*Research complete - ready for Phase 1 design*