using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using PigFarmManagement.Server.Infrastructure.Data;
using PigFarmManagement.Server.Infrastructure.Extensions;
using PigFarmManagement.Server.Services.ExternalServices;
using PigFarmManagement.Server.Features.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "PigFarmManagement API", Version = "v1" });

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Enter your API key (no prefix). Example: abcdef12345",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKey"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                Name = "X-Api-Key",
                In = ParameterLocation.Header,
            },
            new string[] { }
        }
    });
});

// Add controllers support so attribute-based API controllers are mapped
builder.Services.AddControllers();

// Add authentication services with custom API key scheme
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, PigFarmManagement.Server.Features.Authentication.ApiKeyAuthenticationHandler>("ApiKey", options => { });

// Add authorization services
builder.Services.AddAuthorization();

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

// Security headers configuration
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Pospos services
builder.Services.AddSingleton<PigFarmManagement.Server.Services.IMappingStore, PigFarmManagement.Server.Services.FileMappingStore>();
builder.Services.AddHttpClient<IPosposMemberClient, PosposMemberClient>();
builder.Services.AddHttpClient<IPosposProductClient, PosposProductClient>();
// Customer import service depends on scoped services (ICustomerRepository). Register as scoped to avoid
// injecting scoped services into a singleton which causes runtime DI errors.
builder.Services.AddScoped<PigFarmManagement.Server.Features.Customers.ICustomerImportService, PigFarmManagement.Server.Features.Customers.CustomerImportService>();
// Customer feature services (moved into Features.Customers)
builder.Services.AddScoped<PigFarmManagement.Server.Features.Customers.ICustomerDeletionService, PigFarmManagement.Server.Features.Customers.CustomerDeletionService>();
builder.Services.AddScoped<PigFarmManagement.Server.Features.Customers.ICustomerLocationService, PigFarmManagement.Server.Features.Customers.CustomerLocationService>();

// TODO: Enable security services registration after all files are created (Feature 010)
// Security Configuration and Services
// builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));
// builder.Services.AddSingleton<ISecurityConfigurationService, SecurityConfigurationService>();
// builder.Services.AddScoped<IApiKeyValidationService, ApiKeyValidationService>();
// builder.Services.AddSingleton<ISessionService, SessionService>();
// builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
// builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
// builder.Services.AddScoped<ISecurityService, SecurityService>();

// Memory cache for API key validation and rate limiting
builder.Services.AddMemoryCache();

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
            // Production: strict CORS policy with specific allowed origins
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                ?? new[] { 
                    "https://pigfarm-management-client.vercel.app",  // Update with your actual Vercel URL
                    "https://zero71st-pigfarm-management.vercel.app" // Common Vercel URL pattern
                };
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Allow credentials for API key auth
        }
    });
});

// Add HSTS and security headers for production
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    });
}

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PigFarmDbContext>();
    context.Database.Migrate();
}

// One-time admin seeding: create an admin user (and one API key) if none exists
// Security gate: only allow seeding in non-production with explicit environment variable
var allowSeeding = Environment.GetEnvironmentVariable("SEED_ADMIN") == "true" 
    && !app.Environment.IsProduction();

if (allowSeeding)
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<PigFarmDbContext>();
        // Resolve services needed for hashing and api key creation
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<object>();

        // Check if any admin user exists
        var hasAdmin = context.Users.Any(u => u.RolesCsv.Contains("Admin"));

        if (!hasAdmin)
        {
            var adminUser = new PigFarmManagement.Server.Infrastructure.Data.Entities.UserEntity
            {
                Username = "admin",
                Email = "admin@example.com",
                RolesCsv = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system-seed"
            };

            // Hash the default password
            const string defaultPassword = "Admin123!@#";
            adminUser.PasswordHash = hasher.HashPassword(new object(), defaultPassword);

            context.Users.Add(adminUser);
            context.SaveChanges();

        // Create an API key for the admin and store hashed key (use ApiKeyHash to match validation)
        var rawApiKey = PigFarmManagement.Server.Features.Authentication.Helpers.ApiKeyHash.GenerateApiKey();
            var apiKeyEntity = new PigFarmManagement.Server.Infrastructure.Data.Entities.ApiKeyEntity
            {
                UserId = adminUser.Id,
                Label = "Initial Admin Key",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = null,
                UsageCount = 0,
                ExpiresAt = DateTime.UtcNow.AddYears(1)
            };

            // Store only hash of API key for security
        apiKeyEntity.HashedKey = PigFarmManagement.Server.Features.Authentication.Helpers.ApiKeyHash.HashApiKey(rawApiKey);
            context.ApiKeys.Add(apiKeyEntity);
            context.SaveChanges();

            Console.WriteLine("Seeded admin user: 'admin' with password 'Admin123!'");
            Console.WriteLine("Initial admin API Key (store this securely):");
            Console.WriteLine(rawApiKey);
        }
    }
}
else
{
    Console.WriteLine("Admin seeding skipped. To enable: set SEED_ADMIN=true environment variable (non-production only).");
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Production security hardening
    app.UseHsts(); // HTTP Strict Transport Security
    app.UseHttpsRedirection();
    
    // Comprehensive security headers middleware
    app.Use(async (context, next) =>
    {
        var response = context.Response;
        
        // Prevent clickjacking attacks
        response.Headers["X-Frame-Options"] = "DENY";
        
        // Prevent MIME type sniffing
        response.Headers["X-Content-Type-Options"] = "nosniff";
        
        // Control referrer information
        response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        
        // Prevent cross-domain policy access
        response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
        
        // Cross-Origin Embedder Policy for isolation
        response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
        
        // Cross-Origin Opener Policy for security
        response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        
        // Cross-Origin Resource Policy
        response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
        
        // Content Security Policy - restrictive but allows API functionality
        var csp = "default-src 'self'; " +
                  "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://maps.googleapis.com; " +
                  "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                  "font-src 'self' https://fonts.gstatic.com; " +
                  "img-src 'self' data: https:; " +
                  "connect-src 'self' https://go.pospos.co https://maps.googleapis.com; " +
                  "frame-ancestors 'none'; " +
                  "base-uri 'self'; " +
                  "form-action 'self'";
        response.Headers["Content-Security-Policy"] = csp;
        
        // Remove server information disclosure
        response.Headers.Remove("Server");
        response.Headers.Remove("X-Powered-By");
        response.Headers.Remove("X-AspNet-Version");
        response.Headers.Remove("X-AspNetMvc-Version");
        
        await next.Invoke();
    });
}

// Add forwarded headers support for reverse proxies
app.UseForwardedHeaders();

// Add authentication middleware (must come before authorization)
app.UseAuthentication();

// Add authorization middleware
app.UseAuthorization();

// Add health check endpoint for Railway
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .RequireAuthorization();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseBlazorFrameworkFiles();

// Map attribute-routed controllers (if any remain)
app.MapControllers();

// Map all feature endpoints
app.MapFeatureEndpoints();

app.Run();
