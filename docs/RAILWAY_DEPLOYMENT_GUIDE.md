# Railway Deployment Guide

**Last Updated**: October 11, 2025  
**Project**: PigFarm Management System  
**Version**: 1.0

## Overview

This guide documents the complete process of deploying the PigFarm Management System to Railway, including PostgreSQL database setup, migration execution, and admin user seeding.

## Prerequisites

- Railway CLI installed and configured
- .NET 8 SDK installed
- Entity Framework Core tools installed
- Access to Railway project with PostgreSQL service

## Project Structure

```
PigFarmManagement/
├── src/
│   ├── server/PigFarmManagement.Server/     # .NET 8 Web API
│   ├── client/PigFarmManagement.Client/     # Blazor WebAssembly
│   └── shared/PigFarmManagement.Shared/     # Common DTOs
├── scripts/
│   └── railway-migrate.ps1                  # Migration script
├── railway.json                             # Railway configuration
└── Dockerfile                               # Container configuration
```

## Step-by-Step Deployment Process

### 1. Railway CLI Setup

First, ensure you're logged into Railway and have the correct project selected:

```powershell
# Check Railway version
railway --version

# Login to Railway (if not already logged in)
railway login

# Check current status
railway whoami

# Link to your project
railway link
```

### 2. Railway Project Configuration

Verify your Railway project has the following services:
- **Postgres**: Database service
- **pigfarm-management**: Application service

```powershell
# Check current project status
railway status

# Switch between services
railway service  # Interactive service selection
```

### 3. Database Migration Process

#### 3.1 Create PostgreSQL-Specific Migration

The key issue we solved was ensuring migrations are generated specifically for PostgreSQL rather than SQLite:

```powershell
# Remove any existing SQLite-based migrations
dotnet ef migrations remove --project src/server/PigFarmManagement.Server

# Set PostgreSQL connection string for migration generation
$env:DATABASE_URL='postgresql://postgres:password@host:port/database'

# Create PostgreSQL-specific migration
dotnet ef migrations add InitialPostgreSQLCreate --project src/server/PigFarmManagement.Server
```

#### 3.2 Execute Migration Script

Use the provided PowerShell script to migrate the database:

```powershell
# Method 1: Direct execution with DATABASE_URL
$env:DATABASE_URL='postgresql://postgres:password@shuttle.proxy.rlwy.net:11475/railway'
./scripts/railway-migrate.ps1

# Method 2: Using Railway variables (preferred)
railway run ./scripts/railway-migrate.ps1
```

The `railway-migrate.ps1` script automatically:
- Handles Railway internal vs external hostname resolution
- Converts DATABASE_URL to proper Npgsql connection string
- Executes Entity Framework migrations
- Provides detailed logging with password redaction

### 4. Application Environment Configuration

#### 4.1 Database Connection

Configure the application service to use PostgreSQL:

```powershell
# Set database URL to reference the Postgres service
railway variables --set DATABASE_URL='${{Postgres.DATABASE_PUBLIC_URL}}'
```

#### 4.2 Admin User Configuration

Set up environment variables for admin user seeding:

```powershell
# Required admin user credentials
railway variables --set ADMIN_USERNAME=admin
railway variables --set ADMIN_EMAIL=your-email@domain.com
railway variables --set ADMIN_PASSWORD=YourSecurePassword123!
railway variables --set ADMIN_APIKEY=admin-api-key-$(Get-Random -Maximum 9999)
```

#### 4.3 POSPOS API Configuration

**Optional**: Configure POSPOS API integration for customer and product import:

```powershell
# Required: POSPOS API key from your POSPOS admin panel
railway variables --set POSPOS_API_KEY="your-pospos-api-key"

# Required: POSPOS API base URLs (replace with your actual domain)
railway variables --set POSPOS_PRODUCT_API_BASE="https://go.pospos.co/api"
railway variables --set POSPOS_MEMBER_API_BASE="https://go.pospos.co/api"

# Optional: For transaction import functionality
railway variables --set POSPOS_TRANSACTIONS_API_BASE="https://go.pospos.co/api"
```

