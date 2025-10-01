
# Tasks: 004-fix-incorrect-feed — Server → Shared → Client

Purpose: implement correct POSPOS invoice imports end-to-end: tolerant parsing, bag-count coercion, consistent pricing, invoice reference wiring, product/buyer mapping, idempotency, persistence, tests, and client UI updates.

How to use this file
- Tasks are ordered and grouped by responsibility: Server (core import & persistence), Shared (DTOs/value objects/tests), Client (UI). Each task lists files, acceptance criteria, and a short implementation note.
- Follow TDD where practical: write small tests first, implement, then validate with the sample JSON.

Server (core)

S-1: Add EF migration to persist new Feed fields
 - Files/Commands:
	 - `src/server/PigFarmManagement.Server/Infrastructure/Data/Entities/FeedEntity.cs` (ensure fields exist)
	 - Create migration in `src/server/PigFarmManagement.Server/Migrations/`
	 - Command: run `dotnet ef migrations add AddPosposFields --project src/server/PigFarmManagement.Server` locally
 - Acceptance:
	 - Migration file exists, compiles, and (optionally) `dotnet ef database update` applies without errors.
 - Notes:
	 - Fields: `ExternalProductCode` (string), `ExternalProductName` (string), `UnmappedProduct` (bool), `InvoiceReferenceCode` (string).

S-2: Implement import coercion & invoice wiring in `FeedImportService`
 - Files:
	 - `src/server/PigFarmManagement.Server/Services/FeedImportService.cs`
 - Changes:
	 - Coerce `orderItem.Stock` decimal → integer `bags` using MidpointRounding.AwayFromZero.
	 - Set `Feed.Quantity = bags` (int), `Feed.UnitPrice = orderItem.Price` (decimal), `Feed.TotalPrice = orderItem.TotalPriceIncludeDiscount`.
	 - Set `Feed.InvoiceNumber = transaction.Code` (unchanged) and `Feed.InvoiceReferenceCode = transaction.InvoiceReference?.Code`.
 - Acceptance:
	 - Unit tests/integration tests show Quantity and UnitPrice align with sample JSON.

S-3: Product mapping, unmapped behavior, and buyer mapping
 - Files:
	 - `src/server/PigFarmManagement.Server/Services/FeedImportService.cs`
	 - `src/server/PigFarmManagement.Server/Infrastructure/Data/Repositories/FeedRepository.cs` (or product repository)
 - Changes:
	 - Match POSPOS item `code` (trim, case-insensitive exact) → internal `ProductCode` first.
	 - If no code match, fallback to exact `name` match.
	 - If no match, create Feed with `UnmappedProduct = true` and set `ExternalProductCode`/`ExternalProductName`.
	 - Map buyer by `buyer_detail.code` only (fail import item if buyer missing?) — at minimum record buyer code on Feed.
 - Acceptance:
	 - Mapped items link to product entity (if mapping exists), unmapped items flagged and external fields populated.

S-4: Idempotency & dedupe
 - Files:
	 - `src/server/PigFarmManagement.Server/Services/FeedImportService.cs`
	 - `src/server/PigFarmManagement.Server/Infrastructure/Data/Repositories/IFeedRepository.cs`
 - Changes:
	 - Add/ensure repository method `ExistsByInvoiceNumberAsync(string invoiceNumber)`.
	 - Skip creating duplicate Feed if `InvoiceNumber` already exists; return duplicate result entry in import diagnostics.
 - Acceptance:
	 - Re-running import with identical transaction `code` does not create duplicate records and the import result marks them skipped.

S-5: Integration scenario and import runner
 - Files:
	 - `src/server/PigFarmManagement.Server/Fixtures/run-import.ps1` (small script to call import endpoint locally) or a test harness in tests project.
	 - Update `specs/004-fix-incorrect-feed/quickstart.md` with commands to run the runner.
 - Acceptance:
	 - Running the script against the sample JSON produces a diagnostics JSON saved in `specs/004-fix-incorrect-feed/validation/`.

