
# Implementation Plan: Server-Side Endpoint Security Without Database Tables

**Branch**: `010-secure-all-the` | **Date**: October 9, 2025 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/010-secure-all-the/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from file system structure or context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, `GEMINI.md` for Gemini CLI, `QWEN.md` for Qwen Code or `AGENTS.md` for opencode).
7. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
Implement comprehensive API endpoint security using existing API Keys table for authentication, role-based authorization (Admin/User), RequireAuthorization on all endpoints, removed debug endpoints, Swagger authentication section, client-side role restrictions, removed default admin access, removed test and tools projects, reset database migrations, and production-safe admin seeding - all without adding new database tables. Technical approach leverages ASP.NET Core middleware pipeline with configuration-based security policies and Railway/Postgres deployment support.

## Technical Context
**Language/Version**: C# .NET 8 (ASP.NET Core Web API)  
**Primary Dependencies**: ASP.NET Core 8.0, Entity Framework Core 8.0, BCrypt.Net-Next 4.0.3, Swashbuckle.AspNetCore 6.5.0  
**Storage**: Existing SQLite/PostgreSQL database (no new tables), configuration files, in-memory storage for sessions/rate limits  
**Testing**: xUnit with ASP.NET Core TestHost, integration tests for middleware pipeline  
**Target Platform**: Cross-platform server (Linux/Windows hosting via Kestrel)
**Project Type**: Web application (Blazor WebAssembly frontend + ASP.NET Core Web API backend)  
**Performance Goals**: 500 req/s baseline capacity, 200ms p95 response time for authentication checks  
**Constraints**: No new database tables, maintain existing endpoint contracts, configuration-based approach only  
**Scale/Scope**: 1000+ concurrent users, existing ~20 API endpoints, 2-role authorization hierarchy (Admin/User)

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Data Integrity**: ✅ PASS - Uses existing API Keys table without modifications, maintains referential integrity  
**Simplicity**: ✅ PASS - Configuration-based approach without new tables, leverages ASP.NET Core built-in middleware  
**Feature-Based Architecture**: ✅ PASS - Security feature isolated in `/Features/Authentication/` following existing pattern  
**Performance**: ✅ PASS - In-memory rate limiting and session storage minimize database load  
**No Breaking Changes**: ✅ PASS - Middleware approach preserves all existing endpoint contracts

## Project Structure

### Documentation (this feature)
```
specs/[###-feature]/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
# Web application structure (Blazor WebAssembly + ASP.NET Core Web API)
src/
├── server/PigFarmManagement.Server/     # .NET 8 Web API Backend
│   ├── Features/                        # Feature-based architecture
│   │   ├── Authentication/              # Security middleware and services
│   │   │   ├── Middleware/              # Authentication, Authorization, Rate Limiting
│   │   │   ├── Services/                # Security validation, audit logging
│   │   │   ├── Configuration/           # Security policy configuration
│   │   │   └── Models/                  # Security DTOs and responses
│   │   ├── Customers/                   # Existing customer endpoints (to be secured)
│   │   ├── PigPens/                     # Existing pig pen endpoints (to be secured)
│   │   └── Feeds/                       # Existing feed endpoints (to be secured)
│   └── Infrastructure/                  # EF Core, base configurations
├── client/PigFarmManagement.Client/     # Blazor WebAssembly Frontend
│   └── Services/                        # HTTP client with authentication headers
├── shared/PigFarmManagement.Shared/     # Common DTOs and models
│   ├── DTOs/                            # Security response DTOs
│   ├── Domain/                          # Security domain models
│   └── Contracts/                       # Security service interfaces
└── server/PigFarmManagement.Server.Tests/  # Security integration tests
    ├── Features/Authentication/         # Security middleware tests
    ├── Integration/                     # End-to-end security validation
    └── Unit/                           # Security service unit tests
```

**Structure Decision**: Web application with feature-based organization. Security implementation will be contained within the `/Features/Authentication/` directory following the established pattern. All security middleware will be registered in the existing startup pipeline without breaking changes to current endpoint structure.

