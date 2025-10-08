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
            .Produces<IEnumerable<FeedFormulaWithCalculationDto>>();

        group.MapPost("/calculate-requirements", CalculateFeedRequirements)
            .WithName("CalculateFeedRequirements")
            .WithSummary("Calculate feed requirements for given pig quantity")
            .Produces<FeedCalculationDto>();
    }

    private static async Task<IResult> GetFeedFormulasByCategory(string categoryName, int? pigCount, IFeedFormulaService feedFormulaService)
    {
        try
        {
            var feedFormulas = await feedFormulaService.GetAllFeedFormulasAsync();
            var formulasByCategory = feedFormulas
                .Where(f => !string.IsNullOrEmpty(f.CategoryName) && f.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
                .Select(f => new FeedFormulaWithCalculationDto
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
                    PigCount = pigCount ?? 0,
                    Brand = f.Brand ?? string.Empty,
                    DisplayName = f.DisplayName,
                    ConsumptionRate = f.ConsumptionRate,
                    BrandDisplayName = f.Brand ?? string.Empty
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

            var response = new FeedCalculationDto
            {
                FeedFormulaId = feedFormula.Id,
                Name = feedFormula.Name ?? string.Empty,
                CategoryName = feedFormula.CategoryName ?? string.Empty,
                PigCount = request.PigCount,
                ConsumeRate = feedFormula.ConsumeRate ?? 0,
                TotalBagsRequired = totalBags,
                BagPrice = request.BagPrice,
                TotalCost = totalCost,
                CalculationDate = DateTime.UtcNow,
                Brand = feedFormula.Brand ?? string.Empty
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error calculating feed requirements: {ex.Message}");
        }
    }
}
