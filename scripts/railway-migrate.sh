#!/usr/bin/env bash
set -euo pipefail

# scripts/railway-migrate.sh
# Run EF Core migrations using Railway's DATABASE_URL env var.
# Usage: railway run ./scripts/railway-migrate.sh

if [ -z "${DATABASE_URL:-}" ]; then
  echo "DATABASE_URL is not set. Aborting." >&2
  exit 1
fi

# Convert DATABASE_URL (postgres://user:pass@host:port/db) to Npgsql connection string
PY_CONV=$(python - <<'PY'
import os,urllib.parse
u=urllib.parse.urlparse(os.environ['DATABASE_URL'])
print(f"Host={u.hostname};Port={u.port};Username={u.username};Password={u.password};Database={u.path.lstrip('/')}"+";Ssl Mode=Require;Trust Server Certificate=true")
PY
)

echo "Using connection: <redacted>"

# Run EF Core migrations
dotnet ef database update --project src/server/PigFarmManagement.Server --context PigFarmDbContext -- --connection "$PY_CONV"

echo "Migrations applied successfully."