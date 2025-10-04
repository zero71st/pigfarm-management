namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Core domain entities with identity and business logic
/// Responsibility: Define the main business entities and their behavior
/// </summary>

public record Customer(Guid Id, string Code, CustomerStatus Status)
{
    public string? ExternalId { get; init; }
    public string? KeyCardId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    // POSPOS-aligned fields
    public string? Address { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Sex { get; init; }
    public string? Zipcode { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime UpdatedAt { get; init; } = DateTime.Now;
    
    // Business logic
    public bool IsActive => Status == CustomerStatus.Active;
    public string DisplayName => $"{(string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName) ? Code : $"{FirstName} {LastName}" )} ({Code})";
};

public record PigPen(Guid Id, Guid CustomerId, string PenCode, int PigQty,
    DateTime RegisterDate, DateTime? ActHarvestDate, DateTime? EstimatedHarvestDate,
    decimal FeedCost, decimal Investment, decimal ProfitLoss, 
    PigPenType Type, decimal DepositPerPig, DateTime CreatedAt, DateTime UpdatedAt)
{
    // Feed formula brand selection (for display purposes)
    public string? SelectedBrand { get; init; }
    
    // Unified formula assignments - replaces the old FeedFormulaId, FeedFormulaSnapshot, and FeedFormulaAllocations systems
    public List<PigPenFormulaAssignment> FormulaAssignments { get; init; } = new();
    
    // Flag to indicate if calculations are locked (for closed pig pens)
    public bool IsCalculationLocked { get; init; }
    
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

public record FeedFormula(
    Guid Id,
    Guid? ExternalId, // from POSPOS _id
    string? Code, // POSPOS code, primary product code
    string? Name, // POSPOS name
    decimal? Cost, // POSPOS cost, used for profit calculations
    decimal? ConsumeRate, // user input, e.g., 0.5 per pig
    string? CategoryName, // POSPOS category.name
    string? Brand, // User-defined brand, different from CategoryName
    string? UnitName, // POSPOS unit.name
    DateTime? LastUpdate, // POSPOS lastupdate
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    // Business computed properties
    public string DisplayName => $"{Name} ({Code})";
    public string ConsumptionRate => ConsumeRate.HasValue ? $"{ConsumeRate:F1} per pig" : "Not set";
    
    // Business logic
    public decimal CalculateTotalConsumption(int pigCount) => pigCount * (ConsumeRate ?? 0);
    public decimal CalculateCost(int pigCount) => CalculateTotalConsumption(pigCount) * (Cost ?? 0);
};

/// <summary>
/// Unified formula assignment for pig pens - replaces the old FeedFormulaId, FeedFormulaSnapshot, and FeedFormulaAllocation systems
/// Handles both active and locked assignments with single entity
/// </summary>
public record PigPenFormulaAssignment(
    Guid Id,
    Guid PigPenId,
    Guid OriginalFormulaId,
    string ProductCode,
    string ProductName,
    string Brand,
    string? Stage, // "Starter", "Grower", "Finisher" - null for single formula assignments
    int AssignedPigQuantity,
    decimal AssignedBagPerPig, // Value at time of assignment
    decimal AssignedTotalBags,  // Calculated at time of assignment
    DateTime AssignedAt,
    DateTime? EffectiveUntil,
    bool IsActive,              // For active pig pens - can be modified
    bool IsLocked,              // For closed pig pens - historical protection
    string? LockReason,         // "ForceClosed", "Completed", etc.
    DateTime? LockedAt)
{
    // Business computed properties
    public string DisplayName => Stage != null ? $"{ProductName} - {Stage}" : $"{ProductName} ({ProductCode})";
    public bool CanBeModified => IsActive && !IsLocked;
    public string Status => IsLocked ? "Locked" : IsActive ? "Active" : "Inactive";
    public decimal EstimatedCost(decimal bagPrice) => AssignedTotalBags * bagPrice;

    // Business logic
    public bool IsCurrentlyEffective => !EffectiveUntil.HasValue || EffectiveUntil > DateTime.Now;
    public int DaysAssigned => (DateTime.Now - AssignedAt).Days;
};
