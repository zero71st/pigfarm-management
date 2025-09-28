# Research: Fix incorrect feed item mapping

Summary of decisions and findings relevant to implementing POSPOS import mapping.

- Product matching: exact match on POSPOS item `code` first, then exact match on `name` as fallback.
- Buyer mapping: use `buyer_detail.code` only. Do not use `key_card_id` or buyer name.
- Quantity: map `order_list[*].stock` directly to `Feed.Quantity` as integer bag count.
- Price: use `order_list[*].price` as UnitPrice (decimal). Use `total_price_include_discount` for TotalPrice.
- Idempotency: dedupe imports by POSPOS transaction `code` (invoice id).
- Unmapped products: create Feed records with `UnmappedProduct = true` and persist `ExternalProductCode` and `ExternalProductName` for manual mapping.

Notes:
- Upstream `stock` and `price` can be integers, decimals, or numeric-strings. Use tolerant parsing.
- Logging of raw POSPOS JSON responses proved essential to diagnose type mismatches.
