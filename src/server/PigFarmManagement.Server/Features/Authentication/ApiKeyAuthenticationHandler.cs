using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using PigFarmManagement.Server.Infrastructure.Data;
using PigFarmManagement.Server.Features.Authentication.Helpers;

namespace PigFarmManagement.Server.Features.Authentication;

/// <summary>
/// Authentication handler for API key authentication scheme
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly PigFarmDbContext _dbContext;
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        PigFarmDbContext dbContext)
        : base(options, logger, encoder)
    {
        _dbContext = dbContext;
        _logger = logger.CreateLogger<ApiKeyAuthenticationHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if X-Api-Key header is present
        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKeyValues))
        {
            return AuthenticateResult.NoResult(); // No API key provided, allow anonymous access
        }

        var rawApiKey = apiKeyValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            return AuthenticateResult.NoResult(); // Empty API key, allow anonymous access
        }

        try
        {
            // Hash the incoming API key to find it in the database
            var hashedKey = ApiKeyHash.HashApiKey(rawApiKey);

            // Query database with explicit conditions
            var apiKey = await _dbContext.ApiKeys
                .Include(k => k.User)
                .Where(k => k.HashedKey == hashedKey)
                .Where(k => k.IsActive)
                .Where(k => k.RevokedAt == null)
                .Where(k => k.ExpiresAt == null || k.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (apiKey == null)
            {
                _logger.LogWarning("Invalid or expired API key provided");
                return AuthenticateResult.Fail("Invalid or expired API key");
            }

            // Update usage statistics
            apiKey.MarkAsUsed();
            await _dbContext.SaveChangesAsync();

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

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);

            _logger.LogInformation("API key authenticated for user {Username} (ApiKeyId={ApiKeyId})",
                apiKey.User.Username, apiKey.Id);

            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            return AuthenticateResult.Fail("Error validating API key");
        }
    }
}