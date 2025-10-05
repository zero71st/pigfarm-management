```markdown
# Tasks: 007-title-fix-product

Feature: Fix product import - search & selection
Branch: `007-title-fix-product`

Order: Setup → Contracts/DTOs → Server mapping & repository → Endpoints → Client UI → Polish/Docs

Notes:
- This tasks list intentionally does NOT create automated test files (per user request). Please run the Quickstart steps in `quickstart.md` to verify behavior manually.
- All file paths are relative to repository root.

T001 (Setup) — Confirm branch & artifacts [COMPLETED]
- Path: `specs/007-title-fix-product/`
- Action: Ensure you are on branch `007-title-fix-product` (script already created it). Confirm plan/research/data-model/contracts/quickstart exist.
- Output: Ready-to-work environment.

T002 (Contract) [P] — Add/verify API contract for search & import
- Path: `specs/007-title-fix-product/contracts/openapi.yaml`
- Action: Confirm the OpenAPI contract contains `/api/products/search` (GET q) and `/api/products/import` (POST ImportRequest) as in the spec. If you change request/response shapes, update this file first.
- Output: Contract stable and reviewable.

T003 (DTOs) — Add import DTOs to shared models
- Path: `src/shared/PigFarmManagement.Shared/Domain/DTOs.cs`
- Action: Add/ensure the following DTOs exist and are exported in the shared project:
  - `PosposProductDto` (already present under `Server/Services/ExternalServices` — ensure client/server share shape or map as needed)
  - `ImportRequest { List<Guid> ProductIds }`
  - `ImportItemResult { Guid? ProductId; string Status; string? Message }`
  - `ImportResult { Summary(created, updated, failed), List<ImportItemResult> Items }`
- Output: Shared DTOs available for server and client to reference.

T004 (Server - Data) — Add mapping helper: PosposProductDto -> FeedFormula/Feed DTO
- Path: `src/server/PigFarmManagement.Server/Services/FeedImportService.cs`
- Action: Implement a small, well-tested mapping method that converts a `PosposProductDto` (or `PosPosFeedItem`) into the internal `FeedFormulaDto` / `Feed` DTO used by persistence. Include: product code normalization (trim, ToUpperInvariant), name mapping, nullable cost mapping, unit/category mapping, LastUpdate preservation.
- Output: Reusable mapping method `MapPosposProductToFeedFormula(PosposProductDto)`.

T005 (Server - Repository) — Implement upsert behavior in repository
- Path: `src/server/PigFarmManagement.Server/Infrastructure/Data/Repositories/FeedRepository.cs` (or appropriate Feed/FeedFormula repository files)
- Action: Add an `UpsertFeedsAsync(IEnumerable<FeedDto>)` method that:
  - Uses product code or product id to find existing Feed/FeedFormula rows
  - Updates matching records (idempotent) and inserts new records for non-matches
  - Returns per-item result (created vs updated vs failed)
- Output: Repository method supporting FR-011 (idempotent upsert) and returning item-level results.

T006 (Server - Endpoint) — Implement product search endpoint
- Path: `src/server/PigFarmManagement.Server/Features/Products/ProductEndpoints.cs` (create if missing)
- Action: Add GET `/api/products/search?q={q}`:
  - Validate `q` is non-empty; if empty return 400 or empty array per spec.
  - If `q` looks like a code (single token, no spaces) do case-insensitive exact match against `Code`.
  - Otherwise do case-insensitive substring search against `Name`.
  - Return `PosposProductDto[]` (id, code, name, cost, unit, category, lastUpdate).
  - Use `IPosposProductClient` or product repository as the source (do not leak internal DB-only fields).
- Output: API ready for client to call.

T007 (Server - Endpoint) — Implement import endpoint
- Path: `src/server/PigFarmManagement.Server/Features/Products/ProductImportEndpoints.cs` (or reuse ProductEndpoints)
- Action: Add POST `/api/products/import` that accepts `ImportRequest`:
  - Validate non-empty `productIds` (return 400 if empty).
  - Load `PosposProductDto`s for the provided ids (via IPosposProductClient or DB if cached).
  - Map to `FeedFormulaDto` with mapping helper (T004).
  - Call repository `UpsertFeedsAsync` (T005).
  - Return `ImportResult` summarizing created vs updated and per-item details.
- Output: Import endpoint that performs idempotent upsert and returns itemized results.

T008 (Server - Validation & Contracts) — Wire DTOs into endpoint signatures and update OpenAPI
- Path: `src/server/PigFarmManagement.Server/Features/Products/*` and `specs/007-title-fix-product/contracts/openapi.yaml`
- Action: Ensure endpoints use shared `ImportRequest` and `ImportResult` DTOs. Update OpenAPI YAML if shapes changed. Regenerate any API docs if applicable.
- Output: Contract and server agree on payload shapes.

T009 (Client - Service) [P] — Implement client-side service calls
- Path: `src/client/PigFarmManagement.Client/Features/FeedFormulas/Services/FeedFormulaService.cs`
- Action: Add two methods:
  - `Task<List<PosposProductDto>> SearchPosposProductsAsync(string q)` — calls `/api/products/search?q=` and returns product list.
  - `Task<ImportResult> ImportSelectedFromPosposAsync(List<Guid> productIds)` — posts `ImportRequest` with selected ids.
  - Ensure the HTTP payloads match shared DTO shapes and handle ImportResult items.
- Output: Client service usable by UI.

T010 (Client - UI) — Update `ProductSelectionDialog.razor` UI
- Path: `src/client/PigFarmManagement.Client/Features/FeedFormulas/Components/ProductSelectionDialog.razor`
- Action:
  - Hide or show empty state: initially results area must be empty/hidden until a search is performed (FR-001).
  - Add explicit Search button (FR-003) and wire to `SearchPosposProductsAsync(q)`.
  - Implement selection checkboxes and multi-select list (FR-005).
  - Clear selections when a new search runs as per clarification.
  - Disable Import button until at least one item selected (FR-006) and display tooltip explaining why when disabled.
  - On Import click, call `ImportSelectedFromPosposAsync` and display per-item result summary modal/toast.
  - Ensure keyboard navigation and ARIA attributes for accessibility (NFR-002).
- Output: Working UI for searching, selecting and importing.

T011 (Client - Wiring) — Connect UI to service and handle results
- Path: `src/client/PigFarmManagement.Client/Features/FeedFormulas/Components/ProductSelectionDialog.razor` and `FeedFormulaService.cs`
- Action: Wire the dialog's Search and Import actions to the service methods; show progress indicators and success/failure messages. Ensure only selected product ids are sent (FR-009).
- Output: End-to-end client flow.

T012 (Server - Indexing & Performance) — Add DB indexes for product lookups
- Path: `src/server/PigFarmManagement.Server/Infrastructure/Data/*` and migrations folder
- Action: Add an EF migration (or SQL) to ensure `Product.Code` and `Product.Name` are indexed (case-insensitive where supported) to keep search fast. (Optional if using external POSPOS client search.)
- Output: Migration file and DB updated (run locally).

T013 (Polish) [P] — Logging, metrics and error handling
- Path: `src/server/PigFarmManagement.Server/Services/*` and `src/client/...`
- Action: Add structured logs around search/import flows (requests, counts, failures). Add counters for import-created/import-updated/import-failed. Ensure graceful error messages for client.
- Output: Better observability for imports.

T014 (Docs) [P] — Update README and quickstart
- Path: `README.md`, `specs/007-title-fix-product/quickstart.md`
- Action: Add a one-line changelog to the project README about the product import change and link to the feature quickstart. Ensure quickstart has exact steps for manual verification (it already does) and add example cURL commands.
- Output: Documentation for reviewers and QA.

Parallel execution guidance
- Group A [P]: T002 (contract review) and T003 (DTO updates) can be prepared in parallel.
- Group B [P]: T009 (client service) and T004 (server mapping) can be developed in parallel once DTOs/contracts are stable. However, T010 (UI) depends on T009.

How to run locally (developer commands)
1. Build server and client:
   - dotnet build src/server/PigFarmManagement.Server
   - dotnet build src/shared/PigFarmManagement.Shared
   - npm/yarn not required (Blazor WASM client uses dotnet run)
2. Run server locally:
   - dotnet run --project src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj --urls http://localhost:5000
3. Run client locally:
   - dotnet run --project src/client/PigFarmManagement.Client/PigFarmManagement.Client.csproj --urls http://localhost:7000

Deliverables
- `specs/007-title-fix-product/tasks.md` (this file)
- Server endpoints for `/api/products/search` and `/api/products/import`
- Shared DTOs for import request/result
- Client ProductSelectionDialog wired to service and import flow

``` 
