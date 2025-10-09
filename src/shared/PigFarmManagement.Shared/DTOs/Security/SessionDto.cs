namespace PigFarmManagement.Shared.DTOs.Security;

/// <summary>
/// Session information for authenticated users
/// </summary>
public class SessionDto
{
    /// <summary>
    /// Unique session identifier
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// User identifier associated with session
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's role in the system
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// When the session was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Last time session was accessed/refreshed
    /// </summary>
    public DateTimeOffset LastAccessed { get; set; }

    /// <summary>
    /// When session expires (based on idle timeout)
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Whether session is still active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Source IP address for the session
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string from session creation
    /// </summary>
    public string? UserAgent { get; set; }
}

/// <summary>
/// Request to create or refresh a session
/// </summary>
public class SessionCreateDto
{
    /// <summary>
    /// API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Client IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Client user agent
    /// </summary>
    public string? UserAgent { get; set; }
}

/// <summary>
/// Session validation response
/// </summary>
public class SessionValidationDto
{
    /// <summary>
    /// Whether session is valid and active
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Session information (if valid)
    /// </summary>
    public SessionDto? Session { get; set; }

    /// <summary>
    /// Reason for validation failure (if invalid)
    /// </summary>
    public string? InvalidReason { get; set; }

    /// <summary>
    /// Time remaining before session expires
    /// </summary>
    public TimeSpan? TimeRemaining { get; set; }
}

/// <summary>
/// Session statistics for monitoring
/// </summary>
public class SessionStatsDto
{
    /// <summary>
    /// Total active sessions
    /// </summary>
    public int ActiveSessions { get; set; }

    /// <summary>
    /// Sessions by role
    /// </summary>
    public Dictionary<string, int> SessionsByRole { get; set; } = new();

    /// <summary>
    /// Sessions created in last hour
    /// </summary>
    public int RecentSessions { get; set; }

    /// <summary>
    /// Sessions expired in last hour
    /// </summary>
    public int ExpiredSessions { get; set; }
}