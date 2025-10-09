# Security Feature Manual Testing Script

## Feature 010: Secure All API Endpoints

This document provides step-by-step manual testing procedures for validating the security implementation.

## Prerequisites

1. **Server Running**: Start the development server
   ```bash
   cd src/server/PigFarmManagement.Server
   dotnet run --urls http://localhost:5000
   ```

2. **Admin User Setup**: Ensure admin user is seeded
   ```bash
   # Set environment variable to enable seeding (development only)
   $env:SEED_ADMIN = "true"
   dotnet run
   ```

3. **Tools Required**:
   - curl (command line)
   - Postman or similar REST client
   - Browser for Swagger UI testing

## Test Cases

### TC001: API Key Authentication

#### Test 1.1: Missing API Key
```bash
curl -X GET http://localhost:5000/api/customers
```
**Expected**: 401 Unauthorized with error message

#### Test 1.2: Invalid API Key
```bash
curl -X GET http://localhost:5000/api/customers \
  -H "X-Api-Key: invalid-key-123"
```
**Expected**: 401 Unauthorized with "INVALID_API_KEY" error

#### Test 1.3: Valid API Key
```bash
# Use the API key from admin seeding output
curl -X GET http://localhost:5000/api/customers \
  -H "X-Api-Key: [ADMIN_API_KEY_FROM_SEEDING]"
```
**Expected**: 200 OK with customer data

### TC002: Authorization (Role-Based Permissions)

#### Test 2.1: Admin Access to All Endpoints
```bash
# GET (read:customers)
curl -X GET http://localhost:5000/api/customers \
  -H "X-Api-Key: [ADMIN_API_KEY]"

# POST (write:customers) 
curl -X POST http://localhost:5000/api/customers \
  -H "X-Api-Key: [ADMIN_API_KEY]" \
  -H "Content-Type: application/json" \
  -d '{"firstName":"Test","lastName":"Customer","code":"TEST001"}'

# DELETE (delete:customers)
curl -X DELETE http://localhost:5000/api/customers/[CUSTOMER_ID] \
  -H "X-Api-Key: [ADMIN_API_KEY]"
```
**Expected**: All requests succeed (200/201/204)

#### Test 2.2: ReadOnly Role Restrictions
```bash
# Create ReadOnly user API key first through admin endpoints
# Then test limited access:

# GET should work
curl -X GET http://localhost:5000/api/customers \
  -H "X-Api-Key: [READONLY_API_KEY]"

# POST should fail
curl -X POST http://localhost:5000/api/customers \
  -H "X-Api-Key: [READONLY_API_KEY]" \
  -H "Content-Type: application/json" \
  -d '{"firstName":"Test","lastName":"Customer"}'
```
**Expected**: GET succeeds, POST returns 403 Forbidden

### TC003: Rate Limiting

#### Test 3.1: Normal Usage
```bash
# Send multiple requests within limits
for i in {1..10}; do
  curl -X GET http://localhost:5000/api/customers \
    -H "X-Api-Key: [ADMIN_API_KEY]" \
    -H "X-Request-Id: test-$i"
done
```
**Expected**: All requests succeed, check rate limit headers

#### Test 3.2: Rate Limit Exceeded
```bash
# Send requests rapidly to exceed limits (Admin: 1000/hour, User: 500/hour, ReadOnly: 200/hour)
# For ReadOnly user, send 201+ requests quickly:
for i in {1..201}; do
  curl -X GET http://localhost:5000/api/customers \
    -H "X-Api-Key: [READONLY_API_KEY]" \
    -s -o /dev/null
done

# This request should be blocked:
curl -X GET http://localhost:5000/api/customers \
  -H "X-Api-Key: [READONLY_API_KEY]" \
  -v
```
**Expected**: Final request returns 429 Too Many Requests

### TC004: Session Management

#### Test 4.1: Session Creation
```bash
# Authenticate to get session
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!@#"}'
```
**Expected**: 200 OK with session ID and API key

