using PigFarmManagement.Server.Infrastructure.Data.Entities;

namespace PigFarmManagement.Server.Infrastructure.Repositories;

public interface IAuthRepository
{
    // User operations
    Task<UserEntity?> GetUserByIdAsync(Guid userId);
    Task<UserEntity?> GetUserByUsernameAsync(string username);
    Task<UserEntity?> GetUserByEmailAsync(string email);
    Task<IEnumerable<UserEntity>> GetAllUsersAsync(bool includeInactive = false);
    Task<UserEntity> CreateUserAsync(UserEntity user);
    Task<UserEntity> UpdateUserAsync(UserEntity user);
    Task DeleteUserAsync(Guid userId, string deletedBy);
    Task<bool> UserExistsAsync(string username, string email, Guid? excludeId = null);

    // API Key operations
    Task<ApiKeyEntity?> GetApiKeyByIdAsync(Guid keyId);
    Task<ApiKeyEntity?> GetApiKeyByHashAsync(string keyHash);
    Task<IEnumerable<ApiKeyEntity>> GetUserApiKeysAsync(Guid userId, bool includeInactive = false);
    Task<ApiKeyEntity> CreateApiKeyAsync(ApiKeyEntity apiKey);
    Task<ApiKeyEntity> UpdateApiKeyAsync(ApiKeyEntity apiKey);
    Task RevokeApiKeyAsync(Guid keyId, string revokedBy);
    Task RevokeAllUserApiKeysAsync(Guid userId, string revokedBy);

    // Authentication operations
    Task<UserEntity?> ValidateCredentialsAsync(string username, string passwordHash);
    Task UpdateLastLoginAsync(Guid userId);
    Task UpdateApiKeyUsageAsync(Guid keyId);

    // Admin operations
    Task<int> GetActiveUserCountAsync();
    Task<int> GetActiveApiKeyCountAsync();
    Task<IEnumerable<ApiKeyEntity>> GetExpiringApiKeysAsync(int daysUntilExpiry = 7);
    Task<IEnumerable<UserEntity>> GetUsersWithRoleAsync(string role);

    // Cleanup operations
    Task CleanupExpiredApiKeysAsync();
    Task<int> GetExpiredApiKeyCountAsync();
}