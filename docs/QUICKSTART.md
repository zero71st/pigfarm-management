# Quickstart — Run PigFarmManagement locally

This is a short step-by-step to run the app locally (server + client).

1. Clone repository and install .NET 8 SDK.

2. Start the server (API)
```powershell
cd "D:\dz Projects\PigFarmManagement"
dotnet run --project "src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj" --urls http://localhost:5000
```

3. Start the client (Dev host) in another terminal
```powershell
dotnet run --project "src/client/PigFarmManagement.Client/PigFarmManagement.Client.csproj"
```

4. Open the app
- Visit http://localhost:5000 in a browser.

5. Import customers from POSPOS
- Navigate to Customers, click `Import customers` and use the dialog.
- Mapping will be written to `src/server/PigFarmManagement.Server/customer_id_mapping.json` by default.

6. Debugging client
- Use the provided VS Code launch config (press F5) which runs the client and opens Chrome with remote debugging.
- Or open Chrome manually and attach the debugger to http://localhost:5000

7. Notes
- The database is in-memory for development and seeded as configured. If you want persistent data between runs, update the server EF provider in `PigFarmDbContext`.
- If you encounter port conflicts, use `Get-NetTCPConnection -LocalPort <port>` to find and stop the process or run the client on a different port.

Enjoy developing! If you want, I can add a small diagram or sequence flow to the `docs/` folder describing the client→server→pospos interactions.
