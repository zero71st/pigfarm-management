using Microsoft.AspNetCore.Mvc;
using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Services.ExternalServices;

namespace PigFarmManagement.Server.Features.Customers;

public static class CustomerImportEndpoints
{
    public static void MapCustomerImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers/import").WithTags("Customer Import");

        // Core import endpoints
        group.MapPost("/", ImportAllCustomers)
            .WithName("ImportAllCustomers")
            .WithSummary("Import all customers from POSPOS")
            .Produces<object>();

        group.MapPost("/selected", ImportSelectedCustomers)
            .WithName("ImportSelectedCustomers")
            .WithSummary("Import selected customers by IDs")
            .Accepts<IEnumerable<string>>("application/json")
            .Produces<object>();

        group.MapPost("/sync", ManualPosSync)
            .WithName("ManualPosSync")
            .WithSummary("Manual POS sync with location preservation")
            .Produces<object>();

        // Data endpoints
        group.MapGet("/candidates", GetCandidates)
            .WithName("GetCustomerImportCandidates")
            .WithSummary("Get POSPOS members available for import")
            .Produces<object>();

        group.MapGet("/summary", GetImportSummary)
            .WithName("GetCustomerImportSummary")
            .WithSummary("Get last import summary")
            .Produces<object>()
            .Produces(404);

        // Maintenance endpoints
        group.MapPost("/fix-codes", FixCustomerCodes)
            .WithName("FixCustomerCodes")
            .WithSummary("Backfill customer codes using POSPOS member codes")
            .Produces<object>();

        // Debug endpoints
        group.MapGet("/debug/pospos", DebugPosposConfig)
            .WithName("DebugPosposConfig")
            .WithSummary("Debug POSPOS configuration")
            .Produces<object>();

        group.MapGet("/debug/raw", DebugPosposRaw)
            .WithName("DebugPosposRaw")
            .WithSummary("Fetch raw POSPOS API response for debugging")
            .Produces<object>();

        group.MapGet("/debug/inspect", DebugPosposInspect)
            .WithName("DebugPosposInspect")
            .WithSummary("Inspect POSPOS JSON structure")
            .Produces<object>();

