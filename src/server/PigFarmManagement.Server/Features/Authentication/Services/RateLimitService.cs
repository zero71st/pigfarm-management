using Microsoft.Extensions.Logging;
using PigFarmManagement.Server.Features.Authentication.Configuration;
using PigFarmManagement.Shared.Contracts.Security;
using PigFarmManagement.Shared.DTOs.Security;
using System.Collections.Concurrent;

namespace PigFarmManagement.Server.Features.Authentication.Services;

/// <summary>
/// In-memory sliding window rate limiting service
/// </summary>
public class RateLimitService : IRateLimitService
{
    private readonly ISecurityConfigurationService _configService;
    private readonly ILogger<RateLimitService> _logger;
    private readonly ConcurrentDictionary<string, UserRateLimitInfo> _userLimits;
    private readonly Timer _cleanupTimer;

    public RateLimitService(
        ISecurityConfigurationService configService,
        ILogger<RateLimitService> logger)
    {
        _configService = configService;
        _logger = logger;
        _userLimits = new ConcurrentDictionary<string, UserRateLimitInfo>();

        // Set up cleanup timer to run every 5 minutes
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<RateLimitResponseDto> CheckRateLimitAsync(
        string userId, 
        string endpoint, 
        string role, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var policy = await GetRateLimitPolicyAsync(role, cancellationToken);
            if (policy == null)
            {
                return new RateLimitResponseDto
                {
                    GlobalStatus = "normal",
                    Message = "No rate limit policy applies"
                };
            }

            var userKey = $"{userId}:{role}";
            var userLimitInfo = _userLimits.GetOrAdd(userKey, k => new UserRateLimitInfo());

            var now = DateTimeOffset.UtcNow;
            var windowStart = now.Add(-TimeSpan.FromMinutes(policy.WindowMinutes));

            // Clean up old requests outside the window
            userLimitInfo.CleanupOldRequests(windowStart);

            // Check if user is currently blocked
            if (userLimitInfo.IsBlocked && userLimitInfo.BlockedUntil > now)
            {
                var status = new RateLimitStatusDto
                {
                    PolicyName = policy.Name,
                    RequestsRemaining = 0,
                    RequestsLimit = policy.RequestsPerHour,
                    WindowReset = now.Add(TimeSpan.FromMinutes(policy.WindowMinutes)),
                    IsBlocked = true,
                    BlockedUntil = userLimitInfo.BlockedUntil
                };

                return new RateLimitResponseDto
                {
                    Policies = new List<RateLimitStatusDto> { status },
                    GlobalStatus = "blocked",
                    Message = $"Rate limit exceeded. Blocked until {userLimitInfo.BlockedUntil:yyyy-MM-dd HH:mm:ss} UTC"
                };
            }

            // Clear block if it has expired
            if (userLimitInfo.IsBlocked && userLimitInfo.BlockedUntil <= now)
            {
                userLimitInfo.IsBlocked = false;
                userLimitInfo.BlockedUntil = null;
                userLimitInfo.Requests.Clear();
                _logger.LogDebug("Rate limit block expired for user {UserId}", userId);
            }

            // Count requests in current window
            var requestsInWindow = userLimitInfo.Requests.Count(r => r >= windowStart);
            var requestsRemaining = Math.Max(0, policy.RequestsPerHour - requestsInWindow);

            // Determine status
            var globalStatus = "normal";
            if (requestsRemaining <= 0)
            {
                globalStatus = "blocked";
            }
            else if (requestsRemaining <= policy.RequestsPerHour * 0.1) // 10% remaining
            {
                globalStatus = "warning";
            }

            var statusDto = new RateLimitStatusDto
            {
                PolicyName = policy.Name,
                RequestsRemaining = requestsRemaining,
                RequestsLimit = policy.RequestsPerHour,
                WindowReset = now.Add(TimeSpan.FromMinutes(policy.WindowMinutes)),
                IsBlocked = globalStatus == "blocked"
            };

            return new RateLimitResponseDto
            {
                Policies = new List<RateLimitStatusDto> { statusDto },
                GlobalStatus = globalStatus,
                Message = globalStatus switch
                {
                    "warning" => $"Rate limit warning: {requestsRemaining} requests remaining",
                    "blocked" => "Rate limit exceeded",
                    _ => null
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for user {UserId}", userId);
            
            // Fail open - allow request but log error
            return new RateLimitResponseDto
            {
                GlobalStatus = "normal",
                Message = "Rate limit check failed - allowing request"
            };
        }
    }

    public async Task<RateLimitResponseDto> RecordRequestAsync(
        string userId, 
        string endpoint, 
        string role, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var policy = await GetRateLimitPolicyAsync(role, cancellationToken);
            if (policy == null)
            {
                return new RateLimitResponseDto
                {
                    GlobalStatus = "normal",
                    Message = "No rate limit policy applies"
                };
            }

            var userKey = $"{userId}:{role}";
            var userLimitInfo = _userLimits.GetOrAdd(userKey, k => new UserRateLimitInfo());

            var now = DateTimeOffset.UtcNow;
            var windowStart = now.Add(-TimeSpan.FromMinutes(policy.WindowMinutes));

            // Clean up old requests
            userLimitInfo.CleanupOldRequests(windowStart);

            // Record this request
            userLimitInfo.Requests.Add(now);

            // Check if limit is exceeded
            var requestsInWindow = userLimitInfo.Requests.Count;
            if (requestsInWindow > policy.RequestsPerHour)
            {
                // Block the user
                userLimitInfo.IsBlocked = true;
                userLimitInfo.BlockedUntil = now.Add(policy.BlockDuration);

                _logger.LogWarning("User {UserId} rate limit exceeded. Blocked until {BlockedUntil}", 
                    userId, userLimitInfo.BlockedUntil);

                var blockedStatus = new RateLimitStatusDto
                {
                    PolicyName = policy.Name,
                    RequestsRemaining = 0,
                    RequestsLimit = policy.RequestsPerHour,
                    WindowReset = now.Add(TimeSpan.FromMinutes(policy.WindowMinutes)),
                    IsBlocked = true,
                    BlockedUntil = userLimitInfo.BlockedUntil
                };

                return new RateLimitResponseDto
                {
                    Policies = new List<RateLimitStatusDto> { blockedStatus },
                    GlobalStatus = "blocked",
                    Message = $"Rate limit exceeded after {requestsInWindow} requests"
                };
            }

            // Return current status
            var requestsRemaining = Math.Max(0, policy.RequestsPerHour - requestsInWindow);
            var globalStatus = requestsRemaining <= policy.RequestsPerHour * 0.1 ? "warning" : "normal";

            var statusDto = new RateLimitStatusDto
            {
                PolicyName = policy.Name,
                RequestsRemaining = requestsRemaining,
                RequestsLimit = policy.RequestsPerHour,
                WindowReset = now.Add(TimeSpan.FromMinutes(policy.WindowMinutes)),
                IsBlocked = false
            };

            return new RateLimitResponseDto
            {
                Policies = new List<RateLimitStatusDto> { statusDto },
                GlobalStatus = globalStatus,
                Message = globalStatus == "warning" ? $"{requestsRemaining} requests remaining" : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording request for user {UserId}", userId);
            
            return new RateLimitResponseDto
            {
                GlobalStatus = "normal",
                Message = "Request recording failed"
            };
        }
    }

    public async Task<RateLimitPolicyDto?> GetRateLimitPolicyAsync(string role, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.FromResult(_configService.GetRateLimitPolicy(role));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate limit policy for role {Role}", role);
            return null;
        }
    }

    public async Task<int> CleanupExpiredEntriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var cleanedCount = 0;

            foreach (var kvp in _userLimits.ToList())
            {
                var userInfo = kvp.Value;
                
                // Clean up old requests (older than 2 hours)
                var oldRequestCount = userInfo.Requests.Count;
                userInfo.CleanupOldRequests(now.AddHours(-2));
                
                // Remove user info if no recent requests and not blocked
                if (!userInfo.Requests.Any() && !userInfo.IsBlocked)
                {
                    if (_userLimits.TryRemove(kvp.Key, out _))
                    {
                        cleanedCount++;
                    }
                }
                
                // Clear expired blocks
                if (userInfo.IsBlocked && userInfo.BlockedUntil <= now)
                {
                    userInfo.IsBlocked = false;
                    userInfo.BlockedUntil = null;
                    userInfo.Requests.Clear();
                }
            }

            if (cleanedCount > 0)
            {
                _logger.LogDebug("Cleaned up rate limit data for {Count} users", cleanedCount);
            }

            return await Task.FromResult(cleanedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during rate limit cleanup");
            return 0;
        }
    }

    /// <summary>
    /// Timer callback for automatic cleanup
    /// </summary>
    private async void CleanupExpiredEntries(object? state)
    {
        try
        {
            await CleanupExpiredEntriesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic rate limit cleanup");
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }

    /// <summary>
    /// Internal rate limit tracking for a user
    /// </summary>
    private class UserRateLimitInfo
    {
        public List<DateTimeOffset> Requests { get; } = new();
        public bool IsBlocked { get; set; }
        public DateTimeOffset? BlockedUntil { get; set; }

        public void CleanupOldRequests(DateTimeOffset cutoff)
        {
            for (int i = Requests.Count - 1; i >= 0; i--)
            {
                if (Requests[i] < cutoff)
                {
                    Requests.RemoveAt(i);
                }
            }
        }
    }
}