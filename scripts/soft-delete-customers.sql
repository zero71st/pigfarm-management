-- Soft delete all customers in Railway PostgreSQL database
-- This marks customers as deleted without permanently removing data
-- Can be easily reversed by setting IsDeleted = false

BEGIN;

-- Show current customer count before deletion
SELECT 
    COUNT(*) as total_customers,
    COUNT(*) FILTER (WHERE IsDeleted = true) as already_deleted,
    COUNT(*) FILTER (WHERE IsDeleted = false) as active_customers
FROM "Customers";

-- Soft delete all active customers
UPDATE "Customers" 
SET 
    "IsDeleted" = true,
    "DeletedAt" = NOW(),
    "DeletedBy" = 'manual_cleanup'
WHERE "IsDeleted" = false;

-- Show results after soft deletion
SELECT 
    COUNT(*) as total_customers,
    COUNT(*) FILTER (WHERE IsDeleted = true) as deleted_customers,
    COUNT(*) FILTER (WHERE IsDeleted = false) as remaining_active
FROM "Customers";

-- Uncomment the line below to commit changes
-- COMMIT;

-- If you want to rollback, uncomment the line below instead
ROLLBACK;