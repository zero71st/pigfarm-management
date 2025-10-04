# Tasks: 006-update-import-feed

Feature: Update import — compute feed consumption & expense from POSPOS
Branch: `006-update-import-feed`

## Overview
This tasks list implements the feature to import POSPOS transactions into Feed records, compute consumption (bags, bags-per-pig, coverage) and attribute expense per pig pen. It also records formula cost and POS discount data on feeds and exposes fields to the API/UI.

Order: Setup → TDD tests → Models → Services → Endpoints → UI → Polish

Parallelization notes: Tasks marked `[P]` can run in parallel when they touch different files. Sequential tasks must be done in order.

---

T001 (Setup) — Confirm feature artifacts and environment [✅ COMPLETED]
- Path: `specs/006-update-import-feed/`
- Action: Verify `spec.md` and `plan.md` exist (they do). Ensure you are on branch `006-update-import-feed`.
- Output: Ready-to-work environment. (Manual)

T002 (Test) [P] — Add unit tests for POSPOS parsing (PosPosFeedItem tolerant parsing) [✅ COMPLETED - SKIPPED per user request]
- Path: `src/server/PigFarmManagement.Server.Tests/PosposParsingTests.cs`
- Action: Add/ensure tests cover: numeric-string parsing for `stock`, `price`, `special_price`, `total_price_include_discount` and `DiscountAmount` correctness.
- Purpose: Prevent regressions in tolerant parsing used by import logic.

T003 (Model) [P] — Add Feed DTO fields [✅ COMPLETED]
- Path: `src/shared/PigFarmManagement.Shared/Domain/DTOs.cs`
- Action: Add `Cost`, `CostDiscountPrice`, `PriceIncludeDiscount`, `Sys_TotalPriceIncludeDiscount` to `Feed` DTO (already applied). Add unit tests that constructing a Feed and calling `RecalculateTotalPrice()` works with nullable fields.
- Output: Shared DTOs updated and unit tests added.

T004 (Model) — Add DB fields on FeedEntity and update mappings [✅ COMPLETED]
- Path: `src/server/PigFarmManagement.Server/Infrastructure/Data/Entities/FeedEntity.cs`
- Action: Add nullable decimal columns: `Cost`, `CostDiscountPrice`, `PriceIncludeDiscount`, `Sys_TotalPriceIncludeDiscount` (already applied). Update `ToModel()` and `FromModel()` mappings (already applied).
- Output: Entity updated; requires DB migration to apply schema changes.

T005 (DB Migration) — Create EF Core migration for FeedEntity schema change [✅ COMPLETED]
- Path: Repository root (dotnet ef commands) and optionally `src/server/.../Migrations/` if project uses migrations
- Action: Generate EF migration to add four nullable decimal columns to `Feeds` table; apply locally (or provide SQL). Include migration description: "Add Cost and discount/actual price fields for POSPOS import".
- Output: Migration files + SQL; DB schema updated locally.

T006 (Service) — Wire FeedFormula lookup into import flow [✅ COMPLETED]
- Path: `src/server/PigFarmManagement.Server/Services/FeedImportService.cs`
- Action: Inject `IFeedFormulaService`, preload formulas and map `Feed.Cost` from `FeedFormula.Cost` using product code. Implement caching for the import run (already applied). Add unit test that when a formula exists, imported feed has Cost populated.

T007 (Service) — Use POSPOS discount as cost_discount_price and compute actuals [✅ COMPLETED]
- Path: `src/server/PigFarmManagement.Server/Services/FeedImportService.cs`
- Action: Use `PosPosFeedItem.DiscountAmount` as `CostDiscountPrice`, compute `PriceIncludeDiscount = UnitPrice - CostDiscountPrice`, `Sys_TotalPriceIncludeDiscount = PriceIncludeDiscount * Quantity` and persist these fields on `Feed` when creating. Add validation to clamp negative PriceIncludeDiscount to 0 if desired.
- Output: Import populates CostDiscountPrice/PriceIncludeDiscount/Sys_TotalPriceIncludeDiscount (already applied).

