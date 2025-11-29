# Connect to Railway PostgreSQL Database and Remove Customer Data
# 
# Prerequisites:
# - Railway CLI installed and logged in
# - Railway project with PostgreSQL service

# Step 1: Connect to Railway PostgreSQL
Write-Host "Connecting to Railway PostgreSQL database..."
railway connect Postgres

# Alternative: Run SQL commands via Railway CLI
# For soft delete (safer option):
railway run psql $DATABASE_URL -c "
BEGIN;
UPDATE \"Customers\" 
SET \"IsDeleted\" = true, \"DeletedAt\" = NOW(), \"DeletedBy\" = 'railway_cleanup'
WHERE \"IsDeleted\" = false;
SELECT COUNT(*) as deleted_customers FROM \"Customers\" WHERE \"IsDeleted\" = true;
COMMIT;
"

# For hard delete (permanent removal):
# WARNING: This permanently deletes data!
# railway run psql $DATABASE_URL -c "
# BEGIN;
# DELETE FROM \"Deposits\" WHERE \"PigPenId\" IN (SELECT \"Id\" FROM \"PigPens\");
# DELETE FROM \"Harvests\" WHERE \"PigPenId\" IN (SELECT \"Id\" FROM \"PigPens\");
# DELETE FROM \"PigPenFormulaAssignments\" WHERE \"PigPenId\" IN (SELECT \"Id\" FROM \"PigPens\");
# DELETE FROM \"PigPens\";
# DELETE FROM \"Customers\";
# COMMIT;
# "

Write-Host "Customer data removal completed."