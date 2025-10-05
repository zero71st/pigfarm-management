# PigFarmManagement Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-10-05

## Active Technologies
- C# .NET 8 + Blazor WebAssembly, .NET Core Web API, Entity Framework Core (Primary Stack)
- MudBlazor UI Components for modern, responsive interface design
- SQLite (development), Supabase PostgreSQL (production) for data persistence
- Google Maps JavaScript API for location tracking and mapping features
- POSPOS API integration for feed data import and customer synchronization

## Project Structure
```
src/
├── client/PigFarmManagement.Client/     # Blazor WebAssembly Frontend
│   ├── Features/                        # Feature-based architecture
│   │   ├── Customers/                   # Enhanced customer management
│   │   ├── PigPens/                     # Pig pen management
│   │   └── Dashboard/                   # Main dashboard
│   └── Services/                        # Client-side services
├── server/PigFarmManagement.Server/     # .NET 8 Web API Backend
│   ├── Features/                        # Feature-based architecture
│   │   ├── Customers/                   # Customer CRUD + location + POS sync
│   │   ├── PigPens/                     # Pig pen management
│   │   └── Feeds/                       # Feed import and management
│   └── Infrastructure/                  # EF Core, migrations, services
└── shared/PigFarmManagement.Shared/     # Common DTOs and models
```

## Commands
```bash
# Development
cd src/server/PigFarmManagement.Server && dotnet run --urls http://localhost:5000
cd src/client/PigFarmManagement.Client && dotnet run --urls http://localhost:7000

# Database
dotnet ef database update
dotnet ef migrations add <MigrationName>

# Testing
dotnet test
```

## Code Style
- C# .NET 8: Follow standard Microsoft conventions with async/await patterns
- Blazor Components: Use feature-based organization with proper component isolation
- Entity Framework: Code-first approach with proper migrations
- API Design: RESTful endpoints with proper HTTP status codes and OpenAPI documentation

## Recent Changes
- 008-update-manage-customer: Completed enhanced customer management with Google Maps integration, soft deletion, location tracking, POS synchronization, and dual view modes (card/table)
- Enhanced database schema with location fields and soft deletion support
- Implemented comprehensive customer filtering and search functionality
- Added modern UI with icon-only buttons and responsive design
- Integrated Google Maps JavaScript API for location management

## Key Features Implemented
### Enhanced Customer Management (Feature 008)
- Complete CRUD operations with soft deletion and audit trail
- Google Maps integration for customer location tracking
- Dual view modes (card view default, table view) with persistent preferences
- Advanced filtering by status (Active/Inactive/All) and real-time text search
- POS system synchronization with conflict resolution (POS data takes precedence)
- Modern UI with MudBlazor components, icon-only buttons, and responsive design
- Comprehensive validation and error handling

### Database Enhancements
- Entity Framework Core with SQLite (dev) and PostgreSQL (prod)
- Customer location fields: Latitude, Longitude with validation
- Soft deletion: IsDeleted, DeletedAt, DeletedBy fields
- Enhanced audit trail and relationship tracking

### POSPOS Integration
- Feed import with pricing calculations and discount processing
- Customer data synchronization with manual triggers
- Comprehensive logging and error handling

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
