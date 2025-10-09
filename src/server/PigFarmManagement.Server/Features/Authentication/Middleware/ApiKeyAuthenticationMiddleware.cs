using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using PigFarmManagement.Server.Features.Authentication.Configuration;
using PigFarmManagement.Shared.Contracts.Security;
using System.Text.Json;

namespace PigFarmManagement.Server.Features.Authentication.Middleware;

/// <summary>
/// Middleware for API key authentication and validation
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
    private readonly SecurityOptions _options;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<ApiKeyAuthenticationMiddleware> logger,
        IOptions<SecurityOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, ISecurityService securityService)
    {
        try
        {
            // Skip if security middleware is disabled
            if (!_options.Middleware.Enabled)
            {
                await _next(context);
                return;
            }

            // Skip authentication for excluded paths
            var path = context.Request.Path.Value ?? string.Empty;
            if (IsPathExcluded(path))
            {
                _logger.LogDebug("Skipping authentication for excluded path: {Path}", path);
                await _next(context);
                return;
            }

            // Skip in development if configured
            if (_options.Middleware.SkipInDevelopment && 
                context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
            {
                _logger.LogDebug("Skipping authentication in development mode");
                await _next(context);
                return;
            }

            // Extract API key from header
            var apiKey = ExtractApiKey(context);
            if (string.IsNullOrEmpty(apiKey))
            {
                await HandleUnauthorizedAsync(context, "API key is required", "MISSING_API_KEY");
                return;
            }

            // Extract session ID (optional)
            var sessionId = ExtractSessionId(context);

            // Get endpoint and determine required permission
            var endpoint = GetEndpointPath(context);
            var requiredPermission = DetermineRequiredPermission(context.Request.Method, endpoint);

            // Perform comprehensive security validation
            var clientIpAddress = GetClientIpAddress(context);
            var validationResult = await securityService.ValidateRequestAsync(
                apiKey, sessionId, endpoint, requiredPermission, clientIpAddress);

            if (!validationResult.IsAuthorized)
            {
                var errorMessage = string.Join("; ", validationResult.ValidationErrors);
                var errorCode = DetermineErrorCode(validationResult);
                
                _logger.LogWarning("Request authorization failed for {Endpoint}: {Errors}", endpoint, errorMessage);
                await HandleUnauthorizedAsync(context, errorMessage, errorCode);
                return;
            }

            // Add security context to request for downstream use
            if (validationResult.SecurityContext != null)
            {
                context.Items["SecurityContext"] = validationResult.SecurityContext;
                context.Items["UserId"] = validationResult.SecurityContext.UserId;
                context.Items["UserRole"] = validationResult.SecurityContext.Role;
                context.Items["SessionId"] = validationResult.SecurityContext.SessionId;
            }

            // Add rate limit headers to response
            AddRateLimitHeaders(context, validationResult.RateLimitStatus);

            _logger.LogDebug("Request authorized for user {UserId} with role {Role} on endpoint {Endpoint}",
                validationResult.SecurityContext?.UserId, 
                validationResult.SecurityContext?.Role, 
                endpoint);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in API key authentication middleware");
            await HandleInternalErrorAsync(context, "Internal authentication error");
        }
    }

    /// <summary>
    /// Extracts API key from X-Api-Key header
    /// </summary>
    private string? ExtractApiKey(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_options.ApiKey.HeaderName, out var headerValues))
        {
            return headerValues.FirstOrDefault();
        }
        
        return null;
    }

    /// <summary>
    /// Extracts session ID from X-Session-Id header
    /// </summary>
    private string? ExtractSessionId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_options.Session.HeaderName, out var headerValues))
        {
            return headerValues.FirstOrDefault();
        }
        
        return null;
    }

    /// <summary>
    /// Gets the endpoint path for permission checking
    /// </summary>
    private string GetEndpointPath(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;
        return $"{method}:{path}";
    }

    /// <summary>
    /// Determines required permission based on HTTP method and path
    /// </summary>
    private string DetermineRequiredPermission(string method, string path)
    {
        // Extract resource from path (simplified logic)
        var resource = "general";
        if (path.Contains("/customers", StringComparison.OrdinalIgnoreCase))
            resource = "customers";
        else if (path.Contains("/pigpens", StringComparison.OrdinalIgnoreCase))
            resource = "pigpens";
        else if (path.Contains("/feeds", StringComparison.OrdinalIgnoreCase))
            resource = "feeds";
        else if (path.Contains("/admin", StringComparison.OrdinalIgnoreCase))
            resource = "users";

        // Determine action based on HTTP method
        var action = method.ToLowerInvariant() switch
        {
            "get" => "read",
            "post" => "write",
            "put" => "write",
            "patch" => "write",
            "delete" => "delete",
            _ => "read"
        };

        return $"{action}:{resource}";
    }

    /// <summary>
    /// Checks if the path should be excluded from authentication
    /// </summary>
    private bool IsPathExcluded(string path)
    {
        return _options.Middleware.ExcludePaths.Any(excluded =>
            path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets client IP address for logging and rate limiting
    /// </summary>
    private string? GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded header first (for load balancers/proxies)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        // Check for real IP header
        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            var ip = realIp.FirstOrDefault();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Adds rate limit information to response headers
    /// </summary>
    private void AddRateLimitHeaders(HttpContext context, Shared.DTOs.Security.RateLimitResponseDto rateLimitStatus)
    {
        try
        {
            if (rateLimitStatus.Policies.Any())
            {
                var policy = rateLimitStatus.Policies.First();
                context.Response.Headers.Add("X-RateLimit-Limit", policy.RequestsLimit.ToString());
                context.Response.Headers.Add("X-RateLimit-Remaining", policy.RequestsRemaining.ToString());
                context.Response.Headers.Add("X-RateLimit-Reset", policy.WindowReset.ToUnixTimeSeconds().ToString());
                
                if (policy.IsBlocked && policy.BlockedUntil.HasValue)
                {
                    context.Response.Headers.Add("X-RateLimit-RetryAfter", 
                        policy.BlockedUntil.Value.ToUnixTimeSeconds().ToString());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add rate limit headers");
        }
    }

    /// <summary>
    /// Determines error code based on validation result
    /// </summary>
    private string DetermineErrorCode(Shared.DTOs.Security.SecurityValidationDto validationResult)
    {
        if (!validationResult.ApiKeyValidation.IsValid)
            return validationResult.ApiKeyValidation.ErrorCode ?? "INVALID_API_KEY";
        
        if (!validationResult.SessionValidation.IsValid)
            return "INVALID_SESSION";
        
        if (!validationResult.AuthorizationValidation.IsValid)
            return validationResult.AuthorizationValidation.ErrorCode ?? "INSUFFICIENT_PERMISSIONS";
        
        if (validationResult.RateLimitStatus.GlobalStatus == "blocked")
            return "RATE_LIMIT_EXCEEDED";
        
        return "UNAUTHORIZED";
    }

    /// <summary>
    /// Handles unauthorized requests
    /// </summary>
    private async Task HandleUnauthorizedAsync(HttpContext context, string message, string errorCode)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = errorCode,
            message = _options.Middleware.DetailedErrors ? message : "Unauthorized",
            timestamp = DateTimeOffset.UtcNow
        };

        var jsonResponse = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(jsonResponse);
    }

    /// <summary>
    /// Handles internal server errors
    /// </summary>
    private async Task HandleInternalErrorAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "INTERNAL_ERROR",
            message = _options.Middleware.DetailedErrors ? message : "Internal server error",
            timestamp = DateTimeOffset.UtcNow
        };

        var jsonResponse = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(jsonResponse);
    }
}