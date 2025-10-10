using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PigFarmManagement.Shared.DTOs.Security;

namespace PigFarmManagement.Server.Features.Authentication.Configuration;

/// <summary>
/// Configuration options for security settings
/// </summary>
public class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// API key validation settings
    /// </summary>
    public ApiKeyOptions ApiKey { get; set; } = new();

    /// <summary>
    /// Session management settings
    /// </summary>
    public SessionOptions Session { get; set; } = new();

    /// <summary>
    /// Rate limiting policies
    /// </summary>
    public List<RateLimitOptions> RateLimiting { get; set; } = new();

    /// <summary>
    /// Role configuration and permissions
    /// </summary>
    public RoleOptions Roles { get; set; } = new();

    /// <summary>
    /// Security middleware settings
    /// </summary>
    public MiddlewareOptions Middleware { get; set; } = new();
}

/// <summary>
/// API key configuration options
/// </summary>
public class ApiKeyOptions
{
    /// <summary>
    /// Header name for API key (default: X-Api-Key)
    /// </summary>
    public string HeaderName { get; set; } = "X-Api-Key";

    /// <summary>
    /// Enable API key validation
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// Allow expired API keys (for testing)
    /// </summary>
    public bool AllowExpired { get; set; } = false;

    /// <summary>
    /// Cache API key validation results (in minutes)
    /// </summary>
    public int CacheMinutes { get; set; } = 5;
}

/// <summary>
/// Session configuration options
/// </summary>
public class SessionOptions
{
    /// <summary>
    /// Session idle timeout in hours
    /// </summary>
    public int IdleTimeoutHours { get; set; } = 2;

    /// <summary>
    /// Maximum session duration in hours (regardless of activity)
    /// </summary>
    public int MaxDurationHours { get; set; } = 24;

    /// <summary>
    /// Session cleanup interval in minutes
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Enable session validation
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// Header name for session ID
    /// </summary>
    public string HeaderName { get; set; } = "X-Session-Id";
}

/// <summary>
/// Rate limiting configuration options
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Policy name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Roles this policy applies to
    /// </summary>
    public List<string> AppliesTo { get; set; } = new();

    /// <summary>
    /// Requests per hour limit
    /// </summary>
    public int RequestsPerHour { get; set; } = 1000;

    /// <summary>
    /// Time window in minutes
    /// </summary>
    public int WindowMinutes { get; set; } = 60;

    /// <summary>
    /// Block duration after limit exceeded (in minutes)
    /// </summary>
    public int BlockDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Cleanup interval for expired entries (in minutes)
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Enable rate limiting
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Role and permission configuration options
/// </summary>
public class RoleOptions
{
    /// <summary>
    /// Role hierarchy levels (higher number = more permissions)
    /// </summary>
    public Dictionary<string, int> Hierarchy { get; set; } = new()
    {
        { "ReadOnly", 1 },
        { "User", 2 },
        { "Admin", 3 }
    };

    /// <summary>
    /// Permissions by role
    /// </summary>
    public Dictionary<string, List<string>> Permissions { get; set; } = new()
    {
        { "ReadOnly", new() { "read:customers", "read:pigpens", "read:feeds" } },
        { "User", new() { "read:customers", "write:customers", "read:pigpens", "write:pigpens", "read:feeds" } },
        { "Admin", new() { "read:customers", "write:customers", "delete:customers", "read:pigpens", "write:pigpens", "delete:pigpens", "read:feeds", "write:feeds", "delete:feeds", "admin:users" } }
    };

    /// <summary>
    /// Default role for new users
    /// </summary>
    public string DefaultRole { get; set; } = "ReadOnly";
}

/// <summary>
/// Security middleware configuration options
/// </summary>
public class MiddlewareOptions
{
    /// <summary>
    /// Enable security middleware
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Skip authentication for development
    /// </summary>
    public bool SkipInDevelopment { get; set; } = false;

    /// <summary>
    /// Paths to exclude from authentication
    /// </summary>
    public List<string> ExcludePaths { get; set; } = new()
    {
        "/health",
        "/swagger",
        "/api/v1/health"
    };

