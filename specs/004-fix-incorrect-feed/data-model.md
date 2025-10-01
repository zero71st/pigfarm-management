# Data Model Notes

Entities impacted by the POSPOS import flow:

- Feed (existing)
  - Add: ExternalProductCode (string, nullable)
  - Add: ExternalProductName (string, nullable)
  - Add: UnmappedProduct (bool, default=false)
  - Ensure: InvoiceNumber (string) stores POSPOS transaction `code` for dedupe

- Product (existing)
  - Matching: ProductCode must be compared to POSPOS `code` (trim/uppercase)
  - Fallback: Product.Name exact match against POSPOS `name`

- Customer / PigPen mapping
  - Use buyer_detail.code to resolve pigpen/customer

Migration notes:
- If Feed table is persisted via EF Core, add the three new columns and backfill as needed.
