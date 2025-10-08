# Tasks: API-Key Authentication System

**Input**: Design documents from `/specs/009-api-key-authentication/`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/

## Execution Flow (main)
```
1. Load plan.md from feature directory
   → If not found: ERROR "No implementation plan found"
   → Extract: tech stack, libraries, structure
2. Load optional design documents:
   → data-model.md: Extract entities → model tasks
   → contracts/: Each file → contract test task
   → research.md: Extract decisions → setup tasks
3. Generate tasks by category:
   → Setup: project init, dependencies, linting
   → Core: models, services, CLI commands
   → Integration: DB, middleware, logging
   → Polish: unit tests, performance, docs
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same file = sequential (no [P])
   → Implementation before integration
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph
7. Create parallel execution examples
8. Validate task completeness:
   → All contracts have implementations?
   → All entities have models?
```

## Task Summary

**Total Tasks**: 23 implementation tasks
**Parallel Groups**: 3 groups with [P] marking
**Dependencies**: Models → Services → Endpoints → Integration → Polish

## Core Implementation Tasks

### [X] T001: Setup Project Dependencies [P]
**Description**: Add required NuGet packages for authentication implementation
**Files**: 
- `src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj`
- `src/client/PigFarmManagement.Client/PigFarmManagement.Client.csproj`
**Dependencies**: None
**Details**:
- Server: BCrypt.Net-Next, Microsoft.AspNetCore.Authentication
- Client: System.Net.Http extensions for authentication
- Shared: System.ComponentModel.DataAnnotations

### [X] T002: Create UserEntity Model [P]
**Description**: Implement UserEntity with authentication fields and audit properties
**Files**: `src/shared/PigFarmManagement.Shared/Domain/Authentication/UserEntity.cs`
**Dependencies**: T001
**Details**:
- GUID primary key with proper Entity Framework configuration
- Username, Email, PasswordHash, Roles (CSV), IsActive fields
- Audit fields: CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
- Soft delete: IsDeleted, DeletedAt, DeletedBy
- Role helper methods: HasRole(), IsInRole(), GetRoles()

### [X] T003: Create ApiKeyEntity Model [P]
**Description**: Implement ApiKeyEntity with lifecycle management and security features
**Files**: `src/shared/PigFarmManagement.Shared/Domain/Authentication/ApiKeyEntity.cs`
**Dependencies**: T001
**Details**:
- GUID primary key, relationship to UserEntity
- KeyHash (SHA-256), Label, ExpiresAt, IsActive
- Usage tracking: LastUsedAt, UsageCount
- Audit fields: CreatedAt, CreatedBy, RevokedAt, RevokedBy
- Validation methods: IsExpired(), IsValid(), CanBeUsed()

### [X] T004: Create Authentication DTOs [P]
**Description**: Implement data transfer objects for authentication API contracts
**Files**: `src/shared/PigFarmManagement.Shared/Contracts/Authentication/`
**Dependencies**: T001
**Details**:
- LoginRequest, LoginResponse, UserInfo records
- CreateUserRequest, UpdateUserRequest, ApiKeyRequest records
- Validation attributes and proper documentation
- Error response DTOs for authentication failures

### [X] T005: Update DbContext Configuration
**Description**: Add authentication entities to PigFarmDbContext and configure relationships
**Files**: `src/server/PigFarmManagement.Server/Infrastructure/Data/PigFarmDbContext.cs`
**Dependencies**: T002, T003
**Details**:
- Add DbSet<UserEntity> Users and DbSet<ApiKeyEntity> ApiKeys
- Configure entity relationships (User 1:N ApiKeys)
- Add indexes for performance (Username, Email, KeyHash)
- Configure cascade delete and constraints

### [X] T006: Create Entity Framework Migration
**Description**: Generate and review EF Core migration for authentication tables
**Files**: `src/server/PigFarmManagement.Server/Migrations/`
**Dependencies**: T005
**Details**:
- Run: `dotnet ef migrations add AddAuthenticationEntities`
- Review generated migration for correctness
- Ensure proper indexes and constraints
- Test migration on clean database

### [X] T007: Implement Password Utilities [P]
**Description**: Create secure password hashing utilities using BCrypt
**Files**: `src/server/PigFarmManagement.Server/Features/Authentication/Helpers/PasswordUtil.cs`
**Dependencies**: T001
**Details**:
- HashPassword() method with configurable work factor
- VerifyPassword() for authentication
- GenerateTemporaryPassword() for admin user creation
- Security configuration from appsettings

### [X] T008: Implement API Key Utilities [P]
**Description**: Create API key generation and hashing utilities
**Files**: `src/server/PigFarmManagement.Server/Features/Authentication/Helpers/ApiKeyHash.cs`
**Dependencies**: T001
**Details**:
- GenerateApiKey() using cryptographically secure RNG
- HashApiKey() using SHA-256 for storage
- ValidateApiKey() for middleware validation
- Key format and length configuration

