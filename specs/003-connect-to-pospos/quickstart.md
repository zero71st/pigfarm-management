# Quickstart: POSPOS Invoice Import (developer)

1. Set your POSPOS API key in environment:

```powershell
$env:POSPOS_API_KEY = "<your_api_key>"
```

2. Fetch a sample transaction dump for dates of interest:

```powershell
.\scripts\pospos_fetch_transactions.ps1 -Start 2025-09-03 -End 2025-09-03
```

3. Inspect the raw JSON in `data/pospos/transactions_<Start>_<End>.json`.

4. Run contract tests (to be generated) to validate mapping expectations.

5. When mapping is stable, run the importer in live mode (TBD) or enable via config.
