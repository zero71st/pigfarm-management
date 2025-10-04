namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Immutable value objects without identity
/// Responsibility: Define data containers that are identified by their values, not identity
/// </summary>

public record FeedItem(
    Guid Id, 
    Guid PigPenId, 
    string FeedType, 
    string ProductCode, 
    string ProductName, 
    string TransactionCode,
    string? InvoiceReferenceCode,
    decimal Quantity, // number of bags
    decimal PricePerBag, 
    decimal Cost,
    DateTime Date)
{
    // Backwards-compatible convenience properties expected by the UI
    // Some UI code references QuantityKg and PricePerKg; expose them here
    public decimal QuantityKg => Quantity; // quantity is provided in kilograms by service mapping
    public decimal PricePerKg => PricePerBag; // price per kg (service mapping sets this)

    public string? ExternalReference { get; init; }
    public string? ExternalProductCode { get; init; }
    public string? ExternalProductName { get; init; }
    public bool UnmappedProduct { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime UpdatedAt { get; init; } = DateTime.Now;
    
    // New pricing fields from POSPOS import enhancement
    public decimal? FeedCost { get; init; } // From FeedFormula.Cost
    public decimal? CostDiscountPrice { get; init; } // From PosPosFeedItem.CostDiscountPrice directly
    public decimal? PriceIncludeDiscount { get; init; } // UnitPrice - CostDiscountPrice
    public decimal? Sys_TotalPriceIncludeDiscount { get; init; } // PriceIncludeDiscount * Quantity
    public decimal? Pos_TotalPriceIncludeDiscount { get; init; } // POS-provided total for comparison
    
    // Backward compatibility property
    [Obsolete("Use TransactionCode instead")]
    public string InvoiceNumber => TransactionCode;
    
    // Value calculations
    public decimal TotalCost => Quantity * PricePerBag;
    public bool IsExpensive => PricePerBag > 50; // Business rule for expensive feed
};

public record Deposit(Guid Id, Guid PigPenId, decimal Amount, DateTime Date, string? Remark)
{
    // Value validations
    public bool IsValid => Amount > 0 && Date <= DateTime.Now;
    public string FormattedAmount => $"${Amount:N2}";
};

public record HarvestResult(Guid Id, Guid PigPenId, DateTime HarvestDate, int PigCount, 
    decimal AvgWeight, decimal TotalWeight, decimal SalePricePerKg, decimal Revenue)
{
    // Calculated properties
    public decimal TotalValue => TotalWeight * SalePricePerKg;
    public decimal AverageRevenuePerPig => PigCount > 0 ? Revenue / PigCount : 0;
    public bool IsSuccessfulHarvest => AvgWeight > 0 && Revenue > 0;
};
