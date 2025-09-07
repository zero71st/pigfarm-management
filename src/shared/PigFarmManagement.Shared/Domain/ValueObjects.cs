namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Immutable value objects without identity
/// Responsibility: Define data containers that are identified by their values, not identity
/// </summary>

public record FeedItem(Guid Id, Guid PigPenId, string FeedType, decimal QuantityKg, decimal PricePerKg, decimal Cost, DateTime Date)
{
    public string? ExternalReference { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime UpdatedAt { get; init; } = DateTime.Now;
    
    // Value calculations
    public decimal TotalCost => QuantityKg * PricePerKg;
    public bool IsExpensive => PricePerKg > 50; // Business rule for expensive feed
};

public record Deposit(Guid Id, Guid PigPenId, decimal Amount, DateTime Date, string? Remark)
{
    // Value validations
    public bool IsValid => Amount > 0 && Date <= DateTime.Now;
    public string FormattedAmount => $"${Amount:N2}";
};

public record HarvestResult(Guid Id, Guid PigPenId, DateTime HarvestDate, int PigCount, 
    decimal AvgWeight, decimal MinWeight, decimal MaxWeight, decimal TotalWeight, 
    decimal SalePricePerKg, decimal Revenue)
{
    // Calculated properties
    public decimal TotalValue => TotalWeight * SalePricePerKg;
    public decimal AverageRevenuePerPig => PigCount > 0 ? Revenue / PigCount : 0;
    public bool IsSuccessfulHarvest => AvgWeight >= MinWeight && Revenue > 0;
};
