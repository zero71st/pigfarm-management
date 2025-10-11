````markdown
# Tasks: Deploy server to Railway Postgres and seed admin

**Feature**: Deploy server to Railway Postgres and seed admin
**Feature dir**: `specs/011-title-deploy-server`
**Input docs**: `plan.md`, `research.md`, `data-model.md`, `contracts/openapi.yaml`, `quickstart.md`

## How to use
- Each task includes the file(s) to edit and the exact acceptance criteria.
- Tasks marked [P] are safe to work on in parallel (different files, no shared conflicts).
- Follow dependency notes: prepare validation checklists first, then implement core changes, then polish.

## Numbered Tasks

- [x] T001 Setup: Verify local build & prerequisites
  - Files / locations: repository root
  - Action: From repo root, run `dotnet --list-sdks`, `dotnet restore src/server/PigFarmManagement.Server` and `dotnet build src/server/PigFarmManagement.Server` to confirm the server builds locally.
  - Success criteria: `dotnet build` completes with exit code 0 and no compilation errors.
  - Dependency: none

- [x] T002 [P] Validation: Review contracts and produce a contract validation checklist
  - Files / locations to update: `specs/011-title-deploy-server/contracts/openapi.yaml`, `specs/011-title-deploy-server/manual-testing.md` (create)
  - Action: Read `contracts/openapi.yaml` and produce a short checklist that lists each endpoint, the expected status codes, required auth, and a minimal request/response example. Commit the checklist to `manual-testing.md`.
  - Success criteria: `manual-testing.md` created with checklist lines for `/health`, `/admin/seed`, and `/migrations/run` including expected responses from the contract.
  - Dependency: T001 can run before or alongside

- [x] T003 [P] Validation: Compose integration validation scenarios (connectivity & migrations)
  - Files / locations to update: `specs/011-title-deploy-server/manual-testing.md`
  - Action: Add scenarios to `manual-testing.md` for:
    - Fresh DB boot (no migrations applied)
    - Missing production secrets (ensure startup fails)
    - Seeder run with valid production secrets
    - Running migrations via `/migrations/run` (when protected)
  - Success criteria: `manual-testing.md` contains step-by-step instructions for the scenarios above that an operator can follow.
  - Dependency: T002 recommended first

- [x] T004 Implement: Parse `DATABASE_URL` into Npgsql connection string
  - Files to edit: `src/server/PigFarmManagement.Server/Program.cs` (primary), optionally `src/server/PigFarmManagement.Server/Infrastructure/ConfigurationExtensions.cs` if present.
  - Action: Add code that, when `DATABASE_URL` is present, converts it into a valid Npgsql connection string (host, port, user, password, database, sslmode) and uses it for `DbContextOptions`.
  - Success criteria: When `DATABASE_URL` is set to a typical Railway value, the application uses it to connect (no manual config required). Add a short comment referencing `specs/011-title-deploy-server/quickstart.md`.
  - Dependency: T001, T002

- [ ] T005 Implement: Automatic EF Core startup migrations with fail-fast behavior
  - Files to edit: `src/server/PigFarmManagement.Server/Program.cs`, and any startup helper class where migrations run.
  - Action: Ensure `context.Database.Migrate()` is invoked before the web host starts. On migration failure, write a clear error to the console and exit non-zero. Optionally record a `MigrationJob` row when migrations start/finish/fail.
  - Success criteria: App fails startup with a non-zero exit code on migration failure; migrations are applied successfully in a local test run.
  - Dependency: T004
- [x] T005 Plan: Migration strategy & CI migration job (DO NOT apply migrations now)
  - Files to edit: `specs/011-title-deploy-server/quickstart.md`, `.github/workflows/` (example), and `docs/DEPLOYMENT.md` (optional)
  - Action: Produce a migration-run plan and example CI job or Railway one-off command that can be used by maintainers/CI to run `dotnet ef database update` against Postgres. Do NOT add or invoke `context.Database.Migrate()` in this change and do NOT run migrations as part of startup.
  - Success criteria: A concrete migration plan is documented (one-off commands, CI job YAML snippet, rollback notes) and added to `quickstart.md` or `docs/DEPLOYMENT.md`. No code changes that execute migrations at startup are included.
  - Dependency: T004

- [x] T006 Implement: Idempotent admin seeder with production safety
  - Files to edit: `src/server/PigFarmManagement.Server/Program.cs` (seeder registration) and `src/server/PigFarmManagement.Server/Features/Authentication/AdminSeeder.cs` (create if missing)
  - Action: Implement seeder behavior per `specs/011-title-deploy-server/spec.md`:
    - In non-production: generate a strong ADMIN_PASSWORD and ADMIN_APIKEY if not provided, print them exactly once to startup output (do NOT print in Production).
    - In Production (ASPNETCORE_ENVIRONMENT=Production): require `ADMIN_PASSWORD` and `ADMIN_APIKEY` and fail startup with a clear message if missing.
    - Seeder must be idempotent: create admin only when none exists.
    - Do NOT log raw secrets in production logs.
  - Success criteria: Seeder creates an admin when none exists and respects production rules. Manual test steps added to `manual-testing.md` pass.
  - Dependency: T004

