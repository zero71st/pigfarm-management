using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PigFarmManagement.Server.Services;

namespace PigFarmManagement.Server.Controllers
{
    [ApiController]
    [Route("import")]
    public class ImportController : ControllerBase
    {
        private readonly IPosposImporter _importer;
        private readonly IPosposMemberClient _posposClient;
        private readonly Microsoft.Extensions.Options.IOptions<PigFarmManagement.Server.Infrastructure.Settings.PosposOptions> _posposOptions;
        private readonly System.Net.Http.IHttpClientFactory _httpClientFactory;
        private readonly Features.Customers.ICustomerService _customerService;

        public ImportController(IPosposImporter importer, IPosposMemberClient posposClient, Microsoft.Extensions.Options.IOptions<PigFarmManagement.Server.Infrastructure.Settings.PosposOptions> posposOptions, System.Net.Http.IHttpClientFactory httpClientFactory, Features.Customers.ICustomerService customerService)
        {
            _importer = importer;
            _posposClient = posposClient;
            _posposOptions = posposOptions;
            _httpClientFactory = httpClientFactory;
            _customerService = customerService;
        }

        /// <summary>
        /// Trigger a one-shot import from POSPOS. By default does not persist mapping unless ?persist=true
        /// Returns the import summary JSON.
        /// </summary>
        [HttpPost("customers")]
        public async Task<IActionResult> ImportCustomers([FromQuery] bool persist = false)
        {
            var summary = await _importer.RunImportAsync(persist);
            return Ok(summary);
        }

        /// <summary>
        /// List POSPOS members available for import (candidates).
        /// </summary>
        [HttpGet("customers/candidates")]
        public async Task<IActionResult> GetCandidates()
        {
            var members = await _posposClient.GetMembersAsync();

            // Project to a shape the Blazor client expects (PascalCase properties)
            var projected = members.Select(m => new
            {
                Id = m.Id,
                Code = string.IsNullOrWhiteSpace(m.Code) ? m.Id : m.Code,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Phone = string.IsNullOrWhiteSpace(m.Phone) ? m.PhoneNumber : m.Phone,
                Email = m.Email,
                Address = m.Address,
                KeyCardId = m.KeyCardId,
                ExternalId = m.Id,
                Sex = m.Sex,
                Zipcode = m.Zipcode,
                CreatedAt = m.CreatedAt
            });

            return Ok(projected);
        }

        // Temporary debug endpoint to show Pospos configuration presence (does not reveal API key)
        [HttpGet("debug/pospos")]
        public IActionResult DebugPospos()
        {
            var opts = _posposOptions?.Value;
            if (opts == null)
                return Ok(new { ApiBase = (string?)null, HasApiKey = false });
            return Ok(new { ApiBase = string.IsNullOrWhiteSpace(opts.ApiBase) ? null : opts.ApiBase, HasApiKey = !string.IsNullOrWhiteSpace(opts.ApiKey) });
        }

        // Temporary debug endpoint: fetch raw POSPOS API response body so we can inspect its JSON shape
        [HttpGet("debug/raw")]
        public async Task<IActionResult> DebugPosposRaw()
        {
            var opts = _posposOptions?.Value;
            if (opts == null || string.IsNullOrWhiteSpace(opts.ApiBase))
                return BadRequest(new { message = "POSPOS ApiBase not configured" });

            var client = _httpClientFactory.CreateClient();
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, opts.ApiBase);
            if (!string.IsNullOrWhiteSpace(opts.ApiKey)) req.Headers.Add("apikey", opts.ApiKey);

            try
            {
                var res = await client.SendAsync(req);
                var body = await res.Content.ReadAsStringAsync();
                return Ok(new { Status = (int)res.StatusCode, Body = body });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch POSPOS API", error = ex.Message });
            }
        }

        // Inspect the raw JSON and return diagnostics: first array length and sample element property names
        [HttpGet("debug/inspect")]
        public async Task<IActionResult> DebugPosposInspect()
        {
            var opts = _posposOptions?.Value;
            if (opts == null || string.IsNullOrWhiteSpace(opts.ApiBase))
                return BadRequest(new { message = "POSPOS ApiBase not configured" });

            var client = _httpClientFactory.CreateClient();
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, opts.ApiBase);
            if (!string.IsNullOrWhiteSpace(opts.ApiKey)) req.Headers.Add("apikey", opts.ApiKey);

            try
            {
                var res = await client.SendAsync(req);
                var body = await res.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(body);

                // Recursive search for first array with object elements
                System.Text.Json.JsonElement? foundArray = null;

                void Walk(System.Text.Json.JsonElement el)
                {
                    if (foundArray != null) return;
                    if (el.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in el.EnumerateArray())
                        {
                            if (item.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                foundArray = el;
                                return;
                            }
                        }
                    }
                    else if (el.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        foreach (var p in el.EnumerateObject()) Walk(p.Value);
                    }
                }