### [X] T009: Create Authentication Repository Interface
**Description**: Define IAuthRepository interface for authentication data access
**Files**: `src/server/PigFarmManagement.Server/Infrastructure/Repositories/IAuthRepository.cs`
**Dependencies**: T002, T003, T004
**Details**:
- User CRUD operations: GetByUsername, GetByEmail, CreateUser, UpdateUser
- API key operations: CreateApiKey, GetApiKey, RevokeApiKey, GetUserApiKeys
- Authentication: ValidateCredentials, UpdateLastLogin
- Async methods with proper return types

### [X] T010: Implement Authentication Repository
**Description**: Implement AuthRepository with Entity Framework operations
**Files**: `src/server/PigFarmManagement.Server/Infrastructure/Repositories/AuthRepository.cs`
**Dependencies**: T005, T009
**Details**:
- Implement all IAuthRepository methods
- Use Entity Framework with proper tracking
- Include error handling and logging
- Optimize queries with appropriate includes

### T011: Create Authentication Service Interface
**Description**: Define IAuthService interface for business logic operations
**Files**: `src/server/PigFarmManagement.Server/Features/Authentication/IAuthService.cs`
**Dependencies**: T004, T009
**Details**:
- Login operations: LoginAsync, LogoutAsync
- User management: CreateUserAsync, UpdateUserAsync, DeactivateUserAsync
- API key management: GenerateApiKeyAsync, RevokeApiKeyAsync
- Validation: ValidateApiKeyAsync, CheckPermissions

### T012: Implement Authentication Service
**Description**: Implement AuthService with business logic and security validation
**Files**: `src/server/PigFarmManagement.Server/Features/Authentication/AuthService.cs`
**Dependencies**: T007, T008, T010, T011
**Details**:
- Implement all IAuthService methods
- Password validation and hashing
- API key lifecycle management
- Role-based permission checking
- Comprehensive audit logging

### T013: Create API Key Middleware
**Description**: Implement middleware to validate X-Api-Key header and populate claims
**Files**: `src/server/PigFarmManagement.Server/Features/Authentication/ApiKeyMiddleware.cs`
**Dependencies**: T008, T010
**Details**:
- Extract X-Api-Key header from requests
- Validate API key hash against database
- Populate ClaimsPrincipal with user and role claims
- Handle expired and inactive keys
- Performance optimization with caching

### T014: Create Authentication Endpoints
**Description**: Implement REST API endpoints for authentication operations
**Files**: `src/server/PigFarmManagement.Server/Features/Authentication/AuthEndpoints.cs`
**Dependencies**: T011, T012
**Details**:
- POST /api/auth/login for user authentication
- POST /api/auth/logout for session termination
- GET /api/auth/me for current user information
- Proper HTTP status codes and error handling
- OpenAPI documentation attributes

### T015: Create Admin User Management Endpoints
**Description**: Implement REST API endpoints for admin user management
**Files**: `src/server/PigFarmManagement.Server/Features/Admin/AdminUserEndpoints.cs`
**Dependencies**: T011, T012
**Details**:
- GET /api/admin/users for user listing
- POST /api/admin/users for user creation
- PUT /api/admin/users/{id} for user updates
- DELETE /api/admin/users/{id} for user deactivation
- Require Admin role authorization

### T016: Create API Key Management Endpoints
**Description**: Implement REST API endpoints for API key lifecycle management
**Files**: `src/server/PigFarmManagement.Server/Features/Authentication/ApiKeyEndpoints.cs`
**Dependencies**: T011, T012
**Details**:
- GET /api/auth/keys for user's API keys
- POST /api/auth/keys for key generation
- DELETE /api/auth/keys/{id} for key revocation
- Proper role-based access control

### T017: Configure Dependency Injection
**Description**: Configure DI container with authentication services and repositories
**Files**: `src/server/PigFarmManagement.Server/Program.cs`
**Dependencies**: T010, T012
**Details**:
- Register IAuthRepository and AuthRepository
- Register IAuthService and AuthService
- Configure authentication options
- Add helper services and utilities

### T018: Configure Authentication Middleware
**Description**: Configure middleware pipeline with API key authentication
**Files**: `src/server/PigFarmManagement.Server/Program.cs`
**Dependencies**: T013, T017
**Details**:
- Add ApiKeyMiddleware to pipeline
- Configure authentication and authorization
- Set up CORS for client requests
- Configure security headers

### T019: Create Admin Seeding Service
**Description**: Implement service to seed initial admin user on startup
**Files**: `src/server/PigFarmManagement.Server/Infrastructure/Services/AdminSeedService.cs`
**Dependencies**: T012, T017
**Details**:
- Create default admin user if none exists
- Generate secure initial password
- Log admin creation securely
- Run during application startup

