# PigFarmManagement Development Guidelines

Last updated: 2025-11-29

## Tech Stack
- **Backend**: C# .NET 8 ASP.NET Core Web API with Entity Framework Core
- **Frontend**: Blazor WebAssembly with MudBlazor UI components
- **Database**: SQLite (dev), Railway PostgreSQL (production)
- **Auth**: API-key based with ApiKeyAuthenticationHandler (ASP.NET Core authentication scheme)
- **External APIs**: POSPOS (feeds/customers), Google Maps JavaScript API (location)

## Feature-Based Architecture (Critical Pattern)
**Why**: Vertical slices by business domain, not horizontal technical layers. Each feature owns its UI, logic, and data access.

```
src/
├── client/Features/{Feature}/          # Client feature slice
│   ├── Pages/{Feature}Page.razor       # Route component
│   ├── Components/                     # Feature-specific UI
│   └── Services/{Feature}Service.cs    # HTTP client wrapper
├── server/Features/{Feature}/          # Server feature slice  
│   ├── {Feature}Endpoints.cs           # Minimal API endpoints (extension methods)
│   ├── {Feature}Service.cs             # Business logic (optional, prefer direct in endpoints)
│   └── {Feature}Repository.cs          # Data access (if complex queries needed)
└── shared/PigFarmManagement.Shared/    # Cross-cutting concerns
    ├── DTOs/                           # Request/response contracts
    ├── Domain/External/                # External API models (POSPOS, etc.)
    └── Models/                         # Domain entities
```

**Key Pattern**: Endpoints use extension methods for registration. See `WebApplicationExtensions.cs`:
```csharp
app.MapCustomerEndpoints();  // Each feature has MapXxxEndpoints()
```

## Development Workflow

### Local Development
```powershell
# Server (defaults to SQLite)
cd src/server/PigFarmManagement.Server
dotnet run --urls http://localhost:5000

# Client  
cd src/client/PigFarmManagement.Client
dotnet run --urls http://localhost:7000

# Database Migrations (from project root)
dotnet ef migrations add MigrationName --project src/server/PigFarmManagement.Server
dotnet ef database update --project src/server/PigFarmManagement.Server
```

### Database Connection Strategy
- **Dev**: SQLite via `Data Source=pigfarm.db` (automatic)
- **Prod**: PostgreSQL via `DATABASE_URL` environment variable (Railway-style)
- **Switching**: Set `DATABASE_URL` env var → auto-switches to PostgreSQL with Npgsql
- See `Program.cs` lines 85-110 for parsing logic

### Railway Deployment (Production)
```bash
# Required environment variables
ASPNETCORE_ENVIRONMENT=Production
DATABASE_URL=${{Postgres.DATABASE_PUBLIC_URL}}
ADMIN_PASSWORD=<strong-password>       # Required, app exits if missing
ADMIN_APIKEY=<secure-api-key>         # Required, app exits if missing

# Optional customization
ADMIN_USERNAME=admin
ADMIN_EMAIL=admin@company.com
ALLOWED_ORIGINS=https://your-app.vercel.app

# Manual migration (before first deploy)
railway run "dotnet ef database update --project src/server/PigFarmManagement.Server"
```

**Critical**: Admin seeding is production-safe. App WILL EXIT on startup if `ADMIN_PASSWORD` or `ADMIN_APIKEY` are missing in production. This is intentional to prevent weak defaults.

## Authentication Architecture (Feature 009)

**Current Implementation**: Direct endpoint pattern with `ApiKeyAuthenticationHandler`
- **Handler**: `ApiKeyAuthenticationHandler.cs` - ASP.NET Core authentication scheme (registered in `Program.cs` line 51)
- **Endpoints**: `AuthEndpoints.cs` - Login/logout with BCrypt password hashing, SHA-256 API key hashing
- **NO middleware**: Previous `ApiKeyMiddleware.cs` implementations were removed (cleaner architecture)
- **NO service layer**: Business logic lives directly in endpoints (simpler for this use case)

**How it works**:
1. User logs in → `POST /api/auth/login` → generates API key → returns to client
2. Client stores API key → sends via `X-Api-Key` header on subsequent requests
3. `ApiKeyAuthenticationHandler` intercepts → validates → populates `HttpContext.User`
4. Endpoints use `[Authorize]` attributes for protection

**Adding new authenticated endpoints**:
```csharp
group.MapPost("/protected", [Authorize] async (HttpContext context) => {
    var userId = context.User.FindFirst("user_id")?.Value;
    // Business logic here
});
```

## Code Patterns

### Minimal API Endpoints (Server)
```csharp
public static class CustomerEndpoints
{
    public static WebApplication MapCustomerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Customers");

        group.MapGet("", async (PigFarmDbContext db) =>
            await db.Customers.Where(c => !c.IsDeleted).ToListAsync());
            
        group.MapPost("", [Authorize] async (CustomerCreateDto dto, PigFarmDbContext db) =>
        {
            var customer = CustomerEntity.FromModel(dto);
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            return Results.Created($"/api/customers/{customer.Id}", customer.ToModel());
        });
        
        return app;
    }
}
```
**Register in `WebApplicationExtensions.cs`**: `app.MapCustomerEndpoints();`

### Blazor Service Pattern (Client)
```csharp
public class CustomerService : ICustomerService
{
    private readonly HttpClient _http;
    
    public async Task<List<Customer>> GetCustomersAsync()
        => await _http.GetFromJsonAsync<List<Customer>>("/api/customers") ?? [];
        
    public async Task<Customer> CreateAsync(CustomerCreateDto dto)
    {
        var response = await _http.PostAsJsonAsync("/api/customers", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Customer>() 
            ?? throw new InvalidOperationException();
    }
}
```

