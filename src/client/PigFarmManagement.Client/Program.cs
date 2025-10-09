using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using PigFarmManagement.Client;
using PigFarmManagement.Client.Features.Customers.Services;
using PigFarmManagement.Client.Features.PigPens.Services;
using PigFarmManagement.Client.Features.Dashboard.Services;
using PigFarmManagement.Client.Features.Feeds.Services;
using PigFarmManagement.Client.Features.FeedFormulas.Services;
using PigFarmManagement.Client.Features.Admin.Services;
using PigFarmManagement.Client.Services;
using PigFarmManagement.Client.Features.Authentication.Services;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient with environment-specific base URL
var baseAddress = builder.Configuration["ApiBaseUrl"];

// If not configured via environment variable, use defaults
if (string.IsNullOrEmpty(baseAddress))
{
    baseAddress = builder.HostEnvironment.IsProduction() ? 
        "https://pigfarm-management-production.up.railway.app" :  // Your actual Railway URL
        "http://localhost:5000";
}

Console.WriteLine($"API Base Address: {baseAddress}"); // For debugging

// Configure HttpClient with API key handler
builder.Services.AddScoped<PigFarmManagement.Client.Features.Authentication.Services.ApiKeyHandler>();
builder.Services.AddScoped<IApiKeyStorage, BrowserApiKeyStorage>();
builder.Services.AddScoped<PigFarmManagement.Client.Features.Authentication.Services.AuthApiService>();

// Configure HTTP client with handler
builder.Services.AddHttpClient("AuthenticatedClient", client =>
{
    client.BaseAddress = new Uri(baseAddress);
}).AddHttpMessageHandler<PigFarmManagement.Client.Features.Authentication.Services.ApiKeyHandler>();

// Register primary HttpClient
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("AuthenticatedClient"));

// Configure Authentication State Service
builder.Services.AddScoped<PigFarmManagement.Client.Features.Authentication.Services.AuthenticationStateService>();

// Configure Authentication
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, ApiKeyAuthenticationStateProvider>();

builder.Services.AddMudServices();

// Register feature services
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICustomerLocationService, CustomerLocationService>();
builder.Services.AddScoped<IGoogleMapsService, GoogleMapsService>();
builder.Services.AddScoped<IPigPenService, PigPenService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFeedService, FeedService>();
builder.Services.AddScoped<IFeedFormulaService, FeedFormulaService>();
builder.Services.AddScoped<IFeedFormulaCalculationService, FeedFormulaCalculationService>();
builder.Services.AddScoped<IFeedImportService, FeedImportService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();

await builder.Build().RunAsync();
