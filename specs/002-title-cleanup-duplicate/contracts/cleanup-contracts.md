# Contracts: Docs Cleanup (preview)

This document lists the minimal APIs and file-level contracts that the cleanup process will produce or depend on.

## File-level outputs (no network APIs)

- `docs/cleanup-report.md` — report of actions taken (or preview for dry-run)
- `docs/_archive/<timestamp>/...` — archived original files
- `specs/002-title-cleanup-duplicate/research.md` — research and decisions

## Contract tests (concept)

- Test: `cleanup-report-exists` (dry-run): assert `docs/cleanup-report.md` exists after dry-run.
- Test: `archive-folder-created` (apply): assert `docs/_archive/<timestamp>/` exists after apply.