### T020: Implement Client API Service [P]
**Description**: Create client-side service for authentication API calls
**Files**: `src/client/PigFarmManagement.Client/Services/AuthApiService.cs`
**Dependencies**: T004, T014
**Details**:
- HTTP client methods for all auth endpoints
- Error handling and response parsing
- Proper async/await patterns
- Dependency injection configuration

### T021: Create API Key Handler [P]
**Description**: Implement HTTP handler to automatically add X-Api-Key header
**Files**: `src/client/PigFarmManagement.Client/Services/ApiKeyHandler.cs`
**Dependencies**: T020
**Details**:
- DelegatingHandler implementation
- Automatic header injection for authenticated requests
- Token storage and retrieval
- Error handling for 401 responses

### T022: Implement Login UI Components [P]
**Description**: Create Blazor components for user authentication interface
**Files**: `src/client/PigFarmManagement.Client/Features/Authentication/Pages/`
**Dependencies**: T020, T021
**Details**:
- Login.razor page with username/password form
- MudBlazor form components and validation
- Error display and loading states
- Navigation after successful login

### T023: Configure Client Authentication Services
**Description**: Configure client-side dependency injection and HTTP client
**Files**: `src/client/PigFarmManagement.Client/Program.cs`
**Dependencies**: T020, T021, T022
**Details**:
- Register authentication services
- Configure HTTP client with API key handler
- Set up client-side routing for auth pages
- Configure base API URLs

## Parallel Execution Examples

### Group 1: Foundation Setup (Parallel)
```bash
# These can be executed simultaneously as they work on different files
Task T001 # Project dependencies
Task T002 # UserEntity model  
Task T003 # ApiKeyEntity model
Task T004 # Authentication DTOs
Task T007 # Password utilities
Task T008 # API key utilities
```

### Group 2: Client Components (Parallel)
```bash
# Client-side components can be built in parallel
Task T020 # Client API service
Task T021 # API key handler
Task T022 # Login UI components
```

### Group 3: API Endpoints (Sequential)
```bash
# These share Program.cs configuration so must be sequential
Task T017 # Configure DI
Task T018 # Configure middleware
Task T019 # Admin seeding
Task T023 # Client configuration
```

## Dependency Graph

```
T001 (Dependencies)
├── T002 (UserEntity) [P]
├── T003 (ApiKeyEntity) [P] 
├── T004 (Auth DTOs) [P]
├── T007 (Password Utils) [P]
└── T008 (API Key Utils) [P]

T002,T003 → T005 (DbContext) → T006 (Migration)

T002,T003,T004 → T009 (Auth Interface)
T005,T009 → T010 (Auth Repository)

T004,T009 → T011 (Auth Service Interface)
T007,T008,T010,T011 → T012 (Auth Service)

T008,T010 → T013 (API Key Middleware)
T011,T012 → T014 (Auth Endpoints)
T011,T012 → T015 (Admin Endpoints)
T011,T012 → T016 (API Key Endpoints)

T010,T012 → T017 (Configure DI)
T013,T017 → T018 (Configure Middleware)
T012,T017 → T019 (Admin Seeding)

T004,T014 → T020 (Client API Service) [P]
T020 → T021 (API Key Handler) [P]
T020,T021 → T022 (Login UI) [P]
T020,T021,T022 → T023 (Client Config)
```

## Validation Checklist

### Core Implementation ✅
- [ ] UserEntity with all required fields and relationships
- [ ] ApiKeyEntity with lifecycle management
- [ ] Authentication DTOs for all API contracts
- [ ] Password and API key hashing utilities
- [ ] Database context and migration
- [ ] Authentication repository and service
- [ ] API key middleware with claims population

### API Endpoints ✅
- [ ] Login endpoint with API key generation
- [ ] User information endpoint
- [ ] Admin user management endpoints (CRUD)
- [ ] API key management endpoints
- [ ] Proper role-based authorization
- [ ] Error handling and status codes

### Client Integration ✅
- [ ] HTTP client handler for automatic header injection
- [ ] Client authentication service
- [ ] Login UI components with MudBlazor
- [ ] Client-side configuration and routing

### Configuration ✅
- [ ] Dependency injection registration
- [ ] Middleware pipeline configuration
- [ ] Admin user seeding on startup
- [ ] Security configuration and CORS

### Documentation ✅
- [ ] All endpoints documented with OpenAPI attributes
- [ ] Client integration examples
- [ ] Security configuration guide
- [ ] Deployment and operational procedures

---

**Next Steps**: Execute tasks in dependency order, validating each completion against the specifications in the design documents. Use quickstart.md for integration testing after core implementation.