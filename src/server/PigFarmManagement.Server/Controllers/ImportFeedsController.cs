using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PigFarmManagement.Server.Infrastructure.Settings;
using System.Linq;
using System.Collections.Generic;

namespace PigFarmManagement.Server.Controllers
{
    [ApiController]
    [Route("import/feeds")]
    public class ImportFeedsController : ControllerBase
    {
        private readonly System.Net.Http.IHttpClientFactory _httpFactory;
        private readonly Microsoft.Extensions.Options.IOptions<PigFarmManagement.Server.Infrastructure.Settings.PosposOptions> _opts;

        public ImportFeedsController(System.Net.Http.IHttpClientFactory httpFactory, Microsoft.Extensions.Options.IOptions<PigFarmManagement.Server.Infrastructure.Settings.PosposOptions> opts)
        {
            _httpFactory = httpFactory;
            _opts = opts;
        }

        /// <summary>
        /// Fetch raw POSPOS transactions (no import). Useful for debugging or mapping before importing.
        /// </summary>
        [HttpGet("pospos/raw")]
        public async Task<IActionResult> GetRawPosposTransactions([FromQuery] string? start = null, [FromQuery] string? end = null, [FromQuery] int page = 1, [FromQuery] int limit = 200)
        {
            var opts = _opts?.Value;
            if (opts == null || (string.IsNullOrWhiteSpace(opts.TransactionsApiBase) && string.IsNullOrWhiteSpace(opts.ApiBase)))
                return BadRequest(new { message = "POSPOS transactions endpoint not configured (TransactionsApiBase or ApiBase)" });

            // Prefer TransactionsApiBase when provided, otherwise fall back to ApiBase
            var baseUri = !string.IsNullOrWhiteSpace(opts.TransactionsApiBase) ? opts.TransactionsApiBase : opts.ApiBase;
            // Build URI with query params
            var uri = baseUri.TrimEnd('/');
            // If ApiBase already contains query string, append appropriately
            var hasQuery = uri.Contains('?');
            var q = new List<string>();
            q.Add($"page={page}");
            q.Add($"limit={limit}");
            if (!string.IsNullOrWhiteSpace(start)) q.Add($"start={Uri.EscapeDataString(start)}");
            if (!string.IsNullOrWhiteSpace(end)) q.Add($"end={Uri.EscapeDataString(end)}");
            uri += (hasQuery ? "&" : "?") + string.Join("&", q);

            var client = _httpFactory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Get, uri);
            if (!string.IsNullOrWhiteSpace(opts.ApiKey)) req.Headers.Add("apikey", opts.ApiKey);

            try
            {
                var res = await client.SendAsync(req);
                var body = await res.Content.ReadAsStringAsync();

                // Try return parsed array if possible
                var trimmed = body.TrimStart();
                if (trimmed.StartsWith("["))
                {
                    var arr = JsonSerializer.Deserialize<IEnumerable<JsonElement>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return Ok(new { Status = (int)res.StatusCode, Data = arr ?? Array.Empty<JsonElement>() });
                }

                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    var arr = data.EnumerateArray().Select(e => e).ToArray();
                    return Ok(new { Status = (int)res.StatusCode, Data = arr });
                }

                // fallback: find first array node
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var arr = prop.Value.EnumerateArray().Select(e => e).ToArray();
                        if (arr.Length > 0) return Ok(new { Status = (int)res.StatusCode, Data = arr });
                    }
                }

                // otherwise return raw body for inspection
                return Ok(new { Status = (int)res.StatusCode, Body = body });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch POSPOS transactions", error = ex.Message });
            }
        }
    }
}

