# Quickstart: Deploy to Railway Postgres

Prerequisites
- Railway account and a Postgres service
- Railway CLI (optional)
- `dotnet` SDK 8 installed locally for local testing

Steps

1. Provision a Postgres database on Railway and copy `DATABASE_URL` to the project environment.

2. Set production environment variables in Railway project settings:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `DATABASE_URL=<railway-provided-connection-string>`
   - `ADMIN_USERNAME`, `ADMIN_EMAIL`, `ADMIN_PASSWORD`, `ADMIN_APIKEY` (must be set)

3. **Run database migrations BEFORE deploying** (see Migration Strategy below).

4. Deploy the server to Railway.

5. On startup, the application will:
   - parse `DATABASE_URL` and connect to Postgres
   - run the seeder which will fail startup if production secrets are missing
   - NOTE: Migrations are NOT run automatically at startup

## Migration Strategy

### Manual Migration Commands

The application does NOT run migrations automatically at startup. Migrations must be executed manually via one of these methods:

#### Method 1: Railway One-off Command (Recommended for production)
```bash
# Run migrations via Railway CLI
railway run "dotnet ef database update --project src/server/PigFarmManagement.Server --connection \"$DATABASE_URL\""
```

#### Method 2: CI Pipeline Migration Job
Create a CI workflow step that runs migrations before deployment:

```yaml
# Example GitHub Actions step
- name: Run Database Migrations
  run: |
    dotnet ef database update --project src/server/PigFarmManagement.Server --connection "${{ secrets.DATABASE_URL }}"
  env:
    DATABASE_URL: ${{ secrets.DATABASE_URL }}
```

#### Method 3: Via Migration Endpoint (Protected)
After deployment, call the protected migration endpoint:

```bash
curl -X POST https://your-app.railway.app/migrations/run \
  -H "X-Api-Key: YOUR_ADMIN_API_KEY" \
  -H "Content-Type: application/json"
```

#### Method 4: Local Migration with Production Database (Use with caution)
```powershell
# Set DATABASE_URL to production connection string
$env:DATABASE_URL="postgresql://user:password@host:port/database"
dotnet ef database update --project src/server/PigFarmManagement.Server
```

### Migration Rollback Strategy

To rollback to a specific migration:

```bash
# List available migrations
dotnet ef migrations list --project src/server/PigFarmManagement.Server

# Rollback to specific migration
railway run "dotnet ef database update <migration-name> --project src/server/PigFarmManagement.Server --connection \"$DATABASE_URL\""
```

### Migration Safety Notes

- Always backup the database before running migrations in production
- Test migrations on a staging environment that mirrors production data
- Consider using Railway's database forking feature for testing
- Monitor the migration endpoint logs for any failures
- The `/migrations/run` endpoint creates `MigrationJob` records for audit purposes

## Local Development

For local development with SQLite:

```powershell
# No DATABASE_URL set - uses SQLite
dotnet run --project src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj --urls http://localhost:5000
```

For local development with PostgreSQL (testing Railway setup):

```powershell
# Set DATABASE_URL to test against local PostgreSQL
$env:DATABASE_URL="postgresql://user:password@localhost:5432/testdb"
dotnet run --project src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj --urls http://localhost:5000
```

Notes
- Ensure SSL and SSL Mode settings in connection string are compatible with Railway/Postgres client.
- Do not log raw secrets in production. Seeder prints generated secrets only in non-production.
- Migrations are intentionally NOT automatic to provide better control over database schema changes in production.

Prerequisites
- Railway account and a Postgres service
- Railway CLI (optional)
- `dotnet` SDK 8 installed locally for local testing

Steps

1. Provision a Postgres database on Railway and copy `DATABASE_URL` to the project environment.

2. Set production environment variables in Railway project settings:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `DATABASE_URL=<railway-provided-connection-string>`
   - `ADMIN_USERNAME`, `ADMIN_EMAIL`, `ADMIN_PASSWORD`, `ADMIN_APIKEY` (must be set)

3. Deploy the server to Railway.

4. On startup, the application will:
   - parse `DATABASE_URL` and connect to Postgres
   - run EF Core migrations automatically
   - run the seeder which will fail startup if production secrets are missing

5. To run migrations from CI or manually, call the migration endpoint or run a one-off job:

```powershell
dotnet run --project src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj --urls http://localhost:5000
# or use Railway one-off: railway run "dotnet ef database update --project src/server/PigFarmManagement.Server"
```

Notes
- Ensure SSL and SSL Mode settings in connection string are compatible with Railway/Postgres client.
- Do not log raw secrets in production. Seeder prints generated secrets only in non-production.