    /// <summary>
    /// Return detailed error messages
    /// </summary>
    public bool DetailedErrors { get; set; } = false;

    /// <summary>
    /// Log security events
    /// </summary>
    public bool LogSecurityEvents { get; set; } = true;
}

/// <summary>
/// Service for managing security configuration
/// </summary>
public interface ISecurityConfigurationService
{
    /// <summary>
    /// Gets current security configuration
    /// </summary>
    /// <returns>Security configuration DTO</returns>
    SecurityConfigurationDto GetConfiguration();

    /// <summary>
    /// Gets rate limit policy for a role
    /// </summary>
    /// <param name="role">Role name</param>
    /// <returns>Rate limit policy or null</returns>
    RateLimitPolicyDto? GetRateLimitPolicy(string role);

    /// <summary>
    /// Gets permissions for a role
    /// </summary>
    /// <param name="role">Role name</param>
    /// <returns>List of permissions</returns>
    List<string> GetRolePermissions(string role);

    /// <summary>
    /// Gets role hierarchy level
    /// </summary>
    /// <param name="role">Role name</param>
    /// <returns>Hierarchy level or 0 if not found</returns>
    int GetRoleHierarchyLevel(string role);

    /// <summary>
    /// Checks if a path should be excluded from authentication
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>True if path should be excluded</returns>
    bool IsPathExcluded(string path);
}

/// <summary>
/// Implementation of security configuration service
/// </summary>
public class SecurityConfigurationService : ISecurityConfigurationService
{
    private readonly SecurityOptions _options;

    public SecurityConfigurationService(IOptions<SecurityOptions> options)
    {
        _options = options.Value;
    }

    public SecurityConfigurationDto GetConfiguration()
    {
        return new SecurityConfigurationDto
        {
            ApiKeySettings = new ApiKeySettingsDto
            {
                HeaderName = _options.ApiKey.HeaderName,
                EnableValidation = _options.ApiKey.EnableValidation,
                AllowExpired = _options.ApiKey.AllowExpired,
                CacheMinutes = _options.ApiKey.CacheMinutes
            },
            SessionSettings = new SessionSettingsDto
            {
                IdleTimeoutHours = _options.Session.IdleTimeoutHours,
                MaxDurationHours = _options.Session.MaxDurationHours,
                CleanupIntervalMinutes = _options.Session.CleanupIntervalMinutes,
                EnableValidation = _options.Session.EnableValidation,
                HeaderName = _options.Session.HeaderName
            },
            RoleSettings = new RoleSettingsDto
            {
                Hierarchy = _options.Roles.Hierarchy,
                Permissions = _options.Roles.Permissions,
                DefaultRole = _options.Roles.DefaultRole
            },
            MiddlewareEnabled = _options.Middleware.Enabled,
            ExcludePaths = _options.Middleware.ExcludePaths,
            DetailedErrors = _options.Middleware.DetailedErrors
        };
    }

    public RateLimitPolicyDto? GetRateLimitPolicy(string role)
    {
        var policy = _options.RateLimiting.FirstOrDefault(p => p.AppliesTo.Contains(role));
        if (policy == null) return null;

        return new RateLimitPolicyDto
        {
            Name = policy.Name,
            RequestsPerHour = policy.RequestsPerHour,
            WindowMinutes = policy.WindowMinutes,
            AppliesTo = policy.AppliesTo,
            BlockDuration = TimeSpan.FromMinutes(policy.BlockDurationMinutes),
            CleanupInterval = TimeSpan.FromMinutes(policy.CleanupIntervalMinutes)
        };
    }

    public List<string> GetRolePermissions(string role)
    {
        return _options.Roles.Permissions.TryGetValue(role, out var permissions) 
            ? permissions 
            : new List<string>();
    }

    public int GetRoleHierarchyLevel(string role)
    {
        return _options.Roles.Hierarchy.TryGetValue(role, out var level) ? level : 0;
    }

    public bool IsPathExcluded(string path)
    {
        return _options.Middleware.ExcludePaths.Any(excluded => 
            path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
    }
}