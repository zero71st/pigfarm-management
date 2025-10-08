# Quickstart: Enhanced Customer Management

## Development Setup

### Prerequisites
- .NET 8 SDK installed
- Visual Studio 2022 or VS Code with C# extension
- Google Maps API key (for location features)
- Git repository cloned locally

### Environment Setup
1. **Configure Google Maps API**:
   ```json
   // appsettings.Development.json
   {
     "GoogleMaps": {
       "ApiKey": "your-google-maps-api-key-here"
     }
   }
   ```

2. **Run Database Migrations**:
   ```bash
   cd src/server/PigFarmManagement.Server
   dotnet ef database update
   ```

3. **Start Development Servers**:
   ```bash
   # Terminal 1: Backend
   cd src/server/PigFarmManagement.Server
   dotnet run --urls http://localhost:5000

   # Terminal 2: Frontend  
   cd src/client/PigFarmManagement.Client
   dotnet run --urls http://localhost:7000
   ```

4. **Verify Setup**:
   - Open https://localhost:7000/customers
   - Verify customer list loads
   - Test view mode toggle (card ↔ table)

## Feature Testing Scenarios

### 1. Customer Deletion Flow
```gherkin
GIVEN I have customers in the system
WHEN I navigate to Customer Management
AND I click delete on a customer without active pig pens
THEN I should see a confirmation dialog
AND I can successfully delete the customer

WHEN I try to delete a customer with active pig pens
THEN I should see a blocking message
AND deletion should be prevented
```

**Manual Test Steps**:
1. Go to `/customers`
2. Find customer without pig pens → Click delete → Confirm → Verify removal
3. Find customer with pig pens → Click delete → Verify blocking message

### 2. Location Management
```gherkin
GIVEN I'm editing a customer
WHEN I enter valid latitude and longitude coordinates
AND I save the customer
THEN the location should be stored
AND I should be able to view it on Google Maps

WHEN I enter invalid coordinates (e.g., lat > 90)
THEN I should see validation errors
AND the form should not submit
```

**Manual Test Steps**:
1. Edit any customer
2. Enter coordinates: `lat: 40.7128, lng: -74.0060` (NYC)
3. Save and verify map display
4. Try invalid coordinates: `lat: 100, lng: 200`
5. Verify validation prevents save

### 3. View Mode Switching
```gherkin
GIVEN I'm on the Customer Management page
WHEN I toggle from card view to table view
AND I refresh the page
THEN the table view should be remembered

WHEN I switch back to card view
THEN the preference should persist across sessions
```

**Manual Test Steps**:
1. Start in card view (default)
2. Toggle to table view → Verify layout change
3. Refresh page → Verify table view persists
4. Toggle back to card view → Refresh → Verify persistence

### 4. Manual POS Sync
```gherkin
GIVEN the POS system has updated customer data
WHEN I trigger a manual sync
THEN customer information should update from POS
AND location data should remain unchanged
AND sync results should be displayed
```

**Manual Test Steps**:
1. Click "Sync with POS" button
2. Verify sync progress indicator
3. Check updated customer fields
4. Confirm location coordinates unchanged
5. Review sync summary report

## API Testing

### Customer Deletion API
```bash
# Test deletion validation
curl -X GET "http://localhost:5000/customers/{customer-id}/deletion-validation"

# Test soft deletion
curl -X DELETE "http://localhost:5000/customers/{customer-id}/delete" \
  -H "Content-Type: application/json" \
  -d '{"deletedBy": "admin", "reason": "Test deletion"}'

# Test hard deletion (force)
curl -X DELETE "http://localhost:5000/customers/{customer-id}/delete?force=true" \
  -H "Content-Type: application/json" \
  -d '{"deletedBy": "admin", "reason": "Force deletion"}'
```

