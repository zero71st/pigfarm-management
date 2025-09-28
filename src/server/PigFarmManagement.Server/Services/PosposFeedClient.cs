using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PigFarmManagement.Server.Infrastructure.Settings;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Services
{
    public class PosposFeedClient : IPosposFeedClient
    {
        private readonly HttpClient _http;
        private readonly PosposOptions _opts;
        private readonly Microsoft.Extensions.Logging.ILogger<PosposFeedClient> _logger;

        public PosposFeedClient(HttpClient http, IOptions<PosposOptions> opts, Microsoft.Extensions.Logging.ILogger<PosposFeedClient> logger)
        {
            _http = http;
            _opts = opts.Value;
            if (string.IsNullOrWhiteSpace(_opts.ApiBase))
            {
                var envBase = Environment.GetEnvironmentVariable("POSPOS_API_BASE");
                if (!string.IsNullOrWhiteSpace(envBase)) _opts.ApiBase = envBase;
            }
            if (string.IsNullOrWhiteSpace(_opts.ApiKey))
            {
                var envKey = Environment.GetEnvironmentVariable("POSPOS_API_KEY");
                if (!string.IsNullOrWhiteSpace(envKey)) _opts.ApiKey = envKey;
            }
            _logger = logger;
        }

        public async Task<IEnumerable<PosPosFeedTransaction>> GetTransactionsAsync(string? start = null, string? end = null, int page = 1, int limit = 200)
        {
            if (string.IsNullOrWhiteSpace(_opts.ApiBase) || !Uri.IsWellFormedUriString(_opts.ApiBase, UriKind.Absolute))
            {
                _logger.LogWarning("POSPOS ApiBase not configured or invalid. Returning empty transactions.");
                return Array.Empty<PosPosFeedTransaction>();
            }

            var uri = _opts.ApiBase.TrimEnd('/') + $"?page={page}&limit={limit}";
            if (!string.IsNullOrWhiteSpace(start)) uri += $"&start={Uri.EscapeDataString(start)}";
            if (!string.IsNullOrWhiteSpace(end)) uri += $"&end={Uri.EscapeDataString(end)}";

            var req = new HttpRequestMessage(HttpMethod.Get, uri);
            if (!string.IsNullOrWhiteSpace(_opts.ApiKey)) req.Headers.Add("apikey", _opts.ApiKey);

            try
            {
                var res = await _http.SendAsync(req);
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("POSPOS returned status {Status}", res.StatusCode);
                    return Array.Empty<PosPosFeedTransaction>();
                }

                var body = await res.Content.ReadAsStringAsync();

                // try to parse common shapes: array or { data: [...] }
                var trimmed = body.TrimStart();
                if (trimmed.StartsWith("["))
                {
                    var arr = JsonSerializer.Deserialize<IEnumerable<PosPosFeedTransaction>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return arr ?? Array.Empty<PosPosFeedTransaction>();
                }

                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("data", out var data))
                {
                    var json = data.GetRawText();
                    var arr = JsonSerializer.Deserialize<IEnumerable<PosPosFeedTransaction>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return arr ?? Array.Empty<PosPosFeedTransaction>();
                }

                // otherwise, search for first array node
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        try
                        {
                            var arr = JsonSerializer.Deserialize<IEnumerable<PosPosFeedTransaction>>(prop.Value.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (arr != null && arr.Any()) return arr;
                        }
                        catch { }
                    }
                }

                _logger.LogWarning("Failed to locate transactions array in POSPOS response");
                return Array.Empty<PosPosFeedTransaction>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch transactions from POSPOS");
                return Array.Empty<PosPosFeedTransaction>();
            }
        }
    }
}
