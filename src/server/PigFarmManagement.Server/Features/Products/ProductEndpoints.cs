using Microsoft.AspNetCore.Mvc;
using PigFarmManagement.Server.Services.ExternalServices;
using PigFarmManagement.Server.Features.FeedFormulas;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.Products;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/search", SearchProducts)
            .WithName("SearchProducts")
            .WithSummary("Search products by code or name")
            .Produces<IEnumerable<PosposProductDto>>()
            .Produces(400);

        group.MapPost("/import", ImportProducts)
            .WithName("ImportProducts")
            .WithSummary("Import selected products (upsert duplicates)")
            .Produces<PigFarmManagement.Shared.Models.ImportResult>()
            .Produces(400);
    }

    private static async Task<IResult> SearchProducts(
        [FromQuery] string q,
        [FromServices] IPosposProductClient posposClient,
        [FromServices] ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ProductEndpoints");
        
        // Validate query parameter
        if (string.IsNullOrWhiteSpace(q))
        {
            logger.LogWarning("Product search attempted with empty query");
            return Results.BadRequest("Search query 'q' cannot be empty");
        }

        logger.LogInformation("Searching products with query: {Query}", q);

        try
        {
            // Fetch all products from POSPOS
            var allProducts = await posposClient.GetAllProductsAsync();
            
            // Search for partial matches in both code and name
            var results = allProducts
                .Where(p => 
                    (p.Code?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) ||
                    (p.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) == true))
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

    private static async Task<IResult> ImportProducts(
        [FromBody] ImportRequest request,
        [FromServices] IFeedFormulaService feedFormulaService,
        [FromServices] ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ProductEndpoints");
        
        // Validate request
        if (request.ProductIds == null || !request.ProductIds.Any())
        {
            logger.LogWarning("Product import attempted with empty ProductIds");
            return Results.BadRequest("ProductIds cannot be empty");
        }

        logger.LogInformation("Starting import of {Count} products", request.ProductIds.Count);

        try
        {
            var result = await feedFormulaService.ImportProductsByIdsAsync(request);
            
            logger.LogInformation(
                "Product import completed: {Created} created, {Updated} updated, {Failed} failed", 
                result.Summary.Created, result.Summary.Updated, result.Summary.Failed);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error importing {Count} products", request.ProductIds.Count);
            return Results.Problem($"Error importing products: {ex.Message}");
        }
    }
}