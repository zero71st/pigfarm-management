# Tasks: Serve   → quickstart.md: Manual testing scenarios for validation
3. Generate tasks by category:
   → Setup: Security feature structure, configuration, dependencies
   → Core: Configuration models, services, middleware components
   → Integration: Middleware pipeline, DI registration, endpoint security
   → Validation: Manual testing procedures and performance validation
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Middleware pipeline = sequential (order matters)
   → Implementation-first approach with manual validation
5. Generated 30 tasks focused on implementation and manual testingint Security Without Database Tables

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

## Phase 3.1: Setup and Foundation

- [ ] **T001** Create security feature directory structure in `src/server/PigFarmManagement.Server/Features/Authentication/` with subfolders: Middleware/, Services/, Configuration/, Models/
- [ ] **T002** [P] Add security configuration models in `src/shared/PigFarmManagement.Shared/DTOs/Security/SecurityConfigurationDto.cs`
- [ ] **T003** [P] Add rate limiting models in `src/shared/PigFarmManagement.Shared/DTOs/Security/RateLimitDto.cs`
- [ ] **T004** [P] Add session management models in `src/shared/PigFarmManagement.Shared/DTOs/Security/SessionDto.cs`
- [ ] **T005** [P] Add input validation models in `src/shared/PigFarmManagement.Shared/DTOs/Security/ValidationDto.cs`
- [ ] **T006** [P] Add security service contracts in `src/shared/PigFarmManagement.Shared/Contracts/Security/ISecurityServices.cs`

## Phase 3.2: Core Implementation

### Configuration and Models
- [ ] **T007** [P] SecuritySettings configuration model in `src/server/PigFarmManagement.Server/Features/Authentication/Configuration/SecuritySettings.cs`
- [ ] **T008** [P] RateLimitPolicy model in `src/server/PigFarmManagement.Server/Features/Authentication/Models/RateLimitPolicy.cs`
- [ ] **T009** [P] SessionToken model in `src/server/PigFarmManagement.Server/Features/Authentication/Models/SessionToken.cs`
- [ ] **T010** [P] InputValidationRule model in `src/server/PigFarmManagement.Server/Features/Authentication/Models/InputValidationRule.cs`

### Security Services
- [ ] **T011** [P] IApiKeyAuthenticationService implementation in `src/server/PigFarmManagement.Server/Features/Authentication/Services/ApiKeyAuthenticationService.cs`
- [ ] **T012** [P] IRoleAuthorizationService implementation in `src/server/PigFarmManagement.Server/Features/Authentication/Services/RoleAuthorizationService.cs`
- [ ] **T013** [P] IRateLimitingService implementation in `src/server/PigFarmManagement.Server/Features/Authentication/Services/RateLimitingService.cs`
- [ ] **T014** [P] IInputValidationService implementation in `src/server/PigFarmManagement.Server/Features/Authentication/Services/InputValidationService.cs`
- [ ] **T015** [P] ISessionManagementService implementation in `src/server/PigFarmManagement.Server/Features/Authentication/Services/SessionManagementService.cs`

### Middleware Components (Sequential - Order Matters)
- [ ] **T016** ApiKeyAuthenticationMiddleware in `src/server/PigFarmManagement.Server/Features/Authentication/Middleware/ApiKeyAuthenticationMiddleware.cs`
- [ ] **T017** RoleBasedAuthorizationMiddleware in `src/server/PigFarmManagement.Server/Features/Authentication/Middleware/RoleBasedAuthorizationMiddleware.cs`
- [ ] **T018** RateLimitingMiddleware in `src/server/PigFarmManagement.Server/Features/Authentication/Middleware/RateLimitingMiddleware.cs`
- [ ] **T019** InputValidationMiddleware in `src/server/PigFarmManagement.Server/Features/Authentication/Middleware/InputValidationMiddleware.cs`

