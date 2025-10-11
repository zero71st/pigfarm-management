```markdown
# Feature Specification: Deploy server to Railway Postgres and seed admin

**Feature Branch**: `011-title-deploy-server`  
**Created**: October 10, 2025  
**Status**: Draft  
**Input**: User description: "Connect the server to PostgreSQL on Railway, run EF migrations, and provide a safe admin seeding path for production using ADMIN_* env vars. Provide deployment scripts and CI guidance."

## Execution Flow (main)
```
1. Parse user description from Input
   → "Connect the server to PostgreSQL on Railway, run EF migrations, and provide a safe admin seeding path for production using ADMIN_* env vars. Provide deployment scripts and CI guidance."
2. Extract key concepts from description
   → Actors: DevOps/maintainer, System Administrator, CI pipeline
   → Actions: configure DATABASE_URL, run EF migrations, seed admin user/API key, deploy server to Railway, set environment secrets
   → Data: connection string (DATABASE_URL), admin secrets (ADMIN_PASSWORD, ADMIN_APIKEY), migration artifacts
   → Constraints: production safety (do not leak secrets), ability to run migrations reliably, compatibility with Railway environment
3. For each unclear aspect:
   → [NEEDS CLARIFICATION: Do you want automatic migrations on app startup, or prefer CI/one-off migration runs?]
   → [NEEDS CLARIFICATION: In production, should the seeder refuse to run if ADMIN_PASSWORD/ADMIN_APIKEY are missing?]
4. Fill User Scenarios & Testing section
   → Scenarios cover successful deploy, first-time database init+seed, secret provisioning, and rotation.
5. Generate Functional Requirements
   → Each requirement is focused on safe provisioning, migrations, and deploy reproducibility.
6. Identify Key Entities (configurations and secrets)
7. Run Review Checklist
   → If any [NEEDS CLARIFICATION] remain, note them for follow-up
