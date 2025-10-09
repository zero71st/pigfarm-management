using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using PigFarmManagement.Server.Infrastructure.Data;
using PigFarmManagement.Server.Infrastructure.Data.Entities;
using PigFarmManagement.Server.Features.Authentication.Helpers;
using PigFarmManagement.Shared.Contracts.Authentication;
using System.Security.Claims;

namespace PigFarmManagement.Server.Features.Admin;

public static class AdminUserEndpoints
{
    public static WebApplication MapAdminUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin").WithTags("Admin").RequireAuthorization();

        // Create new user
        group.MapPost("/users", async (CreateUserRequest req, HttpContext http, PigFarmDbContext db) =>
        {
            // Check admin role
            if (!http.User.IsInRole("Admin"))
                return Results.Forbid();

            if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "Username and password are required" });

            // Check if username or email already exists
            var existingUser = await db.Users.FirstOrDefaultAsync(u => 
                u.Username == req.Username || u.Email == req.Email);
            if (existingUser != null)
                return Results.BadRequest(new { error = "Username or email already exists" });

            // Hash password
            var hasher = new PasswordHasher<object>();
            var hashedPassword = hasher.HashPassword(new object(), req.Password);

            var user = new UserEntity
            {
                Id = Guid.NewGuid(),
                Username = req.Username,
                Email = req.Email,
                PasswordHash = hashedPassword,
                RolesCsv = string.Join(",", req.Roles),
                IsActive = req.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = http.User.Identity?.Name ?? "system"
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var userInfo = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            };

