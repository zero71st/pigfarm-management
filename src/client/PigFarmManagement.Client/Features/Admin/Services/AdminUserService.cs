using System.Net.Http.Json;
using PigFarmManagement.Shared.Contracts.Authentication;

namespace PigFarmManagement.Client.Features.Admin.Services;

public interface IAdminUserService
{
    Task<ApiResponse<UserInfo[]>> GetUsersAsync(int page = 1, int pageSize = 20, bool? isActive = null, string? role = null, string? search = null);
    Task<ApiResponse<UserInfo>> GetUserAsync(Guid userId);
    Task<ApiResponse<UserInfo>> CreateUserAsync(CreateUserRequest request);
    Task<ApiResponse<UserInfo>> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task<ApiResponse<bool>> DeleteUserAsync(Guid userId);
    Task<ApiResponse<ApiKeyInfo[]>> GetUserApiKeysAsync(Guid userId);
    Task<ApiResponse<CreateApiKeyResponse>> CreateApiKeyAsync(Guid userId, CreateApiKeyRequest request);
    Task<ApiResponse<bool>> RevokeApiKeyAsync(Guid keyId);
    Task<ApiResponse<RevokeAllKeysResponse>> RevokeAllUserKeysAsync(Guid userId);
    Task<ApiResponse<bool>> ResetPasswordAsync(Guid userId, ResetPasswordRequest request);
}

public class AdminUserService : IAdminUserService
{
    private readonly HttpClient _httpClient;

    public AdminUserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<UserInfo[]>> GetUsersAsync(int page = 1, int pageSize = 20, bool? isActive = null, string? role = null, string? search = null)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (isActive.HasValue)
                queryParams.Add($"isActive={isActive.Value}");
            
            if (!string.IsNullOrWhiteSpace(role))
                queryParams.Add($"role={Uri.EscapeDataString(role)}");
            
            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");

            var query = string.Join("&", queryParams);
            var response = await _httpClient.GetFromJsonAsync<PaginatedResponse<UserInfo>>($"/api/admin/users?{query}");
            
            return new ApiResponse<UserInfo[]>
            {
                IsSuccess = true,
                Data = response?.Items ?? Array.Empty<UserInfo>()
            };
        }
        catch (HttpRequestException ex)
        {
            return new ApiResponse<UserInfo[]>
            {
                IsSuccess = false,
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<UserInfo[]>
            {
                IsSuccess = false,
                ErrorMessage = $"Error fetching users: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<UserInfo>> GetUserAsync(Guid userId)
    {
        try
        {
            var user = await _httpClient.GetFromJsonAsync<UserInfo>($"/api/admin/users/{userId}");
            return new ApiResponse<UserInfo>
            {
                IsSuccess = true,
                Data = user!
            };
        }
        catch (HttpRequestException ex) when (ex.Data.Contains("StatusCode") && ex.Data["StatusCode"]!.ToString() == "404")
        {
            return new ApiResponse<UserInfo>
            {
                IsSuccess = false,
                ErrorMessage = "User not found"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<UserInfo>
            {
                IsSuccess = false,
                ErrorMessage = $"Error fetching user: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<UserInfo>> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/admin/users", request);
            response.EnsureSuccessStatusCode();
            
            var user = await response.Content.ReadFromJsonAsync<UserInfo>();
            return new ApiResponse<UserInfo>
            {
                IsSuccess = true,
                Data = user!
            };
        }
        catch (HttpRequestException ex)
        {
            return new ApiResponse<UserInfo>
            {
                IsSuccess = false,
                ErrorMessage = $"Error creating user: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<UserInfo>> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/admin/users/{userId}", request);
            response.EnsureSuccessStatusCode();
            
            var user = await response.Content.ReadFromJsonAsync<UserInfo>();
            return new ApiResponse<UserInfo>
            {
                IsSuccess = true,
                Data = user!
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<UserInfo>
            {
                IsSuccess = false,
                ErrorMessage = $"Error updating user: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/admin/users/{userId}");
            return new ApiResponse<bool>
            {
                IsSuccess = response.IsSuccessStatusCode,
                Data = response.IsSuccessStatusCode,
                ErrorMessage = response.IsSuccessStatusCode ? null : "Failed to delete user"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Data = false,
                ErrorMessage = $"Error deleting user: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<ApiKeyInfo[]>> GetUserApiKeysAsync(Guid userId)
    {
        try
        {
            var keys = await _httpClient.GetFromJsonAsync<ApiKeyInfo[]>($"/api/admin/users/{userId}/apikeys");
            return new ApiResponse<ApiKeyInfo[]>
            {
                IsSuccess = true,
                Data = keys ?? Array.Empty<ApiKeyInfo>()
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ApiKeyInfo[]>
            {
                IsSuccess = false,
                Data = Array.Empty<ApiKeyInfo>(),
                ErrorMessage = $"Error fetching API keys: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<CreateApiKeyResponse>> CreateApiKeyAsync(Guid userId, CreateApiKeyRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/admin/users/{userId}/apikeys", request);
            response.EnsureSuccessStatusCode();
            
            var keyResponse = await response.Content.ReadFromJsonAsync<CreateApiKeyResponse>();
            return new ApiResponse<CreateApiKeyResponse>
            {
                IsSuccess = true,
                Data = keyResponse!
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<CreateApiKeyResponse>
            {
                IsSuccess = false,
                ErrorMessage = $"Error creating API key: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> RevokeApiKeyAsync(Guid keyId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/admin/apikeys/{keyId}");
            return new ApiResponse<bool>
            {
                IsSuccess = response.IsSuccessStatusCode,
                Data = response.IsSuccessStatusCode
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Data = false,
                ErrorMessage = $"Error revoking API key: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<RevokeAllKeysResponse>> RevokeAllUserKeysAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/admin/users/{userId}/revoke-all-keys", null);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<RevokeAllKeysResponse>();
            return new ApiResponse<RevokeAllKeysResponse>
            {
                IsSuccess = true,
                Data = result!
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<RevokeAllKeysResponse>
            {
                IsSuccess = false,
                ErrorMessage = $"Error revoking all keys: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> ResetPasswordAsync(Guid userId, ResetPasswordRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/admin/users/{userId}/reset-password", request);
            return new ApiResponse<bool>
            {
                IsSuccess = response.IsSuccessStatusCode,
                Data = response.IsSuccessStatusCode
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                IsSuccess = false,
                Data = false,
                ErrorMessage = $"Error resetting password: {ex.Message}"
            };
        }
    }
}

// Helper DTOs for admin operations
public record PaginatedResponse<T>
{
    public T[] Items { get; init; } = Array.Empty<T>();
    public PaginationInfo Pagination { get; init; } = new();
}

public record PaginationInfo
{
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
}

public record ApiResponse<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
}

// All DTOs are now in shared contracts