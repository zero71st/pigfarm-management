using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PigFarmManagement.Server.Infrastructure.Settings;
using System.Linq;
using System.Collections.Generic;
using PigFarmManagement.Shared.Contracts;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Controllers
{
    [ApiController]
    [Route("import/feeds")]
    public class ImportPosFeedsController : ControllerBase
    {
        private readonly System.Net.Http.IHttpClientFactory _httpFactory;
        private readonly Microsoft.Extensions.Options.IOptions<PigFarmManagement.Server.Infrastructure.Settings.PosposOptions> _opts;

        public ImportPosFeedsController(System.Net.Http.IHttpClientFactory httpFactory, Microsoft.Extensions.Options.IOptions<PigFarmManagement.Server.Infrastructure.Settings.PosposOptions> opts)
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
                    // body is a raw JSON array; wrap it in our debug envelope and return as raw JSON content to avoid JsonDocument lifecycle issues
                    var outJson = $"{{\"Status\":{(int)res.StatusCode},\"Data\":{body}}}";
                    return Content(outJson, "application/json");
                }

                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    // Deserialize the array node into a standalone JsonElement[] so it doesn't reference the JsonDocument buffer
                    var outJson = $"{{\"Status\":{(int)res.StatusCode},\"Data\":{data.GetRawText()}}}";
                    return Content(outJson, "application/json");
                }

                // fallback: find first array node
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var outJson = $"{{\"Status\":{(int)res.StatusCode},\"Data\":{prop.Value.GetRawText()}}}";
                        if (!string.IsNullOrWhiteSpace(prop.Value.GetRawText())) return Content(outJson, "application/json");
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

        /// <summary>
        /// Get all POSPOS transactions within a date range (used by client for manual selection).
        /// </summary>
        [HttpGet("pospos/daterange/all")]
        public async Task<IActionResult> GetAllPosPosTransactionsByDateRange([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            var opts = _opts?.Value;
            if (opts == null || (string.IsNullOrWhiteSpace(opts.TransactionsApiBase) && string.IsNullOrWhiteSpace(opts.ApiBase)))
                return BadRequest(new { message = "POSPOS transactions endpoint not configured (TransactionsApiBase or ApiBase)" });

            try
            {
                var allTransactions = new List<PosPosFeedTransaction>();

                // Break down wide date ranges into smaller chunks to work around POSPOS API limitations
                var currentDate = fromDate.Date;
                var endDate = toDate.Date;

                while (currentDate <= endDate)
                {
                    var chunkEndDate = currentDate.AddDays(2); // 3-day chunks
                    if (chunkEndDate > endDate) chunkEndDate = endDate;

                    var chunkTransactions = await GetTransactionsForDateRangeAsync(opts, currentDate, chunkEndDate);
                    allTransactions.AddRange(chunkTransactions);

                    currentDate = chunkEndDate.AddDays(1);
                }

                // Filter by exact date range and remove duplicates
                var filteredTransactions = allTransactions
                    .Where(t => t.Timestamp >= fromDate && t.Timestamp <= toDate)
                    .GroupBy(t => t.Id) // Remove duplicates by _id
                    .Select(g => g.First())
                    .OrderByDescending(t => t.Timestamp)
                    .ToList();

                return Ok(filteredTransactions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch POSPOS transactions", error = ex.Message });
            }
        }

        private async Task<List<PosPosFeedTransaction>> GetTransactionsForDateRangeAsync(PigFarmManagement.Server.Infrastructure.Settings.PosposOptions opts, DateTime fromDate, DateTime toDate)
        {
            var transactions = new List<PosPosFeedTransaction>();
            var page = 1;
            const int pageSize = 200;

            while (true)
            {
                var baseUri = !string.IsNullOrWhiteSpace(opts.TransactionsApiBase) ? opts.TransactionsApiBase : opts.ApiBase;
                var uri = baseUri.TrimEnd('/');
                var hasQuery = uri.Contains('?');
                var q = new List<string>();
                q.Add($"page={page}");
                q.Add($"limit={pageSize}");
                q.Add($"start={fromDate:yyyy-MM-dd}");
                q.Add($"end={toDate:yyyy-MM-dd}");
                uri += (hasQuery ? "&" : "?") + string.Join("&", q);

                var client = _httpFactory.CreateClient();
                var req = new HttpRequestMessage(HttpMethod.Get, uri);
                if (!string.IsNullOrWhiteSpace(opts.ApiKey)) req.Headers.Add("apikey", opts.ApiKey);

                var res = await client.SendAsync(req);
                var body = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    throw new Exception($"API returned {res.StatusCode}: {body}");
                }

                // Parse the response
                List<PosPosFeedTransaction>? pageTransactions = null;
                var trimmed = body.TrimStart();
                if (trimmed.StartsWith("["))
                {
                    pageTransactions = JsonSerializer.Deserialize<List<PosPosFeedTransaction>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                else
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                    {
                        pageTransactions = JsonSerializer.Deserialize<List<PosPosFeedTransaction>>(data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    else
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Array)
                            {
                                pageTransactions = JsonSerializer.Deserialize<List<PosPosFeedTransaction>>(prop.Value.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (pageTransactions != null) break;
                            }
                        }
                    }
                }

                if (pageTransactions == null || pageTransactions.Count == 0)
                {
                    break; // No more transactions
                }

                transactions.AddRange(pageTransactions);

                // If we got fewer than pageSize, we've reached the end for this chunk
                if (pageTransactions.Count < pageSize)
                {
                    break;
                }

                page++;
            }

            return transactions;
        }
    }
}

