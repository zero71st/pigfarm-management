# Runbook: Development & Import Flow

This runbook explains how to start the system for local development, debug the Blazor client, and run import tasks safely.

Prerequisites
- .NET 8 SDK installed
- Node/Chrome/Edge available for browser testing
- (Optional) Docker if you prefer container-based run

Start the server (API)
```powershell
cd "D:\dz Projects\PigFarmManagement"
dotnet run --project "src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj" --urls http://localhost:5000
```
This will start the server and expose the import endpoints.

Start the client (Blazor WebAssembly) — recommended
```powershell
# from workspace root
dotnet run --project "src/client/PigFarmManagement.Client/PigFarmManagement.Client.csproj"
```
- The client dev host will serve the static files and host the WASM runtime.
- Open http://localhost:5000 in your browser to access the app (server serves the client).

If the client fails to bind due to port-in-use
- detect the PID:
```powershell
(Get-NetTCPConnection -LocalPort 7100 -ErrorAction SilentlyContinue).OwningProcess
```
- kill safely:
```powershell
Stop-Process -Id <pid> -Force
```
- Or start the client on a different port:
```powershell
dotnet run --project "src/client/PigFarmManagement.Client/PigFarmManagement.Client.csproj" --urls "http://localhost:7101;https://localhost:7102"
```

Debugging the client in VS Code (Chrome)
- I added `.vscode/launch.json` and `.vscode/tasks.json` to the project that run the `run-client` task and open Chrome with remote debugging. In VS Code press F5 and choose **Launch Chrome - PigFarm Client**.
- Alternatively, launch Chrome manually with remote debugging:
```powershell
Start-Process "chrome" "--remote-debugging-port=9222 http://localhost:5000"
```

Import run (safe steps)
1. Open Customers page in browser and click `Import customers`. Use the dialog to preview candidates.
2. Select members you want to import and proceed. Mapping is persisted by default.
3. After import, check server logs and `customer_id_mapping.json` to verify mapping entries.

Backfill / fix codes (administrative)
- Before running, back up DB and `customer_id_mapping.json`.
- Call POST `/import/customers/fix-codes` (via curl or Postman).

Troubleshooting
- hostpolicy.dll error when running the client: do not execute the client DLL directly — run via the dev host or serve from the server. See QUICKSTART.md for recommended flow.
- Port-binding errors: see above for how to detect/kill the blocking process or change ports.

Contacts
- If you hit an unexpected runtime NullReferenceException in the client, open browser devtools console and share the stack trace. I can inspect and patch the client accordingly.
