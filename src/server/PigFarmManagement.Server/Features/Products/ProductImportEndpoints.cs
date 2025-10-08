using Microsoft.AspNetCore.Mvc;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.Products;

public static class ProductImportEndpoints
{
    public static void MapProductImportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products/import")
            .WithTags("Product Import");

        group.MapPost("/", ImportProducts)
            .WithName("ImportProducts")
            .WithSummary("Import selected products from POSPOS")
            .Produces<ProductImportResultDto>()
            .Produces(400)
            .Produces(500);
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