**Get Your POSPOS API Information**:
1. Log into POSPOS admin panel → Settings → API Settings
2. Copy your API key (format: `pk_live_...`)
3. Verify your POSPOS domain (usually `go.pospos.co`)

**Verification**: Check logs for POSPOS configuration:
```
info: PosposMemberClient configured. MemberApiBase='https://go.pospos.co/api', ApiKeySet=True
```

#### 4.4 Health Endpoint Configuration

**Critical**: Ensure the health endpoint is publicly accessible for Railway's health checks:

```csharp
// In Program.cs - Health endpoint must NOT require authorization
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
```

**Verification**: Check `railway.json` health check configuration:
```json
{
  "deploy": {
    "healthcheckPath": "/health",
    "healthcheckTimeout": 100
  }
}
```

#### 4.5 CORS Configuration for Client Applications

**Critical**: Configure CORS to allow requests from your client application(s):

```powershell
# Set allowed origins for CORS (replace with your actual client URLs)
railway variables --set ALLOWED_ORIGINS="https://your-client-app.vercel.app,https://localhost:7000,https://localhost:5173"
```

**Common Client Deployment Platforms**:
- **Vercel**: `https://your-app-name.vercel.app`
- **Netlify**: `https://your-app-name.netlify.app`
- **Local Development**: `https://localhost:7000,https://localhost:5173`

**Server-side CORS Configuration** (already implemented in Program.cs):
```csharp
// Production CORS policy reads from ALLOWED_ORIGINS environment variable
var envAllowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',', StringSplitOptions.RemoveEmptyEntries);
var allowedOrigins = envAllowedOrigins ?? /* fallback URLs */;
policy.WithOrigins(allowedOrigins)
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();
```

**Verification**: Check logs for CORS configuration:
```
CORS: Allowing origins: https://your-client-app.vercel.app, https://localhost:7000
```

### 5. Application Deployment

Deploy the application with all configurations:

```powershell
# Deploy application
railway up --detach

# Monitor deployment progress
railway logs --follow
```

### 6. Verification

#### 6.1 Check Deployment Status

```powershell
# Verify deployment status
railway status

# Check application logs
railway logs --tail 10

# View all environment variables
railway variables
```

#### 6.2 Access Application

Your application will be available at:
```
https://your-service-name-production.up.railway.app
```

#### 6.3 Admin Login

Use the configured credentials:
- **Username**: `admin`
- **Password**: Your configured password
- **API Key**: Your generated API key

## Common Issues and Solutions

### Issue 1: SQLite vs PostgreSQL Migration Conflicts

**Problem**: Migration generated for SQLite tries to run on PostgreSQL
```
PostgresException: column "ModifiedAt" cannot be cast automatically to type timestamp with time zone
```

**Solution**: 
1. Remove all existing migrations
2. Set DATABASE_URL to PostgreSQL connection
3. Generate new PostgreSQL-specific migration
4. Run migration script

### Issue 2: Admin Seeding Failures

**Problem**: Application fails to start due to missing admin environment variables
```
Production seeder failure: ADMIN_PASSWORD environment variable is required
```

**Solution**: Set all required admin environment variables:
- `ADMIN_USERNAME`
- `ADMIN_EMAIL` 
- `ADMIN_PASSWORD`
- `ADMIN_APIKEY`

### Issue 3: Railway Health Check Failures

**Problem**: Application builds successfully but fails health checks with "service unavailable"
```
Attempt #1 failed with service unavailable. Continuing to retry for 1m38s
Healthcheck failed!
```

**Root Cause**: Health endpoint requires authorization but Railway's health checker cannot authenticate

**Solution**: Ensure health endpoint is publicly accessible without authentication:

```csharp
// ❌ Wrong - requires authentication
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .RequireAuthorization();

// ✅ Correct - publicly accessible
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
```

