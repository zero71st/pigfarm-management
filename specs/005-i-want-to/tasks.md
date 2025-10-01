# Tasks: Import POSPOS Stock to Feed Formula

**Input**: Design documents from `/specs/005-i-want-to/`
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
- **Web app**: `backend/src/server/`, `frontend/src/client/`
- Paths adjusted based on plan.md structure

## Phase 3.1: Setup
- [ ] T001 Update FeedFormula entity in `src/shared/PigFarmManagement.Shared/Domain/Entities.cs`
- [ ] T002 Create DB migration for FeedFormula new fields in `src/server/PigFarmManagement.Server/Infrastructure/Data/Migrations/`

## Phase 3.2: Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3
**CRITICAL: These tests MUST be written and MUST FAIL before ANY implementation**
- [ ] T003 [P] Contract test GET /api/feed-formulas in `src/server/PigFarmManagement.Server.Tests/ContractTests/FeedApiTests.cs`
- [ ] T004 [P] Contract test POST /api/feed-formulas in `src/server/PigFarmManagement.Server.Tests/ContractTests/FeedApiTests.cs`
- [ ] T005 [P] Contract test GET /api/feed-formulas/{id} in `src/server/PigFarmManagement.Server.Tests/ContractTests/FeedApiTests.cs`
- [ ] T006 [P] Integration test successful import in `src/server/PigFarmManagement.Server.Tests/IntegrationTests/FeedImportTests.cs`
- [ ] T007 [P] Integration test network timeout in `src/server/PigFarmManagement.Server.Tests/IntegrationTests/FeedImportTests.cs`
- [ ] T008 [P] Integration test duplicate codes in `src/server/PigFarmManagement.Server.Tests/IntegrationTests/FeedImportTests.cs`
- [ ] T009 [P] Integration test profit calculation in `src/server/PigFarmManagement.Server.Tests/IntegrationTests/FeedImportTests.cs`

## Phase 3.3: Core Implementation (ONLY after tests are failing)
- [ ] T010 Update FeedImportService to handle stock import in `src/server/PigFarmManagement.Server/Services/FeedImportService.cs`
- [ ] T011 Implement GET /api/feed-formulas endpoint in `src/server/PigFarmManagement.Server/Features/Feeds/FeedEndpoints.cs`
- [ ] T012 Implement POST /api/feed-formulas endpoint in `src/server/PigFarmManagement.Server/Features/Feeds/FeedEndpoints.cs`
- [ ] T013 Implement GET /api/feed-formulas/{id} endpoint in `src/server/PigFarmManagement.Server/Features/Feeds/FeedEndpoints.cs`
- [ ] T014 Update UI to display feed history with code, name, unit in `src/client/PigFarmManagement.Client/Features/Feeds/`

## Phase 3.4: Integration
- [ ] T015 Update POSPOS client for product fetching in `src/server/PigFarmManagement.Server/Services/ExternalServices/PosposClient.cs`
- [ ] T016 Add error handling and logging in `src/server/PigFarmManagement.Server/Services/FeedImportService.cs`

## Phase 3.5: Polish
- [ ] T017 [P] Unit tests for FeedFormula in `src/server/PigFarmManagement.Server.Tests/UnitTests/FeedFormulaTests.cs`
- [ ] T018 Performance validation (<1s import)
- [ ] T019 [P] Update docs in `docs/feed-api.md`

## Dependencies
- Setup (T001-T002) before tests (T003-T009)
- Tests (T003-T009) before implementation (T010-T014)
- T001 blocks T010, T011-T013
- T010 blocks T015-T016
- Implementation before polish (T017-T019)

## Parallel Example
```
# Launch T003-T005 together:
Task: "Contract test GET /api/feed-formulas in src/server/PigFarmManagement.Server.Tests/ContractTests/FeedApiTests.cs"
Task: "Contract test POST /api/feed-formulas in src/server/PigFarmManagement.Server.Tests/ContractTests/FeedApiTests.cs"
Task: "Contract test GET /api/feed-formulas/{id} in src/server/PigFarmManagement.Server.Tests/ContractTests/FeedApiTests.cs"
```

## Notes
- [P] tasks = different files, no dependencies
- Verify tests fail before implementing
- Commit after each task
- Avoid: vague tasks, same file conflicts