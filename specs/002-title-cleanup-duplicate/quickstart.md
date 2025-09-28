# Quickstart: Docs cleanup

1. Preview changes (dry-run):

```powershell
.\scripts\cleanup-docs.ps1 -SimilarityThreshold 0.85
```

2. If the report looks good, apply changes:

```powershell
.\scripts\cleanup-docs.ps1 -SimilarityThreshold 0.85 -Apply
```

3. Review `docs/cleanup-report.md` and `docs/_archive/<timestamp>/`.
4. Run `git status` and confirm or edit changes; then open a PR from the feature branch.
