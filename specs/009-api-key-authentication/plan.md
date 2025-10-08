
# Implementation Plan: API-Key Authentication (Option B) — Admin-managed users

**Branch**: `009-api-key-authentication` | **Date**: October 8, 2025 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/009-api-key-authentication/spec.md`

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
API-key based authentication system with admin-managed user accounts. Implements secure X-Api-Key header authentication, role-based authorization (Admin/Manager/Worker/Viewer), and comprehensive user lifecycle management. No self-registration - all user creation handled by administrators. Uses hashed password storage, secure API key generation, and comprehensive audit logging.

## Design & Approach

### Phase 1: Planning Complete ✅

**Design Review Summary**: The API-key authentication system follows a straightforward admin-managed approach that aligns with the project's single-owner operational model. The design prioritizes simplicity while maintaining enterprise-grade security patterns.

**Key Design Decisions**:
- **Authentication Method**: X-Api-Key header authentication with admin-generated keys
- **User Management**: Admin-only user creation with role-based authorization (Admin/Manager/Worker/Viewer)
- **Security**: BCrypt password hashing, SHA-256 API key hashing, comprehensive audit logging
- **Architecture**: Feature-based organization following project patterns
- **Database**: UserEntity and ApiKeyEntity with proper relationships and lifecycle management

**Constitutional Compliance**: ✅ Verified against constitution.md
- Supports mission: Secure access to farm operations management
- Data integrity: Audit logging preserves authentication history
- Simplicity: Straightforward API-key authentication model
- Feature architecture: Proper folder structure under Authentication feature
- No backwards compatibility concerns (new feature)

**Contract Verification**: ✅ API contracts exist and documented
- Login/logout endpoints with proper request/response schemas
- User management endpoints with role-based access
- API key lifecycle management with security considerations
- Complete OpenAPI documentation with examples

**Artifacts Generated**:
- ✅ Comprehensive specification (641 lines) with security requirements
- ✅ Complete entity implementations (UserEntity, ApiKeyEntity)
- ✅ API contracts documentation with integration examples
- ✅ Research document with security patterns and ASP.NET Core guidance
- ✅ Quickstart guide with setup procedures and testing scenarios
- ✅ Agent context updated with feature details

**Next Phase**: Ready for task generation and implementation

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**✅ Data Integrity**: Authentication system preserves existing user data and provides migration paths. Audit logs are immutable once created.

**✅ Simplicity & Minimalism**: Uses proven ASP.NET Core Identity patterns rather than custom auth. Admin-only user management avoids complex self-registration flows.

**✅ Feature-Based Architecture**: Authentication components organized under `Features/Auth/` and `Features/Admin/` following established project patterns.

**✅ Single Owner Model**: All authentication management controlled by admin users, consistent with project governance.

**Initial Constitution Check**: PASS - No violations detected

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
src/
├── client/PigFarmManagement.Client/     # Blazor WebAssembly Frontend
│   ├── Features/
│   │   ├── Auth/                        # Authentication UI components
│   │   │   ├── Pages/
│   │   │   │   ├── ApiKeyLogin.razor   # Login page
│   │   │   │   └── AuthCallback.razor  # Auth result handling
│   │   │   └── Services/
│   │   │       └── AuthService.cs      # Client auth service
│   │   └── Admin/                       # Admin user management
│   │       ├── Pages/
│   │       │   ├── Users.razor         # User management page
│   │       │   └── UserDetails.razor   # User details/edit
│   │       └── Services/
│   │           └── AdminUserService.cs # Admin API service
│   └── Services/
│       └── ApiKeyHandler.cs            # HTTP client handler
├── server/PigFarmManagement.Server/     # ASP.NET Core Backend
│   ├── Features/
│   │   ├── Auth/                        # Authentication logic
│   │   │   ├── AuthService.cs          # Core auth service
│   │   │   ├── AuthEndpoints.cs        # Login endpoints
│   │   │   ├── ApiKeyMiddleware.cs     # Auth middleware
│   │   │   └── Helpers/
│   │   │       ├── ApiKeyHash.cs       # Key hashing utilities
│   │   │       └── PasswordUtil.cs     # Password utilities
│   │   └── Admin/                       # Admin user management
│   │       ├── AdminUserEndpoints.cs   # Admin API endpoints
│   │       └── AdminUserService.cs     # Admin business logic
│   └── Infrastructure/
│       └── Data/
│           ├── Entities/
│           │   ├── UserEntity.cs       # User entity
│           │   └── ApiKeyEntity.cs     # API key entity
│           ├── Repositories/
│           │   ├── IAuthRepository.cs  # Auth repo interface
│           │   └── AuthRepository.cs   # Auth repo implementation
│           └── Configurations/
│               ├── UserEntityConfiguration.cs
│               └── ApiKeyEntityConfiguration.cs
└── shared/PigFarmManagement.Shared/     # Common DTOs and contracts
    ├── Contracts/
    │   ├── LoginRequest.cs             # Auth DTOs
    │   ├── LoginResponse.cs
    │   ├── CreateUserRequest.cs        # Admin DTOs
    │   └── UserInfo.cs
    └── Domain/
        └── External/                    # External API models (existing)
```

**Structure Decision**: Web application structure selected. Authentication components follow feature-based organization under `Features/Auth/` and `Features/Admin/` in both client and server projects. Shared contracts in dedicated shared project.

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

3. **Extract implementation scenarios** from user stories:
   - Each story → feature implementation guide
   - Quickstart guide = story validation steps

4. **Update agent file incrementally** (O(1) operation):
   - Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot`
     **IMPORTANT**: Execute it exactly as specified above. Do not add or remove any arguments.
   - If exists: Add only NEW tech from current plan
   - Preserve manual additions between markers
   - Update recent changes (keep last 3)
   - Keep under 150 lines for token efficiency
   - Output to repository root

**Output**: data-model.md, /contracts/*, quickstart.md, agent-specific file

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Load `.specify/templates/tasks-template.md` as base
- Generate tasks from Phase 1 design docs (contracts, data model, quickstart)
- Each contract → implementation task [P]
- Each entity → model creation task [P] 
- Each user story → feature implementation task
- Implementation tasks to deliver working features

**Ordering Strategy**:
- Dependency order: Models before services before UI
- Mark [P] for parallel execution (independent files)

**Estimated Output**: 25-30 numbered, ordered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run quickstart.md, performance validation)

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |


## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [ ] Phase 0: Research complete (/plan command)
- [ ] Phase 1: Design complete (/plan command)
- [ ] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [ ] Initial Constitution Check: PASS
- [ ] Post-Design Constitution Check: PASS
- [ ] All NEEDS CLARIFICATION resolved
- [ ] Complexity deviations documented

---
*Based on Constitution v2.1.1 - See `/memory/constitution.md`*
