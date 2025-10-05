# PigFarmManagement Documentation

Comprehensive pig farm management system with enhanced customer management, POS integration, and feed tracking.

## System Overview

* **Server**: .NET 8 Web API with Entity Framework Core, SQLite database, and POSPOS integration
* **Client**: Blazor WebAssembly with MudBlazor UI components for modern, responsive interface
* **Shared**: Common DTOs and models for seamless client-server communication

## Key Features

### Enhanced Customer Management
- Complete CRUD operations with soft deletion
- Google Maps integration for location tracking
- Dual view modes (card/table) with persistent preferences
- Advanced filtering and real-time search
- POS system synchronization with conflict resolution

### Feed Management & Import
- POSPOS integration for automated feed data import
- Advanced pricing calculations with discount processing
- Feed formula cost integration and validation
- Comprehensive audit trail and reporting

### Modern UI/UX
- Responsive design optimized for desktop and mobile
- Icon-based navigation with tooltips
- Real-time data updates and validation
- Comprehensive error handling and user feedback

## Documentation Structure

- **[QUICKSTART.md](QUICKSTART.md)**: Quick setup and getting started guide
- **[ARCHITECTURE.md](ARCHITECTURE.md)**: System architecture and technical overview
- **[DEPLOYMENT.md](DEPLOYMENT.md)**: Production deployment instructions
- **[RUNBOOK.md](RUNBOOK.md)**: Operational procedures and maintenance
- **[IMPORT_API.md](IMPORT_API.md)**: POS integration and import procedures

## Development Setup

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

> Project constitution: .specify/memory/constitution.md — contains governance, ownership, and template propagation rules.

---
Mock only. No persistence yet.
