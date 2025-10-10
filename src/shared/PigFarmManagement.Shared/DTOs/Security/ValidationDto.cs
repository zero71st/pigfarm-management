namespace PigFarmManagement.Shared.DTOs.Security;

/// <summary>
/// Validation result for authentication/authorization requests
/// </summary>
public class ValidationResultDto
{
    /// <summary>
    /// Whether validation was successful
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Error code if validation failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional validation details
    /// </summary>
    public Dictionary<string, string> Details { get; set; } = new();

    /// <summary>
    /// Timestamp of validation
    /// </summary>
    public DateTimeOffset ValidatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// API key validation response
/// </summary>
public class ApiKeyValidationDto : ValidationResultDto
{
    /// <summary>
    /// User ID associated with valid API key
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Role associated with valid API key
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// API key expiration date (if applicable)
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Whether API key is expired
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Rate limit status for this key
    /// </summary>
    public RateLimitStatusDto? RateLimit { get; set; }
}

/// <summary>
/// Authorization validation response
/// </summary>
public class AuthorizationValidationDto : ValidationResultDto
{
    /// <summary>
    /// Required permission for the operation
    /// </summary>
    public string RequiredPermission { get; set; } = string.Empty;

    /// <summary>
    /// User's actual permissions
    /// </summary>
    public List<string> UserPermissions { get; set; } = new();

    /// <summary>
    /// Whether user has required permission
    /// </summary>
    public bool HasPermission { get; set; }

    /// <summary>
    /// Role hierarchy level (for role-based checks)
    /// </summary>
    public int? RoleLevel { get; set; }
}

/// <summary>
/// Security validation response with comprehensive context
/// </summary>
public class SecurityValidationDto
{
    /// <summary>
    /// API key validation result
    /// </summary>
    public ApiKeyValidationDto ApiKeyValidation { get; set; } = new();

    /// <summary>
    /// Authorization validation result
    /// </summary>
    public AuthorizationValidationDto AuthorizationValidation { get; set; } = new();

    /// <summary>
    /// Session validation result
    /// </summary>
    public SessionValidationDto SessionValidation { get; set; } = new();

    /// <summary>
    /// Rate limiting status
    /// </summary>
    public RateLimitResponseDto RateLimitStatus { get; set; } = new();

    /// <summary>
    /// Overall validation result
    /// </summary>
    public bool IsAuthorized => ApiKeyValidation.IsValid && 
                               AuthorizationValidation.IsValid && 
                               SessionValidation.IsValid && 
                               RateLimitStatus.GlobalStatus != "blocked";

    /// <summary>
    /// Combined error messages for failures
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Security context for the validated request
    /// </summary>
    public SecurityContextDto? SecurityContext { get; set; }
}

/// <summary>
/// Security context for validated requests
/// </summary>
public class SecurityContextDto
{
    /// <summary>
    /// Authenticated user ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's role
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// User's permissions
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Session ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Request IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTimeOffset RequestTime { get; set; } = DateTimeOffset.UtcNow;
}