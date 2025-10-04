using Microsoft.AspNetCore.Mvc;
using PigFarmManagement.Server.Features.FeedFormulas;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.FeedFormulas;

public static class FeedFormulaCalculationEndpoints
{
    public static void MapFeedFormulaCalculationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/feed-formulas")
            .WithTags("Feed Formula Calculations");

        group.MapGet("/by-category/{categoryName}", GetFeedFormulasByCategory)
            .WithName("GetFeedFormulasByCategory")
            .WithSummary("Get feed formulas by category name")
            .Produces<IEnumerable<FeedFormulaWithCalculationResponse>>();

        group.MapPost("/calculate-requirements", CalculateFeedRequirements)
            .WithName("CalculateFeedRequirements")
            .WithSummary("Calculate feed requirements for given pig quantity")
            .Produces<FeedCalculationResponse>();
    }

    private static async Task<IResult> GetFeedFormulasByCategory(string categoryName, int? pigCount, IFeedFormulaService feedFormulaService)
    {
        try
        {
            var feedFormulas = await feedFormulaService.GetAllFeedFormulasAsync();
            var formulasByCategory = feedFormulas
                .Where(f => !string.IsNullOrEmpty(f.CategoryName) && f.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
                .Select(f => new FeedFormulaWithCalculationResponse
                {
                    Id = f.Id,
                    Code = f.Code ?? string.Empty,
                    Name = f.Name ?? string.Empty,
                    CategoryName = f.CategoryName ?? string.Empty,
                    ConsumeRate = f.ConsumeRate ?? 0,
                    Cost = f.Cost ?? 0,
                    UnitName = f.UnitName ?? string.Empty,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt,
                    TotalBagsRequired = pigCount.HasValue ? (f.ConsumeRate ?? 0) * pigCount.Value : 0,
                    PigCount = pigCount ?? 0
                });

            return Results.Ok(formulasByCategory);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving feed formulas by category: {ex.Message}");
        }
    }

    private static async Task<IResult> CalculateFeedRequirements(FeedCalculationRequest request, IFeedFormulaService feedFormulaService)
    {
        try
        {
            var feedFormula = await feedFormulaService.GetFeedFormulaByIdAsync(request.FeedFormulaId);
            if (feedFormula == null)
                return Results.NotFound($"Feed formula with ID {request.FeedFormulaId} not found");

            var totalBags = (feedFormula.ConsumeRate ?? 0) * request.PigCount;
            var totalCost = request.BagPrice.HasValue ? (decimal?)(totalBags * request.BagPrice.Value) : null;

            var response = new FeedCalculationResponse
            {
                FeedFormulaId = feedFormula.Id,
                Name = feedFormula.Name ?? string.Empty,
                CategoryName = feedFormula.CategoryName ?? string.Empty,
                PigCount = request.PigCount,
                ConsumeRate = feedFormula.ConsumeRate ?? 0,
                TotalBagsRequired = totalBags,
                BagPrice = request.BagPrice,
                TotalCost = totalCost,
                CalculationDate = DateTime.UtcNow
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error calculating feed requirements: {ex.Message}");
        }
    }
}

// Request and Response DTOs
public record FeedCalculationRequest(Guid FeedFormulaId, int PigCount, decimal? BagPrice = null);

public class FeedFormulaWithCalculationResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal ConsumeRate { get; set; }
    public decimal Cost { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public decimal TotalBagsRequired { get; set; }
    public int PigCount { get; set; }
}

public class FeedCalculationResponse
{
    public Guid FeedFormulaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int PigCount { get; set; }
    public decimal ConsumeRate { get; set; }
    public decimal TotalBagsRequired { get; set; }
    public decimal? BagPrice { get; set; }
    public decimal? TotalCost { get; set; }
    public DateTime CalculationDate { get; set; }
}
