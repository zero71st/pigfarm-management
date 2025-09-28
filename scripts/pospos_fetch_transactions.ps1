<#
Fetch POSPOS transactions for a date range and save raw JSON to data/pospos/
Usage:
  $env:POSPOS_API_KEY must be set (same env var approach as customers import)
  .\scripts\pospos_fetch_transactions.ps1 -Start 2025-09-03 -End 2025-09-03

Params: Start (YYYY-MM-DD), End (YYYY-MM-DD)
Defaults: Page=1, Limit=200
#>
param(
    [Parameter(Mandatory=$true)]
    [string]$Start,
    [Parameter(Mandatory=$true)]
    [string]$End,
    [int]$Page = 1,
    [int]$Limit = 200
)

if (-not $env:POSPOS_API_KEY) {
    Write-Error "POSPOS_API_KEY environment variable not set. Please set it to your API key and retry."
    exit 1
}

$base = 'https://go.pospos.co/developer/api/transactions'
$query = "?page=$Page&limit=$Limit&start=$Start&end=$End"
$url = "$base$query"

$headers = @{ 'Authorization' = "Bearer $($env:POSPOS_API_KEY)" }

try {
    $resp = Invoke-RestMethod -Uri $url -Headers $headers -Method Get -ErrorAction Stop
} catch {
    Write-Error "Failed to call POSPOS: $_"
    exit 1
}

# Ensure data dir exists
$dataDir = Join-Path (Resolve-Path .).Path 'data\pospos'
if (-not (Test-Path $dataDir)) { New-Item -ItemType Directory -Path $dataDir | Out-Null }
$outFile = Join-Path $dataDir ("transactions_{0}_{1}.json" -f $Start, $End)
$resp | ConvertTo-Json -Depth 10 | Out-File -FilePath $outFile -Encoding UTF8
Write-Host "Wrote response to: $outFile"