## Phase 0: Outline & Research
1. **Extract unknowns from Technical Context** above:
   - For each NEEDS CLARIFICATION → research task
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:
   ```
   For each unknown in Technical Context:
     Task: "Research {unknown} for {feature context}"
   For each technology choice:
     Task: "Find best practices for {tech} in {domain}"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

**Output**: research.md with all NEEDS CLARIFICATION resolved

## Phase 1: Design & Contracts
*Prerequisites: research.md complete*

1. **Extract entities from feature spec** → `data-model.md`:
   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Generate API contracts** from functional requirements:
   - For each user action → endpoint
   - Use standard REST/GraphQL patterns
   - Output OpenAPI/GraphQL schema to `/contracts/`

3. **Generate contract tests** from contracts:
   - One test file per endpoint
   - Assert request/response schemas
   - Tests must fail (no implementation yet)

4. **Extract test scenarios** from user stories:
   - Each story → integration test scenario
   - Quickstart test = story validation steps

5. **Update agent file incrementally** (O(1) operation):
   - Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot`
     **IMPORTANT**: Execute it exactly as specified above. Do not add or remove any arguments.
   - If exists: Add only NEW tech from current plan
   - Preserve manual additions between markers
   - Update recent changes (keep last 3)
   - Keep under 150 lines for token efficiency
   - Output to repository root

**Output**: data-model.md, /contracts/*, failing tests, quickstart.md, agent-specific file

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Load `.specify/templates/tasks-template.md` as base structure
- Generate tasks from Phase 1 design documents (contracts, data model, quickstart)
- Each middleware component → implementation task [P] (parallel execution)
- Each security service → service implementation task [P]
- Each contract endpoint → contract test task [P]
- Each configuration model → configuration setup task [P]
- Integration tasks for middleware pipeline setup
- Validation tasks for each test scenario from quickstart

**Ordering Strategy**:
- **Phase 1**: Foundation tasks (configuration, models, services) [P]
- **Phase 2**: Middleware implementation tasks [P] 
- **Phase 3**: Integration tasks (middleware registration, pipeline setup)
- **Phase 4**: Testing tasks (contract tests, integration tests) [P]
- **Phase 5**: Documentation and validation tasks

**Specific Task Categories**:

1. **Configuration Tasks [P]**:
   - SecuritySettings configuration model
   - IOptions pattern integration
   - Validation attributes for configuration

2. **Service Implementation Tasks [P]**:
   - IApiKeyAuthenticationService implementation
   - IRoleAuthorizationService implementation  
   - IRateLimitingService implementation
   - ISecurityEventLogger implementation
   - IInputValidationService implementation

3. **Middleware Implementation Tasks [P]**:
   - ApiKeyAuthenticationMiddleware
   - RoleBasedAuthorizationMiddleware
   - RateLimitingMiddleware
   - InputValidationMiddleware
   - SecurityEventLoggingMiddleware

4. **Contract Test Tasks [P]**:
   - Authentication endpoint tests (/api/security/auth/*)
   - Authorization verification tests
   - Rate limiting enforcement tests
   - Input validation blocking tests
   - Security event logging tests

5. **Integration Tasks** (Sequential):
   - Middleware pipeline registration in Program.cs
   - DI container service registration
   - Configuration binding setup
   - Existing endpoint security attribute application

6. **Validation Tasks [P]**:
   - Quickstart test scenario execution
   - Performance benchmark validation
   - Security vulnerability testing
   - Memory usage validation

**Estimated Task Count**: 35-40 numbered, ordered tasks

**Dependencies**:
- Configuration tasks → Service tasks
- Service tasks → Middleware tasks  
- Middleware tasks → Integration tasks
- Integration tasks → Testing tasks
- All implementation → Validation tasks

**Parallel Execution Markers**:
- [P] indicates tasks that can be executed independently
- No [P] indicates sequential dependency requirement
- Within-category parallelization for related components

**Risk Mitigation in Task Planning**:
- Early configuration validation to catch issues before implementation
- Incremental middleware addition to isolate integration problems
- Performance validation throughout to prevent late-stage optimizations
- Manual testing procedures to validate functionality at each step

**Expected Output**: 35-40 numbered, dependency-ordered tasks in tasks.md with clear [P] markers for parallel execution

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (manual testing, execute quickstart.md, performance validation)

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |


## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command)
- [x] Phase 1: Design complete (/plan command)
- [x] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS - No new violations introduced, design maintains existing principles
- [x] All NEEDS CLARIFICATION resolved
- [ ] Complexity deviations documented

---
*Based on Constitution v2.1.1 - See `/memory/constitution.md`*
