using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PigFarmManagement.Server.Infrastructure.Data;
using PigFarmManagement.Server.Features.Authentication.Configuration;
using PigFarmManagement.Shared.Contracts.Security;
using PigFarmManagement.Shared.DTOs.Security;
using System.Security.Cryptography;
using System.Text;

namespace PigFarmManagement.Server.Features.Authentication.Services;

/// <summary>
/// Service for validating API keys against the database
/// </summary>
public class ApiKeyValidationService : IApiKeyValidationService
{
    private readonly PigFarmDbContext _context;
    private readonly ISecurityConfigurationService _configService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ApiKeyValidationService> _logger;

    public ApiKeyValidationService(
        PigFarmDbContext context,
        ISecurityConfigurationService configService,
        IMemoryCache cache,
        ILogger<ApiKeyValidationService> logger)
    {
        _context = context;
        _configService = configService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ApiKeyValidationDto> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = _configService.GetConfiguration();
            
            if (!config.ApiKeySettings.EnableValidation)
            {
                _logger.LogWarning("API key validation is disabled in configuration");
                return new ApiKeyValidationDto
                {
                    IsValid = false,
                    ErrorCode = "VALIDATION_DISABLED",
                    ErrorMessage = "API key validation is not enabled"
                };
            }

            // Check cache first if caching is enabled
            var cacheKey = $"apikey_{ComputeHash(apiKey)}";
            if (config.ApiKeySettings.CacheMinutes > 0 && 
                _cache.TryGetValue(cacheKey, out ApiKeyValidationDto? cachedResult))
            {
                _logger.LogDebug("API key validation result found in cache");
                return cachedResult!;
            }

            // Hash the provided API key for database lookup
            var hashedKey = ComputeHash(apiKey);

            // Query database for the API key
            var apiKeyEntity = await _context.ApiKeys
                .Include(ak => ak.User)
                .FirstOrDefaultAsync(ak => ak.HashedKey == hashedKey && ak.IsActive, cancellationToken);

            if (apiKeyEntity == null)
            {
                _logger.LogWarning("Invalid API key attempted: {HashedKey}", hashedKey[..8] + "...");
                var invalidResult = new ApiKeyValidationDto
                {
                    IsValid = false,
                    ErrorCode = "INVALID_API_KEY",
                    ErrorMessage = "API key not found or inactive"
                };
                
                // Cache negative results for shorter time
                CacheResult(cacheKey, invalidResult, TimeSpan.FromMinutes(1));
                return invalidResult;
            }

            // Check if API key is expired
            var isExpired = apiKeyEntity.ExpiresAt.HasValue && apiKeyEntity.ExpiresAt.Value <= DateTime.UtcNow;
            if (isExpired && !config.ApiKeySettings.AllowExpired)
            {
                _logger.LogWarning("Expired API key attempted: User {UserId}", apiKeyEntity.UserId);
                var expiredResult = new ApiKeyValidationDto
                {
                    IsValid = false,
                    ErrorCode = "API_KEY_EXPIRED",
                    ErrorMessage = "API key has expired",
                    IsExpired = true,
                    ExpiresAt = apiKeyEntity.ExpiresAt
                };
                
                CacheResult(cacheKey, expiredResult, TimeSpan.FromMinutes(1));
                return expiredResult;
            }

            // Check if user is active
            if (!apiKeyEntity.User.IsActive)
            {
                _logger.LogWarning("API key for inactive user attempted: User {UserId}", apiKeyEntity.UserId);
                var inactiveResult = new ApiKeyValidationDto
                {
                    IsValid = false,
                    ErrorCode = "USER_INACTIVE",
                    ErrorMessage = "User account is inactive"
                };
                
                CacheResult(cacheKey, inactiveResult, TimeSpan.FromMinutes(1));
                return inactiveResult;
            }

            // Update last used timestamp and usage count
            await UpdateApiKeyUsageAsync(apiKeyEntity, cancellationToken);

            // Create successful validation result
            var validResult = new ApiKeyValidationDto
            {
                IsValid = true,
                UserId = apiKeyEntity.UserId.ToString(),
                Role = apiKeyEntity.User.Role,
                ExpiresAt = apiKeyEntity.ExpiresAt,
                IsExpired = isExpired,
                Details = new Dictionary<string, string>
                {
                    { "UsageCount", apiKeyEntity.UsageCount.ToString() },
                    { "LastUsed", apiKeyEntity.LastUsedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never" },
                    { "UserName", apiKeyEntity.User.Name ?? "Unknown" }
                }
            };

            // Cache successful result
            CacheResult(cacheKey, validResult, TimeSpan.FromMinutes(config.ApiKeySettings.CacheMinutes));

            _logger.LogDebug("API key validated successfully for user {UserId}", apiKeyEntity.UserId);
            return validResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            return new ApiKeyValidationDto
            {
                IsValid = false,
                ErrorCode = "VALIDATION_ERROR",
                ErrorMessage = "An error occurred during API key validation"
            };
        }
    }

    public async Task<(string UserId, string Role)?> GetUserInfoAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateApiKeyAsync(apiKey, cancellationToken);
        
        if (!validation.IsValid || validation.UserId == null || validation.Role == null)
        {
            return null;
        }

        return (validation.UserId, validation.Role);
    }

    public async Task<bool> IsApiKeyExpiredAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var hashedKey = ComputeHash(apiKey);
            
            var apiKeyEntity = await _context.ApiKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(ak => ak.HashedKey == hashedKey && ak.IsActive, cancellationToken);

            if (apiKeyEntity == null)
            {
                return true; // Consider non-existent keys as expired
            }

            return apiKeyEntity.ExpiresAt.HasValue && apiKeyEntity.ExpiresAt.Value <= DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking API key expiration");
            return true; // Fail safe - consider expired on error
        }
    }

    /// <summary>
    /// Updates API key usage statistics
    /// </summary>
    private async Task UpdateApiKeyUsageAsync(Infrastructure.Data.Entities.ApiKeyEntity apiKeyEntity, CancellationToken cancellationToken)
    {
        try
        {
            apiKeyEntity.LastUsedAt = DateTime.UtcNow;
            apiKeyEntity.UsageCount++;
            
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update API key usage statistics for key {KeyId}", apiKeyEntity.Id);
            // Don't fail validation if we can't update usage stats
        }
    }

    /// <summary>
    /// Computes SHA-256 hash of the API key for database storage
    /// </summary>
    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Caches validation result if caching is configured
    /// </summary>
    private void CacheResult(string cacheKey, ApiKeyValidationDto result, TimeSpan duration)
    {
        try
        {
            if (duration.TotalMinutes > 0)
            {
                _cache.Set(cacheKey, result, duration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache API key validation result");
            // Don't fail validation if caching fails
        }
    }
}