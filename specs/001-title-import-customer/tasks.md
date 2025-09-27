```markdown
# Tasks: Import customer from POSPOS API (memory-only)

Feature: Import customer from POSPOS API (memory-only)
Branch: `001-title-import-customer`
Spec: `specs/001-title-import-customer/spec.md`
Plan: `specs/001-title-import-customer/plan.md`

Overview
--------
Create a small in-memory importer service that fetches customers from the POSPOS API and exposes two backend endpoints:

- POST `/import/customers` — trigger an import; returns import summary {created, updated, skipped, errors}
- GET `/import/customers/summary` — last import summary

Constraints: memory-only (no DB), credentials via env vars `POSPOS_API_BASE` and `POSPOS_API_KEY`, mapping file `customer_id_mapping.json` used for lightweight persistence of POSPOS->internal mapping.

Execution rules
---------------
- Follow TDD ordering where possible: create contract tests first ([P] parallelizable), then models, then services, then endpoints, then UI wiring and polish.
- Mark tasks that can run in parallel with [P]. Tasks that touch the same file should be executed sequentially.

Task list
---------

T001. (Setup) Verify local dev environment and run server (blocking)
- Purpose: Ensure developer machine can build/run the existing solution before implementing feature.
- How: From repo root run the server to confirm net8 build.
- Command/Path: `dotnet run --project src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj`
- Dependencies: none

T002. (Test) Create contract test for POST `/import/customers` [P]
- Purpose: Define expected request/response shape from `contracts/import-customers.yaml`.
- File(s): `specs/001-title-import-customer/contracts/import-customers.yaml` -> test file `tests/importers/ImportCustomersContractTests.cs`
- Action:
  1. Add a test that starts the server (test host), POSTs to `/import/customers`, and asserts the response JSON contains integer `created`, `updated`, `skipped`, and `errors` array.
  2. The test should expect 200 status. The implementation will initially return a failing response until implementation exists (TDD).
- Parallel: Yes ([P]) — independent test file.

T003. (Test) Create contract test for GET `/import/customers/summary` [P]
- Purpose: Define expected response shape for summary endpoint.
- File(s): `tests/importers/ImportCustomersSummaryContractTests.cs`
- Action:
  1. Start test host, GET `/import/customers/summary`, and assert JSON has `timestamp` (string), `created`, `updated`, `skipped` and `errors` array.
  2. Expect 200 status.
- Parallel: Yes ([P])

T004. (Model) Implement in-memory domain models [P] - DONE
- Purpose: Add the C# POCOs matching `data-model.md` to the server project.
- Files to create: `src/server/PigFarmManagement.Server/Models/PosposCustomer.cs`, `src/server/PigFarmManagement.Server/Models/Customer.cs`, `src/server/PigFarmManagement.Server/Models/CustomerMapping.cs`
- Actions:
  1. Create POCOs with properties as specified (posposId, name, phone, email, address, createdAt; internal id GUID for Customer).
  2. Add minimal validation attributes or helper validators where appropriate.
- Parallel: Yes ([P]) — independent model files.

T005. (Service) Add `IPosposClient` HTTP client and `PosposImporter` service - DONE
- Purpose: Encapsulate POSPOS API calls (pagination, retries) and import logic (map/create/update internal customers and mapping file).
- Files to create:
  - `src/server/PigFarmManagement.Server/Services/IPosposClient.cs`
  - `src/server/PigFarmManagement.Server/Services/PosposHttpClient.cs` (reads env vars `POSPOS_API_BASE`, `POSPOS_API_KEY`)
  - `src/server/PigFarmManagement.Server/Services/IPosposImporter.cs`
  - `src/server/PigFarmManagement.Server/Services/PosposImporter.cs`
- Actions:
  1. Implement a `PosposHttpClient` with methods to fetch pages of POSPOS customers and simple retry/backoff.
  2. Implement `PosposImporter` to perform import flow:
     - Read `customer_id_mapping.json` if exists into dictionary {posposId -> internalId}.
     - For each POSPOS record: if mapping exists, load internal customer (in-memory), apply overwrite rules (update fields) and mark `updated`. If no mapping, create new internal Customer with GUID, add mapping, mark `created`.
     - Normalize phone, validate email when present; collect any `errors`.
     - Keep an in-memory last-import summary object and expose it to the summary endpoint.
     - On write, persist `customer_id_mapping.json` atomically (write tmp file, rename) and optionally keep a `.bak`.
  3. Register services in DI (in `Program.cs`) scoped as singletons where appropriate (mapping store singleton, importer singleton/service).
- Dependencies: T004 (models) must be done first. Tests T002/T003 can be run in parallel once skeleton endpoints exist.

T006. (Integration) Add mapping file IO helpers and safe write [P] - DONE
- Purpose: Implement robust read/write for `customer_id_mapping.json`.
- File(s): `src/server/PigFarmManagement.Server/Services/MappingStore.cs`
- Actions:
  1. Implement methods: LoadMapping(): Dictionary<string,string>, SaveMapping(dict) (atomic write with .tmp and rename), Backup on write (create `.bak` with timestamp), file path = repo root `customer_id_mapping.json`.
  2. Ensure file IO is safe if file does not exist and is cross-platform.
- Parallel: Can be done while importer is developed ([P]) but must be available before final persistence call in T005.

T007. (Endpoint) Create Import controller and wire routes
- Purpose: Expose API endpoints per contract.
- Files to create: `src/server/PigFarmManagement.Server/Controllers/ImportController.cs`
- Actions:
  1. Implement POST `/import/customers` that calls `IPosposImporter.RunImport()` and returns the summary JSON.
  2. Implement GET `/import/customers/summary` that returns the last import summary from the importer service.
  3. Register controller routing in `Program.cs` if not automatically discovered.
- Dependencies: T004 (models), T005 (importer), T006 (mapping store).

T008. (Test) Implement contract tests to pass against the new endpoints
- Purpose: Make T002/T003 pass by wiring tests to the real server implementation.
- Files: `tests/importers/ImportCustomersContractTests.cs`, `tests/importers/ImportCustomersSummaryContractTests.cs`
- Actions:
  1. Update tests to start the server in test mode, ensure DI registration includes test-friendly PosposHttpClient (e.g., mockable via interface) so tests can stub POSPOS responses.
  2. For now, tests can stub `IPosposClient` to return a small page of customers to exercise created/updated flows.
- Dependencies: T005, T007

T009. (UI) Wire client import trigger (optional small PR)
- Purpose: Add a simple button in the client UI to POST to `/import/customers` and display the returned summary.
- Files: `src/client/PigFarmManagement.Client/Features/Customers/ImportPospos.razor` (new) or integrate into existing import dialog.
- Actions:
  1. Implement a small Razor component that calls the backend endpoint and shows summary results and errors.
  2. Keep UI small and optional; mark as follow-up if client team prefers separate PR.
- Dependencies: T007

T010. (Polish) Add smoke test script and docs [P]
- Purpose: Provide quick verification steps and ensure mapping file behavior documented.
- Files: `specs/001-title-import-customer/README.md` (or update quickstart.md), `scripts/smoke/import-smoke.ps1`
- Actions:
  1. Create a PowerShell smoke script that sets example env vars (POSPOS_API_BASE, POSPOS_API_KEY placeholder), starts server, performs POST import with mocked POSPOS via test DI or stub server, then GET summary.
  2. Update quickstart.md with exact command lines and a note about backing up `customer_id_mapping.json`.
- Parallel: Yes ([P])

T011. (Polish) Add basic logging and error handling
- Purpose: Make the importer resilient (log errors, surface them in the summary errors array).
- Files: `src/server/.../Services/PosposImporter.cs`, `Program.cs` logging config
- Actions:
  1. Ensure importer catches per-record errors, logs them, and appends readable messages to summary.errors.
  2. Add ILogger<T> injection where appropriate.

Parallel task groups (examples)
-----------------------------
- Group A [P]: T002, T003, T004 can be created in parallel (tests and model POCOs).
- Group B [P]: T005 and T006 can be done in parallel if MappingStore is defined early; otherwise T006 should be completed before full persistence calls in T005.
- Group C [P]: T010 and T011 are polish tasks that can run concurrently with UI wiring T009 after endpoints are functioning.

How to run & quick commands
---------------------------
- Run server (dev):
```powershell
dotnet run --project src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj
```

- Run tests (example):
```powershell
dotnet test
```

Notes and acceptance criteria
----------------------------
- Acceptance: POST `/import/customers` returns a JSON summary with counts; mapping file is updated; importer applies overwrite rules described in spec.
- Safety: Mapping file writes are atomic; backups created.
- Scope: No database changes or production auth in this phase.

---

Generated by automation using design artifacts in `specs/001-title-import-customer/` on 2025-09-27

```