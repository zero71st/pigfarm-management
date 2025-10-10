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

// One-time admin seeding: ensure at least one Admin user exists
// Behavior:
//  - If an Admin user already exists, no action is taken.
//  - Otherwise an admin user is created using environment vars when provided:
//      ADMIN_USERNAME, ADMIN_EMAIL, ADMIN_PASSWORD, ADMIN_APIKEY
//  - If secrets are not provided, a secure password and API key are generated and printed once.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PigFarmDbContext>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("AdminSeeder");

    try
    {
        var hasAdmin = context.Users.Any(u => u.RolesCsv != null && u.RolesCsv.Contains("Admin"));

        if (hasAdmin)
        {
            logger.LogInformation("Admin user already exists; skipping admin seed.");
        }
        else
        {
            // Read env vars or use defaults
            var adminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@example.com";
            var providedPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
            var providedApiKey = Environment.GetEnvironmentVariable("ADMIN_APIKEY");

            // Generate password and API key if not provided
            var password = string.IsNullOrWhiteSpace(providedPassword)
                ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(12)).Replace("=", "")
                : providedPassword;

            var rawApiKey = string.IsNullOrWhiteSpace(providedApiKey)
                ? PigFarmManagement.Server.Features.Authentication.Helpers.ApiKeyHash.GenerateApiKey()
                : providedApiKey!;

            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<object>();

            var adminUser = new PigFarmManagement.Server.Infrastructure.Data.Entities.UserEntity
            {
                Username = adminUsername,
                Email = adminEmail,
                RolesCsv = "Admin,User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system-seed"
            };

            adminUser.PasswordHash = hasher.HashPassword(new object(), password);

            context.Users.Add(adminUser);
            context.SaveChanges();

            var apiKeyEntity = new PigFarmManagement.Server.Infrastructure.Data.Entities.ApiKeyEntity
            {
                UserId = adminUser.Id,
                Label = "Initial Admin Key",
                RolesCsv = adminUser.RolesCsv,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1)
            };

            apiKeyEntity.HashedKey = PigFarmManagement.Server.Features.Authentication.Helpers.ApiKeyHash.HashApiKey(rawApiKey);
            context.ApiKeys.Add(apiKeyEntity);
            context.SaveChanges();

            // Only log secrets if they were generated here (not supplied via env vars)
            logger.LogInformation("Seeded initial admin user: {Username} ({Email})", adminUser.Username, adminUser.Email);
            if (string.IsNullOrWhiteSpace(providedPassword))
            {
                logger.LogWarning("Generated admin password (store this securely and rotate on first login): {Password}", password);
            }
            else
            {
                logger.LogInformation("Admin password was provided via ADMIN_PASSWORD environment variable.");
            }

            if (string.IsNullOrWhiteSpace(providedApiKey))
            {
                logger.LogWarning("Generated admin API key (copy now, it will not be shown again): {ApiKey}", rawApiKey);
            }
            else
            {
                logger.LogInformation("Admin API key was provided via ADMIN_APIKEY environment variable.");
            }

            logger.LogInformation("Admin seeding completed.");
        }
    }
    catch (Exception ex)
    {
        // Use the existing logger created above to avoid variable shadowing
        logger.LogError(ex, "Error while attempting to seed admin user");
    }
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
