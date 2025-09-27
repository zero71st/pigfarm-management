# Data Model: Import customer from POSPOS API (memory-only)

Entities:

- POSPOSCustomer
  - posposId: string (unique)
  - name: string
  - phone: string
  - email: string
  - address: string
  - createdAt: string (ISO8601)

- Customer (internal - in-memory representation)
  - id: string (GUID)
  - name: string
  - phone: string
  - email: string
  - address: string
  - createdAt: string

- CustomerMapping
  - posposId: string -> internalId: string
  - Stored in `customer_id_mapping.json` (persisted file for manual backup)

Validation rules:
- posposId is unique and used as the primary identity from POSPOS.
- email, if present, must follow a valid email format.
- phone is optional but normalized on import when possible.

State transitions:
- Imported -> mapped (when mapping created)
- Mapped -> updated (when overwrite behavior occurs on subsequent imports)
