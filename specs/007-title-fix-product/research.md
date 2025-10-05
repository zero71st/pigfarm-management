```markdown
# Research: Fix product import - search & selection

Date: 2025-10-05

Decisions made
- Search behavior: return all matching products in a single response (user confirmed). Rationale: small-to-medium catalogs in current deployments; simpler UX; avoids pagination UI complexity.
- Selection lifecycle: clear selections when a new search runs. Rationale: avoids accidental imports spanning unrelated queries and keeps selection scope obvious.
- Duplicate handling: Upsert (idempotent) on import — update existing records and report created vs updated. Rationale: safer for re-runs and avoids duplicate records.

Alternatives considered
- Server-side pagination: considered for very large catalogs; deferred due to user preference for single-response. If dataset grows, revisit with pagination API and client support.
- Persisting selections across searches: considered but rejected because it increases UI complexity and surprising cross-query behavior.

Open questions (deferred)
- If catalog size grows beyond threshold (e.g., >10k), adopt server-side pagination and incremental selection APIs.

``` 
