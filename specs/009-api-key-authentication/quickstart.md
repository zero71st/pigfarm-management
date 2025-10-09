# Quickstart: API-Key Authentication

**Feature**: 009-api-key-authentication  
**Created**: October 8, 2025  
**Prerequisites**: Development environment with .NET 8, SQLite, and modern browser

## Quick Setup (5 minutes)

### 1. Database Setup
```bash
# Navigate to server project
cd src/server/PigFarmManagement.Server

# Add and apply auth migration
dotnet ef migrations add AddAuthEntities
dotnet ef database update

# Verify tables created
dotnet ef dbcontext info
```

### 2. Seed Initial Admin
```bash
# Run server to trigger admin seed
dotnet run --urls http://localhost:5000

# Default admin credentials (CHANGE IMMEDIATELY):
# Username: admin
# Password: Admin123!
```

### 3. Test Authentication Flow
```bash
# Terminal 1: Start server
cd src/server/PigFarmManagement.Server
dotnet run --urls http://localhost:5000

# Terminal 2: Start client
cd src/client/PigFarmManagement.Client
dotnet run --urls http://localhost:7000
```

### 4. Verify Setup
1. **Open browser**: http://localhost:7000
2. **Navigate to login**: `/auth/login`
3. **Login with admin credentials**
4. **Generate API key**: Should display raw key once
5. **Test API access**: Make authenticated request

## API Testing with curl

### Login and Get API Key
```bash
# Login request
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "Admin123!",
    "keyLabel": "Test Key"
  }'

# Response should include apiKey field
```

### Use API Key for Authenticated Requests
```bash
# Get current user info
curl -X GET http://localhost:5000/api/auth/me \
  -H "X-Api-Key: YOUR_API_KEY_HERE"

# Create new user (admin only)
curl -X POST http://localhost:5000/api/admin/users \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: YOUR_API_KEY_HERE" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "TestPass123!",
    "roles": ["Worker"],
    "isActive": true
  }'
```

## Integration Test Scenarios

### Scenario 1: Admin User Management
**Objective**: Verify complete admin user lifecycle

**Steps**:
1. Admin logs in and gets API key
2. Admin creates new user with Worker role
3. Admin generates API key for new user
4. New user can access Worker-level endpoints
5. New user cannot access Admin-level endpoints
6. Admin revokes user's API key
7. User's requests return 401 Unauthorized

**Expected Results**:
- ✅ Admin can perform all user management operations
- ✅ Role-based access control enforced correctly
- ✅ API key revocation takes effect immediately
- ✅ Audit logs capture all administrative actions

### Scenario 2: Authentication Security
**Objective**: Verify security measures work correctly

**Steps**:
1. Attempt login with invalid credentials → 401
2. Login with valid credentials → API key generated
3. Use API key for authenticated request → Success
4. Use invalid API key → 401
5. Use expired API key → 401
6. Multiple failed login attempts → Rate limiting triggered

**Expected Results**:
- ✅ Invalid credentials properly rejected
- ✅ API key validation works correctly
- ✅ Rate limiting prevents brute force attacks
- ✅ All failed attempts logged for security monitoring

### Scenario 3: Role-Based Authorization
**Objective**: Verify role hierarchy and permissions

**Steps**:
1. Create users with different roles (Admin, Manager, Worker, Viewer)
2. Test access to role-restricted endpoints
3. Verify role inheritance (Admin can access Manager endpoints)
4. Test unauthorized access attempts
5. Verify role changes require new API key

**Expected Results**:
- ✅ Admin has access to all endpoints
- ✅ Manager can access Worker and Viewer endpoints
- ✅ Worker can access Viewer endpoints
- ✅ Viewers have read-only access
- ✅ Role violations return 403 Forbidden

## Common Issues and Solutions

### Issue: Migration Fails
**Symptoms**: `dotnet ef database update` returns errors
**Solutions**:
1. Check connection string in appsettings.json
2. Ensure SQLite file permissions correct
3. Drop and recreate database if needed: `dotnet ef database drop`
4. Verify Entity Framework tools installed: `dotnet tool install --global dotnet-ef`

