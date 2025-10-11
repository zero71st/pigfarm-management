# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`
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
   → Tests: contract tests, integration tests
   → Core: models, services, CLI commands
   → Integration: DB, middleware, logging
   → Polish: unit tests, performance, docs
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same file = sequential (no [P])
   → Tests before implementation (TDD)
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph
7. Create parallel execution examples
8. Validate task completeness:
   → All contracts have tests?
   → All entities have models?
   → All endpoints implemented?
9. Return: SUCCESS (tasks ready for execution)
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Path Conventions
- **Single project**: `src/`, `tests/` at repository root
- **Web app**: `backend/src/`, `frontend/src/`
- **Mobile**: `api/src/`, `ios/src/` or `android/src/`
- Paths shown below assume single project - adjust based on plan.md structure

## Phase 3.1: Setup
- [ ] T001 Create project structure per implementation plan
- [ ] T002 Initialize [language] project with [framework] dependencies
- [ ] T003 [P] Configure linting and formatting tools

## Phase 3.2: Validation (Manual or Automated)
This phase captures contract review, integration checklist, and manual validation tasks. The repository owner may choose to add automated tests, but they are not mandated by the template. Focus on a clear, executable validation checklist that can be run by engineers or QA.

- [ ] T004 [P] Review contracts/ files and produce a contract validation checklist (endpoints, payloads, status codes)
- [ ] T005 [P] Compose integration validation scenarios (DB migrations, auth flows, external API connectivity)
- [ ] T006 [P] Prepare manual test steps or QA scripts for core happy-path and edge cases
- [ ] T007 [P] Document expected responses and error cases for each endpoint in docs/manual-testing.md

## Phase 3.3: Core Implementation (after validation tasks prepared)
 - [ ] T008 [P] User model in src/models/user.py
- [ ] T009 [P] UserService CRUD in src/services/user_service.py
- [ ] T010 [P] CLI --create-user in src/cli/user_commands.py
- [ ] T011 POST /api/users endpoint
- [ ] T012 GET /api/users/{id} endpoint
- [ ] T013 Input validation
- [ ] T014 Error handling and logging

## Phase 3.4: Integration
- [ ] T015 Connect UserService to DB
- [ ] T016 Auth middleware
- [ ] T017 Request/response logging
- [ ] T018 CORS and security headers

## Phase 3.5: Polish
- [ ] T019 [P] Optional: Add or update automated unit tests where helpful (owner decides scope)
- [ ] T020 Performance checks (<200ms) or benchmark notes
- [ ] T021 [P] Update docs/api.md and manual-testing.md
- [ ] T022 Remove duplication
- [ ] T023 Execute manual-testing.md and capture results

## Dependencies
 - Validation checklist items (T004-T007) should be prepared before or alongside implementation so engineers and QA have clear acceptance criteria
 - T008 blocks T009, T015
 - T016 blocks T018
 - Implementation before polish (T019-T023)

## Parallel Example
```
# Launch T004-T007 together:
Task: "Contract test POST /api/users in tests/contract/test_users_post.py"
Task: "Contract test GET /api/users/{id} in tests/contract/test_users_get.py"
Task: "Integration test registration in tests/integration/test_registration.py"
Task: "Integration test auth in tests/integration/test_auth.py"
```

## Notes
- [P] tasks = different files, no dependencies
- Prepare validation checklists and manual-testing instructions that are easy for reviewers to follow
- Commit after each task or logical checkpoint
- Avoid: vague tasks, same-file conflicts, and unverified assumptions

## Task Generation Rules
*Applied during main() execution*

1. **From Contracts**:
   - Each contract file → a contract validation task or checklist item
   - Each endpoint → implementation task (with validation criteria)

2. **From Data Model**:
   - Each entity → model creation task [P]
   - Relationships → service layer tasks

3. **From User Stories**:
   - Each story → validation scenario (integration/manual) or an automated test if the owner adds one
   - Quickstart scenarios → manual validation tasks

4. **Ordering**:
   - Setup → Validation → Models → Services → Endpoints → Polish
   - Dependencies block parallel execution

## Validation Checklist
*GATE: Use this checklist to ensure readiness before merging*

- [ ] All contracts have documented validation checklist items (or tests where present)
- [ ] All entities have model tasks
- [ ] Validation criteria are documented and executable by QA or the implementer
- [ ] Parallel tasks truly independent
- [ ] Each task specifies exact file path
- [ ] No task modifies the same file as another [P] task