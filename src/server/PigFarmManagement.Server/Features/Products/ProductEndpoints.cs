using Microsoft.AspNetCore.Mvc;
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
            .Produces<IEnumerable<ProductDto>>()
            .Produces(400);

        group.MapPost("/import", ImportProducts)
            .WithName("ImportProducts")
            .WithSummary("Import selected products (upsert duplicates)")
            .Produces<ProductImportResultDto>()
            .Produces(400);

        group.MapGet("/", GetAllProducts)
            .WithName("GetAllProducts")
            .WithSummary("Get all available products")
            .Produces<IEnumerable<ProductDto>>()
            .Produces(400);

        group.MapGet("/{code}", GetProductByCode)
            .WithName("GetProductByCode")
            .WithSummary("Get product by code")
            .Produces<ProductDto>()
            .Produces(404)
            .Produces(400);
    }

    private static async Task<IResult> SearchProducts(
        [FromQuery] string q,
        [FromServices] IProductService productService)
    {
        try
        {
            var searchDto = new ProductSearchDto { Query = q };
            var results = await productService.SearchProductsAsync(searchDto);
            return Results.Ok(results);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error searching products: {ex.Message}");
        }
    }

    private static async Task<IResult> ImportProducts(
        [FromBody] ProductImportDto importDto,
        [FromServices] IProductService productService)
    {
        try
        {
            var result = await productService.ImportProductsAsync(importDto);
            return Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error importing products: {ex.Message}");
        }
    }

    private static async Task<IResult> GetAllProducts(
        [FromServices] IProductService productService)
    {
        try
        {
            var products = await productService.GetAllProductsAsync();
            return Results.Ok(products);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving products: {ex.Message}");
        }
    }

    private static async Task<IResult> GetProductByCode(
        string code,
        [FromServices] IProductService productService)
    {
        try
        {
            var product = await productService.GetProductByCodeAsync(code);
            return product == null ? Results.NotFound($"Product with code '{code}' not found") : Results.Ok(product);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving product: {ex.Message}");
        }
    }
}