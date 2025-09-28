# Tasks: Connect to POSPOS (feature 003-connect-to-pospos)

Feature: Fetch invoice/transaction data from POSPOS and remove mock invoice dependencies. Preserve CI mock mode and make importer idempotent and observable.

Ordering: setup → contract tests (TDD) → data/model tasks → core importer → integration & polish → PR/review

[P] = can run in parallel

T001 — Setup: create scripts and quickstart [X]
- Owner: maintainer
- Path: `scripts/pospos_fetch_transactions.ps1`, `specs/003-connect-to-pospos/quickstart.md`
- Output: adapter script available and quickstart documented

T002 — Contract tests (transactions endpoint) [P]
- Owner: backend
- Action: Create tests that call POSPOS transactions endpoint with sample dates and validate response shape (presence of invoice id, totals, lines)
- Path: `specs/003-connect-to-pospos/contracts/cleanup-contracts.md`, `tests/pospos/contract_transactions.ps1`
- Output: failing contract tests until mapping is implemented

T003 — Sample capture (manual) [X]
- Action: Run `scripts/pospos_fetch_transactions.ps1` locally with `POSPOS_API_KEY` to capture raw JSON into `data/pospos/` for mapping
- Output: sample JSON files for contract refinement

T004 — Data model stabilization [X]
- Owner: backend
- Action: Finalize `data-model.md` based on sample JSON and update types
- Path: `specs/003-connect-to-pospos/data-model.md`

T005 — Similarity & dedupe tests (T006 equivalent from docs feature) [ ]
- Action: Add unit tests for deduplication/idempotency behavior when importing the same invoice twice
- Path: `tests/pospos/test_dedupe.ps1`

T006 — Importer implementation (idempotent adapter)
- Owner: backend
- Action: Implement importer that calls POSPOS endpoint (start/end, page=1, limit=200), validates invoice shape, writes logs and Audit entries, and persists to internal store (or hands to existing import pipeline)
- Path: `src/server/...` or `scripts/importers/pospos_importer.*` (language TBD)

T007 — Archive & remove mock fixtures [ ]
- Action: Identify Python/JSON mock fixtures used for invoices, move them to `tests/fixtures/ci-only/` or remove from production code paths
- Path: repository-wide search; update tests and CI

T008 — Integration tests
- Action: Run import flow in disposable environment (or mocked POSPOS) to validate end-to-end behavior
- Path: `tests/pospos/integration_*`

T009 — Observability and metrics
- Action: Add metrics for imports (counts, latency, failures) and logging of schema mismatches

T010 — PR & manual review [X]
- Action: Create PR from branch `003-connect-to-pospos` summarizing changes and attaching `docs/cleanup-report.md` and POSPOS samples

Notes:
- Several tasks are marked complete where adapter script, quickstart, research and plan steps are already done.
- T006 requires a decision about implementation language and destination in repo; default is to add a small adapter under `scripts/` or a server-side service in `src/server/` if you prefer.
