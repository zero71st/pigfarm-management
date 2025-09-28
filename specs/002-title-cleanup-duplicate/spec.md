# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]`  
**Created**: [DATE]  
**Status**: Draft  
**Input**: User description: "$ARGUMENTS"

## Execution Flow (main)
# Feature Specification: Cleanup duplicate docs

**Feature Branch**: `002-title-cleanup-duplicate`  
**Created**: 2025-09-28  
**Status**: Draft  
**Input**: User description: "clean up duplicate docs except specify kit docs (spec/plan/task)"

## Objective

Consolidate or remove duplicate and overlapping documentation in the repository while preserving the `specify` kit artifacts (files created/managed by the `.specify` tooling under `specs/` and the `.specify/` directory). Produce a small, reviewable PR that archives non-canonical copies and leaves a single authoritative document for each topic.

## Execution Flow (main)

1. Scan the repository for documentation files under paths: `docs/`, `README.md`, `specs/`, and any top-level `*.md` that appear to be documentation.
2. Compute hashes and a similarity score for every pair of candidate documents (exact match or fuzzy text similarity). Group near-duplicates (similarity >= 85%).
3. Exclude from automated deletion any file under `.specify/` and any `specs/**` file whose header or path indicates it was created by the specify tooling. These are "exempt".
4. For each group of near-duplicates, generate a proposed action: (A) keep canonical file and archive others, (B) consolidate by merging unique sections into canonical file and archive others, or (C) manual review if differences are non-trivial.
5. Produce a preview PR branch that contains: the proposed deletions (moved to `docs/_archive/<timestamp>/`), any consolidations applied as edits to the canonical file, and a summary file `docs/cleanup-report.md` listing all actions and reasons.
6. Open a draft PR for human review with a checklist for reviewers to sign off.

If any ambiguous case is found (e.g., docs with small but important differences), mark them for manual review and do not auto-delete.

## User Scenarios & Testing

### Primary User Story
As a documentation maintainer, I want duplicate documentation removed or consolidated so that the repo docs are easier to navigate, authoritative, and free of confusing copies.

### Acceptance Scenarios
1. Given the repo contains multiple versions of the same doc, when the cleanup PR is created, then only a single canonical doc remains in place and non-canonical files are moved to `docs/_archive/<timestamp>/` with a backup entry.
2. Given a doc is referenced in `README.md` or `specs/`, when the cleanup completes, then links resolve to the canonical doc and no broken links are introduced.
3. Given a doc under `.specify/` or a `specs/**` file identified as from the specify kit, when the cleanup runs, then those files are not changed or deleted.

### Edge Cases
- Generated artifacts (e.g., `docs/generated/`) should be ignored unless explicitly listed for cleanup.
- If two docs differ only by formatting or white-space, prefer canonicalizing formatting rather than deleting content.
- If two docs differ by minor but important content (e.g., a command vs. a script update), surface to reviewer for manual merge.

## Requirements

### Functional Requirements
- FR-006: The system MUST detect and group duplicate or highly similar documentation files under repository doc paths.
- FR-007: The system MUST exclude any file under `.specify/` and any `specs/**` file marked as created by the specify tooling from automatic deletion or modification.
- FR-008: For each duplicate-group, the system MUST produce a proposed action and preview (diff) that a human reviewer can approve.
- FR-009: The system MUST move removed files into an archival location `docs/_archive/<timestamp>/` and record an `ArchiveEntry` in `docs/cleanup-report.md`.
- FR-010: The system MUST create a draft PR containing all proposed changes and a human review checklist before any deletion is merged.

### Non-functional Requirements
- NFR-001: The process MUST be reversible (archived files restoreable) via the repo history and archived folder.
- NFR-002: The tool or process MUST run quickly on a developer machine (< 30s for a medium repo) and provide a concise report.

## Key Entities
- Document: { path, checksum, title, headings, references }
- DuplicateCandidate: { canonicalPath, otherPaths[], similarityScore, action }
- ArchiveEntry: { originalPath, archivePath, timestamp, commit }

## Review & Acceptance Checklist

Before merging the cleanup PR, ensure:

- [ ] A reviewer has validated that no specify-tooling files (`.specify/` or specify-created `specs/**`) were removed.
- [ ] All removed files exist in `docs/_archive/<timestamp>/` and are referenced in `docs/cleanup-report.md`.
- [ ] Internal links from `README.md`, `specs/`, and other index pages resolve to the canonical documents.
- [ ] The PR description lists all removed/consolidated files and the justification for each.
- [ ] At least one maintainer has approved the PR.

## Proposed Implementation Steps (developer-facing)

1. Implement a script `scripts/cleanup-docs.ps1` (or `scripts/cleanup-docs.sh`) that performs the flow above. The script must accept a `--dry-run` flag to produce the preview without modifying files.
2. Run `scripts/cleanup-docs.ps1 --dry-run` and review the generated `docs/cleanup-report.md`.
3. If results are acceptable, run without `--dry-run` which will move files to `docs/_archive/<timestamp>/` and commit the changes to a feature branch `002-title-cleanup-duplicate`.
4. Open a PR and request reviewer approval.

Notes: This script is optional; maintainers may perform manual cleanup guided by `docs/cleanup-report.md` if preferred.

## Execution Status

- [x] User description parsed
- [x] Key concepts extracted
- [x] Ambiguities marked: none critical; confirm generated assets policy (assumed ignored)
- [x] User scenarios defined
- [x] Requirements generated
- [x] Entities identified
- [ ] Review checklist to be completed during PR review