### Security Controllers
- [ ] **T020** [P] SecurityAuthController for /api/security/auth/* endpoints in `src/server/PigFarmManagement.Server/Features/Authentication/Controllers/SecurityAuthController.cs`
- [ ] **T021** [P] SecurityMonitoringController for /api/security/ratelimit/* endpoints in `src/server/PigFarmManagement.Server/Features/Authentication/Controllers/SecurityMonitoringController.cs`
- [ ] **T026** [P] IRateLimitingService implementation in `src/server/PigFarmManagement.Server/Features/Authentication/Services/RateLimitingService.cs`
- [ ] **T027** [P] IInputValidationService implementation in `src/server/PigFarmManagement.Server/Features/Authentication/Services/InputValidationService.cs`
- [ ] **T028** [P] ISessionManagementService implementation in `src/server/PigFarmManagement.Server/Features/Authentication/Services/SessionManagementService.cs`

## Phase 3.3: Integration and Pipeline Setup

- [ ] **T022** Register security services in DI container in `src/server/PigFarmManagement.Server/Program.cs` (services registration section)
- [ ] **T023** Configure SecuritySettings with IOptions pattern in `src/server/PigFarmManagement.Server/Program.cs` (configuration section)
- [ ] **T024** Register middleware pipeline in correct order in `src/server/PigFarmManagement.Server/Program.cs` (middleware pipeline section)
- [ ] **T025** Add security attributes to existing controllers in `src/server/PigFarmManagement.Server/Features/Customers/Controllers/CustomersController.cs`
- [ ] **T026** Update appsettings.json with security configuration in `src/server/PigFarmManagement.Server/appsettings.json`

## Phase 3.4: Client Integration and Manual Validation

- [ ] **T027** [P] Update HTTP client service with X-Api-Key header handling in `src/client/PigFarmManagement.Client/Services/ApiClient.cs`
- [ ] **T028** [P] Manual testing: Authentication flow validation using quickstart guide procedures
- [ ] **T029** [P] Manual testing: Role-based authorization scenarios with different user roles
- [ ] **T030** [P] Manual testing: Rate limiting enforcement and performance validation (200ms p95 target)

## Dependencies

### Phase Dependencies
- Setup (T001-T006) → Implementation (T007-T021) → Integration (T022-T026) → Validation (T027-T030)

### Critical Sequence Dependencies
- T001 (structure) blocks T007-T021 (all implementation files)
- T006 (contracts) blocks T011-T015 (service implementations)
- T016-T019 (middleware) blocks T024 (pipeline registration)
- T022-T023 (DI/config) blocks T024 (pipeline registration)
- T024 (pipeline) blocks T025 (endpoint security)

### Middleware Order Dependencies (Sequential)
- T016 → T017 → T018 → T019 → T024 (pipeline registration maintains order)

## Parallel Execution Examples

### Phase 3.2: Models and Services (can run simultaneously)
```bash
# Launch T007-T015 together:
Task: "SecuritySettings configuration model in SecuritySettings.cs"
Task: "RateLimitPolicy model in RateLimitPolicy.cs"
Task: "SessionToken model in SessionToken.cs"
Task: "IApiKeyAuthenticationService implementation in ApiKeyAuthenticationService.cs"
Task: "IRoleAuthorizationService implementation in RoleAuthorizationService.cs"
```

### Phase 3.4: Manual Testing (can run simultaneously)
```bash
# Launch T028-T030 together:
Task: "Manual testing: Authentication flow validation using quickstart guide procedures"
Task: "Manual testing: Role-based authorization scenarios with different user roles"
Task: "Manual testing: Rate limiting enforcement and performance validation"
```

## Manual Testing Approach

### Authentication Testing
- Use Postman/curl to test API key validation endpoints
- Verify valid/invalid API key responses match contract specifications
- Test authentication flow with different user roles

### Authorization Testing
- Test role-based access with Admin/User/ReadOnly roles
- Verify endpoint access restrictions work correctly
- Test unauthorized access returns proper error responses

### Rate Limiting Testing
- Make multiple requests to test rate limiting thresholds
- Verify 429 responses when limits exceeded
- Test rate limit reset after time window

### Input Validation Testing
- Test XSS and SQL injection attempts are blocked
- Verify malformed requests return validation errors
- Test oversized payloads are rejected

### Performance Testing
- Measure response times with security middleware enabled
- Verify 200ms p95 response time target is met
- Test memory usage under load

## Validation Checklist
*Completed after implementation*

- [ ] All security endpoints respond correctly to manual testing
- [ ] All middleware components function in correct order
- [ ] Configuration-based security policies work without database changes
- [ ] Parallel tasks are truly independent (different files, no shared state)
- [ ] Each task specifies exact file path
- [ ] Middleware order dependencies properly sequenced
- [ ] Manual testing procedures validate all functionality

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