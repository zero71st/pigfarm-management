using PigFarmManagement.Shared.Contracts.Authentication;

namespace PigFarmManagement.Server.Features.Authentication;

public interface IAuthService
{
    // Authentication operations
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<bool> LogoutAsync(string username);
    Task<UserInfo?> GetCurrentUserAsync(string username);
    Task<bool> ValidateApiKeyAsync(string apiKey);

    // User management operations
    Task<UserInfo> CreateUserAsync(CreateUserRequest request, string createdBy);
    Task<UserInfo?> GetUserByIdAsync(Guid userId);
    Task<UserInfo?> GetUserByUsernameAsync(string username);
    Task<IEnumerable<UserInfo>> GetAllUsersAsync(bool includeInactive = false);
    Task<UserInfo?> UpdateUserAsync(Guid userId, UpdateUserRequest request, string modifiedBy);
    Task<bool> DeleteUserAsync(Guid userId, string deletedBy);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

    // API key management operations
    Task<ApiKeyResponse> GenerateApiKeyAsync(Guid userId, ApiKeyRequest request, string createdBy);
    Task<IEnumerable<ApiKeyInfo>> GetUserApiKeysAsync(Guid userId, bool includeInactive = false);
    Task<bool> RevokeApiKeyAsync(Guid keyId, string revokedBy);
    Task<bool> RevokeAllUserApiKeysAsync(Guid userId, string revokedBy);

    // Permission and role operations
    Task<bool> CheckPermissionAsync(string username, string requiredRole);
    Task<bool> IsInRoleAsync(string username, string role);
    Task<string[]> GetUserRolesAsync(string username);

    // Admin and monitoring operations
    Task<int> GetActiveUserCountAsync();
    Task<int> GetActiveApiKeyCountAsync();
    Task<IEnumerable<ApiKeyInfo>> GetExpiringApiKeysAsync(int daysUntilExpiry = 7);
    Task<IEnumerable<UserInfo>> GetUsersWithRoleAsync(string role);

    // Maintenance operations
    Task CleanupExpiredApiKeysAsync();
    Task<int> GetExpiredApiKeyCountAsync();

    // Validation operations
    Task<bool> ValidateUserCredentialsAsync(string username, string password);
    Task<bool> IsUsernameAvailableAsync(string username, Guid? excludeUserId = null);
    Task<bool> IsEmailAvailableAsync(string email, Guid? excludeUserId = null);
}
