# Manual Testing Guide: Deploy server to Railway Postgres and seed admin

This document provides a comprehensive checklist for manually validating the Railway deployment functionality.

## Contract Validation Checklist (T002)

Based on `contracts/openapi.yaml`, the following endpoints must be validated:

### 1. Health Check Endpoint
- **Endpoint**: `GET /health`
- **Expected Response**: 200 OK
- **Required Auth**: None
- **Test Command**:
  ```bash
  curl -X GET http://localhost:5000/health
  ```
- **Expected Response Body**: Simple OK or health status
- **Success Criteria**: Returns 200 status code

### 2. Admin Seed Endpoint
- **Endpoint**: `POST /admin/seed`
- **Expected Responses**: 
  - 201: Admin created
  - 200: Admin already exists (no-op)
  - 400: Bad request (e.g., missing secrets in production)
- **Required Auth**: Admin API key
- **Test Commands**:
  ```bash
  # Without force parameter
  curl -X POST http://localhost:5000/admin/seed \
    -H "X-Api-Key: YOUR_ADMIN_API_KEY" \
    -H "Content-Type: application/json"
  
  # With force parameter
  curl -X POST http://localhost:5000/admin/seed \
    -H "X-Api-Key: YOUR_ADMIN_API_KEY" \
    -H "Content-Type: application/json" \
    -d '{"force": true}'
  ```
