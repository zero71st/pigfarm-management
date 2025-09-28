# Feature Specification: Connect to POSPOS for invoice data

**Feature Branch**: `003-connect-to-pospos`  
**Created**: 2025-09-28  
**Status**: Draft  
**Input**: User description: "connect to pospos to get invoice info, and remove all mock depend of invoice (python, json)"

## Execution Flow (main)
1. Parse the user description and extract scope: retrieve live invoice data from POSPOS and remove mock invoice dependencies from the codebase and tests.
2. Identify actors and data shapes required for invoice retrieval and downstream consumers.
3. Run dry-run integration in an isolated environment to validate live responses and map fields.
4. Replace mock data paths/usages with production data adapters behind a controlled feature flag or configuration.
5. Execute tests (contract + integration) to confirm parity and that no consumer breaks occur.

---

## ⚡ Quick Guidelines
- Focus on WHAT the system must provide: reliable, testable invoice data from POSPOS and removal of mock artifacts that currently stand in for that data.
- Avoid prescribing implementation languages or libraries in this spec. The spec describes behavior, data contracts, and acceptance criteria.

## User Scenarios & Testing *(mandatory)*

### Primary User Story
As a system operator or downstream job, I want the application to use live invoice information from the POSPOS service so that reports and imports reflect real transactions instead of local mock JSON fixtures.

### Acceptance Scenarios
1. Given a valid invoice identifier exists in POSPOS, when the system requests invoice details, then it returns an invoice object containing invoice id, date, customer reference, line items, totals, and payment status.
2. Given POSPOS is unavailable or returns an error for a request, when the system attempts to fetch an invoice, then it surfaces a clear error (or retryable failure) to the caller and does not return stale or partial mock data silently.
3. Given integration tests run in CI with network access disabled, when the mock mode is enabled, then tests use a maintained mock fixture (only for CI isolation) while developers using live mode receive live responses.

### Edge Cases
- Invoice not found: should return a 404-like result with a documented shape so callers can handle missing data.
- Schema drift: if POSPOS adds/removes fields, the importer should ignore unknown fields and log schema differences for later mapping.
- Partial data: if line items are empty or totals missing, the importer must validate and reject inconsistent invoices with an explanatory error.
- Rate limiting / throttling: the system must handle HTTP 429 or equivalent by retrying with exponential backoff or queuing requests.

## Requirements *(mandatory)*

### Functional Requirements
- FR-001: System MUST be able to fetch invoice details using an invoice identifier from the POSPOS service and provide the data to existing consumers in the same logical shape (or via a documented transformation mapping).
- FR-002: System MUST remove or stop using local mock invoice fixtures (Python scripts, JSON files) in production code paths and tests that are intended to exercise live integrations.
- FR-003: System MUST provide a configuration switch or environment-aware behavior to allow tests/CI to run in mock mode while production uses live POSPOS data.
- FR-004: System MUST validate incoming invoice data and reject records that fail basic integrity checks (missing id, negative totals, inconsistent line items).
- FR-005: System MUST surface clear errors when POSPOS is unreachable or returns an error, and not silently fall back to stale mocks.
- FR-006: System MUST write an audit/log entry for each imported invoice (source, timestamp, mapping decisions) to aid debugging and rollback.

### Non-functional Requirements
- NFR-001: The importer should be idempotent for a given invoice id (re-processing the same invoice must not create duplicates).
- NFR-002: The integration must be observable (metrics for success/failure counts and latency) and log schema mismatches with at least WARN level.

### Ambiguities / NEEDS CLARIFICATION
 - AUTH: Credentials will be provided via API key stored in environment variables (same approach as customers import).
- ENDPOINTS / SCHEMA: What is the canonical POSPOS API base URL and the exact invoice payload schema (field names, types)? Provide an OpenAPI or example JSON if available. [NEEDS CLARIFICATION]
- SCOPE: Which Python mock files and JSON fixtures should be removed versus retained for offline CI? Please list paths or confirm policy. [NEEDS CLARIFICATION]
- FAILOVER: Should the system have a fallback to cached invoice snapshots if POSPOS is temporarily unavailable? [NEEDS CLARIFICATION]

**Resolved: POSPOS endpoint (provided by user)**
- POSPOS transactions endpoint: https://go.pospos.co/developer/api/transactions
- The integration will call the endpoint with query parameters: start (YYYY-MM-DD), end (YYYY-MM-DD), page (default: 1), limit (default: 200). Example provided by user:
  - https://go.pospos.co/developer/api/transactions?page=1&limit=200&start=2025-09-03&end=2025-09-03
- For imports we will send start and end date; page will be kept at 1 and limit at 200 as requested.

> Note: The schema (field names and types) is still considered [NEEDS CLARIFICATION] — if you can paste a sample response or OpenAPI, I will generate an exact contract mapping.

## Key Entities *(include if feature involves data)*
- Invoice
  - invoiceId (string)
  - date (ISO 8601)
  - customerRef (string)
  - lines (array of InvoiceLine)
  - subtotal, tax, total (numbers)
  - status (e.g., PAID, UNPAID, CANCELLED)

- InvoiceLine
  - sku or itemCode (string)
  - description (string)
  - quantity (number)
  - unitPrice (number)
  - lineTotal (number)

- InvoiceImportLog
  - source (string: POSPOS)
  - fetchedAt (timestamp)
  - invoiceId (string)
  - mappingNotes (string)

## Review & Acceptance Checklist
- [ ] Specification clearly describes user value and acceptance scenarios
- [ ] All FRs are testable and have an associated acceptance test or contract
- [ ] All [NEEDS CLARIFICATION] items are answered before implementation
- [ ] No production code references local mock fixtures after implementation
- [ ] Integration observability (metrics/logs) is present

## Execution Status
- [X] User description parsed
- [X] Key concepts extracted
- [X] Ambiguities recorded as NEEDS CLARIFICATION
- [ ] User scenarios defined (complete)
- [ ] Requirements generated (complete)
- [ ] Entities identified
- [ ] Review checklist passed

---

### Next steps (planning)
1. Provide POSPOS API details (endpoint, auth, example responses) so contracts can be generated.
2. Inventory mock fixtures and Python/JSON files that act as invoice mocks and mark them for removal or migration to CI-only fixtures.
3. Create contract tests that call a small adapter or test harness to validate field mappings against sample POSPOS responses.
4. Implement importer with idempotency, validation, logging, and a config to select live vs mock modes.
5. Run integration tests in a disposable environment and iterate until contract tests pass.

## Clarifications

### Session 2025-09-28
- Q: Which authentication method should POSPOS use? → A: Same as customers import (API key in env vars)
- Q: Will you provide a sample response or should the agent fetch live data? → A: C (Agent should fetch live from POSPOS; user will supply auth)

---