### Issue: Admin Seed Doesn't Work
**Symptoms**: Cannot login with default admin credentials
**Solutions**:
1. Check server logs for seed errors
2. Verify database connection working
3. Manually create admin user via direct database access
4. Check password hashing configuration

### Issue: API Key Authentication Fails
**Symptoms**: Valid API key returns 401 Unauthorized
**Solutions**:
1. Verify middleware registration order in Program.cs
2. Check API key header format: `X-Api-Key: your-key-here`
3. Confirm API key not expired
4. Check server logs for validation errors
5. Verify HTTPS not required in development

### Issue: CORS Errors in Browser
**Symptoms**: Browser blocks requests to API
**Solutions**:
1. Configure CORS in server Program.cs
2. Ensure client URL in allowed origins
3. Check preflight OPTIONS requests
4. Verify headers allowed in CORS policy

## Development Workflow

### Adding New Protected Endpoint
1. **Create endpoint**: Add method with `[Authorize]` or `[Authorize(Roles="Admin")]`
2. **Add to contracts**: Update OpenAPI documentation
3. **Write tests**: Contract test verifying auth required
4. **Test manually**: Use curl or Postman with API key
5. **Update docs**: Add endpoint to API documentation

### Creating New User Role
1. **Update validation**: Add role to allowed roles list
2. **Update UI**: Add role option to admin interface
3. **Add authorization**: Create role-specific endpoints if needed
4. **Test access**: Verify role-based access control
5. **Document**: Update role hierarchy documentation

### API Key Management
1. **Generation**: Admin creates user and generates initial key
2. **Distribution**: Securely communicate key to user (shown once)
3. **Monitoring**: Review usage logs and detect anomalies
4. **Rotation**: Generate new keys and revoke old ones
5. **Incident Response**: Immediate revocation capability

## Production Checklist

### Security Configuration
- [ ] Change default admin password
- [ ] Configure strong password policy
- [ ] Enable HTTPS with valid certificates
- [ ] Configure rate limiting
- [ ] Set up security headers (HSTS, CSP, etc.)
- [ ] Configure secure API key storage

### Database Configuration
- [ ] Switch to PostgreSQL for production
- [ ] Configure connection pooling
- [ ] Set up database backups
- [ ] Configure audit log retention
- [ ] Index optimization for performance

### Monitoring and Logging
- [ ] Configure structured logging
- [ ] Set up authentication metrics
- [ ] Configure security alerting
- [ ] Monitor API key usage patterns
- [ ] Set up audit log analysis

### Operational Procedures
- [ ] Document user onboarding process
- [ ] Create API key rotation procedures
- [ ] Establish incident response procedures
- [ ] Document emergency admin access
- [ ] Create user offboarding checklist

## Performance Benchmarks

### Expected Performance Targets
- **Authentication Latency**: <50ms for 95% of requests
- **API Key Validation**: <10ms overhead per request
- **Concurrent Users**: Support 1000+ active API keys
- **Database Load**: <100ms for user lookup queries

### Load Testing Commands
```bash
# Install hey for load testing
go install github.com/rakyll/hey@latest

# Test login endpoint
hey -n 1000 -c 10 -m POST \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!"}' \
  http://localhost:5000/api/auth/login

# Test authenticated endpoint
hey -n 10000 -c 50 \
  -H "X-Api-Key: YOUR_API_KEY" \
  http://localhost:5000/api/auth/me
```

## Next Steps

1. **Complete Implementation**: Follow tasks.md for detailed implementation steps
2. **Security Review**: Conduct security audit of authentication flow
3. **Performance Testing**: Validate performance under expected load
4. **Documentation**: Complete operational runbooks and user guides
5. **Deployment**: Follow production checklist for secure deployment

---

*This quickstart guide provides practical steps to get the API-key authentication system running and verified. Follow implementation tasks for detailed development steps.*