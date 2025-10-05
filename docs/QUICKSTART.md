# Quickstart — Run PigFarmManagement locally

This guide will help you set up and run the enhanced pig farm management system with customer management, location tracking, and POS integration.

## Prerequisites

- .NET 8 SDK
- Google Maps API key (for location features)
- Git (for cloning the repository)

## Quick Setup

### 1. Clone and Setup

```powershell
# Clone the repository
git clone https://github.com/zero71st/pigfarm-management.git
cd pigfarm-management

# Apply database migrations
cd src/server/PigFarmManagement.Server
dotnet ef database update
cd ../../..
```

### 2. Configure Google Maps (Optional but Recommended)

```powershell
# Method 1: Environment variable (temporary)
$env:GOOGLE_MAPS_API_KEY="your_google_maps_api_key_here"

# Method 2: Add to appsettings.Development.json (persistent)
# Edit src/client/PigFarmManagement.Client/wwwroot/appsettings.Development.json
```

### 3. Start the Server

```powershell
cd "src/server/PigFarmManagement.Server"
dotnet run --urls http://localhost:5000
```

### 4. Start the Client (New Terminal)

```powershell
cd "src/client/PigFarmManagement.Client"  
dotnet run --urls http://localhost:7000
```

### 5. Access the Application

- **Main Application**: http://localhost:7000
- **API Documentation**: http://localhost:5000/swagger
- **Server API**: http://localhost:5000

## Using the Customer Management Features

### Basic Operations

1. **Navigate to Customers**: Click "Customers" in the main navigation
2. **Add Customer**: Click the "+" button and fill in the customer details
3. **Edit Customer**: Click on any customer card to edit their information
4. **Delete Customer**: Use the delete button with confirmation dialog

### Advanced Features

1. **View Switching**: 
   - Toggle between Card View (default) and Table View
   - Your preference is automatically saved

2. **Location Management**:
   - Add latitude/longitude coordinates to customers
   - View customer locations on Google Maps
   - Edit location data in the customer dialog

3. **Filtering & Search**:
   - Use the status filter (All/Active/Inactive)
   - Search across name, code, phone, and email
   - Filters work together for precise results

4. **POS Integration**:
   - Click the sync button to import/update from POS
   - Manual sync ensures data consistency
   - POS data takes precedence during conflicts

## Import customers from POSPOS
## Import customers from POSPOS

### Using the UI
- Navigate to Customers page
- Click the sync icon button (⟳) to trigger POS synchronization
- Customer data will be imported/updated automatically
- Mapping information is stored in `customer_id_mapping.json`

### Using the API
```bash
curl -X POST "http://localhost:5000/api/customers/sync/pospos"
```

## Debugging & Development

### VS Code Development
- Use the provided VS Code launch configurations (press F5)
- Debugging configurations for both client and server
- Chrome DevTools integration for client-side debugging

### Common Troubleshooting

1. **Port Conflicts**:
   ```powershell
   # Find processes using ports
   Get-NetTCPConnection -LocalPort 5000
   Get-NetTCPConnection -LocalPort 7000
   
   # Kill dotnet processes if needed
   taskkill /F /IM dotnet.exe
   ```

2. **Database Issues**:
   ```powershell
   # Reset database
   cd src/server/PigFarmManagement.Server
   dotnet ef database drop
   dotnet ef database update
   ```

3. **Google Maps Not Loading**:
   - Verify API key is correctly set
   - Check browser console for API errors
   - Ensure Maps JavaScript API is enabled in Google Cloud Console

## Database Configuration

- **Development**: SQLite database (portable, no setup required)
- **Production**: Supabase PostgreSQL (configured via connection strings)
- **Migrations**: Automatically applied on startup in development

## Next Steps

Once you have the system running:

1. **Explore Features**: Try all customer management capabilities
2. **Import Data**: Sync with your POS system or import sample data
3. **Customize**: Modify UI components and business logic as needed
4. **Deploy**: Follow [DEPLOYMENT.md](DEPLOYMENT.md) for production setup

## Getting Help

- **Documentation**: Check the `docs/` folder for detailed guides
- **API Reference**: Visit http://localhost:5000/swagger for API documentation
- **Logs**: Check console output for detailed error information

Enjoy developing! The system is now ready for comprehensive customer management with location tracking and POS integration.
