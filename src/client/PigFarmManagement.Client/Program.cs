using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PigFarmManagement.Client;
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

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });
builder.Services.AddMudServices();

await builder.Build().RunAsync();
