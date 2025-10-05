using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PigFarmManagement.Server.Infrastructure.Settings;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Services.ExternalServices
{
    /// <summary>
    /// Low-level HTTP client for POSPOS Product API.
    /// Handles rate limiting (10 requests/minute), authentication, and error handling.
    /// </summary>
    public class PosposProductClient : IPosposProductClient
    {
        private readonly HttpClient _http;
        private readonly PosposOptions _opts;
        private readonly ILogger<PosposProductClient> _logger;
        
        // Rate limiting: 10 requests per minute = 6 seconds per request
        private static readonly SemaphoreSlim _rateLimiter = new SemaphoreSlim(1, 1);
        private static DateTime _lastRequestTime = DateTime.MinValue;
        private const int MinRequestIntervalMs = 6000; // 6 seconds between requests

        public PosposProductClient(
            HttpClient http, 
            IOptions<PosposOptions> opts, 
            ILogger<PosposProductClient> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _opts = opts?.Value ?? new PosposOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Allow fallback to environment variables if config keys are not set
            if (string.IsNullOrWhiteSpace(_opts.ProductApiBase))
            {
                var env = Environment.GetEnvironmentVariable("POSPOS_PRODUCT_API_BASE");
                if (!string.IsNullOrWhiteSpace(env)) 
                    _opts.ProductApiBase = env;
                // Fallback to legacy environment variable name
                else 
                {
                    var legacyEnv = Environment.GetEnvironmentVariable("POSPOS_API_BASE");
                    if (!string.IsNullOrWhiteSpace(legacyEnv)) 
                        _opts.ProductApiBase = legacyEnv;
                }
            }
            
            if (string.IsNullOrWhiteSpace(_opts.ApiKey))
            {
                var envKey = Environment.GetEnvironmentVariable("POSPOS_API_KEY");
                if (!string.IsNullOrWhiteSpace(envKey)) 
                    _opts.ApiKey = envKey;
            }

            _logger.LogInformation(
                "PosposProductClient configured. ProductApiBase='{Base}', ApiKeySet={HasKey}", 
                _opts.ProductApiBase, 
                !string.IsNullOrEmpty(_opts.ApiKey));
        }

        /// <inheritdoc />
        public async Task<List<PosposProductDto>> GetAllProductsAsync()
        {
            var results = new List<PosposProductDto>();
            var page = 1;
            const int pageSize = 1000;

            _logger.LogInformation("Fetching all products from POSPOS");

            while (true)
            {
                await ApplyRateLimitAsync();

                var baseUrl = _opts.ProductApiBase;
                if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute))
                {
                    _logger.LogWarning("POSPOS product API base URL not configured");
                    throw new InvalidOperationException("POSPOS API base URL is not configured");
                }

                // Build URL with pagination
                var sep = baseUrl.Contains('?') ? '&' : '?';
                var url = $"{baseUrl}{sep}page={page}&limit={pageSize}";

                _logger.LogDebug("POSPOS request URL (products): {Url}", url);

                var response = await SendRequestAsync<PosposProductResponse>(url);
                
                if (response == null || response.Success != 1 || response.Data == null || response.Data.Count == 0)
                {
                    _logger.LogInformation("Fetched {Count} products total from POSPOS", results.Count);
                    break;
                }

                results.AddRange(response.Data);
                _logger.LogDebug("Fetched page {Page} with {Count} products", page, response.Data.Count);

                // If we got less than pageSize, we're on the last page
                if (response.Data.Count < pageSize)
                    break;

                page++;
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<PosposProductDto?> GetProductByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Product code cannot be empty", nameof(code));

            await ApplyRateLimitAsync();

            var baseUrl = _opts.ProductApiBase;
            if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute))
            {
                _logger.LogWarning("POSPOS product API base URL not configured");
                throw new InvalidOperationException("POSPOS API base URL is not configured");
            }

            // Build URL with code filter
            var sep = baseUrl.Contains('?') ? '&' : '?';
            var url = $"{baseUrl}{sep}code={Uri.EscapeDataString(code)}";

            _logger.LogDebug("POSPOS request URL (product by code): {Url}", url);

            var response = await SendRequestAsync<PosposProductResponse>(url);

            if (response?.Data == null || response.Data.Count == 0)
            {
                _logger.LogInformation("Product with code '{Code}' not found in POSPOS", code);
                return null;
            }

            return response.Data[0];
        }

        /// <summary>
        /// Applies rate limiting to ensure we don't exceed 10 requests per minute.
        /// </summary>
        private async Task ApplyRateLimitAsync()
        {
            await _rateLimiter.WaitAsync();
            try
            {
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                if (timeSinceLastRequest.TotalMilliseconds < MinRequestIntervalMs)
                {
                    var delayMs = MinRequestIntervalMs - (int)timeSinceLastRequest.TotalMilliseconds;
                    _logger.LogDebug("Rate limiting: waiting {DelayMs}ms before next request", delayMs);
                    await Task.Delay(delayMs);
                }
                _lastRequestTime = DateTime.UtcNow;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        /// <summary>
        /// Sends an HTTP GET request to POSPOS API with authentication and error handling.
        /// </summary>
        private async Task<T?> SendRequestAsync<T>(string url) where T : class
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            // Add API key header if configured
            if (!string.IsNullOrWhiteSpace(_opts.ApiKey))
            {
                request.Headers.Add("apikey", _opts.ApiKey);
            }

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error calling POSPOS product API: {Message}", ex.Message);
                throw new InvalidOperationException("Failed to connect to POSPOS API", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout calling POSPOS product API");
                throw new InvalidOperationException("POSPOS API request timed out", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "POSPOS product API returned {StatusCode}. Body: {Body}", 
                    response.StatusCode, 
                    errorBody);
                
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new InvalidOperationException("POSPOS API rate limit exceeded");
                }
                
                throw new InvalidOperationException(
                    $"POSPOS API returned error status: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("POSPOS product API returned empty response");
                return null;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                return JsonSerializer.Deserialize<T>(content, options);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse POSPOS product API response: {Content}", content);
                throw new InvalidOperationException("Invalid JSON response from POSPOS API", ex);
            }
        }
    }
}
