namespace PigFarmManagement.Shared.DTOs.Security;

/// <summary>
/// Data transfer object for security configuration settings
/// </summary>
public class SecurityConfigurationDto
{
    /// <summary>
    /// Authentication configuration settings
    /// </summary>
    public ApiKeySettingsDto ApiKeySettings { get; set; } = new();

    /// <summary>
    /// Role definitions and hierarchy
    /// </summary>
    public RoleSettingsDto RoleSettings { get; set; } = new();

    /// <summary>
    /// Role-to-endpoint mapping for authorization rules
    /// </summary>
    public Dictionary<string, List<string>> EndpointGroups { get; set; } = new();

    /// <summary>
    /// Request threshold configurations per role/endpoint
    /// </summary>
    public Dictionary<string, RateLimitPolicyDto> RateLimitPolicies { get; set; } = new();

    /// <summary>
    /// Session timeout and cleanup intervals
    /// </summary>
    public SessionSettingsDto SessionSettings { get; set; } = new();
}

/// <summary>
/// Authentication configuration settings
/// </summary>
public class ApiKeySettingsDto
{
    /// <summary>
    /// Header name for API key authentication (default: X-Api-Key)
    /// </summary>
    public string ApiKeyHeader { get; set; } = "X-Api-Key";

    /// <summary>
    /// Cache timeout for API key validation
    /// </summary>
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Endpoints that allow anonymous access
    /// </summary>
    public List<string> AllowAnonymousEndpoints { get; set; } = new() { "/api/health", "/api/version" };
}

/// <summary>
/// Role definitions and hierarchy
/// </summary>
public class RoleSettingsDto
{
    /// <summary>
    /// Available roles in the system
    /// </summary>
    public List<string> Roles { get; set; } = new() { "Admin", "User", "ReadOnly" };

    /// <summary>
    /// Role hierarchy levels (higher number = more permissions)
    /// </summary>
    public Dictionary<string, int> RoleHierarchy { get; set; } = new()
    {
        { "Admin", 100 },
        { "User", 50 },
        { "ReadOnly", 10 }
    };
}

/// <summary>
/// Session management configuration
/// </summary>
public class SessionSettingsDto
{
    /// <summary>
    /// Idle timeout for sessions
    /// </summary>
    public TimeSpan IdleTimeoutHours { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// Maximum session duration
    /// </summary>
    public TimeSpan MaxSessionHours { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Session cleanup interval
    /// </summary>
    public TimeSpan CleanupIntervalMinutes { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Whether manual refresh is required for session extension
    /// </summary>
    public bool RequireRefresh { get; set; } = true;
}