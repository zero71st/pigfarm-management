using Microsoft.AspNetCore.Mvc;
using PigFarmManagement.Server.Features.FeedFormulas;

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
            .Produces<IEnumerable<FeedFormulaResponse>>();

        group.MapGet("/{id:guid}", GetFeedFormulaById)
            .WithName("GetFeedFormulaById")
            .WithSummary("Get feed formula by ID")
            .Produces<FeedFormulaResponse>()
            .Produces(404);

        group.MapPost("/", CreateFeedFormula)
            .WithName("CreateFeedFormula")
            .WithSummary("Create a new feed formula")
            .Produces<FeedFormulaResponse>(201)
            .Produces(400);

        group.MapPut("/{id:guid}", UpdateFeedFormula)
            .WithName("UpdateFeedFormula")
            .WithSummary("Update an existing feed formula")
            .Produces<FeedFormulaResponse>()
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
    }

    private static async Task<IResult> GetAllFeedFormulas(IFeedFormulaService feedFormulaService)
    {
        try
        {
            var feedFormulas = await feedFormulaService.GetAllFeedFormulasAsync();
            var response = feedFormulas.Select(f => new FeedFormulaResponse
            {
                Id = f.Id,
                ProductCode = f.ProductCode,
                ProductName = f.ProductName,
                Brand = f.Brand,
                BagPerPig = f.BagPerPig,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                DisplayName = f.DisplayName,
                ConsumptionRate = f.ConsumptionRate,
                BrandDisplayName = f.BrandDisplayName
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

            var response = new FeedFormulaResponse
            {
                Id = feedFormula.Id,
                ProductCode = feedFormula.ProductCode,
                ProductName = feedFormula.ProductName,
                Brand = feedFormula.Brand,
                BagPerPig = feedFormula.BagPerPig,
                CreatedAt = feedFormula.CreatedAt,
                UpdatedAt = feedFormula.UpdatedAt,
                DisplayName = feedFormula.DisplayName,
                ConsumptionRate = feedFormula.ConsumptionRate,
                BrandDisplayName = feedFormula.BrandDisplayName
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
            if (await feedFormulaService.ExistsAsync(dto.ProductCode))
            {
                return Results.BadRequest($"Feed formula with product code '{dto.ProductCode}' already exists");
            }

            var feedFormula = await feedFormulaService.CreateFeedFormulaAsync(dto);
            var response = new FeedFormulaResponse
            {
                Id = feedFormula.Id,
                ProductCode = feedFormula.ProductCode,
                ProductName = feedFormula.ProductName,
                Brand = feedFormula.Brand,
                BagPerPig = feedFormula.BagPerPig,
                CreatedAt = feedFormula.CreatedAt,
                UpdatedAt = feedFormula.UpdatedAt,
                DisplayName = feedFormula.DisplayName,
                ConsumptionRate = feedFormula.ConsumptionRate,
                BrandDisplayName = feedFormula.BrandDisplayName
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
            var response = new FeedFormulaResponse
            {
                Id = feedFormula.Id,
                ProductCode = feedFormula.ProductCode,
                ProductName = feedFormula.ProductName,
                Brand = feedFormula.Brand,
                BagPerPig = feedFormula.BagPerPig,
                CreatedAt = feedFormula.CreatedAt,
                UpdatedAt = feedFormula.UpdatedAt,
                DisplayName = feedFormula.DisplayName,
                ConsumptionRate = feedFormula.ConsumptionRate,
                BrandDisplayName = feedFormula.BrandDisplayName
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

    private static async Task<IResult> CheckFeedFormulaExists(string productCode, IFeedFormulaService feedFormulaService)
    {
        try
        {
            var exists = await feedFormulaService.ExistsAsync(productCode);
            return Results.Ok(exists);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error checking feed formula existence: {ex.Message}");
        }
    }
}

// Response DTOs
public class FeedFormulaResponse
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal BagPerPig { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ConsumptionRate { get; set; } = string.Empty;
    public string BrandDisplayName { get; set; } = string.Empty;
}
