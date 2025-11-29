using Microsoft.AspNetCore.Mvc;
using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Services.ExternalServices;

namespace PigFarmManagement.Server.Features.Customers;

public static class CustomerImportEndpoints
{
    public static void MapCustomerImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers/import").WithTags("Customer Import").RequireAuthorization();

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
    }

    /// <summary>
    /// Import all customers from POSPOS
    /// </summary>
    private static async Task<IResult> ImportAllCustomers(
        ICustomerImportService customerImportService)
    {
        try
        {
            var summary = await customerImportService.ImportAllCustomersAsync();
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
        [FromBody] IEnumerable<string> ids)
    {
        try
        {
            if (ids == null || !ids.Any())
            {
                return Results.BadRequest(new { message = "ids required" });
            }

            var summary = await customerImportService.ImportSelectedCustomersAsync(ids);
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
        ICustomerImportService customerImportService)
    {
        try
        {
            var summary = await customerImportService.ImportAllCustomersAsync();
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
        IPosposMemberClient posposClient,
        [FromQuery] string source = "all")
    {
        try
        {
            // Validate source parameter
            if (!string.IsNullOrWhiteSpace(source) && 
                !source.Equals("pospos", StringComparison.OrdinalIgnoreCase) && 
                !source.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error = "Invalid source. Must be 'pospos' or 'all'." });
            }

            var members = await posposClient.GetMembersAsync();

            // Apply source filtering
            if (source.Equals("pospos", StringComparison.OrdinalIgnoreCase))
            {
                // Return only the latest member by CreatedAt, with Id as tiebreaker
                members = members
                    .OrderByDescending(m => m.CreatedAt)
                    .ThenByDescending(m => m.Id)
                    .Take(1)
                    .ToList();
            }
            // If source is "all" or omitted, keep all members (existing behavior)

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
        catch (HttpRequestException ex)
        {
            // POSPOS service unavailable or network error
            return Results.Json(new { error = "POSPOS service unavailable. Please try again later." }, statusCode: 503);
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
}