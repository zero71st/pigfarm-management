using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PigFarmManagement.Server.Infrastructure.Settings;
using PigFarmManagement.Server.Models;

namespace PigFarmManagement.Server.Services
{
    public class PosposClient : IPosposClient
    {
        private readonly HttpClient _http;
        private readonly PosposOptions _opts;
        private readonly Microsoft.Extensions.Logging.ILogger<PosposClient> _logger;

        public PosposClient(HttpClient http, IOptions<PosposOptions> opts, Microsoft.Extensions.Logging.ILogger<PosposClient> logger)
        {
            _http = http;
            _opts = opts.Value;
            // If configuration wasn't provided via appsettings, allow environment variables as a fallback
            if (string.IsNullOrWhiteSpace(_opts.ApiBase))
            {
                var envBase = Environment.GetEnvironmentVariable("POSPOS_API_BASE");
                if (!string.IsNullOrWhiteSpace(envBase))
                    _opts.ApiBase = envBase;
            }
            if (string.IsNullOrWhiteSpace(_opts.ApiKey))
            {
                var envKey = Environment.GetEnvironmentVariable("POSPOS_API_KEY");
                if (!string.IsNullOrWhiteSpace(envKey))
                    _opts.ApiKey = envKey;
            }
            logger.LogInformation("PosposClient configured. ApiBase='{ApiBase}', ApiKeySet={HasKey}", _opts.ApiBase, !string.IsNullOrEmpty(_opts.ApiKey));
            _logger = logger;
        }

        public async Task<IEnumerable<PosposMember>> GetMembersAsync()
        {
            // If ApiBase is not configured or not an absolute URI, log and return empty list
            if (string.IsNullOrWhiteSpace(_opts.ApiBase) || !Uri.IsWellFormedUriString(_opts.ApiBase, UriKind.Absolute))
            {
                _logger.LogWarning("POSPOS ApiBase is not configured or invalid ('{ApiBase}'). No candidates will be returned. Set POSPOS_API_BASE and POSPOS_API_KEY to use the real API.", _opts.ApiBase);
                return Array.Empty<PosposMember>();
            }

            var request = new HttpRequestMessage(HttpMethod.Get, _opts.ApiBase);
            // POSPOS expects 'apikey' header per user guidance
            request.Headers.Add("apikey", _opts.ApiKey);

            HttpResponseMessage res;
            try
            {
                res = await _http.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request to POSPOS failed");
                return Array.Empty<PosposMember>();
            }

            if (!res.IsSuccessStatusCode)
            {
                var txt = await res.Content.ReadAsStringAsync();
                _logger.LogWarning("POSPOS returned non-success status {Status}. Body: {Body}", res.StatusCode, txt);
                return Array.Empty<PosposMember>();
            }

            var s = await res.Content.ReadAsStringAsync();
            try
            {
                // Attempt to parse common shapes: array or { data: [...] }
                var trimmed = s.TrimStart();
                if (trimmed.StartsWith("["))
                {
                    var arr = JsonSerializer.Deserialize<IEnumerable<PosposMember>>(s);
                    return arr ?? Array.Empty<PosposMember>();
                }

                using var doc = JsonDocument.Parse(s);
                // 1) response may be { data: [...] }
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("data", out var data))
                {
                    var json = data.GetRawText();
                    var arr = JsonSerializer.Deserialize<IEnumerable<PosposMember>>(json);
                    return arr ?? Array.Empty<PosposMember>();
                }

                // 2) sometimes API nests under other keys or returns { items: [...] }
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            try
                            {
                                var arr = JsonSerializer.Deserialize<IEnumerable<PosposMember>>(prop.Value.GetRawText());
                                if (arr != null && arr.Any())
                                    return arr;
                            }
                            catch { }
                        }
                    }
                }
            }
            catch
            {
                _logger.LogWarning("Failed to parse POSPOS response JSON");
            }

            return Array.Empty<PosposMember>();
        }

        public async Task<IEnumerable<PosposMember>> GetMembersByIdsAsync(IEnumerable<string> ids)
        {
            // POSPOS API may not expose a batch-get; fallback to fetching all and filtering
            var all = await GetMembersAsync();
            var set = new HashSet<string>(ids ?? Array.Empty<string>());
            return all.Where(m => !string.IsNullOrEmpty(m.id) && set.Contains(m.id));
        }
    }
}