### External API Integration (POSPOS)
- **Models**: All in `shared/Domain/External/` (e.g., `PosposMember.cs`, `PosposProductDtos.cs`)
- **Clients**: HttpClient-based in `server/Services/ExternalServices/` (e.g., `PosposMemberClient.cs`)
- **Mapping**: Use `ExternalId` field on entities for sync (no JSON file persistence)
- **Batch queries**: Repository pattern with `GetByExternalIdsAsync` for performance

## Database Patterns

### Entity Framework Configuration
- **DbContext**: `PigFarmDbContext.cs` with feature entities (Customers, PigPens, Feeds, Users, ApiKeys)
- **Migrations**: PostgreSQL-specific (Railway prod), SQLite fallback (dev)
- **Connection logic**: `Program.cs` parses `DATABASE_URL` → Npgsql for prod, SQLite for dev
- **Soft deletion**: Use `IsDeleted`, `DeletedAt`, `DeletedBy` fields (see `CustomerEntity`)

### Repository Pattern (When Needed)
Only create repositories for complex queries. Most endpoints can use `PigFarmDbContext` directly.
```csharp
public class CustomerRepository : ICustomerRepository
{
    private readonly PigFarmDbContext _context;
    
    // Batch querying for external sync
    public async Task<Dictionary<string, CustomerEntity>> GetByExternalIdsAsync(
        IEnumerable<string> externalIds)
    {
        return await _context.Customers
            .Where(c => externalIds.Contains(c.ExternalId))
            .ToDictionaryAsync(c => c.ExternalId!);
    }
}
```

## Critical Conventions

1. **No middleware for auth** - Use `ApiKeyAuthenticationHandler` (ASP.NET Core authentication scheme)
2. **Extension methods for endpoints** - Each feature has `Map{Feature}Endpoints(this WebApplication app)`
3. **Direct endpoint logic** - Avoid unnecessary service layers unless complexity demands it
4. **External models in shared/** - All POSPOS/external API models go in `Domain/External/`
5. **Production secrets required** - App exits if `ADMIN_PASSWORD`/`ADMIN_APIKEY` missing in production
6. **Feature isolation** - Each feature owns its UI, endpoints, and data access (vertical slices)

## Feature 012: POSPOS Import Enhancement (Latest Member Display)

**Status**: In Development | **Date**: 2025-11-29

**Scope**: Enhance existing POSPOS import workflow to show only the latest customer and disable bulk select-all operation

**Files Modified**:
- Backend: `src/server/PigFarmManagement.Server/Features/Customers/CustomerImportEndpoints.cs`
- Frontend: `src/client/PigFarmManagement.Client/Features/Customers/Components/ImportCandidatesDialog.razor`

**Key Changes**:

1. **API Enhancement** - `GetCandidates()` method:
   - Add optional `source` query parameter: `source=pospos|all` (default: `all`)
   - When `source=pospos`: Return 1 latest member ordered by `CreatedAt DESC, Id DESC`
   - When `source=all`: Return all members (existing behavior, backward compatible)
   - Enhanced error handling: Return 503 with message "POSPOS service unavailable. Please try again later." for service failures

2. **Component Enhancement** - `ImportCandidatesDialog.razor`:
   - Add `_source` field to track context (pospos vs all)
   - Modify `LoadCandidates()` to include source parameter in API URL
   - Conditionally hide select-all checkbox when `_source == "pospos"`
   - Individual row selection remains enabled for both sources
   - Selection state session-scoped (clears on page reload, dialog close)

**Pattern**: Modifies existing import infrastructure rather than creating new search. Server-side filtering (latest member determined by POSPOS API, filtered in endpoint).

**Error Handling**: Distinct 503 status code and message for POSPOS service unavailability vs. other errors (500).

**Selection State**: Session-scoped via component `_candidates` list. No persistence to database.

**Testing**: See `specs/012-update-search-customer/quickstart.md` for validation scenarios.

## Feature 013: Thai Language UI Conversion

**Status**: Planning Complete | **Date**: 2025-11-30

**Scope**: Convert all user-facing UI text from English to Thai language (hardcoded, no language switcher)

**Key Changes**:

1. **Global Culture Configuration** - `Program.cs`:
   - Set `CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("th-TH")`
   - Enables automatic Thai date/number/currency formatting

2. **UI Text Translation** - All Razor components:
   - Direct string replacement in buttons, labels, form fields, dialogs
   - MudBlazor component properties (Label, Title, Text) updated to Thai
   - Table headers, navigation menu items, page titles in Thai

3. **Validation Messages** - DTOs with DataAnnotations:
   - Update `ErrorMessage` parameters to Thai text
   - Example: `[Required(ErrorMessage = "กรุณาระบุชื่อ")]`

4. **Formatting Standards**:
   - **Dates**: ISO format (yyyy-MM-dd)
   - **Numbers**: Arabic numerals (0-9) with comma separators
   - **Currency**: Thai Baht symbol prefix (฿1,234.56)

5. **Technical Text Exclusions**:
   - Backend API errors remain English
   - Logging messages remain English
   - Code comments remain English
   - System documents (PDFs, emails) remain English

**Pattern**: Hardcoded translation approach (no resource files or i18n framework) per simplicity principle. Pure presentation layer change with zero impact on data model or API contracts.

**Affected Areas**: ~30 Razor components in `src/client/Features/`, ~15 DTOs in `src/shared/DTOs/`, ~20 client services

**Testing**: See `specs/013-change-ui-to/quickstart.md` for manual validation scenarios (6 scenarios covering all features and formatting).

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
