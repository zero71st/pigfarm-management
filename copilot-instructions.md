# 🐷 Copilot Instructions - Pig Farm Management System

## 📋 Project Overview

This is a **feature-based full-stack pig farm management system** built with modern .NET technologies and enterprise-ready architecture patterns. The system manages customers, pig pens, feed formulas, and provides comprehensive analytics for pig farm operations.

## 🏗️ Architecture & Technology Stack

### **Frontend (Blazor WebAssembly)**
- **Framework**: Blazor WebAssembly (.NET 8)
- **UI Library**: MudBlazor 6.19.1 (Material Design)
- **Hosting**: Vercel (Static Site Generation)
- **State Management**: Component-based with dependency injection
- **Routing**: Blazor Router with feature-based navigation

### **Backend (ASP.NET Core)**
- **Framework**: ASP.NET Core 8 Minimal API
- **Architecture**: Clean Architecture with feature-based organization
- **Data**: Entity Framework Core with In-Memory Database (prototype)
- **Hosting**: Railway (Docker containers)
- **API Documentation**: Swagger/OpenAPI

### **Shared Layer**
- **Domain Models**: C# Records with immutable patterns
- **DTOs**: Request/Response objects for API communication
- **Contracts**: Service interfaces and shared abstractions

## 📁 Project Structure (Feature-Based Architecture)

```
PigFarmManagement/
├── src/
│   ├── client/PigFarmManagement.Client/           # Blazor WebAssembly
│   │   ├── Features/                              # 🏗️ FEATURE-BASED ORGANIZATION
│   │   │   ├── Customers/                         # Customer Management
│   │   │   │   ├── Pages/CustomersPage.razor     # Main listing page
│   │   │   │   ├── Components/                    # Feature-specific components
│   │   │   │   │   ├── AddCustomerDialog.razor
│   │   │   │   │   ├── EditCustomerDialog.razor
│   │   │   │   │   ├── CustomerDetailsDialog.razor
│   │   │   │   │   └── CustomerCard.razor
│   │   │   │   └── Services/CustomerService.cs   # HTTP client abstraction
│   │   │   ├── PigPens/                          # Pig Pen Management
│   │   │   │   ├── Components/
│   │   │   │   │   ├── AddPigPenDialog.razor
│   │   │   │   │   └── EditPigPenDialog.razor
│   │   │   │   └── Services/PigPenService.cs
│   │   │   ├── FeedFormulas/                     # Feed Formula Management
│   │   │   │   └── Services/FeedFormulaCalculationService.cs
│   │   │   └── Dashboard/                        # Analytics Dashboard
│   │   │       ├── Pages/Dashboard.razor
│   │   │       └── Services/DashboardService.cs
│   │   ├── Core/                                 # Shared Components
│   │   │   └── Components/DeleteConfirmationDialog.razor
│   │   ├── Pages/                                # Route Redirects Only
│   │   └── Shared/                               # Layout Components
│   ├── server/PigFarmManagement.Server/          # ASP.NET Core API
│   │   ├── Features/                             # 🏗️ FEATURE-BASED ORGANIZATION
│   │   │   ├── Customers/
│   │   │   │   ├── CustomerEndpoints.cs          # API endpoints
│   │   │   │   ├── CustomerService.cs            # Business logic
│   │   │   │   └── CustomerRepository.cs         # Data access
│   │   │   ├── PigPens/
│   │   │   ├── Feeds/
│   │   │   └── Dashboard/
│   │   ├── Infrastructure/                       # Cross-cutting concerns
│   │   │   ├── Data/PigFarmDbContext.cs
│   │   │   └── Extensions/ServiceCollectionExtensions.cs
│   │   └── Program.cs                            # Minimal startup (40 lines)
│   └── shared/PigFarmManagement.Shared/          # Shared Contracts
│       ├── Domain/                               # Business entities
│       │   ├── Entities.cs                       # Customer, PigPen, etc.
│       │   ├── DTOs.cs                           # Data transfer objects
│       │   ├── Enums.cs                          # Business enumerations
│       │   └── ValueObjects.cs                   # Domain value objects
│       └── Contracts/                            # Service interfaces
```

## 🎯 Key Design Patterns & Principles

### **1. Feature-Based Architecture**
- **Organization**: Code organized by business feature, not technical layer
- **Isolation**: Each feature contains its own pages, components, and services
- **Benefits**: Better maintainability, team collaboration, and testing

### **2. Clean Architecture Layers**
- **Presentation Layer**: Blazor components and pages
- **Service Layer**: HTTP client abstractions and business logic
- **Domain Layer**: Entities, value objects, and business rules
- **Infrastructure Layer**: Data access and external service integrations

