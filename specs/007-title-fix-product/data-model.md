```markdown
# Data Model: Fix product import - search & selection

Entities
- Product (existing)
  - id: GUID / primary identifier
  - code: string (unique, indexed)
  - name: string
  - description: string (optional)
  - other attributes (price, unit, vendor, etc.)

- ImportRequest (DTO)
  - productIds: list[GUID]
  - initiatedBy: user id (optional)
  - timestamp: ISO8601

Validation rules
- productIds must be non-empty for import (FR-006)
- Each productId must map to an existing Product or be created/updated via upsert behavior (FR-011)

Indexing & performance
- Ensure `code` and `name` fields are indexed for fast lookup. Use case-insensitive indexes if the DB supports them.

``` 
