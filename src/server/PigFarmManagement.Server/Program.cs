using Microsoft.EntityFrameworkCore;
using PigFarmManagement.Server.Infrastructure.Data;
using PigFarmManagement.Server.Infrastructure.Extensions;
using PigFarmManagement.Server.Services.ExternalServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add controllers support so attribute-based API controllers are mapped
builder.Services.AddControllers();

// Bind POSPOS options from configuration / environment
builder.Services.Configure<PigFarmManagement.Server.Infrastructure.Settings.PosposOptions>(builder.Configuration.GetSection("Pospos"));

// Bind Google Maps options from configuration / environment
builder.Services.Configure<PigFarmManagement.Server.Infrastructure.Settings.GoogleMapsOptions>(builder.Configuration.GetSection("GoogleMaps"));

// Add Entity Framework
builder.Services.AddDbContext<PigFarmDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                          ?? Environment.GetEnvironmentVariable("PIGFARM_CONNECTION")
                          ?? "Data Source=pigfarm.db";
    options.UseSqlite(connectionString);
});

// Add application services
builder.Services.AddApplicationServices();

// Pospos services
builder.Services.AddSingleton<PigFarmManagement.Server.Services.IMappingStore, PigFarmManagement.Server.Services.FileMappingStore>();
builder.Services.AddHttpClient<IPosposMemberClient, PosposMemberClient>();
builder.Services.AddHttpClient<IPosposProductClient, PosposProductClient>();
// PosposImporter depends on scoped services (ICustomerRepository). Register as scoped to avoid
// injecting scoped services into a singleton which causes runtime DI errors.
builder.Services.AddScoped<PigFarmManagement.Server.Services.IPosposCustomerImportService, PigFarmManagement.Server.Services.PosposCustomerImportService>();
// Customer feature services (moved into Features.Customers)
builder.Services.AddScoped<PigFarmManagement.Server.Features.Customers.ICustomerDeletionService, PigFarmManagement.Server.Features.Customers.CustomerDeletionService>();
builder.Services.AddScoped<PigFarmManagement.Server.Features.Customers.ICustomerLocationService, PigFarmManagement.Server.Features.Customers.CustomerLocationService>();

// CORS configuration for production
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
            // In production, allow Vercel domains AND localhost for development
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                ?? new[] { 
                    "https://*.vercel.app", 
                    "https://pigfarm-management-client.vercel.app",  // Update with your actual Vercel URL
                    "https://zero71st-pigfarm-management.vercel.app", // Common Vercel URL pattern
                    "http://localhost:7000",     // Local development HTTP
                    "https://localhost:7100"     // Local development HTTPS
                };
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .SetIsOriginAllowedToAllowWildcardSubdomains(); // Allow wildcard subdomains
        }
    });
});

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PigFarmDbContext>();
    context.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Production settings
    app.UseHttpsRedirection();
}

// Add health check endpoint for Railway
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseBlazorFrameworkFiles();

// Map attribute-routed controllers (ImportPosMemberController, ImportPosFeedsController etc.)
app.MapControllers();

// Map all feature endpoints
app.MapFeatureEndpoints();

app.Run();
