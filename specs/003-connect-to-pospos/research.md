# Research: POSPOS invoice import

## Goal
Collect live transaction/invoice data from POSPOS and replace local mock fixtures used for invoice-related flows.

## Decisions
- Integration will call the POSPOS transactions endpoint with start/end dates and fixed pagination (page=1, limit=200) for initial imports.
- Authentication: reuse existing approach used for customers import — API key exposed via environment variable (`POSPOS_API_KEY`).
- Safety: initial adapter will write raw responses to `data/pospos/` for manual inspection before mapping into domain models.

## Risks
- Schema drift between POSPOS and local expectations — mitigate by logging unknown fields and failing fast in tests.
- Rate limiting — implement retry/backoff and keep page=1/limit=200 as requested; large ranges should be chunked.
- Sensitive data in responses — ensure raw dumps stay in repo .gitignore or saved only locally.

## Next steps
- Ask user for sample responses or run fetch adapter to capture live JSON.
- Create contract tests and mapping tasks using the sample responses.