### **3. Dependency Injection Pattern**
```csharp
// Service registration
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IPigPenService, PigPenService>();

// Component usage
@inject ICustomerService CustomerService
@inject IPigPenService PigPenService
```

### **4. Repository Pattern (Server-Side)**
```csharp
// Interface definition
public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer?> UpdateAsync(Customer customer);
    Task<bool> DeleteAsync(Guid id);
}
```

### **5. Record Types for Immutable Data**
```csharp
public record Customer(Guid Id, string Code, CustomerStatus Status)
{
    // Customer now uses FirstName and LastName (POSPOS alignment).
    // DisplayName is computed from FirstName/LastName with fallback to Code.
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string DisplayName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName) ? Code : $"{FirstName} {LastName} ({Code})";
    public bool IsActive => Status == CustomerStatus.Active;
}
```

## 🎨 UI/UX Patterns

### **Material Design with MudBlazor**
- **Components**: MudButton, MudCard, MudDialog, MudDataGrid, MudSelect
- **Layout**: Responsive grid system with MudGrid/MudItem
- **Theming**: Consistent color palette and typography
- **Icons**: Material Design icons for actions and status

### **Component Structure Pattern**
```razor
@using Microsoft.AspNetCore.Components
@using MudBlazor
@using PigFarmManagement.Shared.Models
@inject ICustomerService CustomerService

<MudDialog>
    <DialogContent>
        <MudContainer>
            <MudGrid>
                <!-- Form fields -->
            </MudGrid>
        </MudContainer>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary" OnClick="Submit">Save</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] MudDialogInstance MudDialog { get; set; } = null!;
    
    // Component logic
}
```

### **Loading States & Error Handling**
```razor
@if (isLoading)
{
    <MudProgressCircular Color="Color.Primary" Indeterminate="true" />
}
else if (!string.IsNullOrEmpty(errorMessage))
{
    <MudAlert Severity="Severity.Error">@errorMessage</MudAlert>
}
else
{
    <!-- Content -->
}
```

## 🛠️ Development Guidelines

### **When Adding New Features:**

1. **Create Feature Folder Structure**:
   ```
   Features/NewFeature/
   ├── Pages/NewFeaturePage.razor
   ├── Components/
   │   ├── AddNewFeatureDialog.razor
   │   └── EditNewFeatureDialog.razor
   └── Services/NewFeatureService.cs
   ```

2. **Implement Service Pattern**:
   ```csharp
   public interface INewFeatureService
   {
       Task<IEnumerable<NewFeature>> GetAllAsync();
       Task<NewFeature> CreateAsync(CreateNewFeatureRequest request);
   }
   ```

3. **Register Services in DI Container**:
   ```csharp
   builder.Services.AddScoped<INewFeatureService, NewFeatureService>();
   ```

4. **Create Server-Side Feature**:
   ```
   server/Features/NewFeature/
   ├── NewFeatureEndpoints.cs
   ├── NewFeatureService.cs
   └── NewFeatureRepository.cs
   ```

### **API Endpoint Pattern**
```csharp
public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers")
            .WithOpenApi();

        group.MapGet("", async (ICustomerService service) => 
            await service.GetAllAsync());
            
        group.MapPost("", async (CreateCustomerRequest request, ICustomerService service) => 
            await service.CreateAsync(request));
    }
}
```

### **Component Naming Conventions**
- **Pages**: `{Feature}Page.razor` (e.g., `CustomersPage.razor`)
- **Dialogs**: `{Action}{Feature}Dialog.razor` (e.g., `AddCustomerDialog.razor`)
- **Cards**: `{Feature}Card.razor` (e.g., `CustomerCard.razor`)
- **Services**: `{Feature}Service.cs` (e.g., `CustomerService.cs`)

### **State Management Pattern**
```csharp
// Component state
private List<Customer> customers = new();
private bool isLoading = false;
private string? errorMessage;

// Load data with error handling
protected override async Task OnInitializedAsync()
{
    try
    {
        isLoading = true;
        customers = await CustomerService.GetCustomersAsync();
    }
    catch (Exception ex)
    {
        errorMessage = ex.Message;
    }
    finally
    {
        isLoading = false;
    }
}
```

## 🚀 Build & Deployment

### **Development Environment**
```bash
# Start server (Terminal 1)
cd src
dotnet run --project server/PigFarmManagement.Server/PigFarmManagement.Server.csproj --urls http://localhost:5000

# Start client (Terminal 2)
dotnet run --project client/PigFarmManagement.Client/PigFarmManagement.Client.csproj
```

