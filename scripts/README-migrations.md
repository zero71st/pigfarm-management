Migration scripts for Railway and local use

Overview
- `railway-migrate.sh`: Bash script that converts `DATABASE_URL` to an Npgsql-style connection string and runs `dotnet ef database update`.
- `railway-migrate.ps1`: PowerShell equivalent for Windows.

Railway one-off (recommended)
1. Provision database and set `DATABASE_URL` in Railway project variables.
2. From your local machine with Railway CLI configured:

```bash
# Run the migration script on Railway (reads DATABASE_URL from environment)
railway run ./scripts/railway-migrate.sh
```

Or from the Railway web UI, run a one-off and execute the same script.

Local run (for testing)

PowerShell:
```powershell
$env:DATABASE_URL='postgres://user:pass@host:5432/dbname'
./scripts/railway-migrate.ps1
```

Bash (WSL/Git Bash/CI):
```bash
export DATABASE_URL='postgres://user:pass@host:5432/dbname'
./scripts/railway-migrate.sh
```

Notes & Troubleshooting
- If the migration endpoint `/migrations/run` fails due to missing tables, this script will succeed because it runs EF migrations directly.
- Do NOT commit or print raw DATABASE_URL to logs. Scripts redact the connection display.
- Ensure `dotnet ef` tooling is available in the environment (install the EF tools if necessary).
- For CI (GitHub Actions), consider adding a job that runs these scripts with the `DATABASE_URL` secret set.

If you want, I can also:
- Add a GitHub Actions workflow to run migrations as part of a deploy job
- Create a small Railway entrypoint script to start the app after migration
