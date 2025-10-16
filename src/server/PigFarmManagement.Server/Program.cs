using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using PigFarmManagement.Server.Infrastructure.Data;
using PigFarmManagement.Server.Infrastructure.Extensions;
using PigFarmManagement.Server.Services.ExternalServices;
using PigFarmManagement.Server.Features.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Npgsql;

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
builder.Services.Configure<PigFarmManagement.Server.Infrastructure.Settings.PosposOptions>(options =>
{
    // Bind from Pospos configuration section first
    builder.Configuration.GetSection("Pospos").Bind(options);
    
    // Override with environment variables if they exist (Railway deployment)
    var productApiBase = Environment.GetEnvironmentVariable("POSPOS_PRODUCT_API_BASE");
    if (!string.IsNullOrWhiteSpace(productApiBase))
        options.ProductApiBase = productApiBase;
        
    var memberApiBase = Environment.GetEnvironmentVariable("POSPOS_MEMBER_API_BASE");
    if (!string.IsNullOrWhiteSpace(memberApiBase))
        options.MemberApiBase = memberApiBase;
        
    var transactionsApiBase = Environment.GetEnvironmentVariable("POSPOS_TRANSACTIONS_API_BASE");
    if (!string.IsNullOrWhiteSpace(transactionsApiBase))
        options.TransactionsApiBase = transactionsApiBase;
        
    var apiKey = Environment.GetEnvironmentVariable("POSPOS_API_KEY");
    if (!string.IsNullOrWhiteSpace(apiKey))
        options.ApiKey = apiKey;
});

// Bind Google Maps options from configuration / environment
builder.Services.Configure<PigFarmManagement.Server.Infrastructure.Settings.GoogleMapsOptions>(builder.Configuration.GetSection("GoogleMaps"));

// Add Entity Framework
// Support both PostgreSQL (via DATABASE_URL for Railway) and SQLite (local dev)
// See specs/011-title-deploy-server/quickstart.md for Railway deployment guidance
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    // Parse Railway PostgreSQL DATABASE_URL format: postgresql://user:password@host:port/database
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    
    var npgsqlBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "",
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
        Database = uri.AbsolutePath.TrimStart('/'),
        SslMode = SslMode.Require,
        Pooling = true
    };

    builder.Services.AddDbContext<PigFarmDbContext>(options =>
        options.UseNpgsql(npgsqlBuilder.ToString(), npgOptions => 
            npgOptions.EnableRetryOnFailure()));
}
else
{
    // Fallback to SQLite for local development
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                          ?? Environment.GetEnvironmentVariable("PIGFARM_CONNECTION")
                          ?? "Data Source=pigfarm.db";
    builder.Services.AddDbContext<PigFarmDbContext>(options =>
        options.UseSqlite(connectionString));
}

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
            var allowedOriginsConfig = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
            var envAllowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            var allowedOrigins = envAllowedOrigins ?? allowedOriginsConfig ?? new[] { 
                "https://pigfarm-management.vercel.app",         // Your actual Vercel URL
            };
            
            // Log allowed origins for debugging
            Console.WriteLine($"CORS: Allowing origins: {string.Join(", ", allowedOrigins)}");
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

// Ensure database is created (migration via separate commands - see quickstart.md)
// NOTE: Database.Migrate() is NOT called here per T005 migration strategy

