# PigFarmManagement Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-10-08

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
│   │   ├── Authentication/              # API-key auth system (Feature 009)
│   │   ├── Customers/                   # Enhanced customer management
│   │   ├── PigPens/                     # Pig pen management
│   │   └── Dashboard/                   # Main dashboard
│   └── Services/                        # Client-side services
├── server/PigFarmManagement.Server/     # .NET 8 Web API Backend
│   ├── Features/                        # Feature-based architecture
│   │   ├── Authentication/              # API-key auth + user management
│   │   ├── Customers/                   # Customer CRUD + location + POS sync
│   │   ├── PigPens/                     # Pig pen management
│   │   └── Feeds/                       # Feed import and management
│   └── Infrastructure/                  # EF Core, migrations, services
└── shared/PigFarmManagement.Shared/     # Common DTOs and models
    ├── DTOs/                            # Data Transfer Objects
    ├── Domain/                          # Domain Models
    │   ├── Authentication/              # UserEntity, ApiKeyEntity
    │   └── External/                    # External API Models (POSPOS)
    └── Contracts/                       # Service Interfaces
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
- External Models: All external API models (POSPOS) organized in shared/Domain/External folder
- Repository Pattern: Use batch querying for performance optimization with GetByExternalIdsAsync patterns

## Recent Changes
- 009-api-key-authentication: In development - API-key authentication system with admin-managed users, role-based authorization (Admin/Manager/Worker/Viewer), BCrypt password hashing, audit logging, and secure API key lifecycle management
- 008-update-manage-customer: Completed enhanced customer management with Google Maps integration, soft deletion, location tracking, POS synchronization, and dual view modes (card/table)
- Enhanced database schema with location fields and soft deletion support
- Implemented comprehensive customer filtering and search functionality
- Added modern UI with icon-only buttons and responsive design
- Integrated Google Maps JavaScript API for location management
- **Code Architecture Refactoring (October 2025)**: 
  - Moved all external API models (PosposMember, PosposProductDto, etc.) to shared/Domain/External
  - Refactored CustomerImportService to use database-based mapping with ExternalId field
  - Eliminated JSON file persistence in favor of efficient batch database queries
  - Simplified API contracts by removing persistMapping parameters
  - Enhanced repository pattern with GetByExternalIdsAsync for performance optimization

## Key Features Implemented
### API-Key Authentication System (Feature 009) - In Development
- Admin-managed user system with role-based authorization (Admin/Manager/Worker/Viewer)
- Secure API key lifecycle management with X-Api-Key header authentication
- BCrypt password hashing with configurable work factor for security
- SHA-256 API key hashing with salting for secure storage
- Comprehensive audit logging for security monitoring and compliance
- Role hierarchy enforcement with proper permission inheritance
- Automatic admin seeding for initial system setup
- Rate limiting and security headers for production deployment

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
- Customer data synchronization with database-based ExternalId mapping
- Efficient batch querying for customer import operations
- Streamlined API contracts without complex persistence parameters
- Comprehensive logging and error handling

### External API Model Organization
- All external API models centralized in shared/Domain/External folder
- PosposMember.cs: POSPOS member data integration model
- PosposProductDtos.cs: Product, Category, and Unit DTOs for feed imports
- Consistent namespace organization: PigFarmManagement.Shared.Domain.External
- Global using statements for seamless access across client and server

### Database Optimization Patterns
- Repository pattern with batch querying capabilities (GetByExternalIdsAsync)
- ExternalId field as single source of truth for external system mapping
- Elimination of dual persistence (JSON + Database) for simplified architecture
- Enhanced performance through bulk operations and reduced database round trips

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
