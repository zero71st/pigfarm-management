using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using PigFarmManagement.Server.Infrastructure.Data;
using PigFarmManagement.Server.Infrastructure.Data.Entities;
using PigFarmManagement.Server.Features.Authentication.Helpers;
using PigFarmManagement.Shared.Contracts.Authentication;

namespace PigFarmManagement.Server.Features.Authentication;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/login", async (LoginRequest req, HttpContext http, PigFarmDbContext db) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { message = "username and password required" });

            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username && u.IsActive && !u.IsDeleted);
            if (user == null)
                return Results.Unauthorized();

            var hasher = new PasswordHasher<object>();
            var verify = hasher.VerifyHashedPassword(null, user.PasswordHash, req.Password);
            if (verify == PasswordVerificationResult.Failed)
                return Results.Unauthorized();

            // Generate API key and persist hashed key
            var rawKey = ApiKeyHash.GenerateApiKey();
            var hashed = ApiKeyHash.HashApiKey(rawKey);

            var apiKey = new ApiKeyEntity
            {
                UserId = user.Id,
                HashedKey = hashed,
                Label = req.KeyLabel ?? "web-login",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(req.ExpirationDays ?? 30),
                UsageCount = 0
            };
            db.ApiKeys.Add(apiKey);
            await db.SaveChangesAsync();

            var response = new LoginResponse
            {
                ApiKey = rawKey,
                ExpiresAt = apiKey.ExpiresAt ?? DateTime.UtcNow.AddDays(30),
                User = new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Roles = user.Roles,
                    IsActive = user.IsActive,
                    LastLoginAt = user.LastLoginAt,
                    CreatedAt = user.CreatedAt
                }
            };

            // update last login
            user.LastLoginAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(response);
        }).AllowAnonymous();

        group.MapPost("/logout", async (HttpContext http, PigFarmDbContext db) =>
        {
            // Get the API key from the X-Api-Key header
            if (!http.Request.Headers.TryGetValue("X-Api-Key", out var apiKeyValues))
                return Results.BadRequest(new { message = "API key required" });

            var rawApiKey = apiKeyValues.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(rawApiKey))
                return Results.BadRequest(new { message = "API key required" });

            // Hash the raw key to find it in the database
            var hashedKey = ApiKeyHash.HashApiKey(rawApiKey);
            var apiKey = await db.ApiKeys.FirstOrDefaultAsync(k => k.HashedKey == hashedKey && k.IsActive);
            
            if (apiKey != null)
            {
                // Revoke the API key
                apiKey.IsActive = false;
                apiKey.RevokedAt = DateTime.UtcNow;
                apiKey.RevokedBy = "self-logout";
                await db.SaveChangesAsync();
            }

            return Results.Ok(new { message = "Logged out successfully" });
        }).AllowAnonymous();



        group.MapGet("/me", async (HttpContext http, PigFarmDbContext db) =>
        {
            var user = http.User;
            
            if (user?.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var name = user?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(name))
                return Results.Unauthorized();

            var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Username == name);
            if (dbUser == null) return Results.NotFound();

            var info = new UserInfo
            {
                Id = dbUser.Id,
                Username = dbUser.Username,
                Email = dbUser.Email,
                Roles = dbUser.Roles,
                IsActive = dbUser.IsActive,
                LastLoginAt = dbUser.LastLoginAt,
                CreatedAt = dbUser.CreatedAt
            };

            return Results.Ok(info);
        }).AllowAnonymous();

        return app;
    }
}
