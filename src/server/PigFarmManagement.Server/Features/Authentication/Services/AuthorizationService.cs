using Microsoft.Extensions.Logging;
using PigFarmManagement.Server.Features.Authentication.Configuration;
using PigFarmManagement.Shared.Contracts.Security;
using PigFarmManagement.Shared.DTOs.Security;

namespace PigFarmManagement.Server.Features.Authentication.Services;

/// <summary>
/// Service for authorization checks based on roles and permissions
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly ISecurityConfigurationService _configService;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(
        ISecurityConfigurationService configService,
        ILogger<AuthorizationService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task<AuthorizationValidationDto> CheckPermissionAsync(
        string userId, 
        string role, 
        string requiredPermission, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userPermissions = await GetRolePermissionsAsync(role, cancellationToken);
            var hasPermission = userPermissions.Contains(requiredPermission);

            var result = new AuthorizationValidationDto
            {
                IsValid = hasPermission,
                RequiredPermission = requiredPermission,
                UserPermissions = userPermissions,
                HasPermission = hasPermission,
                RoleLevel = _configService.GetRoleHierarchyLevel(role)
            };

            if (!hasPermission)
            {
                result.ErrorCode = "INSUFFICIENT_PERMISSIONS";
                result.ErrorMessage = $"Role '{role}' does not have permission '{requiredPermission}'";
                
                _logger.LogWarning("Authorization failed for user {UserId} with role {Role}: missing permission {Permission}", 
                    userId, role, requiredPermission);
            }
            else
            {
                _logger.LogDebug("Authorization successful for user {UserId} with role {Role} and permission {Permission}", 
                    userId, role, requiredPermission);
            }

            result.Details.Add("UserId", userId);
            result.Details.Add("UserRole", role);
            result.Details.Add("PermissionCount", userPermissions.Count.ToString());

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId} with role {Role}", 
                requiredPermission, userId, role);
            
            return new AuthorizationValidationDto
            {
                IsValid = false,
                ErrorCode = "AUTHORIZATION_ERROR",
                ErrorMessage = "An error occurred during authorization check",
                RequiredPermission = requiredPermission,
                UserPermissions = new List<string>(),
                HasPermission = false
            };
        }
    }

    public async Task<List<string>> GetRolePermissionsAsync(string role, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.FromResult(_configService.GetRolePermissions(role));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for role {Role}", role);
            return new List<string>();
        }
    }

    public async Task<bool> CheckRoleHierarchyAsync(string userRole, string requiredRole, CancellationToken cancellationToken = default)
    {
        try
        {
            var userLevel = _configService.GetRoleHierarchyLevel(userRole);
            var requiredLevel = _configService.GetRoleHierarchyLevel(requiredRole);

            var hasAccess = userLevel >= requiredLevel;

            _logger.LogDebug("Role hierarchy check: {UserRole}({UserLevel}) vs {RequiredRole}({RequiredLevel}) = {HasAccess}",
                userRole, userLevel, requiredRole, requiredLevel, hasAccess);

            return await Task.FromResult(hasAccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking role hierarchy: {UserRole} vs {RequiredRole}", userRole, requiredRole);
            return false;
        }
    }

    /// <summary>
    /// Checks if user has any of the specified permissions
    /// </summary>
    public async Task<bool> HasAnyPermissionAsync(string userId, string role, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        try
        {
            var userPermissions = await GetRolePermissionsAsync(role, cancellationToken);
            return permissions.Any(permission => userPermissions.Contains(permission));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking any permission for user {UserId} with role {Role}", userId, role);
            return false;
        }
    }

    /// <summary>
    /// Checks if user has all of the specified permissions
    /// </summary>
    public async Task<bool> HasAllPermissionsAsync(string userId, string role, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        try
        {
            var userPermissions = await GetRolePermissionsAsync(role, cancellationToken);
            return permissions.All(permission => userPermissions.Contains(permission));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking all permissions for user {UserId} with role {Role}", userId, role);
            return false;
        }
    }

    /// <summary>
    /// Gets permission information for debugging/admin purposes
    /// </summary>
    public async Task<Dictionary<string, object>> GetPermissionInfoAsync(string role, CancellationToken cancellationToken = default)
    {
        try
        {
            var permissions = await GetRolePermissionsAsync(role, cancellationToken);
            var hierarchyLevel = _configService.GetRoleHierarchyLevel(role);

            return new Dictionary<string, object>
            {
                { "Role", role },
                { "HierarchyLevel", hierarchyLevel },
                { "Permissions", permissions },
                { "PermissionCount", permissions.Count },
                { "HasAdminPermissions", permissions.Any(p => p.StartsWith("admin:")) },
                { "HasWritePermissions", permissions.Any(p => p.StartsWith("write:")) },
                { "HasDeletePermissions", permissions.Any(p => p.StartsWith("delete:")) }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission info for role {Role}", role);
            return new Dictionary<string, object> { { "Error", ex.Message } };
        }
    }

    /// <summary>
    /// Validates that a permission string is well-formed
    /// </summary>
    public static bool IsValidPermissionFormat(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return false;

        // Expected format: action:resource (e.g., "read:customers", "write:pigpens", "admin:users")
        var parts = permission.Split(':');
        if (parts.Length != 2)
            return false;

        var action = parts[0].Trim();
        var resource = parts[1].Trim();

        return !string.IsNullOrWhiteSpace(action) && !string.IsNullOrWhiteSpace(resource);
    }

    /// <summary>
    /// Gets all available permissions across all roles (for admin interface)
    /// </summary>
    public async Task<List<string>> GetAllAvailablePermissionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var config = _configService.GetConfiguration();
            var allPermissions = config.RoleSettings.Permissions.Values
                .SelectMany(permissions => permissions)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            return await Task.FromResult(allPermissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all available permissions");
            return new List<string>();
        }
    }

    /// <summary>
    /// Gets permissions that are missing for a role to have a specific access level
    /// </summary>
    public async Task<List<string>> GetMissingPermissionsAsync(string currentRole, string targetRole, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentPermissions = await GetRolePermissionsAsync(currentRole, cancellationToken);
            var targetPermissions = await GetRolePermissionsAsync(targetRole, cancellationToken);

            return targetPermissions.Except(currentPermissions).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting missing permissions from {CurrentRole} to {TargetRole}", currentRole, targetRole);
            return new List<string>();
        }
    }
}