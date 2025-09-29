using System;
using System.Collections.Generic;
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
	private readonly Microsoft.Extensions.Logging.ILogger<PosposFeedClient>? _logger;

		public PosposFeedClient(HttpClient http, IOptions<PosposOptions> opts, Microsoft.Extensions.Logging.ILogger<PosposFeedClient>? logger)
		{
			_http = http ?? throw new ArgumentNullException(nameof(http));
			_opts = opts?.Value ?? new PosposOptions();
			_logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PosposFeedClient>.Instance;

			// allow fallback to environment variables if config keys are not set
			if (string.IsNullOrWhiteSpace(_opts.TransactionsApiBase) && string.IsNullOrWhiteSpace(_opts.ApiBase))
			{
				var env = Environment.GetEnvironmentVariable("POSPOS_TRANSACTIONS_API_BASE") ?? Environment.GetEnvironmentVariable("POSPOS_API_BASE");
				if (!string.IsNullOrWhiteSpace(env)) _opts.TransactionsApiBase = env;
			}
			if (string.IsNullOrWhiteSpace(_opts.ApiKey))
			{
				var envKey = Environment.GetEnvironmentVariable("POSPOS_API_KEY");
				if (!string.IsNullOrWhiteSpace(envKey)) _opts.ApiKey = envKey;
			}

			_logger?.LogInformation("PosposFeedClient configured. TransactionsApiBase='{Base}', ApiKeySet={HasKey}", _opts.TransactionsApiBase ?? _opts.ApiBase, !string.IsNullOrEmpty(_opts.ApiKey));
		}

		private string GetBase() => !string.IsNullOrWhiteSpace(_opts.TransactionsApiBase) ? _opts.TransactionsApiBase : _opts.ApiBase;

		public async Task<(List<PosPosFeedTransaction> Transactions, bool HasMore)> GetTransactionsPageAsync(int page = 1, int limit = 100)
		{
			var baseUrl = GetBase();
			if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute))
			{
				_logger?.LogWarning("Pospos transactions base URL not configured.");
				return (new List<PosPosFeedTransaction>(), false);
			}

			var url = baseUrl;
			// ensure query params
			var sep = url.Contains('?') ? '&' : '?';
			url = string.Concat(url, sep, "page=", page, "&limit=", limit);

			// Log the exact request URL that will be sent to POSPOS
			_logger?.LogDebug("POSPOS request URL (page): {Url}", url);

			var req = new HttpRequestMessage(HttpMethod.Get, url);
			if (!string.IsNullOrWhiteSpace(_opts.ApiKey)) req.Headers.Add("apikey", _opts.ApiKey);

			HttpResponseMessage res;
			try
			{
				res = await _http.SendAsync(req);
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Failed to call POSPOS transactions API");
				return (new List<PosPosFeedTransaction>(), false);
			}

			if (!res.IsSuccessStatusCode)
			{
				var txt = await res.Content.ReadAsStringAsync();
				_logger?.LogWarning("POSPOS transactions returned non-success {Status}. Body: {Body}", res.StatusCode, txt);
				return (new List<PosPosFeedTransaction>(), false);
			}

			var body = await res.Content.ReadAsStringAsync();
			// Log raw response body for debugging (trim if extremely large)
			try
			{
				if (!string.IsNullOrEmpty(body))
				{
					_logger?.LogDebug("POSPOS response (page) length={Length}", body.Length);
					_logger?.LogTrace("POSPOS response (page) body: {Body}", body);
				}
			}
			catch (Exception logEx)
			{
				_logger?.LogWarning(logEx, "Failed to log POSPOS response body");
			}

			try
			{
				// many POSPOS endpoints return { success:1, data:[...] }
				var trimmed = body.TrimStart();
				List<PosPosFeedTransaction>? items = null;

				if (trimmed.StartsWith("["))
				{
					items = JsonSerializer.Deserialize<List<PosPosFeedTransaction>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
				}
				else
				{
					using var doc = JsonDocument.Parse(body);
					if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("data", out var data))
					{
						items = JsonSerializer.Deserialize<List<PosPosFeedTransaction>>(data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					}
					else
					{
						// find first array property
						foreach (var p in doc.RootElement.EnumerateObject())
						{
							if (p.Value.ValueKind == JsonValueKind.Array)
							{
								try { items = JsonSerializer.Deserialize<List<PosPosFeedTransaction>>(p.Value.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); break; } catch { }
							}
						}
					}
				}

				items ??= new List<PosPosFeedTransaction>();

				// Normalize buyer detail: ensure BuyerDetail.Code is populated (fallback to KeyCardId) and trimmed
				foreach (var it in items)
				{
					if (it?.BuyerDetail != null)
					{
						if (string.IsNullOrWhiteSpace(it.BuyerDetail.Code) && !string.IsNullOrWhiteSpace(it.BuyerDetail.KeyCardId))
						{
							it.BuyerDetail.Code = it.BuyerDetail.KeyCardId?.Trim() ?? "";
						}
						it.BuyerDetail.Code = it.BuyerDetail.Code?.Trim() ?? "";
						it.BuyerDetail.KeyCardId = it.BuyerDetail.KeyCardId?.Trim() ?? "";
					}
				}

				// Determine if there may be more pages. If returned count == limit, assume there's another page.
				var hasMore = items.Count >= limit;
				return (items, hasMore);
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Failed to parse POSPOS transactions response");
				return (new List<PosPosFeedTransaction>(), false);
			}
		}

		public async Task<List<PosPosFeedTransaction>> GetTransactionsByDateRangeAsync(DateTime from, DateTime to, int pageSize = 100)
		{
			var results = new List<PosPosFeedTransaction>();
			var page = 1;
			while (true)
			{
				var baseUrl = GetBase();
				if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute))
				{
					_logger?.LogWarning("Pospos transactions base URL not configured.");
					break;
				}

				// Build url with date filters. POSPOS API uses start/end parameters with YYYY-MM-DD format.
				var startDate = from.ToString("yyyy-MM-dd");
				var endDate = to.ToString("yyyy-MM-dd");
				var sep = baseUrl.Contains('?') ? '&' : '?';
				var url = string.Concat(baseUrl, sep, "page=", page, "&limit=", pageSize, "&start=", startDate, "&end=", endDate);

				// Log the exact request URL used for date-range fetches
				_logger?.LogDebug("POSPOS request URL (daterange): {Url}", url);

				var req = new HttpRequestMessage(HttpMethod.Get, url);
				if (!string.IsNullOrWhiteSpace(_opts.ApiKey)) req.Headers.Add("apikey", _opts.ApiKey);

				HttpResponseMessage res;
				try
				{
					res = await _http.SendAsync(req);
				}
				catch (Exception ex)
				{
					_logger?.LogError(ex, "Failed to call POSPOS transactions API for date range");
					break;
				}

				if (!res.IsSuccessStatusCode)
				{
					var txt = await res.Content.ReadAsStringAsync();
					_logger?.LogWarning("POSPOS transactions returned non-success {Status}. Body: {Body}", res.StatusCode, txt);
					break;
				}

				var body = await res.Content.ReadAsStringAsync();
				try
				{
					// Log raw response body for debugging (trim if extremely large)
					if (!string.IsNullOrEmpty(body))
					{
						_logger?.LogDebug("POSPOS response (daterange) length={Length}", body.Length);
						_logger?.LogTrace("POSPOS response (daterange) body: {Body}", body);
					}
				}
				catch (Exception logEx)
				{
					_logger?.LogWarning(logEx, "Failed to log POSPOS response body (daterange)");
				}
				try
				{
					List<PosPosFeedTransaction>? pageItems = null;
					var trimmed = body.TrimStart();
					if (trimmed.StartsWith("["))
					{
						pageItems = JsonSerializer.Deserialize<List<PosPosFeedTransaction>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					}
					else
					{
						using var doc = JsonDocument.Parse(body);
						if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("data", out var data))
						{
							pageItems = JsonSerializer.Deserialize<List<PosPosFeedTransaction>>(data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
						}
						else
						{
							foreach (var p in doc.RootElement.EnumerateObject())
							{
								if (p.Value.ValueKind == JsonValueKind.Array)
								{
									try { pageItems = JsonSerializer.Deserialize<List<PosPosFeedTransaction>>(p.Value.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); break; } catch { }
								}
							}
						}
					}

					if (pageItems == null || pageItems.Count == 0) break;

					// Normalize buyer detail on paged results as well
					foreach (var it in pageItems)
					{
						if (it?.BuyerDetail != null)
						{
							if (string.IsNullOrWhiteSpace(it.BuyerDetail.Code) && !string.IsNullOrWhiteSpace(it.BuyerDetail.KeyCardId))
							{
								it.BuyerDetail.Code = it.BuyerDetail.KeyCardId?.Trim() ?? "";
							}
							it.BuyerDetail.Code = it.BuyerDetail.Code?.Trim() ?? "";
							it.BuyerDetail.KeyCardId = it.BuyerDetail.KeyCardId?.Trim() ?? "";
						}
					}

					results.AddRange(pageItems);

					if (pageItems.Count < pageSize) break; // last page
					page++;
				}
				catch (Exception ex)
				{
					_logger?.LogError(ex, "Failed to parse POSPOS transactions response while paging");
					break;
				}
			}

			return results;
		}
	}
}

