using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PigFarmManagement.Server.Infrastructure.Data;
using PigFarmManagement.Server.Features.Authentication.Helpers;

namespace PigFarmManagement.Server.Features.Authentication;

/// <summary>
/// Middleware to validate API keys and set HttpContext.User for authenticated requests
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, PigFarmDbContext dbContext)
    {
    // If an X-Api-Key header is present, attempt to validate it. Do this even for endpoints
    // marked [AllowAnonymous] so handlers like /api/auth/me can still receive an authenticated
    // HttpContext.User when a valid key is supplied.
    var endpoint = context.GetEndpoint();
    context.Request.Headers.TryGetValue("X-Api-Key", out var apiKeyValues);
    if (apiKeyValues.Count > 0)
        {
            var rawApiKey = apiKeyValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(rawApiKey))
            {
                try
                {
                    // Hash the incoming API key to find it in the database
                    var hashedKey = ApiKeyHash.HashApiKey(rawApiKey);

                    // Query database with explicit conditions (no computed properties)
                    var apiKey = await dbContext.ApiKeys
                        .Include(k => k.User)
                        .Where(k => k.HashedKey == hashedKey)
                        .Where(k => k.IsActive)
                        .Where(k => k.RevokedAt == null)
                        .Where(k => k.ExpiresAt == null || k.ExpiresAt > DateTime.UtcNow)
                        .FirstOrDefaultAsync();

                    if (apiKey != null)
                    {
                        // Update usage statistics
                        apiKey.MarkAsUsed();
                        await dbContext.SaveChangesAsync();

                        // Create claims for the authenticated user
                        var claims = new List<Claim>
                        {
                            new(ClaimTypes.Name, apiKey.User.Username),
                            new(ClaimTypes.NameIdentifier, apiKey.User.Id.ToString()),
                            new("user_id", apiKey.User.Id.ToString()),
                            new("api_key_id", apiKey.Id.ToString())
                        };

                        // Add role claims
                        foreach (var role in apiKey.User.Roles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role));
                        }

                        var identity = new ClaimsIdentity(claims, "ApiKey");
                        context.User = new ClaimsPrincipal(identity);

                        // Info-level log so validation shows up in default logs
                        _logger.LogInformation("API key validated for user {Username} (ApiKeyId={ApiKeyId}) path={Path} claims={ClaimsCount}",
                            apiKey.User.Username, apiKey.Id, context.Request?.Path, claims.Count);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid or expired API key provided");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating API key");
                }
            }
        }

        await _next(context);
    }
}