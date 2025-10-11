# scripts/railway-migrate.ps1
param()

if (-not $env:DATABASE_URL) {
    Write-Error "DATABASE_URL is not set. Aborting."; exit 1
}

$rawUrl = $env:DATABASE_URL
# Some providers expose a 'tcp://' style URL; normalize to a postgres scheme so .NET Uri parsing works consistently
if ($rawUrl -match '^tcp://') {
    $rawUrl = $rawUrl -replace '^tcp://', 'postgresql://'
}

$u = [System.Uri]::new($rawUrl)
$userInfo = $u.UserInfo.Split(':')
$dbHost = $u.Host
$dbPort = $u.Port
$dbName = $u.AbsolutePath.TrimStart('/')
$dbUser = $userInfo[0]
$dbPass = $userInfo[1]

$originalHost = $dbHost

# If the provided host is Railway's internal DNS (not reachable from local machine), allow rewriting via proxy env vars
if ($dbHost -like '*.railway.internal') {
    if ($env:RAILWAY_PROXY_HOST) {
        $dbHost = $env:RAILWAY_PROXY_HOST
        if ($env:RAILWAY_PROXY_PORT) {
            $dbPort = [int]$env:RAILWAY_PROXY_PORT
        }
    Write-Output ("Detected internal Railway host '" + $originalHost + "' - rewriting host/port to proxy " + $dbHost + ":" + $dbPort + " using RAILWAY_PROXY_HOST/PORT environment variables.")
    }
    else {
    Write-Error ("The DATABASE_URL host '" + $originalHost + "' appears to be an internal Railway hostname which is not reachable from this machine. `nEither set DATABASE_URL to the proxy connection (eg: postgres://user:pass@shuttle.proxy.rlwy.net:11475/dbname) or set RAILWAY_PROXY_HOST and optionally RAILWAY_PROXY_PORT in your session. Aborting."); exit 1
    }
}

$conn = "Host=$dbHost;Port=$dbPort;Username=$dbUser;Password=$dbPass;Database=$dbName;Ssl Mode=Require;Trust Server Certificate=true"
Write-Output "Running migrations using Npgsql connection string (password redacted)"

# Run EF Core migrations (pass connection directly to the EF tool)
# Ensure design-time factory/EF sees the intended connection string as an env var too
$env:PIGFARM_CONNECTION = $conn
dotnet ef database update --project src/server/PigFarmManagement.Server --context PigFarmDbContext --connection "$conn"

# Check exit code and report
if ($LASTEXITCODE -ne 0) {
    Write-Error ("dotnet ef failed with exit code " + $LASTEXITCODE)
    exit $LASTEXITCODE
}

Write-Output "Migrations applied successfully."