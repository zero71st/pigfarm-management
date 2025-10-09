using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PigFarmManagement.Client.Services;

/// <summary>
/// Centralized API client service with authentication header management
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Performs GET request with authentication headers
    /// </summary>
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs POST request with authentication headers
    /// </summary>
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs PUT request with authentication headers
    /// </summary>
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs DELETE request with authentication headers
    /// </summary>
    Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs raw HTTP request with authentication headers
    /// </summary>
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of centralized API client with automatic API key header injection
/// </summary>
public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        await AddAuthenticationHeadersAsync(request);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        
        await HandleErrorResponseAsync(response);
        return default;
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        await AddAuthenticationHeadersAsync(request);
        
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
        }
        
        await HandleErrorResponseAsync(response);
        return default;
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
        await AddAuthenticationHeadersAsync(request);
        
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
        }
        
        await HandleErrorResponseAsync(response);
        return default;
    }

    public async Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
        await AddAuthenticationHeadersAsync(request);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        
        await HandleErrorResponseAsync(response);
        return false;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        await AddAuthenticationHeadersAsync(request);
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Adds authentication headers to the request
    /// </summary>
    private async Task AddAuthenticationHeadersAsync(HttpRequestMessage request)
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                // Get API key from authentication state
                var apiKeyClaim = authState.User.FindFirst("ApiKey");
                if (apiKeyClaim != null && !string.IsNullOrEmpty(apiKeyClaim.Value))
                {
                    request.Headers.Add("X-Api-Key", apiKeyClaim.Value);
                }

                // Get session ID if available
                var sessionClaim = authState.User.FindFirst("SessionId");
                if (sessionClaim != null && !string.IsNullOrEmpty(sessionClaim.Value))
                {
                    request.Headers.Add("X-Session-Id", sessionClaim.Value);
                }
            }

            // Add request ID for tracing
            request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString());
            
            // Add user agent
            request.Headers.UserAgent.ParseAdd("PigFarmManagement-Client/1.0");
        }
        catch (Exception)
        {
            // Don't fail the request if we can't add auth headers
            // The server will handle authentication errors appropriately
        }
    }

    /// <summary>
    /// Handles error responses from the API
    /// </summary>
    private async Task HandleErrorResponseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        
        switch (response.StatusCode)
        {
            case System.Net.HttpStatusCode.Unauthorized:
                throw new UnauthorizedAccessException($"Authentication failed: {content}");
                
            case System.Net.HttpStatusCode.Forbidden:
                throw new UnauthorizedAccessException($"Access denied: {content}");
                
            case System.Net.HttpStatusCode.TooManyRequests:
                throw new InvalidOperationException($"Rate limit exceeded: {content}");
                
            case System.Net.HttpStatusCode.BadRequest:
                throw new ArgumentException($"Bad request: {content}");
                
            case System.Net.HttpStatusCode.NotFound:
                throw new InvalidOperationException($"Resource not found: {content}");
                
            default:
                throw new HttpRequestException($"API request failed with status {response.StatusCode}: {content}");
        }
    }
}

/// <summary>
/// Extension methods for easier API client usage
/// </summary>
public static class ApiClientExtensions
{
    /// <summary>
    /// Gets a list of items from an API endpoint
    /// </summary>
    public static async Task<List<T>> GetListAsync<T>(this IApiClient apiClient, string endpoint, CancellationToken cancellationToken = default)
    {
        var result = await apiClient.GetAsync<List<T>>(endpoint, cancellationToken);
        return result ?? new List<T>();
    }

    /// <summary>
    /// Posts data and expects a specific response type
    /// </summary>
    public static async Task<T?> PostAsync<T>(this IApiClient apiClient, string endpoint, object data, CancellationToken cancellationToken = default)
    {
        return await apiClient.PostAsync<object, T>(endpoint, data, cancellationToken);
    }

    /// <summary>
    /// Puts data and expects a specific response type
    /// </summary>
    public static async Task<T?> PutAsync<T>(this IApiClient apiClient, string endpoint, object data, CancellationToken cancellationToken = default)
    {
        return await apiClient.PutAsync<object, T>(endpoint, data, cancellationToken);
    }
}