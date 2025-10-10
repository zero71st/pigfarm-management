# Tasks: Server-Side Endpoint Security (Implemented)

**Input**: Design documents from `/specs/010-secure-all-the/`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/

## Execution Flow (main)
```
1. Load plan.md from feature directory
   → Tech stack: C# .NET 8 (ASP.NET Core Web API)
   → Libraries: ASP.NET Core 8.0, Entity Framework Core 8.0, BCrypt.Net-Next 4.0.3
   → Structure: Web application (Blazor WebAssembly + ASP.NET Core Web API)
2. Load design documents:
   → data-model.md: 4 entities (SecurityConfiguration, RateLimitPolicy, SessionToken, InputValidationRule)
   → contracts/: Security API endpoints (auth, permissions, rate limiting)
   → quickstart.md: 6 test scenarios (auth, authorization, rate limiting, validation, logging, performance)
3. Generate tasks by category:
   → Setup: Security feature structure, configuration, dependencies
   → Tests: Contract tests for security endpoints, integration tests for middleware
   → Core: Configuration models, services, middleware components
   → Integration: Middleware pipeline, DI registration, endpoint security
   → Polish: Performance validation, security hardening, documentation
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Middleware pipeline = sequential (order matters)
   → Tests before implementation (TDD)
5. Generated 40 tasks with clear dependencies
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Path Conventions
Based on plan.md structure:
- **Server**: `src/server/PigFarmManagement.Server/`
- **Client**: `src/client/PigFarmManagement.Client/`
- **Shared**: `src/shared/PigFarmManagement.Shared/`

## Implemented Tasks (Based on Branch Commits)

### Server-Side Security Implementation
- [x] **T001** Add RequireAuthorization to all API endpoints in `src/server/PigFarmManagement.Server/Program.cs` and endpoint files
- [x] **T002** Remove debug endpoints from customer endpoints in `src/server/PigFarmManagement.Server/Features/Customers/CustomerEndpoints.cs`
- [x] **T003** Implement RBAC with Admin/User roles in authentication system
- [x] **T004** Add authorization section to Swagger UI in `src/server/PigFarmManagement.Server/Program.cs`
- [x] **T005** Apply 2-role system (Admin/User) in client-side components
- [x] **T006** Add restrict page by role in client navigation and components
- [x] **T007** Remove default admin access from authentication flow
- [x] **T008** Remove test and tools projects from solution
- [x] **T009** Reset database migrations to clean initial state
- [x] **T010** Implement production-safe admin seeding in `src/server/PigFarmManagement.Server/Program.cs`

### Client-Side Security Implementation
- [x] **T011** Update client components for Admin/User role system in `src/client/PigFarmManagement.Client/Features/Admin/`
- [x] **T012** Restrict pages by role in client navigation in `src/client/PigFarmManagement.Client/Shared/NavMenu.razor`
- [x] **T013** Update login status and authentication UI for role-based access

### Database and Deployment
- [x] **T014** Create fresh initial migration for clean database state
- [x] **T015** Configure admin seeding for Railway/Postgres deployment with environment variables
- [x] **T016** Update documentation for Railway deployment and seeding requirements

## Notes
- **[P] tasks**: Different files, no dependencies, can run in parallel
- **Middleware sequence**: Authentication → Authorization → Rate Limiting → Input Validation
- **Manual testing approach**: Use quickstart guide procedures for validation
- **Configuration approach**: No new database tables, uses existing API Keys table
- **Performance target**: 200ms p95 response time for authentication checks
- **Security scope**: 3-role authorization (Admin/User/ReadOnly), 500/200 req/hour rate limits

## Implementation Notes
- Use existing API Keys table for authentication (no schema changes)
- Leverage ASP.NET Core middleware pipeline for security layers
- Configuration-based security policies with hot reload support
- In-memory rate limiting and session storage for performance
- Feature-based architecture in `/Features/Authentication/` directory
- Manual testing using quickstart guide scenarios for validation