#### Test 4.2: Session Usage
```bash
# Use session ID in subsequent requests
curl -X GET http://localhost:5000/api/customers \
  -H "X-Api-Key: [API_KEY]" \
  -H "X-Session-Id: [SESSION_ID]"
```
**Expected**: 200 OK, session should be refreshed

#### Test 4.3: Session Expiration
```bash
# Wait for session timeout (2 hours by default) or manually expire
# Then try to use expired session
curl -X GET http://localhost:5000/api/customers \
  -H "X-Api-Key: [API_KEY]" \
  -H "X-Session-Id: [EXPIRED_SESSION_ID]"
```
**Expected**: 401 Unauthorized due to expired session

### TC005: Security Headers and Middleware

#### Test 5.1: Security Headers Present
```bash
curl -I http://localhost:5000/api/customers \
  -H "X-Api-Key: [ADMIN_API_KEY]"
```
**Expected**: Response includes security headers:
- X-RateLimit-Limit
- X-RateLimit-Remaining
- X-RateLimit-Reset

#### Test 5.2: Excluded Paths Work Without Auth
```bash
# Health check should work without authentication
curl -X GET http://localhost:5000/health

# Swagger should work without authentication  
curl -X GET http://localhost:5000/swagger
```
**Expected**: 200 OK responses without authentication

### TC006: Error Handling

#### Test 6.1: Detailed vs Simple Error Messages
```bash
# Check if detailed errors are disabled in production mode
curl -X GET http://localhost:5000/api/customers \
  -H "X-Api-Key: invalid-key"
```
**Expected**: Generic error message (not detailed) in production

#### Test 6.2: Malformed Requests
```bash
# Test with malformed JSON
curl -X POST http://localhost:5000/api/customers \
  -H "X-Api-Key: [ADMIN_API_KEY]" \
  -H "Content-Type: application/json" \
  -d '{"invalid json"}'
```
**Expected**: 400 Bad Request with appropriate error

## Validation Checklist

After running all test cases, verify:

- [ ] API key authentication works correctly
- [ ] Role-based authorization enforces permissions
- [ ] Rate limiting blocks excessive requests
- [ ] Session management creates/validates/expires sessions
- [ ] Security headers are present in responses
- [ ] Error responses are appropriate and don't leak sensitive info
- [ ] Excluded paths work without authentication
- [ ] Server logs security events appropriately

## Test Data Cleanup

After testing:
1. Remove test customers created during testing
2. Check server logs for any security warnings/errors
3. Verify rate limit counters reset appropriately
4. Clear test sessions if needed

## Common Issues and Troubleshooting

### Issue: API key not found
- **Cause**: Admin seeding not completed or API key copied incorrectly
- **Solution**: Re-run server with SEED_ADMIN=true and copy the generated API key

### Issue: Rate limiting not working
- **Cause**: In-memory cache cleared or service not registered
- **Solution**: Check DI registration and ensure services are singleton

### Issue: Sessions not persisting
- **Cause**: In-memory session storage cleared on server restart
- **Solution**: This is expected behavior for development

### Issue: CORS errors in browser
- **Cause**: Client trying to access server from different origin
- **Solution**: Update CORS configuration in Program.cs

## Performance Testing

For load testing the security implementation:

1. **Rate Limit Performance**:
   ```bash
   # Use apache bench or similar to test rate limiting under load
   ab -n 1000 -c 10 -H "X-Api-Key: [API_KEY]" http://localhost:5000/api/customers
   ```

2. **API Key Validation Performance**:
   - Monitor response times with caching enabled vs disabled
   - Check memory usage with large numbers of concurrent users

3. **Session Performance**:
   - Test with many concurrent sessions
   - Monitor memory usage of in-memory session store

## Security Review

Final security validation:
- [ ] No API keys or sensitive data logged in plain text
- [ ] Error messages don't expose internal system details
- [ ] Rate limiting prevents abuse
- [ ] Session timeouts work as configured
- [ ] Authentication bypass attempts fail
- [ ] Authorization checks enforce role permissions correctly