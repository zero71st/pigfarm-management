# Data Model: POSPOS Invoice Import

## Entities

### Invoice
- invoiceId: string (primary identifier)
- date: string (ISO 8601)
- customerRef: string
- lines: array of InvoiceLine
- subtotal: number
- tax: number
- total: number
- status: string (PAID, UNPAID, CANCELLED)

### InvoiceLine
- itemCode: string
- description: string
- quantity: integer
- unitPrice: number
- lineTotal: number

### InvoiceImportLog
- source: string (POSPOS)
- fetchedAt: timestamp
- invoiceId: string
- mappingNotes: string

## Notes
- Fields marked conservatively; exact types will be finalized once sample JSON from POSPOS is obtained.
- Idempotency: invoiceId is the primary deduplication key.