8. Return: SUCCESS (spec ready for planning)
```

---

## ⚡ Quick Guidelines
- ✅ Focus on WHAT the deployment must achieve and WHY (reliability, safety)
- ❌ Avoid low-level implementation details in this spec (scripts and CI steps belong to the plan/tasks sections)
- 👥 Written for maintainers and DevOps engineers

### Section Requirements
- **Mandatory sections**: User Scenarios, Functional Requirements, Key Entities, Review checklist
- **Optional sections**: Edge-case testing steps, rollback guidance (include because this is deploy-related)

### For AI Generation
1. Mark all ambiguities with [NEEDS CLARIFICATION].
2. Prefer secure defaults: require provided secrets in production; allow generation in development only.

---

## User Scenarios & Testing *(mandatory)*

### Primary User Story
As a DevOps engineer, I want to deploy the server to Railway with a PostgreSQL database, ensure EF Core migrations are applied, and have an initial admin account created safely so that the application is operational after the first deploy without exposing secrets.

### Acceptance Scenarios
1. **Given** a fresh Railway Postgres database and required environment variables are set, **When** the deployment runs, **Then** EF Core migrations are applied successfully and the application starts without errors.
2. **Given** no admin user exists in the database, **When** the seeder runs with ADMIN_PASSWORD and ADMIN_APIKEY provided, **Then** an admin user and hashed API key are created and no secrets are written to logs.
3. **Given** no admin credentials are provided in a development environment, **When** the seeder runs, **Then** a secure password and API key are generated and printed once to the startup output for developer capture (not in production).
4. **Given** the database already contains an admin, **When** deployment runs, **Then** the seeder is a no-op and existing credentials are preserved.
5. **Given** a migration failure or connectivity issue, **When** deployment attempts to start, **Then** the process fails fast with clear error output and does not start the web host.

### Edge Cases
- Partial migrations: ensure migration process is atomic or the deploy fails and rolls back.
- Secret rotation: provide guidance to create new API key and remove old one without downtime.
- Railway connection string differences (e.g., SSL requirement) must be handled by configuration.

## Requirements *(mandatory)*

### Functional Requirements
- **FR-001**: The system MUST accept a DATABASE_URL environment variable (Railway Postgres) and connect to it at startup when present.
- **FR-002**: The system MUST support running EF Core migrations against the configured database prior to starting the web host.
- **FR-003**: The system MUST provide a safe admin seeding mechanism that:
  - creates an admin user only if none exists,
  - uses ADMIN_USERNAME, ADMIN_EMAIL, ADMIN_PASSWORD, and ADMIN_APIKEY environment variables when provided,
  - generates secure credentials automatically in non-production only,
   - does NOT print generated secrets when running with ASPNETCORE_ENVIRONMENT=Production.
   - In Production, the seeder MUST refuse to run (fail startup) if ADMIN_PASSWORD or ADMIN_APIKEY are missing.
- **FR-004**: The system MUST provide deployment scripts or entrypoint examples that run migrations and then start the application in the Railway environment.
- **FR-005**: The system MUST allow migrations to be executed from CI or a one-off deployment console as an alternative to automatic startup migration.
- **FR-006**: The system MUST require that ADMIN_PASSWORD and ADMIN_APIKEY are provided via the hosting platform secret store for production-first deployments (or fail startup with a clear error), to avoid secret leakage.

- **FR-007**: The system MUST log non-sensitive seeding events (e.g., "seeded admin user" or "admin exists") but never log raw passwords or API keys in production.

### Non-functional Requirements
- **NFR-001**: Deployment scripts must exit with non-zero status on migration failure (fail fast).
- **NFR-002**: The seeder must be idempotent.
- **NFR-003**: Any generated secrets in development must be strong (at least 96 bits entropy) and printed exactly once.
- **NFR-004**: The deployment process must support zero-downtime migrations or provide rollback guidance.

### Key Entities
- **DATABASE_URL**: Connection string for Postgres provided by Railway.
- **ADMIN_USERNAME / ADMIN_EMAIL / ADMIN_PASSWORD / ADMIN_APIKEY**: Environment variables used by seeder.
- **Migration Job**: The migration operation and its status (success/failure).

---

## Review & Acceptance Checklist

- [ ] Database connection to Railway Postgres is documented and tested.
- [ ] EF Core migrations can be run from CI and as part of startup entrypoint.
- [ ] Seeder behavior is documented for dev vs production environments.
- [ ] Deployment scripts and Railway start command examples are present in the repo (or documented).
- [ ] Secrets are provisioned via Railway environment variables and not leaked in logs.

## Clarifications

### Session 2025-10-10

- Q: Do you want EF Core migrations applied automatically when the app starts, or do you prefer migrations run from CI / a one-off migration job? → A: A

Applied clarification: The deployment shall use automatic startup migrations. The spec and requirements are updated accordingly: the application will run EF Core migrations at startup and must fail fast (exit non-zero) if migrations cannot be applied. In production the migration step must avoid printing secrets and should be guarded by robust connectivity and retry logic in the startup path.

- Q: In Production, should the seeder refuse to run when ADMIN_PASSWORD or ADMIN_APIKEY are missing (fail startup), or auto-generate an admin? → A: A

Applied clarification: In Production the seeder will refuse to run and the application must fail startup with a clear error if `ADMIN_PASSWORD` or `ADMIN_APIKEY` are missing. This is to ensure secrets are provisioned via the platform secret store and avoid accidental production secret leakage.

## Execution Status
*Updated by main() during processing*

- [x] User description parsed
- [x] Key concepts extracted
- [ ] Ambiguities marked (see [NEEDS CLARIFICATION] above)
- [x] User scenarios defined
- [x] Requirements generated
- [x] Entities identified
- [ ] Review checklist pending validation during deployment

---

```
# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]`  
**Created**: [DATE]  
**Status**: Draft  
**Input**: User description: "$ARGUMENTS"

