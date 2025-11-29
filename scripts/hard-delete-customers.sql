-- DANGER: Hard delete all customers and related data from Railway PostgreSQL
-- This PERMANENTLY removes customer data and cannot be easily undone
-- Make sure you have a backup before running this!

BEGIN;

-- Show what will be deleted
SELECT 'Customers' as table_name, COUNT(*) as record_count FROM "Customers"
UNION ALL
SELECT 'PigPens' as table_name, COUNT(*) as record_count FROM "PigPens"
UNION ALL  
SELECT 'Deposits' as table_name, COUNT(*) as record_count FROM "Deposits"
UNION ALL
SELECT 'Harvests' as table_name, COUNT(*) as record_count FROM "Harvests";

-- Delete in proper order to handle foreign key constraints
-- 1. Delete deposits (references PigPens)
DELETE FROM "Deposits" WHERE "PigPenId" IN (
    SELECT "Id" FROM "PigPens"
);

-- 2. Delete harvests (references PigPens)  
DELETE FROM "Harvests" WHERE "PigPenId" IN (
    SELECT "Id" FROM "PigPens"
);

-- 3. Delete pig pen formula assignments (references PigPens)
DELETE FROM "PigPenFormulaAssignments" WHERE "PigPenId" IN (
    SELECT "Id" FROM "PigPens"
);

-- 4. Delete pig pens (references Customers)
DELETE FROM "PigPens";

-- 5. Finally delete customers
DELETE FROM "Customers";

-- Show results after deletion
SELECT 'Customers' as table_name, COUNT(*) as remaining_records FROM "Customers"
UNION ALL
SELECT 'PigPens' as table_name, COUNT(*) as remaining_records FROM "PigPens"
UNION ALL
SELECT 'Deposits' as table_name, COUNT(*) as remaining_records FROM "Deposits"
UNION ALL
SELECT 'Harvests' as table_name, COUNT(*) as remaining_records FROM "Harvests";

-- WARNING: Uncomment the line below ONLY if you're absolutely sure
-- COMMIT;

-- Keep this uncommented to rollback by default
ROLLBACK;