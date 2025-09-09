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

        group.MapGet("/by-brand/{brand}", GetFeedFormulasByBrand)
            .WithName("GetFeedFormulasByBrand")
            .WithSummary("Get feed formulas by brand")
            .Produces<IEnumerable<FeedFormulaWithCalculationResponse>>();

        group.MapPost("/calculate-requirements", CalculateFeedRequirements)
            .WithName("CalculateFeedRequirements")
            .WithSummary("Calculate feed requirements for given pig quantity")
            .Produces<FeedCalculationResponse>();
    }

    private static async Task<IResult> GetFeedFormulasByBrand(string brand, int? pigCount, IFeedFormulaService feedFormulaService)
    {
        try
        {
            var feedFormulas = await feedFormulaService.GetAllFeedFormulasAsync();
            var formulasByBrand = feedFormulas
                .Where(f => f.Brand.Equals(brand, StringComparison.OrdinalIgnoreCase))
                .Select(f => new FeedFormulaWithCalculationResponse
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
                    BrandDisplayName = f.BrandDisplayName,
                    TotalBagsRequired = pigCount.HasValue ? f.CalculateTotalBags(pigCount.Value) : 0,
                    PigCount = pigCount ?? 0
                });

            return Results.Ok(formulasByBrand);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving feed formulas by brand: {ex.Message}");
        }
    }

    private static async Task<IResult> CalculateFeedRequirements(FeedCalculationRequest request, IFeedFormulaService feedFormulaService)
    {
        try
        {
            var feedFormula = await feedFormulaService.GetFeedFormulaByIdAsync(request.FeedFormulaId);
            if (feedFormula == null)
                return Results.NotFound($"Feed formula with ID {request.FeedFormulaId} not found");

            var totalBags = feedFormula.CalculateTotalBags(request.PigCount);
            var totalCost = request.BagPrice.HasValue ? (decimal?)(totalBags * request.BagPrice.Value) : null;

            var response = new FeedCalculationResponse
            {
                FeedFormulaId = feedFormula.Id,
                ProductName = feedFormula.ProductName,
                Brand = feedFormula.Brand,
                PigCount = request.PigCount,
                BagPerPig = feedFormula.BagPerPig,
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
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal BagPerPig { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ConsumptionRate { get; set; } = string.Empty;
    public string BrandDisplayName { get; set; } = string.Empty;
    public decimal TotalBagsRequired { get; set; }
    public int PigCount { get; set; }
}

public class FeedCalculationResponse
{
    public Guid FeedFormulaId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int PigCount { get; set; }
    public decimal BagPerPig { get; set; }
    public decimal TotalBagsRequired { get; set; }
    public decimal? BagPrice { get; set; }
    public decimal? TotalCost { get; set; }
    public DateTime CalculationDate { get; set; }
}
