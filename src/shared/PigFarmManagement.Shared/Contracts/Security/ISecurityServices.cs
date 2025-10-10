using PigFarmManagement.Shared.DTOs.Security;

namespace PigFarmManagement.Shared.Contracts.Security;

/// <summary>
/// Service contract for API key validation and management
/// </summary>
public interface IApiKeyValidationService
{
    /// <summary>
    /// Validates an API key and returns validation result
    /// </summary>
    /// <param name="apiKey">API key to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>API key validation result</returns>
    Task<ApiKeyValidationDto> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user information associated with a valid API key
    /// </summary>
    /// <param name="apiKey">Valid API key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information or null if key is invalid</returns>
    Task<(string UserId, string Role)?> GetUserInfoAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an API key is expired
    /// </summary>
    /// <param name="apiKey">API key to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if expired, false otherwise</returns>
    Task<bool> IsApiKeyExpiredAsync(string apiKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service contract for session management
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Creates a new session for authenticated user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="role">User role</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created session information</returns>
    Task<SessionDto> CreateSessionAsync(string userId, string role, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an existing session
    /// </summary>
    /// <param name="sessionId">Session ID to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session validation result</returns>
    Task<SessionValidationDto> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes session expiration time
    /// </summary>
    /// <param name="sessionId">Session ID to refresh</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated session information</returns>
    Task<SessionDto?> RefreshSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates a session
    /// </summary>
    /// <param name="sessionId">Session ID to invalidate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if session was invalidated</returns>
    Task<bool> InvalidateSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets session statistics for monitoring
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session statistics</returns>
    Task<SessionStatsDto> GetSessionStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired sessions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions cleaned up</returns>
    Task<int> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service contract for rate limiting
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Checks if a request should be rate limited
    /// </summary>
    /// <param name="userId">User making the request</param>
    /// <param name="endpoint">Endpoint being accessed</param>
    /// <param name="role">User's role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rate limit response</returns>
    Task<RateLimitResponseDto> CheckRateLimitAsync(string userId, string endpoint, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a request for rate limiting tracking
    /// </summary>
    /// <param name="userId">User making the request</param>
    /// <param name="endpoint">Endpoint being accessed</param>
    /// <param name="role">User's role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated rate limit status</returns>
    Task<RateLimitResponseDto> RecordRequestAsync(string userId, string endpoint, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets rate limit configuration for a role
    /// </summary>
    /// <param name="role">Role to get configuration for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rate limit policy or null if not found</returns>
    Task<RateLimitPolicyDto?> GetRateLimitPolicyAsync(string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired rate limit entries
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entries cleaned up</returns>
    Task<int> CleanupExpiredEntriesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service contract for authorization checks
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Checks if user has required permission for an operation
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="role">User role</param>
    /// <param name="requiredPermission">Required permission</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization validation result</returns>
    Task<AuthorizationValidationDto> CheckPermissionAsync(string userId, string role, string requiredPermission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a role
    /// </summary>
    /// <param name="role">Role to get permissions for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of permissions</returns>
    Task<List<string>> GetRolePermissionsAsync(string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if role has sufficient hierarchy level for operation
    /// </summary>
    /// <param name="userRole">User's role</param>
    /// <param name="requiredRole">Required role level</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user role is sufficient</returns>
    Task<bool> CheckRoleHierarchyAsync(string userRole, string requiredRole, CancellationToken cancellationToken = default);
}

/// <summary>
/// Main security service contract that orchestrates all security checks
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// Performs comprehensive security validation for a request
    /// </summary>
    /// <param name="apiKey">API key from request</param>
    /// <param name="sessionId">Session ID from request</param>
    /// <param name="endpoint">Endpoint being accessed</param>
    /// <param name="requiredPermission">Required permission for endpoint</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive security validation result</returns>
    Task<SecurityValidationDto> ValidateRequestAsync(
        string apiKey, 
        string? sessionId, 
        string endpoint, 
        string requiredPermission, 
        string? ipAddress = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates authenticated session after successful API key validation
    /// </summary>
    /// <param name="apiKey">Valid API key</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session creation result</returns>
    Task<SessionDto?> AuthenticateAsync(string apiKey, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an existing session
    /// </summary>
    /// <param name="sessionId">Session to refresh</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Refreshed session or null if invalid</returns>
    Task<SessionDto?> RefreshSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out and invalidates session
    /// </summary>
    /// <param name="sessionId">Session to invalidate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if session was invalidated</returns>
    Task<bool> LogoutAsync(string sessionId, CancellationToken cancellationToken = default);
}