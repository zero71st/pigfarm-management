# PigFarmManagement

Comprehensive pig farm management system with integrated customer management, POS system synchronization, and feed tracking capabilities.

* **Server**: .NET 8 Web API with POSPOS integration for live feed imports
* **Client**: Blazor WebAssembly application with modern UI for managing pig pens, customers, and operations
* **Shared**: DTO / record models for seamless client-server communication

## Features

### Enhanced Customer Management (Feature 008)

Complete customer lifecycle management with modern UI and location tracking:

#### Core Customer Operations
- **Customer Database**: Full CRUD operations for customer records with soft deletion support
- **Dual View Modes**: Switch between card view (default) and table view for optimal workflow
- **Advanced Filtering**: Filter customers by status (Active/Inactive) with real-time search across all fields
- **Bulk Operations**: Import and sync customer data from POS systems

#### Location & Mapping Integration
- **Google Maps Integration**: Interactive maps showing customer locations
- **Coordinate Management**: Manual entry and editing of latitude/longitude coordinates
- **Location Validation**: Automatic validation of coordinate ranges and data integrity
- **Address Display**: Full address information with geographic context

#### POS System Integration
- **Manual Sync**: Admin-triggered synchronization with POSPOS for customer data updates
- **Conflict Resolution**: POS data takes precedence during sync conflicts (authoritative source)
- **Real-time Updates**: Automatic UI refresh after successful synchronization
- **Audit Trail**: Complete logging of all customer modifications and sync activities

#### Modern UI/UX Features
- **Icon-Only Buttons**: Clean, modern interface with tooltips for enhanced usability
- **Responsive Design**: Optimized for desktop and mobile devices using MudBlazor components
- **Real-time Filtering**: Instant search and filter results without page refreshes
- **Enhanced Dialogs**: Full-featured customer editing with comprehensive field support

### Database Schema Enhancements

**Customer Entity Updates**:
- Added location fields: `Latitude`, `Longitude`
- Implemented soft deletion: `IsDeleted`, `DeletedAt`, `DeletedBy`
- Enhanced audit tracking for compliance and debugging

### POSPOS Feed Import Enhancement (Feature 006)

Enhanced feed import functionality that calculates consumption and expense per pig pen using POSPOS transaction data:

- **Feed Consumption Calculation**: Rounds stock to integer bags, calculates bags per pig and coverage
- **Expense Attribution**: Uses `TotalPriceIncludeDiscount` from POSPOS as authoritative expense source
- **Formula Cost Integration**: Automatically looks up and applies feed formula costs when product codes match
- **Discount Processing**: Records POSPOS discount amounts and computes actual pricing after discounts
- **Pricing Reconciliation**: Compares system-calculated totals vs POS-provided totals for validation

### New UI Features

The Pig Pen detail page now displays enhanced feed information:
- **INV Date**: Invoice date from POSPOS transactions  
- **Cost**: Feed formula cost (when available)
- **Price+Discount**: Actual unit price after applying discounts
- **Sys Total**: System-calculated total (Price+Discount × Quantity)
- **POS Total**: Total provided by POSPOS for comparison

## Database Schema Changes

### Customer Management Enhancement

**Migrations**: `AddCustomerLocationFields`, `AddCustomerSoftDeletion`

New columns added to `Customers` table:
- `Latitude` (decimal?) - Geographic latitude coordinate (-90 to 90)
- `Longitude` (decimal?) - Geographic longitude coordinate (-180 to 180)
- `IsDeleted` (bool) - Soft deletion flag for data retention
- `DeletedAt` (DateTime?) - Timestamp of deletion for audit trail
- `DeletedBy` (string?) - User identifier who performed deletion

### Feed Import Enhancement

**Migration**: `AddPosTotalPriceIncludeDiscount`