## Execution Flow (main)
```
1. Parse user description from Input
   → If empty: ERROR "No feature description provided"
2. Extract key concepts from description
   → Identify: actors, actions, data, constraints
3. For each unclear aspect:
   → Mark with [NEEDS CLARIFICATION: specific question]
4. Fill User Scenarios & Testing section
   → If no clear user flow: ERROR "Cannot determine user scenarios"
5. Generate Functional Requirements
   → Each requirement must be testable
   → Mark ambiguous requirements
6. Identify Key Entities (if data involved)
7. Run Review Checklist
   → If any [NEEDS CLARIFICATION]: WARN "Spec has uncertainties"
   → If implementation details found: ERROR "Remove tech details"
8. Return: SUCCESS (spec ready for planning)
```

---

## ⚡ Quick Guidelines
- ✅ Focus on WHAT users need and WHY
- ❌ Avoid HOW to implement (no tech stack, APIs, code structure)
- 👥 Written for business stakeholders, not developers

### Section Requirements
- **Mandatory sections**: Must be completed for every feature
- **Optional sections**: Include only when relevant to the feature
- When a section doesn't apply, remove it entirely (don't leave as "N/A")

### For AI Generation
When creating this spec from a user prompt:
1. **Mark all ambiguities**: Use [NEEDS CLARIFICATION: specific question] for any assumption you'd need to make
2. **Don't guess**: If the prompt doesn't specify something (e.g., "login system" without auth method), mark it
3. **Think like a tester**: Every vague requirement should fail the "testable and unambiguous" checklist item
4. **Common underspecified areas**:
   - User types and permissions
   - Data retention/deletion policies  
   - Performance targets and scale
   - Error handling behaviors
   - Integration requirements
   - Security/compliance needs

---

## User Scenarios & Testing *(mandatory)*

### Primary User Story
[Describe the main user journey in plain language]

### Acceptance Scenarios
1. **Given** [initial state], **When** [action], **Then** [expected outcome]
2. **Given** [initial state], **When** [action], **Then** [expected outcome]

### Edge Cases
- What happens when [boundary condition]?
- How does system handle [error scenario]?

## Requirements *(mandatory)*

### Functional Requirements
- **FR-001**: System MUST [specific capability, e.g., "allow users to create accounts"]
- **FR-002**: System MUST [specific capability, e.g., "validate email addresses"]  
- **FR-003**: Users MUST be able to [key interaction, e.g., "reset their password"]
- **FR-004**: System MUST [data requirement, e.g., "persist user preferences"]
- **FR-005**: System MUST [behavior, e.g., "log all security events"]

*Example of marking unclear requirements:*
- **FR-006**: System MUST authenticate users via [NEEDS CLARIFICATION: auth method not specified - email/password, SSO, OAuth?]
- **FR-007**: System MUST retain user data for [NEEDS CLARIFICATION: retention period not specified]

### Key Entities *(include if feature involves data)*
- **[Entity 1]**: [What it represents, key attributes without implementation]
- **[Entity 2]**: [What it represents, relationships to other entities]

---

## Review & Acceptance Checklist
*GATE: Automated checks run during main() execution*

### Content Quality
- [ ] No implementation details (languages, frameworks, APIs)
- [ ] Focused on user value and business needs
- [ ] Written for non-technical stakeholders
- [ ] All mandatory sections completed

### Requirement Completeness
- [ ] No [NEEDS CLARIFICATION] markers remain
- [ ] Requirements are testable and unambiguous  
- [ ] Success criteria are measurable
- [ ] Scope is clearly bounded
- [ ] Dependencies and assumptions identified

---

## Execution Status
*Updated by main() during processing*

- [ ] User description parsed
- [ ] Key concepts extracted
- [ ] Ambiguities marked
- [ ] User scenarios defined
- [ ] Requirements generated
- [ ] Entities identified
- [ ] Review checklist passed

---
