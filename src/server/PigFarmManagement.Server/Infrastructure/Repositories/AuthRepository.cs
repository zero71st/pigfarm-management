using Microsoft.EntityFrameworkCore;
using PigFarmManagement.Server.Infrastructure.Data;
using PigFarmManagement.Server.Infrastructure.Data.Entities;

namespace PigFarmManagement.Server.Infrastructure.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly PigFarmDbContext _context;

    public AuthRepository(PigFarmDbContext context)
    {
        _context = context;
    }

    // User operations
    public async Task<UserEntity?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users
            .Include(u => u.ApiKeys)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<UserEntity?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .Include(u => u.ApiKeys)
            .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);
    }

    public async Task<UserEntity?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.ApiKeys)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
    }

    public async Task<IEnumerable<UserEntity>> GetAllUsersAsync(bool includeInactive = false)
    {
        var query = _context.Users.Include(u => u.ApiKeys).AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive && !u.IsDeleted);
        }
        else
        {
            query = query.Where(u => !u.IsDeleted);
        }

        return await query.OrderBy(u => u.Username).ToListAsync();
    }

    public async Task<UserEntity> CreateUserAsync(UserEntity user)
    {
        user.CreatedAt = DateTime.UtcNow;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<UserEntity> UpdateUserAsync(UserEntity user)
    {
        user.ModifiedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task DeleteUserAsync(Guid userId, string deletedBy)
    {
        var user = await GetUserByIdAsync(userId);
        if (user != null)
        {
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletedBy = deletedBy;
            user.IsActive = false;

            // Revoke all API keys
            foreach (var apiKey in user.ApiKeys)
            {
                apiKey.RevokedAt = DateTime.UtcNow;
                apiKey.RevokedBy = deletedBy;
                apiKey.IsActive = false;
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> UserExistsAsync(string username, string email, Guid? excludeId = null)
    {
        var query = _context.Users.Where(u => !u.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(u => u.Id != excludeId.Value);
        }

        return await query.AnyAsync(u => u.Username == username || u.Email == email);
    }

    // API Key operations
    public async Task<ApiKeyEntity?> GetApiKeyByIdAsync(Guid keyId)
    {
        return await _context.ApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == keyId);
    }

    public async Task<ApiKeyEntity?> GetApiKeyByHashAsync(string keyHash)
    {
        return await _context.ApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.HashedKey == keyHash && k.IsActive && !k.RevokedAt.HasValue);
    }

    public async Task<IEnumerable<ApiKeyEntity>> GetUserApiKeysAsync(Guid userId, bool includeInactive = false)
    {
        var query = _context.ApiKeys.Where(k => k.UserId == userId);

        if (!includeInactive)
        {
            query = query.Where(k => k.IsActive && !k.RevokedAt.HasValue);
        }

        return await query
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }

    public async Task<ApiKeyEntity> CreateApiKeyAsync(ApiKeyEntity apiKey)
    {
        apiKey.CreatedAt = DateTime.UtcNow;
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();
        return apiKey;
    }

    public async Task<ApiKeyEntity> UpdateApiKeyAsync(ApiKeyEntity apiKey)
    {
        _context.ApiKeys.Update(apiKey);
        await _context.SaveChangesAsync();
        return apiKey;
    }

    public async Task RevokeApiKeyAsync(Guid keyId, string revokedBy)
    {
        var apiKey = await GetApiKeyByIdAsync(keyId);
        if (apiKey != null && !apiKey.RevokedAt.HasValue)
        {
            apiKey.RevokedAt = DateTime.UtcNow;
            apiKey.RevokedBy = revokedBy;
            apiKey.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RevokeAllUserApiKeysAsync(Guid userId, string revokedBy)
    {
        var apiKeys = await _context.ApiKeys
            .Where(k => k.UserId == userId && k.IsActive && !k.RevokedAt.HasValue)
            .ToListAsync();

        foreach (var apiKey in apiKeys)
        {
            apiKey.RevokedAt = DateTime.UtcNow;
            apiKey.RevokedBy = revokedBy;
            apiKey.IsActive = false;
        }

        if (apiKeys.Any())
        {
            await _context.SaveChangesAsync();
        }
    }

    // Authentication operations
    public async Task<UserEntity?> ValidateCredentialsAsync(string username, string passwordHash)
    {
        return await _context.Users
            .Include(u => u.ApiKeys)
            .FirstOrDefaultAsync(u => u.Username == username && 
                                    u.PasswordHash == passwordHash && 
                                    u.IsActive && 
                                    !u.IsDeleted);
    }

    public async Task UpdateLastLoginAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateApiKeyUsageAsync(Guid keyId)
    {
        var apiKey = await _context.ApiKeys.FindAsync(keyId);
        if (apiKey != null)
        {
            apiKey.LastUsedAt = DateTime.UtcNow;
            apiKey.UsageCount++;
            await _context.SaveChangesAsync();
        }
    }

    // Admin operations
    public async Task<int> GetActiveUserCountAsync()
    {
        return await _context.Users
            .CountAsync(u => u.IsActive && !u.IsDeleted);
    }

    public async Task<int> GetActiveApiKeyCountAsync()
    {
        return await _context.ApiKeys
            .CountAsync(k => k.IsActive && !k.RevokedAt.HasValue);
    }

    public async Task<IEnumerable<ApiKeyEntity>> GetExpiringApiKeysAsync(int daysUntilExpiry = 7)
    {
        var expiryThreshold = DateTime.UtcNow.AddDays(daysUntilExpiry);
        
        return await _context.ApiKeys
            .Include(k => k.User)
            .Where(k => k.IsActive && 
                       !k.RevokedAt.HasValue && 
                       k.ExpiresAt.HasValue && 
                       k.ExpiresAt <= expiryThreshold)
            .OrderBy(k => k.ExpiresAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserEntity>> GetUsersWithRoleAsync(string role)
    {
        return await _context.Users
            .Where(u => u.IsActive && 
                       !u.IsDeleted && 
                       u.RolesCsv.Contains(role))
            .OrderBy(u => u.Username)
            .ToListAsync();
    }

    // Cleanup operations
    public async Task CleanupExpiredApiKeysAsync()
    {
        var expiredKeys = await _context.ApiKeys
            .Where(k => k.IsActive && 
                       k.ExpiresAt.HasValue && 
                       k.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var key in expiredKeys)
        {
            key.IsActive = false;
            key.RevokedAt = DateTime.UtcNow;
            key.RevokedBy = "System (Expired)";
        }

        if (expiredKeys.Any())
        {
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetExpiredApiKeyCountAsync()
    {
        return await _context.ApiKeys
            .CountAsync(k => k.ExpiresAt.HasValue && 
                            k.ExpiresAt < DateTime.UtcNow && 
                            k.IsActive);
    }
}