        group.MapGet("/debug/members", DebugPosposMembers)
            .WithName("DebugPosposMembers")
            .WithSummary("Debug parsed POSPOS members")
            .Produces<object>();
    }

    /// <summary>
    /// Import all customers from POSPOS
    /// </summary>
    private static async Task<IResult> ImportAllCustomers(
        ICustomerImportService customerImportService,
        [FromQuery] bool persist = false)
    {
        try
        {
            var summary = await customerImportService.ImportAllCustomersAsync(persist);
            return Results.Ok(summary);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to import customers: {ex.Message}");
        }
    }

    /// <summary>
    /// Import selected customers by IDs
    /// </summary>
    private static async Task<IResult> ImportSelectedCustomers(
        ICustomerImportService customerImportService,
        [FromBody] IEnumerable<string> ids,
        [FromQuery] bool persist = false)
    {
        try
        {
            if (ids == null || !ids.Any())
            {
                return Results.BadRequest(new { message = "ids required" });
            }

            var summary = await customerImportService.ImportSelectedCustomersAsync(ids, persist);
            return Results.Ok(summary);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to import selected customers: {ex.Message}");
        }
    }

    /// <summary>
    /// Manual POS sync with location preservation
    /// </summary>
    private static async Task<IResult> ManualPosSync(
        ICustomerImportService customerImportService,
        [FromQuery] bool persist = true)
    {
        try
        {
            var summary = await customerImportService.ImportAllCustomersAsync(persist);
            return Results.Ok(new 
            { 
                success = true,
                message = "Manual POS sync completed successfully",
                summary = summary,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(new 
            { 
                success = false,
                message = "Manual POS sync failed",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            }.ToString());
        }
    }

    /// <summary>
    /// Get POSPOS members available for import
    /// </summary>
    private static async Task<IResult> GetCandidates(
        IPosposMemberClient posposClient)
    {
        try
        {
            var members = await posposClient.GetMembersAsync();

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

            return Results.Ok(projected);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get import candidates: {ex.Message}");
        }
    }

    /// <summary>
    /// Get last import summary
    /// </summary>
    private static IResult GetImportSummary(
        ICustomerImportService customerImportService)
    {
        try
        {
            var summary = customerImportService.LastImportSummary;
            if (summary == null)
            {
                return Results.NotFound(new { message = "No import has been run yet." });
            }
            return Results.Ok(summary);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get import summary: {ex.Message}");
        }
    }

    /// <summary>
    /// Backfill customer codes using POSPOS member codes
    /// </summary>
    private static async Task<IResult> FixCustomerCodes(
        IPosposMemberClient posposClient,
        ICustomerService customerService)
    {
        try
        {
            var members = await posposClient.GetMembersAsync();
            var map = members.Where(m => !string.IsNullOrWhiteSpace(m.Code) && !string.IsNullOrWhiteSpace(m.Id))
                             .ToDictionary(m => m.Id, m => m.Code);

            var customers = await customerService.GetAllCustomersAsync();
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
                        await customerService.UpdateCustomerAsync(updatedCustomer.Id, updatedCustomer.ToUpdateDto());
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
                        await customerService.UpdateCustomerAsync(updatedCustomer.Id, updatedCustomer.ToUpdateDto());
                        updated++;
                        audit.Add(new { CustomerId = cust.Id, Before = cust.ExternalId, After = foundPosId, MatchedBy = "Code" });
                    }
                }
            }

            return Results.Ok(new { Updated = updated, Audit = audit.Take(200) });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to fix customer codes: {ex.Message}");
        }
    }

    /// <summary>
    /// Debug POSPOS configuration
    /// </summary>
    private static IResult DebugPosposConfig(
        Microsoft.Extensions.Options.IOptions<PigFarmManagement.Server.Infrastructure.Settings.PosposOptions> posposOptions)
    {
        try
        {
            var opts = posposOptions?.Value;
            if (opts == null)
                return Results.Ok(new { MemberApiBase = (string?)null, HasApiKey = false });
            
            return Results.Ok(new { 
                MemberApiBase = string.IsNullOrWhiteSpace(opts.MemberApiBase) ? null : opts.MemberApiBase, 
                HasApiKey = !string.IsNullOrWhiteSpace(opts.ApiKey) 
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get POSPOS config: {ex.Message}");
        }
    }

    /// <summary>
    /// Fetch raw POSPOS API response for debugging
    /// </summary>
    private static async Task<IResult> DebugPosposRaw(
        Microsoft.Extensions.Options.IOptions<PigFarmManagement.Server.Infrastructure.Settings.PosposOptions> posposOptions,
        System.Net.Http.IHttpClientFactory httpClientFactory)
    {
        try
        {
            var opts = posposOptions?.Value;
            if (opts == null || string.IsNullOrWhiteSpace(opts.MemberApiBase))
                return Results.BadRequest(new { message = "POSPOS MemberApiBase not configured" });

            var client = httpClientFactory.CreateClient();
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, opts.MemberApiBase);
            if (!string.IsNullOrWhiteSpace(opts.ApiKey)) req.Headers.Add("apikey", opts.ApiKey);

            var res = await client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            return Results.Ok(new { Status = (int)res.StatusCode, Body = body });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to fetch POSPOS API: {ex.Message}");
        }
    }

    /// <summary>
    /// Inspect POSPOS JSON structure for debugging
    /// </summary>
    private static async Task<IResult> DebugPosposInspect(
        Microsoft.Extensions.Options.IOptions<PigFarmManagement.Server.Infrastructure.Settings.PosposOptions> posposOptions,
        System.Net.Http.IHttpClientFactory httpClientFactory)
    {
        try
        {
            var opts = posposOptions?.Value;
            if (opts == null || string.IsNullOrWhiteSpace(opts.MemberApiBase))
                return Results.BadRequest(new { message = "POSPOS MemberApiBase not configured" });

            var client = httpClientFactory.CreateClient();
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, opts.MemberApiBase);
            if (!string.IsNullOrWhiteSpace(opts.ApiKey)) req.Headers.Add("apikey", opts.ApiKey);

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

            if (foundArray == null) 
                return Results.Ok(new { message = "No JSON array of objects found in POSPOS response", Status = (int)res.StatusCode });

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

            return Results.Ok(new { Status = (int)res.StatusCode, Count = count, SampleKeys = sampleKeys.Take(50) });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to fetch/inspect POSPOS API: {ex.Message}");
        }
    }

    /// <summary>
    /// Debug parsed POSPOS members
    /// </summary>
    private static async Task<IResult> DebugPosposMembers(
        IPosposMemberClient posposClient)
    {
        try
        {
            var members = await posposClient.GetMembersAsync();
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
            return Results.Ok(new { Count = members.Count(), Sample = list });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Pospos client failed: {ex.Message}");
        }
    }
}