**Verification**: Check logs show successful container start:
```
Starting Container
info: AdminSeeder[0] Admin user already exists; skipping admin seed.
```

### Issue 4: CORS Policy Blocking Client Requests

**Problem**: Client application cannot access Railway API due to CORS policy
```
Access to fetch at 'https://your-api.railway.app/api/auth/login' from origin 'https://your-client.vercel.app' 
has been blocked by CORS policy: Response to preflight request doesn't pass access control check: 
No 'Access-Control-Allow-Origin' header is present on the requested resource.
```

**Root Cause**: Server doesn't allow requests from client application's domain

**Solution**: Configure CORS with allowed origins:

1. **Set environment variable**:
```powershell
railway variables --set ALLOWED_ORIGINS="https://your-client-app.vercel.app,https://localhost:7000"
```

2. **Verify CORS configuration in logs**:
```
CORS: Allowing origins: https://your-client-app.vercel.app, https://localhost:7000
```

3. **Update origins if client URL changes**:
```powershell
# For multiple origins
railway variables --set ALLOWED_ORIGINS="https://app1.vercel.app,https://app2.netlify.app,https://localhost:7000"
```

**Common Client URLs**:
- Vercel: `https://your-app-name.vercel.app`
- Netlify: `https://your-app-name.netlify.app` 
- Custom domain: `https://yourdomain.com`

### Issue 5: Railway Internal Hostname Issues

**Problem**: Cannot connect from local machine to Railway internal hostnames
```
The DATABASE_URL host 'postgres.railway.internal' appears to be an internal Railway hostname
```

**Solution**: The `railway-migrate.ps1` script automatically handles this by:
1. Detecting internal hostnames
2. Using proxy configuration when available
3. Providing clear error messages with solutions

## Railway Configuration Files

### railway.json
```json
{
  "$schema": "https://railway.app/railway.schema.json",
  "build": {
    "builder": "DOCKERFILE",
    "dockerfilePath": "Dockerfile"
  },
  "deploy": {
    "startCommand": "dotnet PigFarmManagement.Server.dll",
    "healthcheckPath": "/health",
    "healthcheckTimeout": 100,
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 10
  }
}
```

### Required Environment Variables

| Variable | Service | Description | Example |
|----------|---------|-------------|---------|
| `DATABASE_URL` | pigfarm-management | PostgreSQL connection | `${{Postgres.DATABASE_PUBLIC_URL}}` |
| `ADMIN_USERNAME` | pigfarm-management | Admin username | `admin` |
| `ADMIN_EMAIL` | pigfarm-management | Admin email | `admin@company.com` |
| `ADMIN_PASSWORD` | pigfarm-management | Admin password | `SecurePassword123!` |
| `ADMIN_APIKEY` | pigfarm-management | Admin API key | `admin-api-key-1234` |
| `ALLOWED_ORIGINS` | pigfarm-management | CORS allowed origins | `https://app.vercel.app,https://localhost:7000` |

### Optional Environment Variables (POSPOS Integration)

| Variable | Service | Description | Example |
|----------|---------|-------------|---------|
| `POSPOS_API_KEY` | pigfarm-management | POSPOS API key | `pk_live_abc123...` |
| `POSPOS_PRODUCT_API_BASE` | pigfarm-management | POSPOS product API URL | `https://go.pospos.co/api` |
| `POSPOS_MEMBER_API_BASE` | pigfarm-management | POSPOS member API URL | `https://go.pospos.co/api` |
| `POSPOS_TRANSACTIONS_API_BASE` | pigfarm-management | POSPOS transactions API URL | `https://go.pospos.co/api` |

## Security Considerations

1. **Password Security**: Use strong passwords for admin accounts
2. **API Key Management**: Generate unique API keys for each deployment
3. **Environment Variables**: Never commit sensitive values to source control
4. **Database Access**: Use Railway's internal networking when possible
5. **HTTPS**: Railway automatically provides SSL certificates

## Troubleshooting Commands

