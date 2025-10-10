using Microsoft.Extensions.Logging;
using PigFarmManagement.Server.Features.Authentication.Configuration;
using PigFarmManagement.Shared.Contracts.Security;
using PigFarmManagement.Shared.DTOs.Security;

namespace PigFarmManagement.Server.Features.Authentication.Services;

/// <summary>
/// Main security service that orchestrates all security validation components
/// </summary>
public class SecurityService : ISecurityService
{
    private readonly IApiKeyValidationService _apiKeyService;
    private readonly ISessionService _sessionService;
    private readonly IRateLimitService _rateLimitService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ISecurityConfigurationService _configService;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(
        IApiKeyValidationService apiKeyService,
        ISessionService sessionService,
        IRateLimitService rateLimitService,
        IAuthorizationService authorizationService,
        ISecurityConfigurationService configService,
        ILogger<SecurityService> logger)
    {
        _apiKeyService = apiKeyService;
        _sessionService = sessionService;
        _rateLimitService = rateLimitService;
        _authorizationService = authorizationService;
        _configService = configService;
        _logger = logger;
    }

    public async Task<SecurityValidationDto> ValidateRequestAsync(
        string apiKey, 
        string? sessionId, 
        string endpoint, 
        string requiredPermission, 
        string? ipAddress = null, 
        CancellationToken cancellationToken = default)
    {
        var result = new SecurityValidationDto();
        var validationErrors = new List<string>();

        try
        {
            _logger.LogDebug("Starting security validation for endpoint {Endpoint} with permission {Permission}", 
                endpoint, requiredPermission);

            // 1. Validate API Key
            result.ApiKeyValidation = await _apiKeyService.ValidateApiKeyAsync(apiKey, cancellationToken);
            if (!result.ApiKeyValidation.IsValid)
            {
                validationErrors.Add($"API Key validation failed: {result.ApiKeyValidation.ErrorMessage}");
                _logger.LogWarning("API key validation failed: {Error}", result.ApiKeyValidation.ErrorMessage);
            }

            var userId = result.ApiKeyValidation.UserId;
            var userRole = result.ApiKeyValidation.Role;

            // 2. Validate Session (if session ID provided)
            if (!string.IsNullOrEmpty(sessionId))
            {
                result.SessionValidation = await _sessionService.ValidateSessionAsync(sessionId, cancellationToken);
                if (!result.SessionValidation.IsValid)
                {
                    validationErrors.Add($"Session validation failed: {result.SessionValidation.InvalidReason}");
                    _logger.LogWarning("Session validation failed for session {SessionId}: {Reason}", 
                        sessionId, result.SessionValidation.InvalidReason);
                }
            }
            else
            {
                result.SessionValidation = new SessionValidationDto { IsValid = true };
            }

            // 3. Check Authorization (if API key is valid)
            if (result.ApiKeyValidation.IsValid && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userRole))
            {
                result.AuthorizationValidation = await _authorizationService.CheckPermissionAsync(
                    userId, userRole, requiredPermission, cancellationToken);
                
                if (!result.AuthorizationValidation.IsValid)
                {
                    validationErrors.Add($"Authorization failed: {result.AuthorizationValidation.ErrorMessage}");
                    _logger.LogWarning("Authorization failed for user {UserId} with role {Role}: {Error}", 
                        userId, userRole, result.AuthorizationValidation.ErrorMessage);
                }
            }
            else
            {
                result.AuthorizationValidation = new AuthorizationValidationDto
                {
                    IsValid = false,
                    ErrorCode = "INVALID_USER_CONTEXT",
                    ErrorMessage = "Cannot perform authorization check without valid user context"
                };
                validationErrors.Add("Authorization skipped due to invalid user context");
            }

            // 4. Check Rate Limiting (if user context is valid)
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userRole))
            {
                result.RateLimitStatus = await _rateLimitService.CheckRateLimitAsync(
                    userId, endpoint, userRole, cancellationToken);
                
                if (result.RateLimitStatus.GlobalStatus == "blocked")
                {
                    validationErrors.Add($"Rate limit exceeded: {result.RateLimitStatus.Message}");
                    _logger.LogWarning("Rate limit exceeded for user {UserId} on endpoint {Endpoint}", 
                        userId, endpoint);
                }
            }
            else
            {
                result.RateLimitStatus = new RateLimitResponseDto
                {
                    GlobalStatus = "normal",
                    Message = "Rate limiting skipped due to invalid user context"
                };
            }

            // 5. Build Security Context (if validation successful)
            if (result.IsAuthorized && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userRole))
            {
                var permissions = await _authorizationService.GetRolePermissionsAsync(userRole, cancellationToken);
                result.SecurityContext = new SecurityContextDto
                {
                    UserId = userId,
                    Role = userRole,
                    Permissions = permissions,
                    SessionId = sessionId ?? string.Empty,
                    IpAddress = ipAddress,
                    RequestTime = DateTimeOffset.UtcNow
                };

                _logger.LogDebug("Security validation successful for user {UserId} with role {Role}", userId, userRole);
            }

            // 6. Record successful request for rate limiting (if authorized)
            if (result.IsAuthorized && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userRole))
            {
                // Update rate limiting counters
                await _rateLimitService.RecordRequestAsync(userId, endpoint, userRole, cancellationToken);
            }

            result.ValidationErrors = validationErrors;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during security validation for endpoint {Endpoint}", endpoint);
            
            return new SecurityValidationDto
            {
                ApiKeyValidation = new ApiKeyValidationDto
                {
                    IsValid = false,
                    ErrorCode = "SECURITY_VALIDATION_ERROR",
                    ErrorMessage = "An unexpected error occurred during security validation"
                },
                AuthorizationValidation = new AuthorizationValidationDto { IsValid = false },
                SessionValidation = new SessionValidationDto { IsValid = false },
                RateLimitStatus = new RateLimitResponseDto { GlobalStatus = "normal" },
                ValidationErrors = new List<string> { "Security validation failed due to unexpected error" }
            };
        }
    }

    public async Task<SessionDto?> AuthenticateAsync(
        string apiKey, 
        string? ipAddress = null, 
        string? userAgent = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting authentication process");

            // Validate API key first
            var apiKeyValidation = await _apiKeyService.ValidateApiKeyAsync(apiKey, cancellationToken);
            if (!apiKeyValidation.IsValid)
            {
                _logger.LogWarning("Authentication failed: API key validation failed");
                return null;
            }

            if (string.IsNullOrEmpty(apiKeyValidation.UserId) || string.IsNullOrEmpty(apiKeyValidation.Role))
            {
                _logger.LogWarning("Authentication failed: API key validation succeeded but user context is incomplete");
                return null;
            }

            // Create new session
            var session = await _sessionService.CreateSessionAsync(
                apiKeyValidation.UserId, 
                apiKeyValidation.Role, 
                ipAddress, 
                userAgent, 
                cancellationToken);

            _logger.LogInformation("Authentication successful for user {UserId} with role {Role}", 
                apiKeyValidation.UserId, apiKeyValidation.Role);

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication");
            return null;
        }
    }

    public async Task<SessionDto?> RefreshSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Refreshing session {SessionId}", sessionId);
            return await _sessionService.RefreshSessionAsync(sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<bool> LogoutAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Logging out session {SessionId}", sessionId);
            return await _sessionService.InvalidateSessionAsync(sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for session {SessionId}", sessionId);
            return false;
        }
    }

    /// <summary>
    /// Validates request with simplified parameters (for middleware use)
    /// </summary>
    public async Task<bool> IsRequestAuthorizedAsync(
        string apiKey,
        string? sessionId,
        string endpoint,
        string requiredPermission,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validation = await ValidateRequestAsync(apiKey, sessionId, endpoint, requiredPermission, ipAddress, cancellationToken);
            return validation.IsAuthorized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking request authorization");
            return false; // Fail closed for security
        }
    }

    /// <summary>
    /// Gets security status for monitoring/debugging
    /// </summary>
    public async Task<Dictionary<string, object>> GetSecurityStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionStats = await _sessionService.GetSessionStatsAsync(cancellationToken);
            var config = _configService.GetConfiguration();

            return new Dictionary<string, object>
            {
                { "ActiveSessions", sessionStats.ActiveSessions },
                { "SessionsByRole", sessionStats.SessionsByRole },
                { "SecurityEnabled", config.MiddlewareEnabled },
                { "ApiKeyValidationEnabled", config.ApiKeySettings.EnableValidation },
                { "SessionValidationEnabled", config.SessionSettings.EnableValidation },
                { "ExcludedPaths", config.ExcludePaths },
                { "Timestamp", DateTimeOffset.UtcNow }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security status");
            return new Dictionary<string, object> 
            { 
                { "Error", "Failed to retrieve security status" },
                { "Timestamp", DateTimeOffset.UtcNow }
            };
        }
    }
}