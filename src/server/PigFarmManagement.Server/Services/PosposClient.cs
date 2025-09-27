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

        public PosposClient(HttpClient http, IOptions<PosposOptions> opts)
        {
            _http = http;
            _opts = opts.Value;
        }

        public async Task<IEnumerable<PosposMember>> GetMembersAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _opts.ApiBase);
            // POSPOS expects 'apikey' header per user guidance
            request.Headers.Add("apikey", _opts.ApiKey);

            var res = await _http.SendAsync(request);
            res.EnsureSuccessStatusCode();
            var s = await res.Content.ReadAsStringAsync();
            try
            {
                // Attempt to parse common shapes: array or { data: [...] }
                if (s.TrimStart().StartsWith("["))
                {
                    var arr = JsonSerializer.Deserialize<IEnumerable<PosposMember>>(s);
                    return arr ?? Array.Empty<PosposMember>();
                }
                else
                {
                    using var doc = JsonDocument.Parse(s);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var json = data.GetRawText();
                        var arr = JsonSerializer.Deserialize<IEnumerable<PosposMember>>(json);
                        return arr ?? Array.Empty<PosposMember>();
                    }
                }
            }
            catch
            {
                // fallback empty
            }

            return Array.Empty<PosposMember>();
        }
    }
}