```powershell
# Check Railway connection
railway whoami

# View project services
railway service

# Check environment variables
railway variables

# View logs in real-time
railway logs --follow

# Check health endpoint specifically
railway logs | Select-String "health|Health|Healthcheck"

# Check CORS configuration in logs
railway logs | Select-String "CORS"

# Test API endpoints with curl
curl "https://your-railway-url.railway.app/health"

# Update CORS origins if client URL changes
railway variables --set ALLOWED_ORIGINS="https://new-client-url.vercel.app,https://localhost:7000"

# Check POSPOS configuration in logs
railway logs | Select-String "POSPOS|Pospos"

# Test POSPOS import endpoints
curl -X POST "https://your-railway-url.railway.app/api/customers/import" -H "X-Api-Key: your-api-key"
curl -X POST "https://your-railway-url.railway.app/api/feeds/import" -H "X-Api-Key: your-api-key"

# Update POSPOS configuration
railway variables --set POSPOS_API_KEY="new-api-key"

# Restart application after fixes
railway up --detach

# Connect to database (requires psql)
railway connect Postgres

# Run one-off command
railway run "your-command"

# Test health endpoint locally (if running locally)
curl http://localhost:8080/health
```

## Future Deployments

For subsequent deployments:

1. **Code Updates**: Simply run `railway up --detach`
2. **Database Changes**: 
   - Create new migration: `dotnet ef migrations add YourMigrationName`
   - Run migration script: `./scripts/railway-migrate.ps1`
3. **Environment Changes**: Use `railway variables --set KEY=VALUE`

## Backup and Recovery

1. **Database Backup**: Use Railway's built-in backup features
2. **Environment Variables**: Document all variables in secure location
3. **Migration Scripts**: Keep migration scripts in version control
4. **Configuration**: Ensure `railway.json` and `Dockerfile` are committed

## Success Indicators

✅ **Database Migration**: `Migrations applied successfully`  
✅ **Admin Seeding**: No admin seeding error messages in logs  
✅ **Health Check Passing**: No "service unavailable" errors in deployment logs  
✅ **CORS Configuration**: Logs show `CORS: Allowing origins: your-client-url.vercel.app`  
✅ **Application Start**: HTTP request logs visible and "Starting Container" message  
✅ **Public Access**: Application accessible via Railway URL  
✅ **Client Connectivity**: No CORS errors when client accesses API  
✅ **Authentication**: Admin login working with configured credentials  

---

## Recent Updates

### October 11, 2025 - CORS Configuration Fix

**Issue Resolved**: Client applications (Vercel, etc.) were blocked by CORS policy when accessing Railway API.

**Changes Made**:
1. **Added CORS Environment Variable**: `ALLOWED_ORIGINS` for flexible origin configuration
2. **Updated Program.cs**: Enhanced CORS policy to read from environment variables
3. **Added Logging**: Server now logs allowed origins for debugging
4. **Deployed Fix**: Railway deployment updated with proper CORS settings

**Configuration Added**:
```bash
railway variables --set ALLOWED_ORIGINS="https://pigfarm-management.vercel.app,https://localhost:7000,https://localhost:5173"
```

**Impact**: Client applications can now successfully communicate with Railway API without CORS errors.

### October 11, 2025 - Health Check Authorization Fix

**Issue Resolved**: Application was failing Railway health checks due to authorization requirement on `/health` endpoint.

**Changes Made**:
1. **Modified Program.cs**: Removed `.RequireAuthorization()` from health endpoint
2. **Updated Documentation**: Added comprehensive health check troubleshooting
3. **Verified Fix**: Successful deployment with passing health checks

**Impact**: Applications now start successfully on Railway without health check failures.

---

## Contact Information

- **Project Owner**: zero71st
- **Repository**: pigfarm-management
- **Railway Project**: upbeat-healing
- **Production URL**: https://pigfarm-management-production.up.railway.app

---

*This guide was created based on the successful deployment completed on October 11, 2025.*