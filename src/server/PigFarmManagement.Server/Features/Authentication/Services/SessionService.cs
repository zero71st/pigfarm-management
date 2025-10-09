using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PigFarmManagement.Server.Features.Authentication.Configuration;
using PigFarmManagement.Shared.Contracts.Security;
using PigFarmManagement.Shared.DTOs.Security;
using System.Collections.Concurrent;

namespace PigFarmManagement.Server.Features.Authentication.Services;

/// <summary>
/// In-memory session management service for stateless API authentication
/// </summary>
public class SessionService : ISessionService
{
    private readonly ISecurityConfigurationService _configService;
    private readonly ILogger<SessionService> _logger;
    private readonly ConcurrentDictionary<string, SessionInfo> _sessions;
    private readonly Timer _cleanupTimer;

    public SessionService(
        ISecurityConfigurationService configService,
        ILogger<SessionService> logger)
    {
        _configService = configService;
        _logger = logger;
        _sessions = new ConcurrentDictionary<string, SessionInfo>();

        // Set up cleanup timer
        var config = _configService.GetConfiguration();
        var cleanupInterval = TimeSpan.FromMinutes(config.SessionSettings.CleanupIntervalMinutes);
        _cleanupTimer = new Timer(CleanupExpiredSessions, null, cleanupInterval, cleanupInterval);
    }

    public async Task<SessionDto> CreateSessionAsync(
        string userId, 
        string role, 
        string? ipAddress = null, 
        string? userAgent = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = _configService.GetConfiguration();
            var sessionId = GenerateSessionId();
            var now = DateTimeOffset.UtcNow;
            
            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                UserId = userId,
                Role = role,
                CreatedAt = now,
                LastAccessed = now,
                ExpiresAt = now.AddHours(config.SessionSettings.IdleTimeoutHours),
                MaxExpiresAt = now.AddHours(config.SessionSettings.MaxDurationHours),
                IsActive = true,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _sessions.TryAdd(sessionId, sessionInfo);

            var sessionDto = MapToDto(sessionInfo);
            
            _logger.LogDebug("Session created for user {UserId}: {SessionId}", userId, sessionId);
            
            return await Task.FromResult(sessionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for user {UserId}", userId);
            throw;
        }
    }

    public async Task<SessionValidationDto> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = _configService.GetConfiguration();
            
            if (!config.SessionSettings.EnableValidation)
            {
                return new SessionValidationDto
                {
                    IsValid = false,
                    InvalidReason = "Session validation is disabled"
                };
            }

            if (!_sessions.TryGetValue(sessionId, out var sessionInfo))
            {
                return new SessionValidationDto
                {
                    IsValid = false,
                    InvalidReason = "Session not found"
                };
            }

            var now = DateTimeOffset.UtcNow;

            // Check if session is active
            if (!sessionInfo.IsActive)
            {
                return new SessionValidationDto
                {
                    IsValid = false,
                    InvalidReason = "Session is inactive"
                };
            }

            // Check idle timeout
            if (now > sessionInfo.ExpiresAt)
            {
                await InvalidateSessionAsync(sessionId, cancellationToken);
                return new SessionValidationDto
                {
                    IsValid = false,
                    InvalidReason = "Session expired due to inactivity"
                };
            }

            // Check maximum duration
            if (now > sessionInfo.MaxExpiresAt)
            {
                await InvalidateSessionAsync(sessionId, cancellationToken);
                return new SessionValidationDto
                {
                    IsValid = false,
                    InvalidReason = "Session expired due to maximum duration"
                };
            }

            // Session is valid
            var timeRemaining = sessionInfo.ExpiresAt - now;
            return new SessionValidationDto
            {
                IsValid = true,
                Session = MapToDto(sessionInfo),
                TimeRemaining = timeRemaining
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session {SessionId}", sessionId);
            return new SessionValidationDto
            {
                IsValid = false,
                InvalidReason = "Session validation error"
            };
        }
    }

    public async Task<SessionDto?> RefreshSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_sessions.TryGetValue(sessionId, out var sessionInfo))
            {
                _logger.LogWarning("Attempted to refresh non-existent session {SessionId}", sessionId);
                return null;
            }

            var now = DateTimeOffset.UtcNow;
            
            // Check if session can be refreshed (not past max duration)
            if (now > sessionInfo.MaxExpiresAt || !sessionInfo.IsActive)
            {
                await InvalidateSessionAsync(sessionId, cancellationToken);
                return null;
            }

            // Update session timestamps
            var config = _configService.GetConfiguration();
            sessionInfo.LastAccessed = now;
            sessionInfo.ExpiresAt = now.AddHours(config.SessionSettings.IdleTimeoutHours);

            _logger.LogDebug("Session refreshed: {SessionId}", sessionId);
            
            return await Task.FromResult(MapToDto(sessionInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<bool> InvalidateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_sessions.TryGetValue(sessionId, out var sessionInfo))
            {
                sessionInfo.IsActive = false;
                _sessions.TryRemove(sessionId, out _);
                
                _logger.LogDebug("Session invalidated: {SessionId}", sessionId);
                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<SessionStatsDto> GetSessionStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var activeSessions = _sessions.Values.Where(s => s.IsActive && s.ExpiresAt > now).ToList();
            var recentThreshold = now.AddHours(-1);

            var stats = new SessionStatsDto
            {
                ActiveSessions = activeSessions.Count,
                SessionsByRole = activeSessions
                    .GroupBy(s => s.Role)
                    .ToDictionary(g => g.Key, g => g.Count()),
                RecentSessions = _sessions.Values.Count(s => s.CreatedAt > recentThreshold),
                ExpiredSessions = _sessions.Values.Count(s => !s.IsActive || s.ExpiresAt <= now)
            };

            return await Task.FromResult(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session statistics");
            return new SessionStatsDto();
        }
    }

    public async Task<int> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var expiredSessions = _sessions
                .Where(kvp => !kvp.Value.IsActive || kvp.Value.MaxExpiresAt <= now)
                .ToList();

            var cleanedCount = 0;
            foreach (var kvp in expiredSessions)
            {
                if (_sessions.TryRemove(kvp.Key, out _))
                {
                    cleanedCount++;
                }
            }

            if (cleanedCount > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired sessions", cleanedCount);
            }

            return await Task.FromResult(cleanedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during session cleanup");
            return 0;
        }
    }

    /// <summary>
    /// Timer callback for automatic session cleanup
    /// </summary>
    private async void CleanupExpiredSessions(object? state)
    {
        try
        {
            await CleanupExpiredSessionsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic session cleanup");
        }
    }

    /// <summary>
    /// Generates a cryptographically secure session ID
    /// </summary>
    private static string GenerateSessionId()
    {
        return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Maps internal session info to DTO
    /// </summary>
    private static SessionDto MapToDto(SessionInfo sessionInfo)
    {
        return new SessionDto
        {
            SessionId = sessionInfo.SessionId,
            UserId = sessionInfo.UserId,
            Role = sessionInfo.Role,
            CreatedAt = sessionInfo.CreatedAt,
            LastAccessed = sessionInfo.LastAccessed,
            ExpiresAt = sessionInfo.ExpiresAt,
            IsActive = sessionInfo.IsActive,
            IpAddress = sessionInfo.IpAddress,
            UserAgent = sessionInfo.UserAgent
        };
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }

    /// <summary>
    /// Internal session information structure
    /// </summary>
    private class SessionInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastAccessed { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset MaxExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}