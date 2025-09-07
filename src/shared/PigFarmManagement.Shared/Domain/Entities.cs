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
    DateTime StartDate, DateTime? EndDate, DateTime? EstimatedHarvestDate,
    decimal FeedCost, decimal Investment, decimal ProfitLoss, 
    PigPenType Type, DateTime CreatedAt, DateTime UpdatedAt)
{
    // Business computed properties
    public string Name => $"Pen {PenCode}";
    public string Code => PenCode;
    public int CurrentPigCount => PigQty;
    public int MaxCapacity => (int)(PigQty * 1.2); // 20% buffer for capacity
    
    // Business logic
    public bool IsActive => EndDate == null || EndDate > DateTime.Now;
    public bool IsReadyForHarvest => EstimatedHarvestDate <= DateTime.Now;
    public decimal ProfitMargin => Investment != 0 ? (ProfitLoss / Investment) * 100 : 0;
};
