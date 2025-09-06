using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PigFarmManagement.Client;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient with environment-specific base URL
var baseAddress = builder.Configuration["ApiBaseUrl"] ?? 
    (builder.HostEnvironment.IsProduction() ? 
        "https://your-railway-app.railway.app" :  // Replace with your Railway URL
        "http://localhost:5000");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });
builder.Services.AddMudServices();

await builder.Build().RunAsync();