- [x] T007 Implement: Admin seed endpoint (`POST /admin/seed`) (admin-only)
  - Files to edit: `src/server/PigFarmManagement.Server/Program.cs` (endpoint registration) or `src/server/PigFarmManagement.Server/Controllers/AdminController.cs` (create)
  - Action: Add a protected endpoint matching `contracts/openapi.yaml` that triggers the seeder. Accepts `{ "force": true|false }` and returns `201` when created, `200` when no-op, `400` on missing production secrets.
  - Success criteria: Calling the endpoint with proper admin auth returns the expected status codes and does not leak raw secrets in the response or logs.
  - Dependency: T006

- [x] T008 Implement: Migrations endpoint (`POST /migrations/run`) (admin-only)
  - Files to edit: `src/server/PigFarmManagement.Server/Program.cs` or new `Controllers/MigrationsController.cs`
  - Action: Implement an authenticated endpoint that triggers `context.Database.Migrate()` and returns `200` on success, `500` on failure, following `contracts/openapi.yaml`.
  - Success criteria: Endpoint exists, is protected, and returns status codes as specified by the contract. It should also create a `MigrationJob` record if that entity is present.
  - Dependency: T004

- [x] T009 [P] Data model: Add `MigrationJob` entity and DbSet (if not present)
  - Files to edit: `src/server/PigFarmManagement.Server/Domain/MigrationJob.cs` (create) and `src/server/PigFarmManagement.Server/Infrastructure/ApplicationDbContext.cs` (add `DbSet<MigrationJob> MigrationJobs`)
  - Action: Implement MigrationJob entity per `data-model.md` and wire it into the DbContext so migration runs can record status.
  - Success criteria: `MigrationJob` table exists after migrations (when executed via CI/one-off per T005 plan) and a record can be written when migrations run.
  - Dependency: T004 (migrations to be executed separately per T005 plan)

- [x] T010 Security review: API key hashing & admin password handling
  - Files to review/edit: `src/server/PigFarmManagement.Server/Domain/ApiKeyEntity.cs`, `src/server/PigFarmManagement.Server/Features/Authentication/*`
  - Action: Verify that raw API keys are never stored and that a secure hashing algorithm is used (e.g., ASP.NET `PasswordHasher` or BCrypt). Add or update code and comments where necessary.
  - Success criteria: Code includes a secure hashing approach and a short comment documenting where the raw key generation occurs (only in dev printed once).
  - Dependency: T006

- [x] T011 Docs & Quickstart update
  - Files to edit: `specs/011-title-deploy-server/quickstart.md` and `README.md` (or `docs/DEPLOYMENT.md`)
  - Action: Add a small Railway deployment example showing `ASPNETCORE_ENVIRONMENT=Production`, `DATABASE_URL`, and the required admin secrets. Include the one-off migration command used in CI.
  - Success criteria: Quickstart includes clear steps for provisioning secrets on Railway and example commands from `quickstart.md` work for an operator.
  - Dependency: T002, T003

- [x] T012 Observability & logs
  - Files to edit: `src/server/PigFarmManagement.Server/Program.cs`, logging config files
  - Action: Ensure seed/migration operations log non-sensitive status messages (e.g., "Admin created", "admin exists", "migration started/finished/failed") and that exceptions include root cause without credentials. Add migration job logging where applicable.
  - Success criteria: Console output and logs contain helpful non-sensitive messages for seeding/migrations; manual-testing steps can verify logs.
  - Dependency: T006

- [x] T013 Polish: Manual validation and QA checklist
  - Files to create/update: `specs/011-title-deploy-server/manual-testing.md` (extend), `specs/011-title-deploy-server/quickstart.md` (optional additions)
  - Action: Consolidate all manual validation steps (from T002/T003) into a runnable checklist that an operator can follow after deployment. Include exact curl/powershell commands for `/health`, `/admin/seed`, and `/migrations/run` and the expected DB checks.
  - Success criteria: QA can follow `manual-testing.md` and validate the deployed application successfully.
  - Dependency: T006, T008

## Parallel execution suggestions
- Group A (can run in parallel): T002, T003, T010 (validation and security review) — these operate on docs/review and do not touch the same source files.
- Group B: T004, T005, T009, T012 (core infra changes) — these change startup/migrations and should be coordinated but may be parallel if branches avoid overlapping files.
- Group C (after core infra): T006, T007, T008, T013, T011 (seeder, endpoints, docs, QA)

## Dependency summary
- T001 → T004
- T002 → T003 → T013
- T004 → T009, T008 (note: actual migration execution is planned in T005 and must be run via CI/one-off; migrations are NOT applied automatically in this change)
- T005: migration planning task (does NOT apply migrations). When migrations are executed later (via CI or operator), they will satisfy MigrationJob creation and migration-run endpoints.
- T006 depends on T004 (seeder can be developed and tested against a DB connection; actual migrations are handled separately)

## Tasks are executable by an LLM
- For code edits, each task above specifies target file paths and acceptance criteria. If a referenced file does not exist, create it under the paths above using project conventions and wire it into the build.

````
