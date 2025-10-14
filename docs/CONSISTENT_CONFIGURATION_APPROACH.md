# Consistent Configuration Approach

## Overview
This document explains the improved configuration approach that provides consistency between development and production environments by using `appsettings.{Environment}.json` files instead of mixing settings between `launchSettings.json` and configuration files.

## Why This Approach is Better

### ✅ **Environment Parity**
- Same configuration structure for development and production
- Easy to compare settings between environments
- Reduces "works on my machine" issues

### ✅ **Clear Separation of Concerns**
- `launchSettings.json`: Only IDE/launch-specific settings
- `appsettings.{Environment}.json`: Application configuration
- Environment variables: Production secrets and overrides

### ✅ **Better Maintainability**
- All application settings in one place per environment
- Easier to track configuration changes
- Less confusion about where settings come from

## File Structure

### `launchSettings.json` (IDE/Launch Settings Only)
```json
{
  "profiles": {
    "PigFarmManagement.Server": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "SEED_ADMIN": "true"
      },
      "applicationUrl": "https://localhost:5001;http://localhost:5000"
    }
  }
}
```

**Contains only:**
- Environment name (`ASPNETCORE_ENVIRONMENT`)
- Launch URLs (`applicationUrl`)  
- IDE-specific settings (`launchBrowser`)
- System-level overrides (like `SEED_ADMIN` for development)

### `appsettings.Development.json` (Development Configuration)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "PigFarmManagement.Server.Features.Authentication": "Information",
      "PigFarmManagement.Server.Features.Admin": "Information",
      "AdminSeeder": "Information"
    }
  },
  "SeedAdmin": true,
  "AllowedOrigins": [
    "https://localhost:7000",
    "https://localhost:5173",
    "http://localhost:7000",
    "http://localhost:5173"
  ],
  "GoogleMaps": {
    "ApiKey": "your-google-maps-api-key-for-dev",
    "DefaultZoom": 10,
    "DefaultCenter": {
      "Latitude": 13.7563,
      "Longitude": 100.5018
    }
  },
  "Pospos": {
    "ApiKey": "your-development-pospos-jwt-token",
    "ProductApiBase": "https://go.pospos.co/developer/api/stock",
    "MemberApiBase": "https://go.pospos.co/developer/api/member?dbname=1611416010865",
    "TransactionsApiBase": "https://go.pospos.co/developer/api/transactions"
  }
}
```

**Contains:**
- Development-specific logging levels
- Local CORS origins (localhost ports)
- Development API keys (safe to commit)
- Development database settings

### `appsettings.Production.json` (Production Configuration)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "PigFarmManagement.Server.Features.Authentication": "Information",
      "PigFarmManagement.Server.Features.Admin": "Information",
      "AdminSeeder": "Information"
    }
  },
  "AllowedOrigins": [
    "https://pigfarm-management.vercel.app",
    "https://pigfarm-management-client.vercel.app",
    "https://zero71st-pigfarm-management.vercel.app"
  ],
  "GoogleMaps": {
    "ApiKey": "${GOOGLE_MAPS_API_KEY}",
    "DefaultZoom": 10,
    "DefaultCenter": {
      "Latitude": 37.7749,
      "Longitude": -122.4194
    }
  },
  "Pospos": {
    "ApiKey": "${POSPOS_API_KEY}",
    "ProductApiBase": "${POSPOS_PRODUCT_API_BASE}",
    "MemberApiBase": "${POSPOS_MEMBER_API_BASE}",
    "TransactionsApiBase": "${POSPOS_TRANSACTIONS_API_BASE}"
  }
}
```

**Contains:**
- Production logging (Warning level)
- Production CORS origins (Vercel domains)
- Environment variable placeholders for secrets
- Production-specific defaults

## Configuration Loading Order

.NET automatically loads configuration in this priority order:

1. `appsettings.json` (base settings)
2. `appsettings.{Environment}.json` (environment-specific)
3. Environment variables (highest priority - overrides JSON)

## How It Works

### Development Environment
```bash
dotnet run
```

1. ✅ Reads `ASPNETCORE_ENVIRONMENT=Development` from `launchSettings.json`
2. ✅ Loads `appsettings.json` (base)
3. ✅ Loads `appsettings.Development.json` (dev overrides)
4. ✅ Applies environment variables from `launchSettings.json`

### Production Environment (Railway)
```bash
# Railway automatically sets ASPNETCORE_ENVIRONMENT=Production
```

1. ✅ Reads `ASPNETCORE_ENVIRONMENT=Production` from Railway
2. ✅ Loads `appsettings.json` (base)
3. ✅ Loads `appsettings.Production.json` (prod settings)
4. ✅ Applies Railway environment variables (secrets)

## Benefits

### 🔍 **Easy Environment Comparison**
```bash
# Compare settings between environments
diff appsettings.Development.json appsettings.Production.json
```

### 🚀 **Deployment Consistency**
- Both environments use same configuration structure
- Production secrets come from environment variables
- No surprises between local and deployed versions

### 📝 **Clear Configuration History**
- Git tracks all configuration changes
- Easy to see what changed between environments
- Better code reviews for configuration changes

### 🛠 **Easier Debugging**
- Clear hierarchy: JSON → Environment Variables
- Know exactly where each setting comes from
- Less confusion about configuration precedence

## Migration from Old Approach

### Before (Mixed)
- Application settings scattered between `launchSettings.json` and `appsettings.json`
- Hard to compare dev vs prod configurations
- Unclear precedence rules

### After (Consistent)
- All application settings in environment-specific JSON files
- `launchSettings.json` only for launch/IDE settings
- Clear precedence: JSON < Environment Variables

## Best Practices

### ✅ Do
- Keep all app configuration in `appsettings.{Environment}.json`
- Use environment variables for secrets in production
- Use descriptive placeholder values in development
- Document any environment-specific differences

### ❌ Don't
- Put application secrets in `appsettings.json` files
- Mix application settings in `launchSettings.json`
- Use different configuration structures between environments
- Commit real API keys or secrets to Git

## Verification

Test that configuration is working correctly:

```bash
# Development
dotnet run
# Should load: appsettings.json → appsettings.Development.json → launchSettings.json env vars

# Production (Railway)
railway up
# Should load: appsettings.json → appsettings.Production.json → Railway env vars
```

Both environments now follow the same configuration pattern with only environment-specific values differing.