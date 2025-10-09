# Quickstart: Server-Side Endpoint Security Without Database Tables

**Date**: October 9, 2025  
**Feature**: 010-secure-all-the  
**Phase**: 1 - Implementation Guide

## Prerequisites

- Existing PigFarmManagement application running
- .NET 8 SDK installed
- Access to existing API Keys table 
- Admin API key for testing security endpoints

## Quick Setup (5 minutes)

### 1. Configure Security Settings
Create or update `appsettings.Development.json`:

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
        "Admin": ["/api/admin/*", "/api/security/*"],
        "User": ["/api/customers/*", "/api/pigpens/*"],
        "ReadOnly": ["/api/feeds/read"]
      }
    },
    "RateLimiting": {
      "General": { "RequestsPerHour": 500, "WindowMinutes": 60 },
      "Admin": { "RequestsPerHour": 200, "WindowMinutes": 60 }
    },
    "Logging": {
      "SecurityEvents": {
        "RetentionDays": 30,
        "FilePath": "logs/security/events-{Date}.json"
      }
    },
    "Sessions": {
      "IdleTimeoutHours": 2,
      "CleanupIntervalMinutes": 30
    }
  }
}
```

### 2. Add Security Middleware (Program.cs)
```csharp
// Add security services
builder.Services.Configure<SecuritySettings>(
    builder.Configuration.GetSection("Security"));

builder.Services.AddScoped<IApiKeyAuthenticationService, ApiKeyAuthenticationService>();
builder.Services.AddScoped<IRoleAuthorizationService, RoleAuthorizationService>();
builder.Services.AddScoped<IRateLimitingService, RateLimitingService>();
builder.Services.AddSingleton<ISecurityEventLogger, SecurityEventLogger>();

// Register middleware in correct order
app.UseMiddleware<SecurityEventLoggingMiddleware>();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseMiddleware<RoleBasedAuthorizationMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<InputValidationMiddleware>();
```

### 3. Secure Existing Endpoints
Add security attributes to controllers:

```csharp
[ApiController]
[Route("api/[controller]")]
[RequireApiKey] // Custom attribute for API key authentication
public class CustomersController : ControllerBase
{
    [HttpGet]
    [RequireRole("User")] // Custom attribute for role authorization
    public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
    {
        // Existing implementation unchanged
    }

    [HttpPost]
    [RequireRole("User")]
    [ValidateInput] // Custom attribute for input validation
    public async Task<ActionResult<Customer>> CreateCustomer(Customer customer)
    {
        // Existing implementation unchanged
    }

    [HttpDelete("{id}")]
    [RequireRole("Admin")] // Admin-only operation
    public async Task<ActionResult> DeleteCustomer(int id)
    {
        // Existing implementation unchanged
    }
}
```

## Validation Tests (15 minutes)

### Test 1: Authentication Works
```bash
# Valid API key - should succeed
curl -H "X-Api-Key: your-valid-api-key" \
     http://localhost:5000/api/customers

# Invalid API key - should return 401
curl -H "X-Api-Key: invalid-key" \
     http://localhost:5000/api/customers

# Missing API key - should return 401
curl http://localhost:5000/api/customers
```

Expected responses:
- Valid key: HTTP 200 with customer data
- Invalid key: HTTP 401 with `authentication_failed` error
- Missing key: HTTP 401 with `authentication_required` error

### Test 2: Authorization Roles Work
```bash
# ReadOnly user tries to create customer - should fail
curl -X POST \
     -H "X-Api-Key: readonly-user-api-key" \
     -H "Content-Type: application/json" \
     -d '{"name": "Test Customer"}' \
     http://localhost:5000/api/customers

# User creates customer - should succeed  
curl -X POST \
     -H "X-Api-Key: user-api-key" \
     -H "Content-Type: application/json" \
     -d '{"name": "Test Customer"}' \
     http://localhost:5000/api/customers

# Admin deletes customer - should succeed
curl -X DELETE \
     -H "X-Api-Key: admin-api-key" \
     http://localhost:5000/api/customers/123
```

Expected responses:
- ReadOnly POST: HTTP 403 with `insufficient_permissions` error
- User POST: HTTP 201 with created customer data
- Admin DELETE: HTTP 204 with no content

### Test 3: Rate Limiting Works
```bash
# Make 5 rapid requests - should succeed
for i in {1..5}; do
  curl -H "X-Api-Key: your-api-key" \
       http://localhost:5000/api/customers
  echo "Request $i completed"
done

