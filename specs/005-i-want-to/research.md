# Research: Import POSPOS Stock to Feed Formula

## Decisions & Findings

### Decision: POSPOS API Integration Approach
**Rationale**: Use HTTP client to fetch product data from POSPOS API endpoint (assumed GET /products/{code}). Store fetched data in FeedFormula to avoid repeated calls. Handle network failures with retries and fallbacks.

**Alternatives Considered**:
- Batch fetch all products: Rejected due to potential large data volume and unnecessary storage.
- Local cache only: Rejected as it doesn't provide fresh cost data.

### Decision: FeedFormula Entity Structure
**Rationale**: Mirror POSPOS product JSON fields in FeedFormula record for consistency. Add user-input field ConsumeRate for per-pig consumption. Use Guid for Id, map POSPOS _id to ExternalId.

**Alternatives Considered**:
- Separate Product entity: Rejected to keep simplicity and avoid extra joins.
- Minimal fields: Rejected as full POSPOS data is needed for display and calculations.

### Decision: Handling Duplicate Product Codes
**Rationale**: Allow multiple FeedFormula with same Code but different Id. Use Code as lookup key, handle conflicts by logging and skipping duplicates.

**Alternatives Considered**:
- Unique constraint on Code: Rejected as POSPOS may have valid duplicates.
- Merge duplicates: Rejected as it complicates logic.

### Decision: Stock Quantity Mapping
**Rationale**: Use stock from POSPOS transaction order_list as quantity. Convert to appropriate units if needed (e.g., bags to kg).

**Alternatives Considered**:
- Fetch stock from product endpoint: Rejected as transaction order_list is the source of truth for purchased quantities.

### Decision: Cost Calculation for Profit
**Rationale**: Use Cost from FeedFormula for special price calculation. Formula: SpecialPrice = Cost * (1 + ProfitMargin).

**Alternatives Considered**:
- Use external pricing API: Rejected for simplicity and offline capability.

### Decision: Failure Handling
**Rationale**: On network timeout, log error and use cached/default values. For duplicate codes, skip and log.

**Alternatives Considered**:
- Block import on failures: Rejected as it disrupts user workflow.

## Resolved Unknowns
- POSPOS API: Assumed RESTful with product endpoints.
- Data mapping: Direct field mapping with conversions.
- Error recovery: Graceful degradation with logging.