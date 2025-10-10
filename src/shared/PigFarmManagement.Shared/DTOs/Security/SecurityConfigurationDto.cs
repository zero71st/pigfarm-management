namespace PigFarmManagement.Shared.DTOs.Security;

/// <summary>
/// Data transfer object for security configuration settings
/// </summary>
public class SecurityConfigurationDto
{
    /// <summary>
    /// API key configuration settings
    /// </summary>
    public ApiKeySettingsDto ApiKeySettings { get; set; } = new();

    /// <summary>
    /// Role definitions and hierarchy
    /// </summary>
    public RoleSettingsDto RoleSettings { get; set; } = new();

    /// <summary>
    /// Session timeout and cleanup intervals
    /// </summary>
    public SessionSettingsDto SessionSettings { get; set; } = new();

    /// <summary>
    /// Whether middleware is enabled
    /// </summary>
    public bool MiddlewareEnabled { get; set; } = true;

    /// <summary>
    /// Paths to exclude from authentication
    /// </summary>
    public List<string> ExcludePaths { get; set; } = new();

    /// <summary>
    /// Whether to return detailed error messages
    /// </summary>
    public bool DetailedErrors { get; set; } = false;
}

/// <summary>
/// API key configuration settings
/// </summary>
public class ApiKeySettingsDto
{
    /// <summary>
    /// Header name for API key authentication (default: X-Api-Key)
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
/// Session management configuration
/// </summary>
public class SessionSettingsDto
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
/// Role definitions and hierarchy
/// </summary>
public class RoleSettingsDto
{
    /// <summary>
    /// Role hierarchy levels (higher number = more permissions)
    /// </summary>
    public Dictionary<string, int> Hierarchy { get; set; } = new()
    {
        { "User", 1 },
        { "Admin", 2 }
    };

    /// <summary>
    /// Permissions by role
    /// </summary>
    public Dictionary<string, List<string>> Permissions { get; set; } = new()
    {
        { "User", new() { "read:customers", "write:customers", "delete:customers", "read:pigpens", "write:pigpens", "delete:pigpens", "read:feeds", "write:feeds", "delete:feeds", "read:dashboard" } },
        { "Admin", new() { "read:customers", "write:customers", "delete:customers", "read:pigpens", "write:pigpens", "delete:pigpens", "read:feeds", "write:feeds", "delete:feeds", "read:dashboard", "admin:users", "admin:apikeys", "admin:system" } }
    };

    /// <summary>
    /// Default role for new users
    /// </summary>
    public string DefaultRole { get; set; } = "User";
}