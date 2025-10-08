using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PigFarmManagement.Server.Infrastructure.Settings;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Services.ExternalServices
{
	public class PosposTransactionClient : IPosposTransactionClient
	{
		private readonly HttpClient _http;
		private readonly PosposOptions _opts;
	private readonly Microsoft.Extensions.Logging.ILogger<PosposTransactionClient>? _logger;

		public PosposTransactionClient(HttpClient http, IOptions<PosposOptions> opts, Microsoft.Extensions.Logging.ILogger<PosposTransactionClient>? logger)
		{
			_http = http ?? throw new ArgumentNullException(nameof(http));
			_opts = opts?.Value ?? new PosposOptions();
			_logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PosposTransactionClient>.Instance;

			// allow fallback to environment variables if config keys are not set
			if (string.IsNullOrWhiteSpace(_opts.TransactionsApiBase) && string.IsNullOrWhiteSpace(_opts.ProductApiBase))
			{
				var env = Environment.GetEnvironmentVariable("POSPOS_TRANSACTIONS_API_BASE") ?? Environment.GetEnvironmentVariable("POSPOS_PRODUCT_API_BASE") ?? Environment.GetEnvironmentVariable("POSPOS_API_BASE");
				if (!string.IsNullOrWhiteSpace(env)) _opts.TransactionsApiBase = env;
			}
			if (string.IsNullOrWhiteSpace(_opts.ApiKey))
			{
				var envKey = Environment.GetEnvironmentVariable("POSPOS_API_KEY");
				if (!string.IsNullOrWhiteSpace(envKey)) _opts.ApiKey = envKey;
			}

			_logger?.LogInformation("PosposTransactionClient configured. TransactionsApiBase='{Base}', ApiKeySet={HasKey}", _opts.TransactionsApiBase ?? _opts.ProductApiBase, !string.IsNullOrEmpty(_opts.ApiKey));
		}

		private string GetBase() => !string.IsNullOrWhiteSpace(_opts.TransactionsApiBase) ? _opts.TransactionsApiBase : _opts.ProductApiBase;

		public async Task<List<PosPosTransaction>> GetTransactionsByDateRangeAsync(DateTime from, DateTime to, int pageSize = 300)
		{
			var results = new List<PosPosTransaction>();
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
					List<PosPosTransaction>? pageItems = null;
					var trimmed = body.TrimStart();
					var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

					// First try the convenient, attribute-driven deserialization
					try
					{
						if (trimmed.StartsWith("["))
						{
							pageItems = JsonSerializer.Deserialize<List<PosPosTransaction>>(body, jsonOptions);
						}
						else
						{
							using var doc = JsonDocument.Parse(body);
							if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("data", out var data))
							{
								pageItems = JsonSerializer.Deserialize<List<PosPosTransaction>>(data.GetRawText(), jsonOptions);
							}
							else
							{
								foreach (var p in doc.RootElement.EnumerateObject())
								{
									if (p.Value.ValueKind == JsonValueKind.Array)
									{
										try { pageItems = JsonSerializer.Deserialize<List<PosPosTransaction>>(p.Value.GetRawText(), jsonOptions); break; } catch { }
									}
								}
							}
						}
					}
					catch (JsonException)
					{
						// fall through to manual resilient parsing below
						pageItems = null;
					}

					// If direct deserialization failed or produced no items, attempt a tolerant manual parse
					if (pageItems == null || pageItems.Count == 0)
					{
						try
						{
							using var doc = JsonDocument.Parse(body);
							JsonElement arrayEl = default;
							if (doc.RootElement.ValueKind == JsonValueKind.Array)
							{
								arrayEl = doc.RootElement;
							}
							else if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("data", out var data))
							{
								arrayEl = data;
							}
							else
							{
								foreach (var p in doc.RootElement.EnumerateObject())
								{
									if (p.Value.ValueKind == JsonValueKind.Array) { arrayEl = p.Value; break; }
								}
							}

							if (arrayEl.ValueKind == JsonValueKind.Array)
							{
								pageItems = new List<PosPosTransaction>();
								foreach (var txEl in arrayEl.EnumerateArray())
								{
									var tx = new PosPosTransaction();
									if (txEl.TryGetProperty("_id", out var idEl) && idEl.ValueKind == JsonValueKind.String) tx.Id = idEl.GetString() ?? "";
									if (txEl.TryGetProperty("code", out var codeEl) && codeEl.ValueKind == JsonValueKind.String) tx.Code = codeEl.GetString() ?? "";
									if (txEl.TryGetProperty("timestamp", out var tsEl))
									{
										try
										{
											if (tsEl.ValueKind == JsonValueKind.Number && tsEl.TryGetInt64(out var unix))
											{
												// some POSPOS payloads may use unix epoch seconds
												tx.Timestamp = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
											}
											else if (tsEl.ValueKind == JsonValueKind.String && DateTime.TryParse(tsEl.GetString(), out var parsed))
											{
												tx.Timestamp = parsed;
											}
										}
										catch { }
									}

									// BuyerDetail
									if (txEl.TryGetProperty("buyer_detail", out var buyerEl) && buyerEl.ValueKind == JsonValueKind.Object)
									{
										var bd = new PosPosBuyerDetail();
										if (buyerEl.TryGetProperty("code", out var bcode) && bcode.ValueKind == JsonValueKind.String) bd.Code = bcode.GetString() ?? "";
										if (buyerEl.TryGetProperty("firstname", out var bf) && bf.ValueKind == JsonValueKind.String) bd.FirstName = bf.GetString() ?? "";
										if (buyerEl.TryGetProperty("lastname", out var bl) && bl.ValueKind == JsonValueKind.String) bd.LastName = bl.GetString() ?? "";
										if (buyerEl.TryGetProperty("key_card_id", out var bk) && bk.ValueKind == JsonValueKind.String) bd.KeyCardId = bk.GetString() ?? "";
										tx.BuyerDetail = bd;
									}

									// InvoiceReference
									if (txEl.TryGetProperty("reference_tax_invoice_abbreviate", out var invEl) && invEl.ValueKind == JsonValueKind.Object)
									{
										var ir = new PosPosInvoiceReference();
										if (invEl.TryGetProperty("code", out var ic) && ic.ValueKind == JsonValueKind.String) ir.Code = ic.GetString() ?? "";
										tx.InvoiceReference = ir;
									}

									// Totals
									if (txEl.TryGetProperty("sub_total", out var st) && st.ValueKind == JsonValueKind.Number && st.TryGetDecimal(out var sub)) tx.SubTotal = sub;
									if (txEl.TryGetProperty("grand_total", out var gt) && gt.ValueKind == JsonValueKind.Number && gt.TryGetDecimal(out var grand)) tx.GrandTotal = grand;
									if (txEl.TryGetProperty("status", out var stt) && stt.ValueKind == JsonValueKind.String) tx.Status = stt.GetString() ?? "";

									// Order list
									if (txEl.TryGetProperty("order_list", out var ol) && ol.ValueKind == JsonValueKind.Array)
									{
										var items = new List<PosPosOrderItem>();
										foreach (var itemEl in ol.EnumerateArray())
										{
											var it = new PosPosOrderItem();
											if (itemEl.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String) it.Name = n.GetString() ?? "";
											if (itemEl.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String) it.Code = c.GetString() ?? "";

											// numeric tolerant parsing for stock/price/total/cost
											it.Stock = ReadDecimalTolerant(itemEl, "stock");
											it.Price = ReadDecimalTolerant(itemEl, "price");
											it.TotalPriceIncludeDiscount = ReadDecimalTolerant(itemEl, "total_price_include_discount");
											it.CostDiscountPrice = ReadDecimalTolerant(itemEl, "cost_discount_price");

											// note_in_order array
											if (itemEl.TryGetProperty("note_in_order", out var notes) && notes.ValueKind == JsonValueKind.Array)
											{
												var list = new List<string>();
												foreach (var ne in notes.EnumerateArray()) if (ne.ValueKind == JsonValueKind.String) list.Add(ne.GetString() ?? "");
												it.NoteInOrder = list;
											}

											items.Add(it);
										}
										tx.OrderList = items;
									}

									pageItems.Add(tx);
								}
							}
						}
						catch (Exception exManual)
						{
							_logger?.LogError(exManual, "Manual fallback parsing of POSPOS response failed");
							pageItems = null;
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

		/// <summary>
		/// Read a decimal from a JsonElement property in a tolerant way: accepts number, numeric-string, or falls back to 0.
		/// </summary>
		private static decimal ReadDecimalTolerant(JsonElement el, string propertyName)
		{
			try
			{
				if (!el.TryGetProperty(propertyName, out var p)) return 0m;
				if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var d)) return d;
				if (p.ValueKind == JsonValueKind.String)
				{
					var s = p.GetString();
					if (decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed)) return parsed;
					if (!string.IsNullOrWhiteSpace(s) && decimal.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsed)) return parsed;
				}
				return 0m;
			}
			catch { return 0m; }
		}
	}
}