// One-time admin seeding: ensure at least one Admin user exists
// Production safety: requires ADMIN_PASSWORD and ADMIN_APIKEY in production, fails startup if missing
// See specs/011-title-deploy-server/spec.md for requirements
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PigFarmDbContext>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("AdminSeeder");
    var isProduction = app.Environment.IsProduction();

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

            // Production safety: require secrets in production environment
            if (isProduction)
            {
                if (string.IsNullOrWhiteSpace(providedPassword))
                {
                    logger.LogCritical("Production seeder failure: ADMIN_PASSWORD environment variable is required in production but was not provided. Application startup aborted.");
                    Environment.Exit(1);
                }

                if (string.IsNullOrWhiteSpace(providedApiKey))
                {
                    logger.LogCritical("Production seeder failure: ADMIN_APIKEY environment variable is required in production but was not provided. Application startup aborted.");
                    Environment.Exit(1);
                }
            }

            // Generate password and API key if not provided (non-production only)
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

            // Logging: safe in all environments, but secrets only printed in non-production
            logger.LogInformation("Seeded initial admin user: {Username} ({Email})", adminUser.Username, adminUser.Email);
            
            if (!isProduction)
            {
                // Only log/print secrets in non-production environments
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
            }
            else
            {
                // Production: never log raw secrets, only indicate source
                logger.LogInformation("Admin credentials were provided via environment variables (ADMIN_PASSWORD and ADMIN_APIKEY).");
            }

            logger.LogInformation("Admin seeding completed.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error while attempting to seed admin user");
        if (isProduction)
        {
            logger.LogCritical("Admin seeding failed in production environment. Application startup aborted.");
            Environment.Exit(1);
        }
        throw;
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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Admin seed endpoint - idempotent seeder trigger (admin-only)
app.MapPost("/admin/seed", async (HttpContext context, PigFarmDbContext dbContext, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("AdminSeedEndpoint");
    var isProduction = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsProduction();
    
    try
    {
        // Parse request body for force parameter
        var hasAdmin = dbContext.Users.Any(u => u.RolesCsv != null && u.RolesCsv.Contains("Admin"));
        
        if (hasAdmin)
        {
            logger.LogInformation("Admin seed endpoint called: Admin user already exists, no action taken.");
            return Results.Ok(new { message = "Admin already exists", created = false });
        }

        // Check if production secrets are available
        var providedPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        var providedApiKey = Environment.GetEnvironmentVariable("ADMIN_APIKEY");
        
        if (isProduction && (string.IsNullOrWhiteSpace(providedPassword) || string.IsNullOrWhiteSpace(providedApiKey)))
        {
            logger.LogWarning("Admin seed endpoint called in production but required environment variables (ADMIN_PASSWORD, ADMIN_APIKEY) are missing.");
            return Results.BadRequest(new { message = "Missing required environment variables for production seeding", created = false });
        }

        // Create admin user (reuse logic from startup seeder)
        var adminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@example.com";

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
            CreatedBy = "endpoint-seed"
        };

        adminUser.PasswordHash = hasher.HashPassword(new object(), password);
        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync();

        var apiKeyEntity = new PigFarmManagement.Server.Infrastructure.Data.Entities.ApiKeyEntity
        {
            UserId = adminUser.Id,
            Label = "Admin Key (Endpoint Created)",
            RolesCsv = adminUser.RolesCsv,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        apiKeyEntity.HashedKey = PigFarmManagement.Server.Features.Authentication.Helpers.ApiKeyHash.HashApiKey(rawApiKey);
        dbContext.ApiKeys.Add(apiKeyEntity);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Admin seed endpoint: Created admin user {Username} ({Email})", adminUser.Username, adminUser.Email);
        
        // Production-safe logging for secrets (same pattern as startup seeder)
        if (!isProduction)
        {
            // Only log/print secrets in non-production environments
            if (string.IsNullOrWhiteSpace(providedPassword))
            {
                logger.LogWarning("Admin seed endpoint: Generated admin password (store this securely and rotate on first login): {Password}", password);
            }
            else
            {
                logger.LogInformation("Admin seed endpoint: Admin password was provided via ADMIN_PASSWORD environment variable.");
            }

            if (string.IsNullOrWhiteSpace(providedApiKey))
            {
                logger.LogWarning("Admin seed endpoint: Generated admin API key (copy now, it will not be shown again): {ApiKey}", rawApiKey);
            }
            else
            {
                logger.LogInformation("Admin seed endpoint: Admin API key was provided via ADMIN_APIKEY environment variable.");
            }
        }
        else
        {
            // Production: never log raw secrets, only indicate source
            logger.LogInformation("Admin seed endpoint: Admin credentials were provided via environment variables (ADMIN_PASSWORD and ADMIN_APIKEY).");
        }
        
        return Results.Created("/admin/seed", new { message = "Admin user created", created = true, username = adminUser.Username });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Admin seed endpoint failed");
        return Results.Problem("Internal server error during admin seeding");
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"));

// Migrations endpoint - trigger migrations manually (admin-only)
app.MapPost("/migrations/run", async (PigFarmDbContext dbContext, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("MigrationsEndpoint");
    
    // Create migration job record for audit purposes
    var migrationJob = new PigFarmManagement.Server.Infrastructure.Data.Entities.MigrationJobEntity
    {
        StartedAt = DateTime.UtcNow,
        Status = PigFarmManagement.Server.Infrastructure.Data.Entities.MigrationJobStatus.Running,
        Source = "endpoint"
    };

    try
    {
        logger.LogInformation("Migration endpoint called: Starting database migration...");
        
        // Save initial migration job record
        dbContext.MigrationJobs.Add(migrationJob);
        await dbContext.SaveChangesAsync();
        
        // Apply pending migrations
        await dbContext.Database.MigrateAsync();
        
        // Update migration job as successful
        migrationJob.FinishedAt = DateTime.UtcNow;
        migrationJob.Status = PigFarmManagement.Server.Infrastructure.Data.Entities.MigrationJobStatus.Success;
        migrationJob.MigrationsApplied = 1; // Note: EF doesn't provide count of applied migrations
        await dbContext.SaveChangesAsync();
        
        logger.LogInformation("Migration endpoint: Database migration completed successfully. Job ID: {JobId}", migrationJob.Id);
        return Results.Ok(new { 
            message = "Migrations applied successfully", 
            timestamp = DateTime.UtcNow,
            jobId = migrationJob.Id
        });
    }
    catch (Exception ex)
    {
        // Update migration job as failed
        migrationJob.FinishedAt = DateTime.UtcNow;
        migrationJob.Status = PigFarmManagement.Server.Infrastructure.Data.Entities.MigrationJobStatus.Failed;
        migrationJob.ErrorMessage = ex.Message;
        
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception saveEx)
        {
            logger.LogError(saveEx, "Failed to save migration job failure record");
        }
        
        logger.LogError(ex, "Migration endpoint failed. Job ID: {JobId}", migrationJob.Id);
        return Results.Problem("Migration failed: " + ex.Message);
    }
})
.RequireAuthorization(policy => policy.RequireRole("Admin"));

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseBlazorFrameworkFiles();

// Map attribute-routed controllers (if any remain)
app.MapControllers();

// Map all feature endpoints
app.MapFeatureEndpoints();

app.Run();
