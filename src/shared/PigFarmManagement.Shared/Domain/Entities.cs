namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Core domain entities with identity and business logic
/// Responsibility: Define the main business entities and their behavior
/// </summary>

public record Customer(Guid Id, string Code, string Name, CustomerStatus Status)
{
    public string? ExternalId { get; init; }
    public string? KeyCardId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime UpdatedAt { get; init; } = DateTime.Now;
    
    // Business logic
    public bool IsActive => Status == CustomerStatus.Active;
    public string DisplayName => $"{Name} ({Code})";
};

public record PigPen(Guid Id, Guid CustomerId, string PenCode, int PigQty,
    DateTime RegisterDate, DateTime? ActHarvestDate, DateTime? EstimatedHarvestDate,
    decimal FeedCost, decimal Investment, decimal ProfitLoss, 
    PigPenType Type, Guid? FeedFormulaId, decimal DepositPerPig, DateTime CreatedAt, DateTime UpdatedAt)
{
    // Feed formula brand selection (for display purposes)
    public string? SelectedBrand { get; init; }
    
    // Business computed properties
    public string Name => $"Pen {PenCode}";
    public string Code => PenCode;
    public int CurrentPigCount => PigQty;
    public int MaxCapacity => (int)(PigQty * 1.2); // 20% buffer for capacity
    
    // Business logic
    public bool IsActive => ActHarvestDate == null || ActHarvestDate > DateTime.Now;
    public bool IsReadyForHarvest => EstimatedHarvestDate <= DateTime.Now;
    public decimal ProfitMargin => Investment != 0 ? (ProfitLoss / Investment) * 100 : 0;
    
    // Feed calculation methods
    public decimal CalculateRequiredBags(decimal bagPerPig) => PigQty * bagPerPig;
    public decimal CalculateFeedCost(decimal bagPerPig, decimal bagPrice) => CalculateRequiredBags(bagPerPig) * bagPrice;
};

public record FeedFormula(Guid Id, string ProductCode, string ProductName, 
    string Brand, decimal BagPerPig, DateTime CreatedAt, DateTime UpdatedAt)
{
    // Business computed properties
    public string DisplayName => $"{ProductName} ({ProductCode})";
    public string ConsumptionRate => $"{BagPerPig:F1} bags/pig";
    public string BrandDisplayName => $"{Brand} - {ProductName}";
    
    // Business logic
    public decimal CalculateTotalBags(int pigCount) => pigCount * BagPerPig;
    public decimal CalculateCost(int pigCount, decimal bagPrice) => CalculateTotalBags(pigCount) * bagPrice;
};
