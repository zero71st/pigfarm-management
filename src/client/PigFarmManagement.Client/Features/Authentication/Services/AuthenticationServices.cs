using Microsoft.JSInterop;
using PigFarmManagement.Shared.Contracts.Authentication;

namespace PigFarmManagement.Client.Features.Authentication.Services
{
    /// <summary>
    /// Interface for API key storage abstraction
    /// </summary>
    public interface IApiKeyStorage
    {
        Task<string?> GetApiKeyAsync();
        Task SetApiKeyAsync(string apiKey);
        Task ClearApiKeyAsync();
    }

    /// <summary>
    /// Browser localStorage implementation for API key storage
    /// </summary>
    public class BrowserApiKeyStorage : IApiKeyStorage
    {
        private readonly IJSRuntime _jsRuntime;
        private const string API_KEY_STORAGE_KEY = "pigfarm_api_key";

        public BrowserApiKeyStorage(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<string?> GetApiKeyAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", API_KEY_STORAGE_KEY);
            }
            catch
            {
                return null;
            }
        }

        public async Task SetApiKeyAsync(string apiKey)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", API_KEY_STORAGE_KEY, apiKey);
        }

        public async Task ClearApiKeyAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", API_KEY_STORAGE_KEY);
        }
    }

    /// <summary>
    /// Authentication state service for managing user login state and current user information
    /// </summary>
    public class AuthenticationStateService
    {
        private readonly IApiKeyStorage _storage;
        private readonly AuthApiService _authApiService;
        private UserInfo? _currentUser;
        private string? _currentApiKey;

        public event Action? AuthenticationStateChanged;

        public UserInfo? CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null && !string.IsNullOrEmpty(_currentApiKey);
        public string? CurrentApiKey => _currentApiKey;

        public AuthenticationStateService(IApiKeyStorage storage, AuthApiService authApiService)
        {
            _storage = storage;
            _authApiService = authApiService;
        }

        /// <summary>
        /// Initialize authentication state from stored API key
        /// </summary>
        public async Task InitializeAsync()
        {
            _currentApiKey = await _storage.GetApiKeyAsync();
            if (!string.IsNullOrEmpty(_currentApiKey))
            {
                // Validate the API key by getting current user
                _currentUser = await _authApiService.GetCurrentUserAsync();
                if (_currentUser == null)
                {
                    // API key is invalid, clear it
                    await LogoutAsync();
                }
                else
                {
                    // Successfully loaded user, notify authentication state changed
                    NotifyAuthenticationStateChanged();
                }
            }
        }

        /// <summary>
        /// Set authentication state after successful login
        /// </summary>
        public async Task SetAuthenticatedAsync(UserInfo user, string apiKey)
        {
            _currentUser = user;
            _currentApiKey = apiKey;
            await _storage.SetApiKeyAsync(apiKey);
            NotifyAuthenticationStateChanged();
        }

        /// <summary>
        /// Clear authentication state on logout
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_currentApiKey))
                {
                    await _authApiService.LogoutAsync();
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue with local logout
                Console.WriteLine($"Error calling server logout: {ex.Message}");
            }
            
            // Clear local state regardless of server response
            _currentUser = null;
            _currentApiKey = null;
            await _storage.ClearApiKeyAsync();
            NotifyAuthenticationStateChanged();
        }

        /// <summary>
        /// Force refresh of authentication state
        /// </summary>
        public void ForceRefresh()
        {
            NotifyAuthenticationStateChanged();
        }

        /// <summary>
        /// Update current user information
        /// </summary>
        public void UpdateUser(UserInfo user)
        {
            _currentUser = user;
            NotifyAuthenticationStateChanged();
        }

        /// <summary>
        /// Login with username and password
        /// </summary>
        public async Task<bool> LoginAsync(string username, string password, string? keyLabel = null, int expirationDays = 30)
        {
            try
            {
                var request = new LoginRequest 
                { 
                    Username = username, 
                    Password = password,
                    KeyLabel = keyLabel,
                    ExpirationDays = expirationDays
                };

                var response = await _authApiService.LoginAsync(request);
                if (response != null)
                {
                    await SetAuthenticatedAsync(response.User, response.ApiKey);
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void NotifyAuthenticationStateChanged()
        {
            AuthenticationStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// HTTP message handler that automatically adds API key to requests
    /// </summary>
    public class ApiKeyHandler : DelegatingHandler
    {
        private readonly IApiKeyStorage _storage;

        public ApiKeyHandler(IApiKeyStorage storage)
        {
            _storage = storage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var apiKey = await _storage.GetApiKeyAsync();
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Add("X-Api-Key", apiKey);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // Handle 401 Unauthorized responses
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Clear invalid API key
                await _storage.ClearApiKeyAsync();
            }

            return response;
        }
    }
}