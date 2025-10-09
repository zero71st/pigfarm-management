# Admin Seeding Security Guide

## Overview
The admin seeding feature creates an initial admin user and API key when the application starts. This feature is now secured behind environment variables to prevent accidental seeding in production.

## Security Implementation

### Environment Variable Gate
Admin seeding is only enabled when:
1. `SEED_ADMIN=true` environment variable is set
2. Application is NOT running in Production environment

### Default Development Setup
The development launch profile includes `SEED_ADMIN=true` by default, so seeding will work automatically in development mode.

## Usage

### For Development
1. Set environment variable: `SEED_ADMIN=true`
2. Ensure `ASPNETCORE_ENVIRONMENT=Development`
3. Run the application

```bash
# PowerShell
$env:SEED_ADMIN="true"
dotnet run --project src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj

# Bash
SEED_ADMIN=true dotnet run --project src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj
```

### For Production
Admin seeding is automatically disabled in production environments. If you need to seed in production:
1. ⚠️ **NOT RECOMMENDED** - Only for initial setup
2. Temporarily set environment to non-production
3. Use secure, randomly generated credentials

## Seeded Credentials
When seeding runs successfully, you'll see:
```
Seeded admin user: 'admin' with password 'Admin123!'
Initial admin API Key (store this securely):
[generated-api-key]
```

## Security Best Practices
1. **Change default password immediately** after first login
2. **Store API key securely** (password manager, secret store)
3. **Revoke and regenerate** initial API key after setup
4. **Never commit** seed credentials to version control
5. **Monitor** seeding logs in production environments

## File Locations
- Seeding logic: `src/server/PigFarmManagement.Server/Program.cs`
- Launch settings: `src/server/PigFarmManagement.Server/Properties/launchSettings.json`

## Troubleshooting

### Seeding Skipped Message
If you see "Admin seeding skipped. To enable: set SEED_ADMIN=true environment variable (non-production only)":
- Check `SEED_ADMIN` environment variable is set to "true"
- Verify you're not in Production environment
- Restart the application after setting the variable

### No Admin Users Available
If seeding was skipped and you have no admin users:
1. Set `SEED_ADMIN=true`
2. Restart application
3. Or manually create admin user via database migration