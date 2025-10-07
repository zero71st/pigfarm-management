using Microsoft.AspNetCore.Mvc;
using PigFarmManagement.Server.Features.FeedFormulas;
using PigFarmManagement.Server.Services;
using PigFarmManagement.Server.Services.ExternalServices;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.FeedFormulas;

public static class FeedFormulaEndpoints
{
    public static void MapFeedFormulaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/feed-formulas")
            .WithTags("Feed Formulas");

        group.MapGet("/", GetAllFeedFormulas)
            .WithName("GetAllFeedFormulas")
            .WithSummary("Get all feed formulas")
            .Produces<IEnumerable<FeedFormulaDto>>();

        group.MapGet("/{id:guid}", GetFeedFormulaById)
            .WithName("GetFeedFormulaById")
            .WithSummary("Get feed formula by ID")
            .Produces<FeedFormulaDto>()
            .Produces(404);

        group.MapPost("/", CreateFeedFormula)
            .WithName("CreateFeedFormula")
            .WithSummary("Create a new feed formula")
            .Produces<FeedFormulaDto>(201)
            .Produces(400);

        group.MapPut("/{id:guid}", UpdateFeedFormula)
            .WithName("UpdateFeedFormula")
            .WithSummary("Update an existing feed formula")
            .Produces<FeedFormulaDto>()
            .Produces(400)
            .Produces(404);

        group.MapDelete("/{id:guid}", DeleteFeedFormula)
            .WithName("DeleteFeedFormula")
            .WithSummary("Delete a feed formula")
            .Produces(204)
            .Produces(404);

        group.MapGet("/exists/{productCode}", CheckFeedFormulaExists)
            .WithName("CheckFeedFormulaExists")
            .WithSummary("Check if feed formula exists by product code")
            .Produces<bool>();

        group.MapPost("/import", ImportProductsFromPospos)
            .WithName("ImportProductsFromPospos")
            .WithSummary("Import feed formula products from POSPOS API")
            .Produces<ImportResultDto>()
            .Produces(400);

        group.MapGet("/pospos-products", GetPosposProducts)
            .WithName("GetPosposProducts")
            .WithSummary("Get all available products from POSPOS API without importing")
            .Produces<IEnumerable<PosposProductDto>>()
            .Produces(400);

        group.MapPost("/import-selected", ImportSelectedProductsFromPospos)
            .WithName("ImportSelectedProductsFromPospos")
            .WithSummary("Import selected feed formula products from POSPOS API")
            .Produces<ImportResultDto>()
            .Produces(400);

        group.MapPost("/maintenance/validate", ValidateFormulaSystem)
            .WithName("ValidateFormulaSystem")
            .WithSummary("Validate the unified formula assignment system")
            .Produces<PigFarmManagement.Shared.Models.FormulaSystemValidationDto>()
            .Produces(500);

        group.MapPost("/maintenance/repair", RepairFormulaSystem)
            .WithName("RepairFormulaSystem")
            .WithSummary("Repair any issues in the unified formula assignment system")
            .Produces<PigFarmManagement.Shared.Models.FormulaSystemRepairDto>()
            .Produces(500);