- **Success Criteria**: 
  - Returns appropriate status codes
  - Does not leak raw secrets in response
  - Idempotent behavior (multiple calls don't create duplicates)

### 3. Migrations Endpoint
- **Endpoint**: `POST /migrations/run`
- **Expected Responses**:
  - 200: Migrations applied
  - 500: Migration failure
- **Required Auth**: Admin API key
- **Test Command**:
  ```bash
  curl -X POST http://localhost:5000/migrations/run \
    -H "X-Api-Key: YOUR_ADMIN_API_KEY" \
    -H "Content-Type: application/json"
  ```
- **Success Criteria**: 
  - Returns 200 on successful migration
  - Returns 500 on migration failure with helpful error message
  - Protected by admin authentication

## Integration Validation Scenarios (T003)

### Scenario 1: Fresh DB Boot (No migrations applied)
**Purpose**: Verify the application handles a fresh database correctly

**Steps**:
1. Delete the existing database file or use a fresh PostgreSQL database
2. Set `DATABASE_URL` to point to the fresh database
3. Start the application
4. Verify the application starts successfully
5. Check that database tables are created (if auto-migration is enabled)
6. Verify admin seeder runs if no admin exists

**Expected Results**:
- Application starts without errors
- Database schema is created
- Admin user is seeded if environment variables are provided
- Logs show appropriate startup messages

### Scenario 2: Missing Production Secrets (Ensure startup fails)
**Purpose**: Verify production safety - app should fail startup if required secrets are missing

**Steps**:
1. Set `ASPNETCORE_ENVIRONMENT=Production`
2. Set `DATABASE_URL` to a valid PostgreSQL connection
3. Do NOT set `ADMIN_PASSWORD` and `ADMIN_APIKEY` environment variables
4. Start the application
5. Verify the application fails to start

**Expected Results**:
- Application exits with non-zero status code
- Clear error message indicating missing required environment variables
- No admin user is created with default/generated credentials

### Scenario 3: Seeder Run with Valid Production Secrets
**Purpose**: Verify production seeding works when all required secrets are provided

**Steps**:
1. Set `ASPNETCORE_ENVIRONMENT=Production`
2. Set `DATABASE_URL` to a valid PostgreSQL connection
3. Set required environment variables:
   - `ADMIN_USERNAME=admin`
   - `ADMIN_EMAIL=admin@company.com`
   - `ADMIN_PASSWORD=SecurePassword123!`
   - `ADMIN_APIKEY=your-secure-api-key-here`
4. Start the application with a fresh database
5. Verify admin user is created
6. Test authentication with the provided API key

**Expected Results**:
- Application starts successfully
- Admin user is created with provided credentials
- API key authentication works
- No raw secrets are logged in production logs
- Seeder is idempotent (running again doesn't create duplicates)

### Scenario 4: Running Migrations via `/migrations/run` Endpoint
**Purpose**: Verify manual migration trigger works correctly

**Steps**:
1. Set up application with database connection
2. Obtain admin API key
3. Call the migrations endpoint:
   ```bash
   curl -X POST http://localhost:5000/migrations/run \
     -H "X-Api-Key: YOUR_ADMIN_API_KEY"
   ```
4. Verify response indicates success or appropriate failure
5. Check database to ensure migrations were applied
6. Verify `MigrationJob` record is created (if implemented)

**Expected Results**:
- Endpoint returns 200 on successful migration
- Database schema is updated
- Migration job is logged/recorded
- Endpoint is protected by admin authentication

## Database Connectivity Tests

### PostgreSQL Connection Validation
**Purpose**: Verify `DATABASE_URL` parsing and PostgreSQL connectivity

**Steps**:
1. Set `DATABASE_URL` to a typical Railway format:
   ```
   postgresql://username:password@host:port/database?sslmode=require
   ```
2. Start the application
3. Verify successful database connection
4. Check application logs for connection-related messages

**Expected Results**:
- Application connects to PostgreSQL successfully
- SSL mode is handled correctly
- Connection pooling is configured appropriately

### SQLite Fallback Test
**Purpose**: Verify local development still works with SQLite

**Steps**:
1. Remove or unset `DATABASE_URL` environment variable
2. Start the application
3. Verify it falls back to SQLite
4. Verify basic functionality works

**Expected Results**:
- Application uses SQLite for local development
- Database file is created locally
- All functionality works as expected

## Security Validation

### API Key Protection Test
**Purpose**: Verify endpoints are properly protected

**Steps**:
1. Try to access protected endpoints without API key
2. Try to access with invalid API key
3. Try to access with valid API key
4. Verify role-based access (admin-only endpoints)

**Expected Results**:
- Unprotected requests return 401 Unauthorized
- Invalid keys return 401 Unauthorized  
- Valid admin keys allow access to admin endpoints
- Non-admin keys are rejected for admin-only endpoints

### Secret Logging Test
**Purpose**: Verify no raw secrets are logged in production

**Steps**:
1. Set `ASPNETCORE_ENVIRONMENT=Production`
2. Run seeder with provided secrets
3. Check all log output
4. Verify no raw passwords or API keys appear in logs

**Expected Results**:
- Logs contain status messages (e.g., "Admin created")
- No raw passwords or API keys in log output
- Environment variable usage is logged (not values)

## QA Checklist Summary

### Pre-Deployment Validation
- [ ] Local build succeeds: `dotnet build src/server/PigFarmManagement.Server` returns exit code 0
- [ ] All required environment variables documented in `docs/DEPLOYMENT.md`

### Health & Basic Connectivity
- [ ] `/health` endpoint returns 200 OK:
  ```bash
  curl -X GET http://localhost:5000/health
  # Expected: 200 OK response
  ```

### Database Connection Tests
- [ ] PostgreSQL connectivity via `DATABASE_URL` works:
  ```bash
  # Set DATABASE_URL=postgresql://user:pass@host:port/db?sslmode=require
  # Start app and verify logs show successful connection
  ```
- [ ] SQLite fallback for local development works:
  ```bash
  # Unset DATABASE_URL, start app
  # Verify SQLite database file created
  ```

### Production Safety Tests
- [ ] Startup fails when production secrets missing:
  ```bash
  # Set ASPNETCORE_ENVIRONMENT=Production
  # Unset ADMIN_PASSWORD and ADMIN_APIKEY
  # Start app - should fail with clear error message and exit code 1
  ```
- [ ] Production seeding works with provided secrets:
  ```bash
  # Set ASPNETCORE_ENVIRONMENT=Production
  # Set ADMIN_PASSWORD=SecurePassword123! 
  # Set ADMIN_APIKEY=your-secure-api-key-here
  # Start app - should create admin user without logging raw secrets
  ```

### Admin Seeding Tests
- [ ] Admin seed endpoint properly protected and functional:
  ```bash
  # Without API key - should return 401
  curl -X POST http://localhost:5000/admin/seed
  
  # With invalid API key - should return 401  
  curl -X POST http://localhost:5000/admin/seed -H "X-Api-Key: invalid"
  
  # With valid admin API key - should return 201 (created) or 200 (exists)
  curl -X POST http://localhost:5000/admin/seed \
    -H "X-Api-Key: YOUR_ADMIN_API_KEY" \
    -H "Content-Type: application/json"
  
  # Idempotency test - run again, should return 200 (no-op)
  curl -X POST http://localhost:5000/admin/seed \
    -H "X-Api-Key: YOUR_ADMIN_API_KEY" \
    -H "Content-Type: application/json"
  ```

### Migration Management Tests
- [ ] Migration endpoint properly protected and functional:
  ```bash
  # Without API key - should return 401
  curl -X POST http://localhost:5000/migrations/run
  
  # With valid admin API key - should return 200 (success)
  curl -X POST http://localhost:5000/migrations/run \
    -H "X-Api-Key: YOUR_ADMIN_API_KEY" \
    -H "Content-Type: application/json"
  ```

### Database Validation Queries
After seeding and migration, verify database state:

```sql
-- Check admin user exists
SELECT Username, Email, RolesCsv, IsActive FROM Users WHERE Username = 'admin';

-- Check admin API key exists
SELECT Label, RolesCsv, IsActive, ExpiresAt FROM ApiKeys WHERE Label LIKE '%Admin%';

-- Check migration job records (if applicable)
SELECT Id, StartedAt, FinishedAt, Status FROM MigrationJobs ORDER BY StartedAt DESC LIMIT 5;

-- Verify database schema is current
SELECT name FROM sqlite_master WHERE type='table';  -- SQLite
-- OR
SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';  -- PostgreSQL
```

### Security & Logging Validation
- [ ] No raw secrets logged in production environment:
  ```bash
  # Review all log output for passwords, API keys, or sensitive data
  # Should see messages like "Admin created" but no raw values
  ```
- [ ] API key authentication and authorization work correctly:
  ```bash
  # Test non-admin API key against admin endpoints (should fail)
  # Test admin API key against admin endpoints (should succeed)
  ```
- [ ] Error messages are helpful and don't leak sensitive information:
  ```bash
  # Test various failure scenarios
  # Verify error responses don't contain sensitive data
  ```

### Railway Deployment Validation
- [ ] All Railway environment variables set per `docs/DEPLOYMENT.md`
- [ ] PostgreSQL database provisioned and connected
- [ ] Admin seeding completed successfully on first deploy
- [ ] Application starts and responds to health checks
- [ ] Manual migration command works via Railway CLI:
  ```bash
  railway run --service your-service-name dotnet ef database update --project src/server/PigFarmManagement.Server
  ```

### Post-Deployment Smoke Test
Complete end-to-end validation sequence:

1. **Health Check**:
   ```bash
   curl -X GET https://your-app.railway.app/health
   # Expected: 200 OK
   ```

2. **Get Admin API Key** (from deployment logs or environment)

3. **Test Admin Endpoints**:
   ```bash
   # Admin seed (should be no-op on existing deployment)
   curl -X POST https://your-app.railway.app/admin/seed \
     -H "X-Api-Key: YOUR_ADMIN_API_KEY"
   # Expected: 200 (admin already exists)
   
   # Migration run
   curl -X POST https://your-app.railway.app/migrations/run \
     -H "X-Api-Key: YOUR_ADMIN_API_KEY"
   # Expected: 200 (migrations up to date)
   ```

4. **Database Connectivity** (via Railway dashboard or CLI):
   ```bash
   railway connect
   # Run SQL queries above to verify data integrity
   ```

## Success Criteria
✅ **All checklist items pass**  
✅ **No sensitive data in logs**  
✅ **Admin user can be created and used**  
✅ **Database migrations work correctly**  
✅ **Application handles production environment safely**  
✅ **Railway deployment is successful and functional**