using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PigFarmManagement.Shared.Contracts.Authentication;

namespace PigFarmManagement.Client.Features.Authentication.Services
{
    /// <summary>
    /// HTTP client service for authentication API operations
    /// </summary>
    public class AuthApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthApiService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public AuthApiService(HttpClient httpClient, ILogger<AuthApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Login with username and password to get API key
        /// </summary>
        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request, _jsonOptions);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Login failed with status {StatusCode}: {Error}", 
                    response.StatusCode, errorContent);
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login request failed");
                throw;
            }
        }

        /// <summary>
        /// Logout and revoke current API key
        /// </summary>
        public async Task<bool> LogoutAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("/api/auth/logout", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout request failed");
                return false;
            }
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        public async Task<UserInfo?> GetCurrentUserAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/auth/me");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserInfo>(_jsonOptions);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get current user request failed");
                return null;
            }
        }

        /// <summary>
        /// Get all users (Admin only)
        /// </summary>
        public async Task<List<UserInfo>> GetUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/auth/users");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<UserInfo>>(_jsonOptions) ?? new List<UserInfo>();
                }
                
                return new List<UserInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get users request failed");
                return new List<UserInfo>();
            }
        }

        /// <summary>
        /// Create a new user (Admin only)
        /// </summary>
        public async Task<UserInfo?> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/users", request, _jsonOptions);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserInfo>(_jsonOptions);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create user request failed");
                throw;
            }
        }

        /// <summary>
        /// Update user (Admin/Manager only)
        /// </summary>
        public async Task<UserInfo?> UpdateUserAsync(Guid userId, UpdateUserRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/api/auth/users/{userId}", request, _jsonOptions);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserInfo>(_jsonOptions);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update user request failed");
                throw;
            }
        }

        /// <summary>
        /// Delete user (Admin only)
        /// </summary>
        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/auth/users/{userId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete user request failed");
                return false;
            }
        }

        /// <summary>
        /// Change password
        /// </summary>
        public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/change-password", request, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Change password request failed");
                throw;
            }
        }

        /// <summary>
        /// Get API keys for current user
        /// </summary>
        public async Task<List<ApiKeyInfo>> GetApiKeysAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/auth/api-keys");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<ApiKeyInfo>>(_jsonOptions) ?? new List<ApiKeyInfo>();
                }
                
                return new List<ApiKeyInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get API keys request failed");
                return new List<ApiKeyInfo>();
            }
        }

        /// <summary>
        /// Create new API key
        /// </summary>
        public async Task<ApiKeyResponse?> CreateApiKeyAsync(ApiKeyRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/api-keys", request, _jsonOptions);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiKeyResponse>(_jsonOptions);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create API key request failed");
                throw;
            }
        }

        /// <summary>
        /// Revoke API key
        /// </summary>
        public async Task<bool> RevokeApiKeyAsync(Guid keyId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/auth/api-keys/{keyId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Revoke API key request failed");
                return false;
            }
        }

        /// <summary>
        /// Get audit logs (Admin only) - placeholder for future implementation
        /// </summary>
        public async Task<List<object>> GetAuditLogsAsync(Guid? userId = null, int page = 1, int pageSize = 50)
        {
            try
            {
                var queryString = $"?page={page}&pageSize={pageSize}";
                if (userId.HasValue)
                {
                    queryString += $"&userId={userId.Value}";
                }
                
                var response = await _httpClient.GetAsync($"/api/auth/audit-logs{queryString}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<object>>(_jsonOptions) ?? new List<object>();
                }
                
                return new List<object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get audit logs request failed");
                return new List<object>();
            }
        }
    }
}