                Walk(doc.RootElement);

                if (foundArray == null) return Ok(new { message = "No JSON array of objects found in POSPOS response", Status = (int)res.StatusCode });

                var arr = foundArray.Value;
                int count = 0;
                List<string> sampleKeys = new List<string>();
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        count++;
                        if (sampleKeys.Count == 0)
                        {
                            foreach (var p in item.EnumerateObject()) sampleKeys.Add(p.Name);
                        }
                    }
                }

                return Ok(new { Status = (int)res.StatusCode, Count = count, SampleKeys = sampleKeys.Take(50) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch/inspect POSPOS API", error = ex.Message });
            }
        }

        // Debug: call the PosposClient and return parsed members (count + sample)
        [HttpGet("debug/members")]
        public async Task<IActionResult> DebugPosposMembers()
        {
            try
            {
                var members = await _posposClient.GetMembersAsync();
                var list = members.Take(10).Select(m => new
                {
                    Id = m.Id,
                    Code = m.Code,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Phone = string.IsNullOrWhiteSpace(m.Phone) ? m.PhoneNumber : m.Phone,
                    Email = m.Email,
                    Address = m.Address,
                    KeyCardId = m.KeyCardId
                });
                return Ok(new { Count = members.Count(), Sample = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Pospos client failed", error = ex.Message });
            }
        }

        /// <summary>
        /// Import a specific list of POSPOS member ids.
        /// </summary>
        [HttpPost("customers/selected")]
        public async Task<IActionResult> ImportSelected([FromBody] IEnumerable<string> ids, [FromQuery] bool persist = false)
        {
            if (ids == null)
                return BadRequest(new { message = "ids required" });

            var summary = await _importer.RunImportSelectedAsync(ids, persist);
            return Ok(summary);
        }

        /// <summary>
        /// Backfill customer.Code using POSPOS member 'Code' when available.
        /// Matches customers by ExternalId -> POSPOS member Id. This fixes older imports
        /// where Customer.Code was set to the external GUID/id instead of the POSPOS code.
        /// </summary>
        [HttpPost("customers/fix-codes")]
        public async Task<IActionResult> FixCustomerCodes()
        {
            try
            {
                var members = await _posposClient.GetMembersAsync();
                var map = members.Where(m => !string.IsNullOrWhiteSpace(m.Code) && !string.IsNullOrWhiteSpace(m.Id))
                                 .ToDictionary(m => m.Id, m => m.Code);

                var customers = await _customerService.GetAllCustomersAsync();
                int updated = 0;
                var audit = new List<object>();

                // Build reverse map: posCode -> posId for matching by code
                var codeToId = map.ToDictionary(kv => kv.Value, kv => kv.Key);

                foreach (var cust in customers)
                {
                    // Prefer matching by ExternalId -> POSPOS id
                    if (!string.IsNullOrWhiteSpace(cust.ExternalId) && map.TryGetValue(cust.ExternalId, out var posCode))
                    {
                        if (!string.Equals(cust.Code, posCode, StringComparison.Ordinal))
                        {
                            var updatedCustomer = cust with { Code = posCode };
                            await _customerService.UpdateCustomerAsync(updatedCustomer);
                            updated++;
                            audit.Add(new { CustomerId = cust.Id, Before = cust.Code, After = posCode, MatchedBy = "ExternalId" });
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(cust.Code) && codeToId.TryGetValue(cust.Code, out var foundPosId))
                    {
                        // If customer has no ExternalId but their Code matches a POSPOS member code,
                        // set ExternalId so future exchanges can match directly.
                        if (string.IsNullOrWhiteSpace(cust.ExternalId) || !string.Equals(cust.ExternalId, foundPosId, StringComparison.Ordinal))
                        {
                            var updatedCustomer = cust with { ExternalId = foundPosId };
                            await _customerService.UpdateCustomerAsync(updatedCustomer);
                            updated++;
                            audit.Add(new { CustomerId = cust.Id, Before = cust.ExternalId, After = foundPosId, MatchedBy = "Code" });
                        }
                    }
                }

                return Ok(new { Updated = updated, Audit = audit.Take(200) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fix customer codes", error = ex.Message });
            }
        }

        /// <summary>
        /// Get the last import summary produced by the in-memory importer.
        /// </summary>
        [HttpGet("customers/summary")]
        public IActionResult GetSummary()
        {
            var summary = _importer.LastSummary;
            if (summary == null)
                return NotFound(new { message = "No import has been run yet." });
            return Ok(summary);
        }
    }
}
