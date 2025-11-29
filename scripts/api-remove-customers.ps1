# Remove customers via API endpoints (if your app is deployed)
# This uses the application's business logic for safe deletion

# Set your Railway app URL and admin API key
$RAILWAY_APP_URL = "https://your-app.railway.app"  # Replace with your actual URL
$ADMIN_API_KEY = "your-admin-api-key"              # Replace with your admin API key

# Get all customers first
Write-Host "Fetching all customers..."
$customers = Invoke-RestMethod -Uri "$RAILWAY_APP_URL/api/customers" -Headers @{"X-Api-Key" = $ADMIN_API_KEY}

Write-Host "Found $($customers.Count) customers to process"

# Soft delete each customer using the application's delete endpoint
foreach ($customer in $customers) {
    try {
        Write-Host "Deleting customer: $($customer.Code) - $($customer.FirstName) $($customer.LastName)"
        
        # Use the application's delete endpoint (soft delete)
        Invoke-RestMethod -Uri "$RAILWAY_APP_URL/api/customers/$($customer.Id)" -Method DELETE -Headers @{"X-Api-Key" = $ADMIN_API_KEY}
        
        Write-Host "✓ Successfully deleted customer $($customer.Code)"
    }
    catch {
        Write-Host "✗ Failed to delete customer $($customer.Code): $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "Customer deletion process completed."

# Verify deletion
Write-Host "Verifying remaining active customers..."
$remainingCustomers = Invoke-RestMethod -Uri "$RAILWAY_APP_URL/api/customers" -Headers @{"X-Api-Key" = $ADMIN_API_KEY}
Write-Host "Remaining active customers: $($remainingCustomers.Count)"