### **Production Deployment**
- **Client**: Deployed to Vercel as static Blazor WebAssembly app
- **Server**: Deployed to Railway as containerized .NET 8 API
- **Database**: In-memory for prototype (PostgreSQL for production)

### **Environment Configuration**
```json
// appsettings.Development.json
{
  "ApiBaseUrl": "http://localhost:5000",
  "AllowedOrigins": ["https://localhost:7000"]
}

// appsettings.Production.json
{
  "ApiBaseUrl": "https://your-api.railway.app",
  "AllowedOrigins": ["https://your-app.vercel.app"]
}
```

## 🧪 Testing Strategy

### **Unit Testing Pattern**
```csharp
[Test]
public async Task CustomerService_GetAllAsync_ReturnsCustomers()
{
    // Arrange
    var mockHttpClient = new Mock<HttpClient>();
    var service = new CustomerService(mockHttpClient.Object);
    
    // Act
    var result = await service.GetAllAsync();
    
    // Assert
    Assert.IsNotNull(result);
    Assert.IsInstanceOf<IEnumerable<Customer>>(result);
}
```

### **Component Testing with bUnit**
```csharp
[Test]
public void CustomerCard_RendersCorrectly()
{
    // Arrange
    using var ctx = new TestContext();
    var customer = new Customer(Guid.NewGuid(), "C001", "Test Farm", CustomerStatus.Active);
    
    // Act
    var component = ctx.RenderComponent<CustomerCard>(parameters => 
        parameters.Add(p => p.Customer, customer));
    
    // Assert
    Assert.That(component.Find("h6").TextContent, Is.EqualTo("Test Farm"));
}
```

## 📊 Business Domain Models

### **Core Entities**
- **Customer**: Farm owners with Cash/Project types
- **PigPen**: Individual pig enclosures with tracking
- **Feed**: Feed products and formulas
- **FeedFormula**: Calculated feed requirements per pig
- **Deposit**: Financial deposits and investments
- **Harvest**: Pig harvest records and calculations

### **Key Business Rules**
- Estimated harvest date = Register date + 120 days
- Feed calculations based on pig quantity and formula
- Customer status affects pen creation permissions
- Profit/loss calculated from investment vs harvest value

## 🔐 Security Considerations

### **CORS Configuration**
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins("https://your-app.vercel.app")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});
```

### **Input Validation**
```csharp
public record CreateCustomerRequest(
    [Required] string Code,
    [Required] string Name,
    CustomerStatus Status
);
```

## 🎯 Performance Optimization

### **Blazor WebAssembly Optimizations**
- Use `@key` directives for list rendering
- Implement `ShouldRender()` for expensive components
- Use `InvokeAsync(StateHasChanged)` for async UI updates
- Lazy load feature modules when possible

### **API Optimizations**
- Use async/await patterns consistently
- Implement proper cancellation tokens
- Consider pagination for large datasets
- Use appropriate HTTP status codes

## 🔍 Debugging & Logging

### **Client-Side Debugging**
```csharp
// Console logging in components
Console.WriteLine($"[{nameof(CustomersPage)}] Loading customers...");

// Browser developer tools
// Use F12 -> Console to see Blazor WebAssembly logs
```

### **Server-Side Logging**
```csharp
// Built-in logging
app.Logger.LogInformation("Customer created: {CustomerId}", customer.Id);

// Structured logging with Serilog (future enhancement)
Log.Information("Customer {CustomerId} created by {UserId}", customer.Id, userId);
```

## 🚀 Future Enhancements

### **Planned Features**
- [ ] Authentication & authorization (ASP.NET Core Identity)
- [ ] Real database integration (PostgreSQL/SQL Server)
- [ ] POSPOS API integration for external data sync
- [ ] Reporting & PDF generation
- [ ] Mobile-responsive design improvements
- [ ] Real-time updates with SignalR
- [ ] Advanced analytics and dashboards
- [ ] Multi-tenant support for multiple farms

### **Technical Improvements**
- [ ] Unit test coverage > 80%
- [ ] Integration tests for API endpoints
- [ ] End-to-end testing with Playwright
- [ ] Performance monitoring with Application Insights
- [ ] Automated deployment pipelines
- [ ] Database migrations and seeding
- [ ] Caching layer (Redis)
- [ ] Message queuing for background tasks

---

## 📝 Quick Reference Commands

```bash
# Build entire solution
dotnet build

# Run tests
dotnet test

# Create new feature service
dotnet new interface -n INewFeatureService

# Add new NuGet package
dotnet add package PackageName

# Update all packages
dotnet list package --outdated
dotnet add package PackageName
```

This template provides a solid foundation for building enterprise-ready farm management systems with modern .NET technologies and clean architecture principles! 🚀
