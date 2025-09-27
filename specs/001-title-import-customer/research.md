# Research: Import customer from POSPOS API (memory-only)

Date: 2025-09-27

Decision: Implement a simple in-memory POSPOS customer importer (no auth, no database). Rationale: user requested an initial in-memory importer to validate flows quickly before persistence/auth are added.

Alternatives considered:
- Use persistent DB (deferred): more work and migrations; rejected for initial spike.
- Add authentication (deferred): the user asked explicitly to skip auth for now.

Risks & Mitigations:
- Risk: Memory-only approach loses data on restart. Mitigation: Add clear README and migration path to persistent store later; record mapping file `customer_id_mapping.json` for manual backup.
- Risk: Credentials in environment variables during development. Mitigation: Document env var names and example usage in quickstart.

API assumptions:
- POSPOS API exposes a paginated customer endpoint that returns records with an ID, firstName, lastName, phone, email, and address.
- We will implement a simple HTTP client with pagination support and basic retry/backoff.

Research conclusion: proceed with an in-memory importer implementation and simple OpenAPI contract for the local importer endpoints used by UI or CLI.