        group.MapGet("/maintenance/stats", GetFormulaSystemStats)
            .WithName("GetFormulaSystemStats")
            .WithSummary("Get statistics about the formula assignment system")
            .Produces<PigFarmManagement.Shared.Models.FormulaSystemStatsDto>();
    }

    private static async Task<IResult> GetAllFeedFormulas(IFeedFormulaService feedFormulaService)
    {
        try
        {
            var feedFormulas = await feedFormulaService.GetAllFeedFormulasAsync();
            var response = feedFormulas.Select(f => new FeedFormulaDto
            {
                Id = f.Id,
                Code = f.Code,
                Name = f.Name,
                CategoryName = f.CategoryName,
                Brand = f.Brand,
                ConsumeRate = f.ConsumeRate ?? 0,
                Cost = f.Cost ?? 0,
                UnitName = f.UnitName,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                DisplayName = f.DisplayName,
                ConsumptionRate = f.ConsumptionRate,
                BrandDisplayName = f.Brand ?? string.Empty,
                ProductCode = f.Code ?? string.Empty,
                ProductName = f.Name ?? string.Empty,
                BagPerPig = f.ConsumeRate ?? 0
            });

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving feed formulas: {ex.Message}");
        }
    }

    private static async Task<IResult> GetFeedFormulaById(Guid id, IFeedFormulaService feedFormulaService)
    {
        try
        {
            var feedFormula = await feedFormulaService.GetFeedFormulaByIdAsync(id);
            if (feedFormula == null)
                return Results.NotFound($"Feed formula with ID {id} not found");

            var response = new FeedFormulaDto
            {
                Id = feedFormula.Id,
                Code = feedFormula.Code,
                Name = feedFormula.Name,
                CategoryName = feedFormula.CategoryName,
                Brand = feedFormula.Brand,
                ConsumeRate = feedFormula.ConsumeRate ?? 0,
                Cost = feedFormula.Cost ?? 0,
                UnitName = feedFormula.UnitName,
                CreatedAt = feedFormula.CreatedAt,
                UpdatedAt = feedFormula.UpdatedAt,
                DisplayName = feedFormula.DisplayName,
                ConsumptionRate = feedFormula.ConsumptionRate,
                BrandDisplayName = feedFormula.Brand ?? string.Empty,
                ProductCode = feedFormula.Code ?? string.Empty,
                ProductName = feedFormula.Name ?? string.Empty,
                BagPerPig = feedFormula.ConsumeRate ?? 0
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving feed formula: {ex.Message}");
        }
    }

    private static async Task<IResult> CreateFeedFormula(FeedFormulaCreateDto dto, IFeedFormulaService feedFormulaService)
    {
        try
        {
            // Check if product code already exists
            if (dto.Code != null && await feedFormulaService.ExistsAsync(dto.Code))
            {
                return Results.BadRequest($"Feed formula with code '{dto.Code}' already exists");
            }

            var feedFormula = await feedFormulaService.CreateFeedFormulaAsync(dto);
            var response = new FeedFormulaDto
            {
                Id = feedFormula.Id,
                Code = feedFormula.Code,
                Name = feedFormula.Name,
                CategoryName = feedFormula.CategoryName,
                Brand = feedFormula.Brand,
                ConsumeRate = feedFormula.ConsumeRate ?? 0,
                Cost = feedFormula.Cost ?? 0,
                UnitName = feedFormula.UnitName,
                CreatedAt = feedFormula.CreatedAt,
                UpdatedAt = feedFormula.UpdatedAt,
                DisplayName = feedFormula.DisplayName,
                ConsumptionRate = feedFormula.ConsumptionRate,
                BrandDisplayName = feedFormula.Brand ?? string.Empty,
                ProductCode = feedFormula.Code ?? string.Empty,
                ProductName = feedFormula.Name ?? string.Empty,
                BagPerPig = feedFormula.ConsumeRate ?? 0
            };

            return Results.Created($"/api/feed-formulas/{feedFormula.Id}", response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error creating feed formula: {ex.Message}");
        }
    }

    private static async Task<IResult> UpdateFeedFormula(Guid id, FeedFormulaUpdateDto dto, IFeedFormulaService feedFormulaService)
    {
        try
        {
            var feedFormula = await feedFormulaService.UpdateFeedFormulaAsync(id, dto);
            var response = new FeedFormulaDto
            {
                Id = feedFormula.Id,
                Code = feedFormula.Code,
                Name = feedFormula.Name,
                CategoryName = feedFormula.CategoryName,
                Brand = feedFormula.Brand,
                ConsumeRate = feedFormula.ConsumeRate ?? 0,
                Cost = feedFormula.Cost ?? 0,
                UnitName = feedFormula.UnitName,
                CreatedAt = feedFormula.CreatedAt,
                UpdatedAt = feedFormula.UpdatedAt,
                DisplayName = feedFormula.DisplayName,
                ConsumptionRate = feedFormula.ConsumptionRate,
                BrandDisplayName = feedFormula.Brand ?? string.Empty,
                ProductCode = feedFormula.Code ?? string.Empty,
                ProductName = feedFormula.Name ?? string.Empty,
                BagPerPig = feedFormula.ConsumeRate ?? 0
            };

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error updating feed formula: {ex.Message}");
        }
    }

    private static async Task<IResult> DeleteFeedFormula(Guid id, IFeedFormulaService feedFormulaService)
    {
        try
        {
            var deleted = await feedFormulaService.DeleteFeedFormulaAsync(id);
            if (!deleted)
                return Results.NotFound($"Feed formula with ID {id} not found");

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error deleting feed formula: {ex.Message}");
        }
    }

    private static async Task<IResult> CheckFeedFormulaExists(string code, IFeedFormulaService feedFormulaService)
    {
        try
        {
            var exists = await feedFormulaService.ExistsAsync(code);
            return Results.Ok(exists);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error checking feed formula existence: {ex.Message}");
        }
    }

    private static async Task<IResult> ImportProductsFromPospos(IFeedFormulaService feedFormulaService)
    {
        try
        {
            var result = await feedFormulaService.ImportProductsFromPosposAsync();
            // Service already returns ImportResultDto, return it directly and preserve status semantics
            if (result.ErrorCount > 0 && result.SuccessCount == 0)
                return Results.BadRequest(result);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error importing products from POSPOS: {ex.Message}");
        }
    }

    private static async Task<IResult> GetPosposProducts(IFeedFormulaService feedFormulaService)
    {
        try
        {
            var products = await feedFormulaService.GetPosposProductsAsync();
            return Results.Ok(products);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error fetching POSPOS products: {ex.Message}");
        }
    }

    private static async Task<IResult> ImportSelectedProductsFromPospos(
        IFeedFormulaService feedFormulaService,
        ImportSelectedProductsRequestDto request)
    {
        try
        {
            if (request.ProductCodes == null || !request.ProductCodes.Any())
            {
                return Results.BadRequest("No product codes provided for import");
            }
            var result = await feedFormulaService.ImportSelectedProductsFromPosposAsync(request.ProductCodes);

            if (result.ErrorCount > 0 && result.SuccessCount == 0)
                return Results.BadRequest(result);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error importing selected products from POSPOS: {ex.Message}");
        }
    }

    private static async Task<IResult> ValidateFormulaSystem(IFeedFormulaService feedFormulaService)
    {
        try
        {
            var result = await feedFormulaService.ValidateFormulaSystemAsync();
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error validating formula system: {ex.Message}", statusCode: 500);
        }
    }

    private static async Task<IResult> RepairFormulaSystem(IFeedFormulaService feedFormulaService)
    {
        try
        {
            var result = await feedFormulaService.RepairFormulaSystemAsync();
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error repairing formula system: {ex.Message}", statusCode: 500);
        }
    }

    private static async Task<IResult> GetFormulaSystemStats(IFeedFormulaService feedFormulaService)
    {
        try
        {
            var stats = await feedFormulaService.GetFormulaSystemStatsAsync();
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error getting formula system stats: {ex.Message}", statusCode: 500);
        }
    }
}
// Note: formula system DTOs and import request DTO moved to shared models (FeedFormulaDtos.cs)
