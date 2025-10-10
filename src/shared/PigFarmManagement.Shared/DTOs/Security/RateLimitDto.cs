namespace PigFarmManagement.Shared.DTOs.Security;

/// <summary>
/// Data transfer object for rate limiting policy configuration
/// </summary>
public class RateLimitPolicyDto
{
    /// <summary>
    /// Policy name identifier
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Maximum requests allowed per hour
    /// </summary>
    public int RequestsPerHour { get; set; }

    /// <summary>
    /// Time window in minutes for rate limiting
    /// </summary>
    public int WindowMinutes { get; set; } = 60;

    /// <summary>
    /// Roles this policy applies to
    /// </summary>
    public List<string> AppliesTo { get; set; } = new();

    /// <summary>
    /// Duration to block requests after limit exceeded
    /// </summary>
    public TimeSpan BlockDuration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Interval for cleaning up expired rate limit entries
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Rate limit status for a specific user/endpoint combination
/// </summary>
public class RateLimitStatusDto
{
    /// <summary>
    /// Policy name being enforced
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// Number of requests remaining in current window
    /// </summary>
    public int RequestsRemaining { get; set; }

    /// <summary>
    /// Total requests allowed in window
    /// </summary>
    public int RequestsLimit { get; set; }

    /// <summary>
    /// When the current rate limit window resets
    /// </summary>
    public DateTimeOffset WindowReset { get; set; }

    /// <summary>
    /// Whether requests are currently blocked
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// When access will be restored (if blocked)
    /// </summary>
    public DateTimeOffset? BlockedUntil { get; set; }
}

/// <summary>
/// Rate limiting response for multiple policies
/// </summary>
public class RateLimitResponseDto
{
    /// <summary>
    /// Status for each applicable rate limit policy
    /// </summary>
    public List<RateLimitStatusDto> Policies { get; set; } = new();

    /// <summary>
    /// Overall rate limiting status (normal, warning, blocked)
    /// </summary>
    public string GlobalStatus { get; set; } = "normal";

    /// <summary>
    /// Additional context or messages
    /// </summary>
    public string? Message { get; set; }
}