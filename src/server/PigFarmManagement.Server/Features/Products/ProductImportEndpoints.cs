using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PigFarmManagement.Shared.Models;
using PigFarmManagement.Shared.Domain.External;
using PigFarmManagement.Server.Services.ExternalServices;

namespace PigFarmManagement.Server.Features.Products;

public static class ProductImportEndpoints
{
    public static void MapProductImportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Search endpoint (uses server-side filtering via IPosposProductClient)
        endpoints.MapGet("/api/products/search", SearchProducts)
            .WithName("SearchProducts")
            .WithSummary("Search products by code or name")
            .Produces<List<PosposProduct>>(200)
            .Produces(400)
            .Produces(500);

        var group = endpoints.MapGroup("/api/products/import")
            .WithTags("Product Import")
            .RequireAuthorization();

        group.MapPost("/", ImportProducts)
            .WithName("ImportProducts")
            .WithSummary("Import selected products from POSPOS")
            .Produces<ProductImportResultDto>()
            .Produces(400)
            .Produces(500);
    }

    /// <summary>
    /// Search products (server-side filtering). This is a fallback that fetches
    /// products via IPosposProductClient and filters by code or name.
    /// </summary>
    private static async Task<IResult> SearchProducts(
        [FromQuery] string q,
        [FromServices] IPosposProductClient posposClient,
        [FromServices] ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ProductImportEndpoints");

        if (string.IsNullOrWhiteSpace(q))
        {
            logger.LogWarning("Product search attempted with empty query");
            return Results.BadRequest(new { message = "Search query 'q' cannot be empty." });
        }

        try
        {
            logger.LogInformation("Searching products with query: {Query}", q);

            // Fetch all products and filter server-side (POSPOS may not support search)
            var all = await posposClient.GetAllProductsAsync();
            var results = all
                .Where(p => (!string.IsNullOrWhiteSpace(p.Code) && p.Code.Contains(q, StringComparison.OrdinalIgnoreCase))
                         || (!string.IsNullOrWhiteSpace(p.Name) && p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            logger.LogInformation("Search for '{Query}' returned {Count} results", q, results.Count);
            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching products with query: {Query}", q);
            return Results.Problem($"Error searching products: {ex.Message}");
        }
    }

    /// <summary>
    /// Import selected products from POSPOS system
    /// </summary>
    private static async Task<IResult> ImportProducts(
        [FromBody] ProductImportDto importDto,
        [FromServices] IProductImportService productImportService)
    {
        try
        {
            var result = await productImportService.ImportProductsAsync(importDto);
            return Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error importing products: {ex.Message}");
        }
    }
}