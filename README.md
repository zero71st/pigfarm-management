# PigFarmManagement (Mock Prototype)

Initial scaffold based on PRD. Contains:

* Server: Minimal API (.NET 8) with in-memory mock data
* Shared: DTO / record models
* Client: Blazor WebAssembly basic UI for Pig Pens list + detail (feeds, deposits, harvest)

## Run (Dev)

1. Open solution folder `src` in VS Code / terminal
2. Run server:
```
dotnet run --project .\server\PigFarmManagement.Server\PigFarmManagement.Server.csproj
```
(Default: http://localhost:5000 if you set ASPNETCORE_URLS; otherwise shown in output.)
3. Run client:
```
dotnet run --project .\client\PigFarmManagement.Client\PigFarmManagement.Client.csproj
```
4. Open the client dev URL printed (typically https://localhost:7xxx) and ensure the server base address in `Program.cs` matches the server port.

## Next Steps

* Add proper solution file & projects referencing (generate via `dotnet new sln && dotnet sln add ...`)
* Implement authentication & roles
* Persist data to Supabase (PostgreSQL)
* Integrate POSPOS API sync
* Add reporting & printing endpoints
* Refactor into layered architecture (Domain, Application, Infrastructure)

---
Mock only. No persistence yet.
