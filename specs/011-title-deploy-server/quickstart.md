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