# Make 505 requests rapidly - should start returning 429
for i in {1..505}; do
  response=$(curl -s -o /dev/null -w "%{http_code}" \
    -H "X-Api-Key: your-api-key" \
    http://localhost:5000/api/customers)
  
  if [ "$response" == "429" ]; then
    echo "Rate limited at request $i"
    break
  fi
done
```

Expected behavior:
- First 500 requests: HTTP 200 responses
- Request 501+: HTTP 429 with `rate_limit_exceeded` error
- Response headers include `X-RateLimit-Remaining`, `X-RateLimit-Limit`

### Test 4: Input Validation Works
```bash
# Valid input - should succeed
curl -X POST \
     -H "X-Api-Key: user-api-key" \
     -H "Content-Type: application/json" \
     -d '{"name": "Valid Customer Name"}' \
     http://localhost:5000/api/customers

# XSS attempt - should be blocked
curl -X POST \
     -H "X-Api-Key: user-api-key" \
     -H "Content-Type: application/json" \
     -d '{"name": "<script>alert(\"xss\")</script>"}' \
     http://localhost:5000/api/customers

# SQL injection attempt - should be blocked
curl -X POST \
     -H "X-Api-Key: user-api-key" \
     -H "Content-Type: application/json" \
     -d '{"name": "'; DROP TABLE customers; --"}' \
     http://localhost:5000/api/customers
```

Expected responses:
- Valid input: HTTP 201 with created customer
- XSS attempt: HTTP 422 with `validation_failed` error
- SQL injection: HTTP 422 with `validation_failed` error

### Test 5: Security Logging Works
Check log files after running tests:

```bash
# View security events log
cat logs/security/events-2025-10-09.json | jq .

# Should contain entries like:
# {
#   "timestamp": "2025-10-09T14:30:00Z",
#   "userId": "12345", 
#   "action": "Authentication",
#   "result": "Success",
#   "endpoint": "/api/customers"
# }
```

Expected log entries:
- Authentication events (success/failure)
- Authorization checks (permitted/denied) 
- Rate limiting violations
- Input validation failures

## Performance Validation (10 minutes)

### Test 6: Response Time Impact
```bash
# Measure baseline (before security middleware)
time curl -H "X-Api-Key: your-api-key" \
     http://localhost:5000/api/customers

# Compare with security enabled
# Should add ~2-5ms overhead per request
```

### Test 7: Memory Usage
```bash
# Monitor memory before and after 1000 requests
# Security middleware should add ~5-10MB for rate limiting cache
```

### Test 8: Concurrent Load
```bash
# Use Apache Bench or similar tool
ab -n 1000 -c 10 \
   -H "X-Api-Key: your-api-key" \
   http://localhost:5000/api/customers

# All requests should complete successfully
# Rate limiting should be enforced consistently
```

## Security Configuration Examples

### Development Environment
```json
{
  "Security": {
    "RateLimiting": {
      "General": { "RequestsPerHour": 1000 },
      "Admin": { "RequestsPerHour": 500 }
    },
    "Logging": {
      "SecurityEvents": { "RetentionDays": 7 }
    },
    "Sessions": { "IdleTimeoutHours": 8 }
  }
}
```

### Production Environment  
```json
{
  "Security": {
    "RateLimiting": {
      "General": { "RequestsPerHour": 500 },
      "Admin": { "RequestsPerHour": 200 }
    },
    "Logging": {
      "SecurityEvents": { "RetentionDays": 90 }
    },
    "Sessions": { "IdleTimeoutHours": 2 }
  }
}
```

## Troubleshooting

### Common Issues

#### Issue: All requests return 401 
**Cause**: API key authentication misconfigured  
**Solution**: Check `X-Api-Key` header name in configuration matches client requests

#### Issue: Rate limiting not working
**Cause**: Memory cache not configured properly  
**Solution**: Verify `IMemoryCache` is registered in DI container

#### Issue: Logs not being written
**Cause**: File path permissions or disk space  
**Solution**: Check log directory is writable and has sufficient space

#### Issue: Performance degradation
**Cause**: Synchronous middleware operations  
**Solution**: Ensure all middleware uses async/await patterns

### Debug Mode
Enable detailed security logging in development:

```json
{
  "Logging": {
    "LogLevel": {
      "PigFarmManagement.Server.Features.Authentication": "Debug"
    }
  }
}
```

## Next Steps

1. **Monitor Security Events**: Set up log aggregation and alerting
2. **Performance Tuning**: Optimize cache sizes based on actual usage
3. **Extended Validation**: Add custom validation rules for domain-specific security
4. **Session Scaling**: Configure Redis for session storage in production
5. **Security Hardening**: Add additional headers (HSTS, CSP, etc.)

---

*Quickstart complete - security middleware ready for implementation*