New columns added to `Feeds` table:
- `Cost` (decimal?) - From FeedFormula.Cost lookup
- `CostDiscountPrice` (decimal?) - From POSPOS DiscountAmount  
- `PriceIncludeDiscount` (decimal?) - UnitPrice - CostDiscountPrice
- `Sys_TotalPriceIncludeDiscount` (decimal?) - System calculated total
- `TotalPriceIncludeDiscount` (decimal) - Renamed from TotalPrice for clarity
- `Pos_TotalPriceIncludeDiscount` (decimal?) - POS-provided total for comparison

### Migration Commands

Apply all migrations:
```bash
cd src/server/PigFarmManagement.Server
dotnet ef database update
```

## Configuration

### Google Maps API Setup

1. **Get API Key**: Obtain a Google Maps JavaScript API key from [Google Cloud Console](https://console.cloud.google.com/)
2. **Configure Client**: Add the API key to your client configuration:
   ```json
   {
     "GoogleMaps": {
       "ApiKey": "YOUR_GOOGLE_MAPS_API_KEY"
     }
   }
   ```
3. **Enable APIs**: Ensure the following APIs are enabled in Google Cloud Console:
   - Maps JavaScript API
   - Geocoding API (optional, for address lookup)

### Environment Variables

For development, you can also set:
```bash
GOOGLE_MAPS_API_KEY=your_api_key_here
```

### Migration Commands

Apply the migration:
```bash
cd src/server/PigFarmManagement.Server
dotnet ef database update
```

**Rollback Instructions** (if needed):
```bash
# List migrations to find the previous one
dotnet ef migrations list

# Rollback to previous migration (replace with actual previous migration name)
dotnet ef database update <PreviousMigrationName>

# Remove the migration file
dotnet ef migrations remove
```

## Testing Enhanced Feed Import

### Sample Import Data

Use this sample POSPOS JSON to test the new functionality:

```json
[
  {
    "no": "INV001",
    "date": "2024-10-04T10:00:00Z",
    "buyer_detail": {
      "code": "M000001",
      "key_card_id": "12345"
    },
    "order_list": [
      {
        "product_code": "PK64000158",
        "product_name": "เจ็ท 105 หมูเล็ก 6-15 กก.",
        "stock": "20.0",
        "price": "755.00",
        "special_price": "750.00", 
        "discount_amount": "10.00",
        "total_price_include_discount": "14900.00"
      }
    ]
  }
]
```

## Testing Enhanced Customer Management

### Feature Testing Checklist

#### Customer CRUD Operations
- ✅ **Create**: Add new customers with all required and optional fields
- ✅ **Read**: View customer details in both card and table views  
- ✅ **Update**: Edit customer information with real-time validation
- ✅ **Delete**: Soft delete customers with confirmation dialog

#### Location Management
- ✅ **Coordinate Entry**: Manual input of latitude/longitude values
- ✅ **Map Display**: Google Maps integration showing customer locations
- ✅ **Validation**: Coordinate range validation (-90 to 90 lat, -180 to 180 lng)

#### View Mode Switching
- ✅ **Card View**: Default view with customer cards showing key information
- ✅ **Table View**: Comprehensive table with sortable columns
- ✅ **Persistence**: View preference remembered across sessions

#### Filtering & Search
- ✅ **Status Filter**: Filter by Active/Inactive status with "All Statuses" option
- ✅ **Text Search**: Real-time search across name, code, phone, and email fields
- ✅ **Combined Filters**: Status and text filters work together

#### POS Integration
- ✅ **Manual Sync**: Admin-triggered synchronization with POS system
- ✅ **Conflict Resolution**: POS data takes precedence during conflicts
- ✅ **UI Updates**: Real-time refresh after successful sync

### API Testing

#### Customer Management Endpoints

**Get All Customers**:
```bash
curl -X GET "http://localhost:5000/api/customers"
```

**Create Customer**:
```bash
curl -X POST "http://localhost:5000/api/customers" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "John",
    "lastName": "Doe", 
    "code": "C001",
    "phone": "0123456789",
    "email": "john.doe@example.com",
    "address": "123 Farm Road",
    "latitude": 13.7563,
    "longitude": 100.5018
  }'
```

**Update Customer Location**:
```bash
curl -X PUT "http://localhost:5000/api/customers/{id}/location" \
  -H "Content-Type: application/json" \
  -d '{
    "latitude": 13.7563,
    "longitude": 100.5018
  }'
```

**Delete Customer** (Soft Delete):
```bash
curl -X DELETE "http://localhost:5000/api/customers/{id}"
```

**Sync from POS**:
```bash
curl -X POST "http://localhost:5000/api/customers/sync/pospos"
```

#### Feed Import Testing

Import via JSON endpoint:
```bash
curl -X POST "http://localhost:5000/api/feeds/import/pospos/json" \
  -H "Content-Type: application/json" \
  -d '{"jsonContent": "[{\"no\":\"INV001\",\"date\":\"2024-10-04T10:00:00Z\",\"buyer_detail\":{\"code\":\"M000001\"},\"order_list\":[{\"product_code\":\"PK64000158\",\"product_name\":\"เจ็ท 105 หมูเล็ก 6-15 กก.\",\"stock\":\"20.0\",\"price\":\"755.00\",\"special_price\":\"750.00\",\"discount_amount\":\"10.00\",\"total_price_include_discount\":\"14900.00\"}]}]"}'
```

### Expected Results

For the sample data:
- **Bags**: 20 (rounded from stock)
- **Unit Price**: 750.00 (special_price preferred over price)
- **Cost Discount**: 10.00 (from discount_amount)
- **Price+Discount**: 740.00 (750 - 10)
- **Sys Total**: 14800.00 (740 × 20)
- **POS Total**: 14900.00 (from POSPOS)

**Difference Analysis**: The 100.00 difference (14900 - 14800) represents POS-specific pricing logic or rounding that differs from the system calculation.

## Run (Development)

### Prerequisites
- .NET 8 SDK
- Google Maps API key (for location features)

### Setup & Configuration

1. **Configure Google Maps API** (Required for customer location features):
   ```bash
   # Set environment variable (optional)
   set GOOGLE_MAPS_API_KEY=your_api_key_here
   
   # Or add to appsettings.Development.json in client project
   ```

2. **Apply Database Migrations**:
   ```bash
   cd src/server/PigFarmManagement.Server
   dotnet ef database update
   ```

### Running the Application

1. **Open solution folder** `src` in VS Code / terminal

2. **Run server** (Terminal 1):
   ```bash
   dotnet run --project .\server\PigFarmManagement.Server\PigFarmManagement.Server.csproj --urls http://localhost:5000
   ```

3. **Run client** (Terminal 2):
   ```bash
   dotnet run --project .\client\PigFarmManagement.Client\PigFarmManagement.Client.csproj --urls http://localhost:7000
   ```

4. **Access the application**:
   - Client: http://localhost:7000
   - Server API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger

### Using the Customer Management Features

1. **Navigate to Customer Management**: Click on "Customers" in the main navigation
2. **View Modes**: Toggle between card view (default) and table view using the view toggle
3. **Add Customers**: Use the "+" button to add new customers with location data
4. **Edit Customers**: Click on any customer card or table row to edit details
5. **Filter & Search**: Use the status filter and search box to find specific customers
6. **POS Sync**: Use the sync button to import/update customer data from POS system
7. **Location Mapping**: View customer locations on the integrated Google Maps

## Next Steps

### Immediate Improvements
* Enhanced location features: address geocoding, location search
* Advanced customer analytics and reporting
* Bulk customer operations (import/export)
* Customer relationship tracking with pig pens and transactions

### System Enhancements

* Add proper solution file & projects referencing (generate via `dotnet new sln && dotnet sln add ...`)
* Implement authentication & roles
* Persist data to Supabase (PostgreSQL)
* Integrate POSPOS API sync
* Add reporting & printing endpoints
* Refactor into layered architecture (Domain, Application, Infrastructure)

> Project constitution: .specify/memory/constitution.md — contains governance, ownership, and template propagation rules.

---
Note: legacy mock data endpoints were removed. Use the POSPOS integration and the JSON import flow for testing and replay.
