# Test pig pen creation after fix
$baseUrl = "https://pigfarm-management-production.up.railway.app"

# Test data for pig pen creation
$testPigPen = @{
    customerId = "b0a4e0a8-1234-5678-9abc-1234567890ab"  # Use a valid customer ID
    penCode = "TEST-PEN-$(Get-Date -Format 'HHmmss')"
    pigQty = 100
    registerDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    actHarvestDate = $null
    estimatedHarvestDate = (Get-Date).AddDays(120).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    type = "Fattening"
    depositPerPig = 500.00
    selectedBrand = "TestBrand"  # This will trigger formula assignment attempt
    note = "Test pig pen creation with formula fix"
} | ConvertTo-Json

Write-Host "Testing pig pen creation with potential formula assignment..."
Write-Host "Payload: $testPigPen"

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/pig-pens" -Method POST -ContentType "application/json" -Body $testPigPen
    Write-Host "✅ SUCCESS: Pig pen created successfully!" -ForegroundColor Green
    Write-Host "Pig pen ID: $($response.id)"
    Write-Host "Formula assignments count: $($response.formulaAssignments.Length)"
}
catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        Write-Host "Error body: $errorBody" -ForegroundColor Red
    }
}