T008 (Service) — FR-001..FR-006 unit/integration tests [✅ COMPLETED - SKIPPED per user request]
- Path: `src/server/PigFarmManagement.Server.Tests/FeedImportTests.cs`
- Action: Write tests for acceptance scenarios in `spec.md` including: rounding stock to bags, unit price selection (special vs price), TotalPrice fallback, BagsPerPig calculation and CoveragePct behaviour when BagPerPig unknown (must be empty). Use in-memory DB or test DB to assert persisted Feed entity fields.
- Output: Tests failing initially (TDD) then pass when implementation complete.

T009 (API) — Ensure Feed endpoints return new fields [✅ COMPLETED]
- Path: `src/server/PigFarmManagement.Server/Features/Feeds/*` (endpoints and services)
- Action: Update endpoint DTO mappings so `Feed` responses include `Cost`, `CostDiscountPrice`, `PriceIncludeDiscount`, `Sys_TotalPriceIncludeDiscount`. Update OpenAPI/contract files if present.
- Output: API returns new fields for clients to render.

T010 (Client) — Surface new fields in PigPen feed history UI [✅ COMPLETED]
- Path: `src/client/PigFarmManagement.Client/Features/PigPens/Pages/PigPenDetailPage.razor` and related components
- Action: Add columns or expand feed item display to show `INV Date`, `InvoiceReferenceCode`, `Cost`, `PriceIncludeDiscount`, `Sys_TotalPriceIncludeDiscount`. Respect UI compactness (e.g., show tooltip for details). If client build fails, fix client DTO mismatches first.
- Note: Client had unrelated compile errors previously; fix them before running full verification.

T011 (Docs) — Update feature spec and quickstart [✅ COMPLETED]
- Path: `specs/006-update-import-feed/spec.md`, `specs/006-update-import-feed/quickstart.md`
- Action: Add clarification summary (CoveragePct empty when BagPerPig unknown — already recorded). Add quickstart test steps to import a sample POSPOS JSON and verify fields shown in UI/API.

T012 (Polish) [P] — Add logging, metrics and error reporting [✅ COMPLETED]
- Path: `src/server/PigFarmManagement.Server/Services/FeedImportService.cs` and `FeedImportEndpoints.cs`
- Action: Add structured logs for import summary (TotalBags, TotalExpense, items with fallback). Add metrics counters for imports success/failure. Ensure warnings are appended to `FeedImportResult.Errors` and surfaced to UI.

T013 (Polish) — Add migration + rollback QA and update README [✅ COMPLETED]
- Path: `README.md`, `specs/006-update-import-feed/tasks.md`
- Action: Document the migration steps, how to run imports locally with sample JSON and how to interpret the new fields. Add rollback instructions for schema changes.

---

Parallel execution groups (examples)
- Group A [P]: T002, T003 (tests and shared DTOs) can run in parallel.
- Group B [P]: T006 and T007 (service mapping + compute) can be done together if same developer coordinates; they touch same file and should be sequential if separate people edit the same file.

How to run (developer commands)
1. Build & tests:
   - cd src/server/PigFarmManagement.Server; dotnet test
2. Generate migration (if using EF):
   - cd src/server/PigFarmManagement.Server; dotnet ef migrations add AddFeedCostFields; dotnet ef database update
3. Run server locally:
   - dotnet run --project src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj --urls http://localhost:5000

Notes & Dependencies
- T005 requires EF Core migrations configured for the server project. If you prefer manual SQL, create ALTER TABLE statements to add nullable decimal columns.
- Client changes in T010 may require mapping updates in shared DTOs; ensure client references the updated shared project build.

Deliverables
- `specs/006-update-import-feed/tasks.md` (this file)
- Unit tests for parsing and import
- EF migration or SQL statements to update DB schema
- Updated server code and API responses
- Optional client UI updates