Shared (DTOs, converters, and tests)

SH-1: Tolerant numeric parsing for POSPOS DTOs (finish)
 - Files:
	 - `src/shared/PigFarmManagement.Shared/Domain/External/PosApiModels.cs` (ensure `TolerantDecimalConverter` exists and is applied to numeric fields)
 - Acceptance:
	 - `JsonSerializer.Deserialize<PosPosFeedItem>` handles number token and numeric-string token for `stock`, `price`, `special_price`, and `total_price_include_discount`.
 - Tests:
	 - `src/server/PigFarmManagement.Server.Tests/PosposParsingTests.cs` — test cases for numeric and string forms.

SH-2: Shared Feed DTO updates & mapping helpers
 - Files:
	 - `src/shared/PigFarmManagement.Shared/Domain/DTOs.cs` / `ValueObjects.cs` — ensure `InvoiceReferenceCode`, `ExternalProductCode`, `ExternalProductName`, `UnmappedProduct` are defined and serializable.
 - Acceptance:
	 - Server and client build against shared project, and the fields are present in the API responses.

SH-3: Unit tests for import edge-cases
 - Files:
	 - `src/server/PigFarmManagement.Server.Tests/ImportUnitTests.cs`
 - Tests to add:
	 - Missing `stock` → treat as 0 or fail item (decide policy in spec and implement accordingly).
	 - `stock` as string numeric with whitespace → parse and coerce.
	 - `price` zero or missing → fallback logic or fail.
 - Acceptance:
	 - Tests encode chosen policy and pass.

Client (UI)

CL-1: Feed history UI — show InvoiceReferenceCode
 - Files:
	 - `src/client/PigFarmManagement.Client/Features/PigPens/Pages/PigPenDetailPage.razor` (or the feed-history component)
 - Changes:
	 - Add a new column `Invoice Reference` (bound to `Feed.InvoiceReferenceCode`) next to `InvoiceNumber`.
	 - Ensure client DTOs include `InvoiceReferenceCode` (shared DTOs cover this if using the shared project types).
 - Acceptance:
	 - Running the client locally shows `InvoiceReferenceCode` values for feeds imported from POSPOS sample JSON.

CL-2: Client tests / manual validation
 - Files:
	 - Client unit tests for rendering components (if present in repo) or a manual test step in quickstart
 - Acceptance:
	 - Visual verification that feed history includes the new column and values.

Cross-cutting / CI

CI-1: Add tests to pipeline
 - Files:
	 - `azure-pipelines.yml` or GitHub Actions workflow (where CI is configured)
 - Changes:
	 - Ensure tests added above run in CI.
 - Acceptance:
	 - PR runs CI, tests execute, build passes.

Developer workflow & order (short)
1. SH-1 (tolerant parsing tests) → implement converter → run tests.
2. S-2 (import coercion & invoice wiring) → small unit tests → run server build.
3. S-3 (mapping/unmapped) & S-4 (idempotency) → tests & small refactors.
4. S-1 (DB migration) and apply locally if you want DB-validated runs.
5. S-5 (integration runner) → produce validation output.
6. SH-3 (edge-case tests) → fix any failures.
7. CL-1 (client UI) → run client and visually confirm.
8. CI-1 & PR: ensure tests run in CI and open PR.

Acceptance criteria summary
- Import pipeline accepts POSPOS JSON (number or numeric-string) and creates Feed records with bag-count Quantity and per-bag UnitPrice.
- InvoiceReferenceCode is persisted and delivered to client feed-history UI.
- Product matching uses `code` → fallback `name`; unmapped items are flagged with external metadata.
- Import is idempotent by transaction `code`.

If you'd like, I can now: mark this tasks update completed in the todo list and start implementing SH-1 (tolerant parsing) — I will run tests and report back. Say “start tolerant parsing” to begin.

