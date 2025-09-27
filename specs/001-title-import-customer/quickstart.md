# Quickstart: Run In-Memory POSPOS Customer Importer

Prereqs:
- .NET SDK (for client/server dev) or use the existing solution runtime
- Set POSPOS API credentials as environment variables:
  - POSPOS_API_BASE (e.g., https://pospos.example/api)
  - POSPOS_API_KEY

Steps:
1. From repo root, run the server in dev mode (it will read env vars):

```powershell
dotnet run --project src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj
```

2. Start the client and open the PigFarm UI; navigate to the Import dialog for POSPOS customers.

3. Trigger the import. The importer will fetch customers and update `customer_id_mapping.json` in the repo root; note that this file is used as a simple mapping persistence for now.

Manual fetch example (PowerShell)
-------------------------------
If you want to query the POSPOS member endpoint directly (no paging), set env vars in the same terminal and run:

```powershell
$Env:POSPOS_API_BASE = 'https://go.pospos.co/developer/api/member?dbname=1611416010865'
$Env:POSPOS_API_KEY  = '<your-key-here>'
$headers = @{ apikey = $Env:POSPOS_API_KEY }
Invoke-RestMethod -Uri $Env:POSPOS_API_BASE -Headers $headers -Method GET | ConvertTo-Json -Depth 6
```

Notes:
- The POSPOS API expects the `apikey` header (not Authorization: Bearer) for this endpoint. The importer scaffolding uses this header by default.
- To persist mapping, run the import endpoint (when available) or call the importer with persistMapping=true.

Notes:
- This quickstart intentionally avoids auth and persistent DB; data is kept in memory and mapping file only.
- For production readiness, add secure credential storage and a database in a follow-up task.
