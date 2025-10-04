# Tasks: Import POSPOS Product to Feed Formula (Priority: Product Import MVP)

**Input**: Design documents from `/specs/005-i-want-to/`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/

## Priority Focus
**FIRST PRIORITY**: Implement product import from POSPOS to FeedFormula
- Complete setup and core import logic first
- Defer transaction integration and UI updates until product import is working
- MVP scope: Import products → Store in FeedFormula → Basic API access

## Execution Flow (main)
```
1. Load plan.md from feature directory
   → If not found: ERROR "No implementation plan found"
   → Extract: tech stack, libraries, structure
2. Load optional design documents:
   → data-model.md: Extract entities → model tasks
   → contracts/: Each file → contract task
   → research.md: Extract decisions → setup tasks
3. Generate tasks by category:
   → Setup: project init, dependencies, linting
   → Core: models, services, CLI commands
   → Integration: DB, middleware, logging
   → UI: frontend updates
   → Polish: performance, docs
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same file = sequential (no [P])
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph
7. Create parallel execution examples
8. Validate task completeness:
   → All contracts implemented?
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

## Phase 3.1: Setup (Priority: Complete First)
- [x] T001 Update FeedFormula entity in `src/shared/PigFarmManagement.Shared/Domain/Entities.cs`
- [x] T002 Create DB migration for FeedFormula new fields in `src/server/PigFarmManagement.Server/Infrastructure/Data/Migrations/`
  - ✅ Completed breaking change migration from old field names (ProductCode, ProductName, Brand, BagPerPig) to POSPOS-aligned fields (Code, Name, CategoryName, ConsumeRate)
  - ✅ Updated FeedFormulaEntity database entity with all POSPOS fields (ExternalId, Code, Name, Cost, ConsumeRate, CategoryName, UnitName, LastUpdate)
  - ✅ Updated DbContext configuration with proper indexes and seed data
  - ✅ Fixed all affected services: PigPenService, FeedProgressService, FeedFormulaService
  - ✅ Fixed all endpoints: FeedFormulaEndpoints, FeedFormulaCalculationEndpoints
  - ✅ Migration file created: 20251002044944_AddPOSPOSFieldsToFeedFormula.cs

## Phase 3.2: Product Import MVP (Priority: Core Functionality)
**Focus on importing POSPOS products to FeedFormula first**
- [x] T003 Create PosposProductClient for low-level POSPOS API communication in `src/server/PigFarmManagement.Server/Services/PosposProductClient.cs`
  - ✅ Created IPosposProductClient interface with PosposProductDto, PosposCategoryDto, PosposUnitDto
  - ✅ Implemented PosposProductClient with rate limiting (10 requests/minute, 6 seconds between requests)
  - ✅ Added authentication via PosposOptions (ApiKey header)
  - ✅ Error handling for network timeouts, rate limits, invalid JSON
  - ✅ Pagination support for fetching all products
  - ✅ Registered in DI container with AddHttpClient
- [x] T004 Create FeedFormulaService for POSPOS product import business logic in `src/server/PigFarmManagement.Server/Services/FeedFormulaService.cs`
  - ✅ Added ImportProductsFromPosposAsync method to existing FeedFormulaService
  - ✅ Fetches all products via PosposProductClient
  - ✅ Duplicate detection by Code field (case-insensitive)
  - ✅ Transforms POSPOS DTO to FeedFormula entity (ExternalId from MongoDB ObjectId using MD5 hash)
  - ✅ Batch persistence with single SaveChangesAsync call
  - ✅ Returns ImportResult with SuccessCount, ErrorCount, SkippedCount, Errors list, ImportedCodes list
  - ✅ Comprehensive logging at all levels
- [x] T005 Implement POST /api/feed-formulas endpoint in FeedFormulaEndpoints.cs for product import (API layer - request validation, call FeedFormulaService, return responses)
  - ✅ Created POST /api/feed-formulas/import endpoint (no request body needed)
  - ✅ Calls FeedFormulaService.ImportProductsFromPosposAsync
  - ✅ Returns ImportResultResponse DTO with success/error/skip statistics
  - ✅ Returns BadRequest if all imports failed (ErrorCount > 0 && SuccessCount == 0)
  - ✅ Returns OK with statistics otherwise

## Phase 3.3: Transaction Integration (Deferred - After Product Import Works)
**Implement after product import is validated**
- [ ] T006 Update POSPOS client for transaction fetching in `src/server/PigFarmManagement.Server/Services/ExternalServices/PosposClient.cs`
- [ ] T007 Implement GET /api/feed-formulas endpoint in FeedFormulaEndpoints.cs
- [ ] T008 Add error handling and logging in `src/server/PigFarmManagement.Server/Services/FeedFormulaService.cs`

## Phase 3.4: UI and Display (Deferred - After Core Logic)
**Implement after backend import is working**
- [x] T009 Update UI to display feed history with code, name, cost, unit in `src/client/PigFarmManagement.Client/Features/Feeds/`
  - ✅ Added `ImportResultResponse` DTO to `FeedFormulaService.cs`
  - ✅ Added `ImportFromPosposAsync()` method to `IFeedFormulaService` interface
  - ✅ Implemented client-side method to call `POST /api/feed-formulas/import`
  - ✅ Added "Import from POSPOS" button to `FeedFormulaManagement.razor` (green button with cloud icon)
  - ✅ Implemented `ImportFromPospos()` handler with loading state and progress indicator
  - ✅ Added detailed notifications: success count, skipped count, error count with error details
  - ✅ Automatic page refresh after import completion
  - ✅ Error handling for network issues and API failures
  - ✅ Button disabled during import with "Importing..." text and spinner
  - ✅ Build successful with no errors

## Phase 3.5: Polish (Deferred - Final Phase)
- [ ] T010 Performance validation (<10s import)
- [ ] T011 Update docs in `docs/feed-api.md`

## Dependencies
- Setup (T001-T002) before implementation (T003-T005)
- T001 blocks T003, T004, T005
- Product Import MVP (T001-T005) before Transaction Integration (T006-T008)
- Core backend before UI (T009)
- Implementation before polish (T010-T011)

## Parallel Example
```
# Launch T004-T005 together:
Task: "Create FeedFormulaService for POSPOS product import business logic in src/server/PigFarmManagement.Server/Services/FeedFormulaService.cs"
Task: "Implement POST /api/feed-formulas endpoint in FeedFormulaEndpoints.cs for product import (API layer - request validation, call FeedFormulaService, return responses)"
```

## Notes
- [P] tasks = different files, no dependencies
- Commit after each task
- **PRIORITY**: Complete T001-T005 first for product import MVP
- Defer transaction and UI work until product import is validated