            return Results.Created($"/api/admin/users/{user.Id}", userInfo);
        });

        // Get all users with pagination and filtering
        group.MapGet("/users", async (HttpContext http, PigFarmDbContext db,
            int page = 1, int pageSize = 20, bool? isActive = null, 
            string? role = null, string? search = null) =>
        {
            if (!http.User.IsInRole("Admin"))
                return Results.Forbid();

            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;

            var query = db.Users.Where(u => !u.IsDeleted);

            // Apply filters
            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(u => u.Roles.Contains(role));

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var users = await query
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserInfo
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Roles = u.Roles,
                    IsActive = u.IsActive,
                    LastLoginAt = u.LastLoginAt,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            var result = new
            {
                items = users,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalPages,
                    totalCount
                }
            };

            return Results.Ok(result);
        });

        // Get user by ID
        group.MapGet("/users/{userId:guid}", async (Guid userId, HttpContext http, PigFarmDbContext db) =>
        {
            if (!http.User.IsInRole("Admin"))
                return Results.Forbid();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
                return Results.NotFound(new { error = "User not found" });

            var userInfo = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            };

            return Results.Ok(userInfo);
        });

        // Update user
        group.MapPut("/users/{userId:guid}", async (Guid userId, UpdateUserRequest req, HttpContext http, PigFarmDbContext db) =>
        {
            if (!http.User.IsInRole("Admin"))
                return Results.Forbid();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
                return Results.NotFound(new { error = "User not found" });

            // Check if email already exists (excluding current user)
            if (!string.IsNullOrWhiteSpace(req.Email) && req.Email != user.Email)
            {
                var existingEmail = await db.Users.AnyAsync(u => u.Email == req.Email && u.Id != userId);
                if (existingEmail)
                    return Results.BadRequest(new { error = "Email already exists" });
            }

            // Update fields
            if (!string.IsNullOrWhiteSpace(req.Email))
                user.Email = req.Email;
            
            if (req.Roles != null)
                user.RolesCsv = string.Join(",", req.Roles);
            
            if (req.IsActive.HasValue)
                user.IsActive = req.IsActive.Value;
            
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = http.User.Identity?.Name ?? "system";

            await db.SaveChangesAsync();

            var userInfo = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            };

            return Results.Ok(userInfo);
        });

        // Soft delete user
        group.MapDelete("/users/{userId:guid}", async (Guid userId, HttpContext http, PigFarmDbContext db) =>
        {
            if (!http.User.IsInRole("Admin"))
                return Results.Forbid();

            var currentUserId = http.User.FindFirst("user_id")?.Value;
            if (currentUserId == userId.ToString())
                return Results.BadRequest(new { error = "Cannot delete own account" });

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
                return Results.NotFound(new { error = "User not found" });

            // Check for active API keys
            var hasActiveKeys = await db.ApiKeys.AnyAsync(k => k.UserId == userId && k.IsActive);
            if (hasActiveKeys)
                return Results.Conflict(new { error = "User has active API keys", detail = "Revoke all API keys before deleting user" });

            // Soft delete
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletedBy = http.User.Identity?.Name ?? "system";

            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // Generate API key for user
        group.MapPost("/users/{userId:guid}/apikeys", async (Guid userId, CreateApiKeyRequest req, HttpContext http, PigFarmDbContext db) =>
        {
            if (!http.User.IsInRole("Admin"))
                return Results.Forbid();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
                return Results.NotFound(new { error = "User not found" });

            // Generate API key and persist hashed key
            var rawKey = ApiKeyHash.GenerateApiKey();
            var hashed = ApiKeyHash.HashApiKey(rawKey);

            var apiKey = new ApiKeyEntity
            {
                UserId = userId,
                HashedKey = hashed,
                Label = req.Label ?? "admin-generated",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = req.ExpirationDays.HasValue ? DateTime.UtcNow.AddDays(req.ExpirationDays.Value) : null,
                UsageCount = 0
            };
            db.ApiKeys.Add(apiKey);
            await db.SaveChangesAsync();

            var response = new
            {
                apiKey = rawKey,
                keyId = apiKey.Id,
                expiresAt = apiKey.ExpiresAt,
                label = apiKey.Label
            };

            return Results.Created($"/api/admin/apikeys/{apiKey.Id}", response);
        });

        // Get API keys for user
        group.MapGet("/users/{userId:guid}/apikeys", async (Guid userId, HttpContext http, PigFarmDbContext db) =>
        {
            if (!http.User.IsInRole("Admin"))
                return Results.Forbid();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
                return Results.NotFound(new { error = "User not found" });

            var apiKeys = await db.ApiKeys
                .Where(k => k.UserId == userId)
                .OrderByDescending(k => k.CreatedAt)
                .Select(k => new ApiKeyInfo
                {
                    Id = k.Id,
                    Label = k.Label,
                    CreatedAt = k.CreatedAt,
                    ExpiresAt = k.ExpiresAt ?? DateTime.MaxValue,
                    LastUsedAt = k.LastUsedAt,
                    IsActive = k.IsActive,
                    UsageCount = k.UsageCount
                })
                .ToListAsync();

            return Results.Ok(apiKeys);
        });

        // Revoke specific API key
        group.MapDelete("/apikeys/{keyId:guid}", async (Guid keyId, HttpContext http, PigFarmDbContext db) =>
        {
            if (!http.User.IsInRole("Admin"))
                return Results.Forbid();

            var apiKey = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == keyId);
            if (apiKey == null)
                return Results.NotFound(new { error = "API key not found" });

            apiKey.IsActive = false;
            apiKey.RevokedAt = DateTime.UtcNow;
            apiKey.RevokedBy = http.User.Identity?.Name ?? "admin";

            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // Revoke all API keys for user
        group.MapPost("/users/{userId:guid}/revoke-all-keys", async (Guid userId, HttpContext http, PigFarmDbContext db) =>
        {
            if (!http.User.IsInRole("Admin"))
                return Results.Forbid();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
                return Results.NotFound(new { error = "User not found" });

            var activeKeys = await db.ApiKeys.Where(k => k.UserId == userId && k.IsActive).ToListAsync();
            var revokedCount = activeKeys.Count;

            foreach (var key in activeKeys)
            {
                key.IsActive = false;
                key.RevokedAt = DateTime.UtcNow;
                key.RevokedBy = http.User.Identity?.Name ?? "admin";
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "All API keys revoked successfully",
                revokedCount,
                timestamp = DateTime.UtcNow
            });
        });

        // Reset user password
        group.MapPost("/users/{userId:guid}/reset-password", async (Guid userId, ResetPasswordRequest req, HttpContext http, PigFarmDbContext db) =>
        {
            if (!http.User.IsInRole("Admin"))
                return Results.Forbid();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
                return Results.NotFound(new { error = "User not found" });

            // Hash new password
            var hasher = new PasswordHasher<object>();
            user.PasswordHash = hasher.HashPassword(new object(), req.NewPassword);
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = http.User.Identity?.Name ?? "admin";

            var revokedCount = 0;
            if (req.RevokeApiKeys)
            {
                var activeKeys = await db.ApiKeys.Where(k => k.UserId == userId && k.IsActive).ToListAsync();
                revokedCount = activeKeys.Count;

                foreach (var key in activeKeys)
                {
                    key.IsActive = false;
                    key.RevokedAt = DateTime.UtcNow;
                    key.RevokedBy = http.User.Identity?.Name ?? "admin";
                }
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Password reset successfully",
                revokedApiKeys = revokedCount,
                timestamp = DateTime.UtcNow
            });
        });

        return app;
    }
}

// Additional DTOs for admin endpoints
public record CreateApiKeyRequest
{
    public string? Label { get; init; }
    public int? ExpirationDays { get; init; }
}

public record ResetPasswordRequest
{
    public string NewPassword { get; init; } = "";
    public bool RevokeApiKeys { get; init; } = true;
}
