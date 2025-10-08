using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PigFarmManagement.Server.Infrastructure.Settings;
using PigFarmManagement.Shared.Domain.External;

namespace PigFarmManagement.Server.Services.ExternalServices
{
    public class PosposMemberClient : IPosposMemberClient
    {
        private readonly HttpClient _http;
        private readonly PosposOptions _opts;
        private readonly Microsoft.Extensions.Logging.ILogger<PosposMemberClient> _logger;

        public PosposMemberClient(HttpClient http, IOptions<PosposOptions> opts, Microsoft.Extensions.Logging.ILogger<PosposMemberClient> logger)
        {
            _http = http;
            _opts = opts.Value;
            // If configuration wasn't provided via appsettings, allow environment variables as a fallback
            if (string.IsNullOrWhiteSpace(_opts.MemberApiBase))
            {
                var envBase = Environment.GetEnvironmentVariable("POSPOS_MEMBER_API_BASE");
                if (!string.IsNullOrWhiteSpace(envBase))
                    _opts.MemberApiBase = envBase;
                // Fallback to ProductApiBase if MemberApiBase not set
                else if (!string.IsNullOrWhiteSpace(_opts.ProductApiBase))
                    _opts.MemberApiBase = _opts.ProductApiBase;
            }
            if (string.IsNullOrWhiteSpace(_opts.ApiKey))
            {
                var envKey = Environment.GetEnvironmentVariable("POSPOS_API_KEY");
                if (!string.IsNullOrWhiteSpace(envKey))
                    _opts.ApiKey = envKey;
            }
            logger.LogInformation("PosposMemberClient configured. MemberApiBase='{MemberApiBase}', ApiKeySet={HasKey}", _opts.MemberApiBase, !string.IsNullOrEmpty(_opts.ApiKey));
            _logger = logger;
        }

        public async Task<IEnumerable<PosposMember>> GetMembersAsync()
        {
            // If MemberApiBase is not configured or not an absolute URI, log and return empty list
            if (string.IsNullOrWhiteSpace(_opts.MemberApiBase) || !Uri.IsWellFormedUriString(_opts.MemberApiBase, UriKind.Absolute))
            {
                _logger.LogWarning("POSPOS MemberApiBase is not configured or invalid ('{MemberApiBase}'). No candidates will be returned. Set POSPOS_MEMBER_API_BASE and POSPOS_API_KEY to use the real API.", _opts.MemberApiBase);
                return Array.Empty<PosposMember>();
            }

            var request = new HttpRequestMessage(HttpMethod.Get, _opts.MemberApiBase);
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
                    if (arr != null)
                    {
                        // ensure FirstName/LastName populated when API returns 'name' fields
                        return arr.Select(m => EnsureNames(m));
                    }
                    return Array.Empty<PosposMember>();
                }

                using var doc = JsonDocument.Parse(s);
                // 1) response may be { data: [...] }
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("data", out var data))
                {
                    var json = data.GetRawText();
                    var arr = JsonSerializer.Deserialize<IEnumerable<PosposMember>>(json);
                    return (arr ?? Array.Empty<PosposMember>()).Select(m => EnsureNames(m));
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
                                    return arr.Select(m => EnsureNames(m));
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

            // local helper to populate names
            PosposMember EnsureNames(PosposMember m)
            {
                // If the API used lowercase 'name' in JSON, try to read it via JsonDocument fallback
                if (string.IsNullOrWhiteSpace(m.FirstName) && string.IsNullOrWhiteSpace(m.LastName))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(s);
                        // Try to find the object with matching id
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var el in prop.Value.EnumerateArray())
                                {
                                    if (el.ValueKind != JsonValueKind.Object) continue;
                                    // Match either "id" or "_id" to the deserialized member Id
                                    var matched = false;
                                    if (el.TryGetProperty("id", out var idProp) && idProp.GetString() == m.Id) matched = true;
                                    if (!matched && el.TryGetProperty("_id", out var idProp2) && idProp2.GetString() == m.Id) matched = true;
                                    if (!matched) continue;

                                    if (el.TryGetProperty("firstName", out var fn)) m.FirstName = fn.GetString() ?? "";
                                    if (el.TryGetProperty("lastName", out var ln)) m.LastName = ln.GetString() ?? "";
                                    if (string.IsNullOrWhiteSpace(m.FirstName) && el.TryGetProperty("name", out var nameProp))
                                    {
                                        var nm = nameProp.GetString() ?? "";
                                        var parts = nm.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                                        if (parts.Length == 1) m.FirstName = parts[0];
                                        else if (parts.Length >= 2) { m.FirstName = parts[0]; m.LastName = parts[1]; }
                                    }

                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                }
                return m;
            }

            return Array.Empty<PosposMember>();
        }

        public async Task<IEnumerable<PosposMember>> GetMembersByIdsAsync(IEnumerable<string> ids)
        {
            // POSPOS API may not expose a batch-get; fallback to fetching all and filtering
            var all = await GetMembersAsync();
            var set = new HashSet<string>(ids ?? Array.Empty<string>());
            return all.Where(m => !string.IsNullOrEmpty(m.Id) && set.Contains(m.Id));
        }
    }
}