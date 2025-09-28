# Tasks: Cleanup duplicate docs (feature 002-title-cleanup-duplicate)

Feature: Consolidate duplicate documentation into `docs/`, archive duplicates under `docs/_archive/`, and preserve `.specify/` and `specs/` files.

Ordering rules: setup → contract & integration tests (TDD) → models/data → core implementation → integration/connectors → polish & docs.

Parallel marker: [P] = tasks that can run in parallel (different files or independent tasks).

T001 — Setup: Add scripts and lint rules [X]
- Owner: maintainer
- Action: Ensure scripts exist and are executable; add entry to `package.json` or repo README for dev convenience.
- Path: `scripts/cleanup-docs.ps1`, `scripts/cleanup-docs-dryrun.ps1`
- Output: scripts present and documented in `specs/002-title-cleanup-duplicate/quickstart.md`

T002 — Contract test (dry-run report exists) [P] [X]
- Action: Create a test that runs `scripts/cleanup-docs-dryrun.ps1` and asserts `docs/cleanup-report.md` exists and is non-empty.
- Command (example): `powershell -File .\scripts\cleanup-docs-dryrun.ps1; test -f docs/cleanup-report.md` (adapt for CI shell)
- Path: `specs/002-title-cleanup-duplicate/contracts/cleanup-contracts.md`

T003 — Contract test (archive location when applied) [P] [X]
- Action: In a disposable branch or temp workspace, run `scripts/cleanup-docs.ps1 -Apply` and assert `docs/_archive/<timestamp>/` is created and contains expected files.
- Note: This test should run in an isolated environment or with mocked FS.

T004 — Data entity tasks [P] [X]
- Action: Implement code (script) data structures as defined in `data-model.md` for Document and ArchiveEntry used by scripts.
- Path: `scripts/cleanup-docs.ps1` (inline data structures)

T005 — Move top-level docs into `docs/` (dry-run verified) (sequential) [X]
- Action: Implement move logic in `scripts/cleanup-docs.ps1` and verify dry-run output shows moves (already implemented). Add tests to assert proposed destination paths.
- Path: `scripts/cleanup-docs.ps1`

T006 — Duplicate grouping & similarity calculation [P]
- Action: Ensure SHA1 exact matches and Jaccard calculation implemented and unit-tested. Add small unit tests with sample files.
- Path: `scripts/cleanup-docs.ps1`, `tests/docs_cleanup/test_similarity.ps1`

T007 — Canonical selection & merge heuristics
- Action: Implement canonical selection (prefer `docs/`, then largest word count). Implement merging of unique lines into canonical under a 'Merged from' section. Add unit tests that simulate two files with overlapping content.
- Path: `scripts/cleanup-docs.ps1`, `tests/docs_cleanup/test_merge.ps1`

T008 — Archive non-canonical files (apply mode) [X]
- Action: Implement archival to `docs/_archive/<timestamp>/` and ensure atomic moves. Add test that checks files moved to archive in apply mode.
- Path: `scripts/cleanup-docs.ps1`, `tests/docs_cleanup/test_archive.ps1`

T009 — Commit & branch creation automation (apply mode) [X]
- Action: When `-Apply` is used, create the archive folder, stage changes, and commit with message `chore(docs): cleanup duplicates and collect docs into docs/`. Optionally create a feature branch and push (manual approval recommended).
- Path: `scripts/cleanup-docs.ps1`

T010 — Quickstart and docs polish [P] [X]
- Action: Update `specs/002-title-cleanup-duplicate/quickstart.md`, top-level `README.md` with run instructions and caveats (preserve `specs/` and `.specify/`).
- Path: `specs/002-title-cleanup-duplicate/quickstart.md`, `README.md`

T011 — Manual review & PR creation (human step)
- Action: Create a PR from branch `002-title-cleanup-duplicate` with `docs/cleanup-report.md` summary and checklist. Request at least one reviewer.

T012 — Link validation [X]
- Action: After applying, run a link-checker (basic grep or `markdown-link-check`) to ensure no broken links in docs/ and specs/; list failures in the PR.
- Path: `scripts/link-check.ps1` (optional) or use existing CI link checker.

Parallel execution examples
- Can run T002, T003, T004, T006, T010 in parallel as they are test/data setup tasks [P].
- Core implementation (T005, T007, T008, T009) should be sequential: T005 → T006 → T007 → T008 → T009.

How to run (developer)
1. Dry-run: `powershell -File .\scripts\cleanup-docs.ps1 -SimilarityThreshold 0.85`
2. Review `docs/cleanup-report.md` and discuss any manual merges.
3. Apply (when ready): `powershell -File .\scripts\cleanup-docs.ps1 -SimilarityThreshold 0.85 -Apply`
