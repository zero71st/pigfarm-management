# Research: Cleanup duplicate docs

## Decision: Scope and exclusion
- Decision: Exclude `.specify/` and entire `specs/` folder from automated cleanup.
- Rationale: User explicitly requested not to touch specify-kit artifacts; these files are often authoritative and machine-managed.
- Alternatives considered: selective protection based on metadata; rejected because user demanded full protection.

## Decision: Similarity detection method
- Decision: Use SHA1 for exact duplicates and Jaccard word-set similarity (threshold 0.85) for near-duplicates.
- Rationale: Simple, fast, and robust for markdown docs; avoids heavy NLP deps.
- Alternatives: Levenshtein/diff-based similarity or embedding-based semantic similarity. Rejected for added complexity and dependencies.

## Decision: Canonical selection heuristic
- Decision: Prefer files already located under `docs/` as canonical, otherwise select file with highest unique word count.
- Rationale: Keeps canonical content centralized in `docs/` when possible; word-count heuristic favors more complete docs.

## Risk analysis
- Risk: Generated artifacts (e.g., `docs/generated/`) may be misclassified as duplicates. Mitigation: exclude `docs/generated/` by default and add to report for manual review.
- Risk: Important small variations may be lost by automatic consolidation. Mitigation: only auto-merge unique lines; always archive originals.

## Tooling choices
- Implemented in PowerShell for cross-platform compatibility with repository workflows (authors work on Windows). Script will support `--dry-run` and `--apply` modes and produce a `docs/cleanup-report.md`.