### Location Management API
```bash
# Update customer location
curl -X PUT "http://localhost:5000/customers/{customer-id}/location" \
  -H "Content-Type: application/json" \
  -d '{"customerId": "{customer-id}", "latitude": 40.7128, "longitude": -74.0060}'

# Test invalid coordinates
curl -X PUT "http://localhost:5000/customers/{customer-id}/location" \
  -H "Content-Type: application/json" \
  -d '{"customerId": "{customer-id}", "latitude": 100, "longitude": 200}'
```

### POS Sync API
```bash
# Trigger manual POS sync
curl -X POST "http://localhost:5000/customers/sync-pos" \
  -H "Content-Type: application/json" \
  -d '{"preserveLocation": true}'

# Sync specific customers
curl -X POST "http://localhost:5000/customers/sync-pos" \
  -H "Content-Type: application/json" \
  -d '{"customerIds": ["{customer-id-1}", "{customer-id-2}"], "preserveLocation": true}'
```

## Database Verification

### Check New Schema Fields
```sql
-- Verify location columns added
SELECT name, type FROM pragma_table_info('Customers') 
WHERE name IN ('Latitude', 'Longitude', 'IsDeleted', 'DeletedAt', 'DeletedBy');

-- Check customer location data
SELECT Id, Code, FirstName, LastName, Latitude, Longitude, IsDeleted 
FROM Customers 
WHERE Latitude IS NOT NULL;
```

### Sample Data Setup
```sql
-- Add test customers with locations
UPDATE Customers 
SET Latitude = 40.7128, Longitude = -74.0060 
WHERE Code = 'CUST001';

UPDATE Customers 
SET Latitude = 34.0522, Longitude = -118.2437 
WHERE Code = 'CUST002';
```

## Component Testing

### Blazor Component Tests
```bash
# Run component tests
cd src/client/PigFarmManagement.Client.Tests
dotnet test --filter "Category=Components"

# Run specific customer component tests
dotnet test --filter "FullyQualifiedName~CustomerManagement"
```

### Manual UI Testing Checklist
- [ ] Customer list loads in card view by default
- [ ] View toggle switches between card and table layouts
- [ ] View preference persists after page refresh
- [ ] Delete button shows confirmation dialog
- [ ] Delete is blocked for customers with pig pens
- [ ] Location fields accept valid coordinates
- [ ] Location validation prevents invalid coordinates
- [ ] Google Maps displays customer locations correctly
- [ ] POS sync button triggers manual synchronization
- [ ] Location data preserved during POS sync

## Error Scenarios

### Google Maps API Failures
1. **Invalid API Key**: Set wrong API key → Maps should show fallback
2. **Network Failure**: Disconnect internet → Coordinates should still display
3. **Rate Limiting**: Exceed API limits → Graceful degradation expected

### POS API Failures
1. **POS Unavailable**: Stop POS service → Sync should report failure
2. **Partial Sync**: Mock partial response → Should handle mixed results
3. **Network Timeout**: Simulate timeout → Should retry with backoff

### Validation Failures
1. **Invalid Coordinates**: Test boundary values (`-91`, `181`)
2. **Missing Required Fields**: Empty customer code, first name
3. **Deletion Constraints**: Try deleting customer with active relationships

## Performance Verification

### Load Testing
```bash
# Test with large customer list (1000+ customers)
# Verify view switching performance
# Check Google Maps rendering with multiple locations
# Validate POS sync with batch operations
```

### Expected Performance
- Customer list loading: < 500ms
- View mode switching: < 100ms  
- Location map rendering: < 200ms
- POS sync (100 customers): < 30 seconds

## Troubleshooting

### Common Issues
1. **Google Maps not loading**: Check API key configuration
2. **Location validation errors**: Verify coordinate ranges
3. **Delete button disabled**: Check customer has no active pig pens
4. **POS sync fails**: Verify POS API connectivity
5. **View preference not saving**: Check browser localStorage support

### Debug Steps
1. Check browser console for JavaScript errors
2. Verify backend API responses with network tab
3. Check database constraints for deletion issues
4. Validate Google Maps API quotas and billing