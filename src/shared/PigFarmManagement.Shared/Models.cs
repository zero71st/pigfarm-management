namespace PigFarmManagement.Shared.Models;

public enum CustomerStatus
{
    Active,
    Inactive
}

public enum PigPenType
{
    Cash,
    Project
}

public record Customer(Guid Id, string Code, string Name, CustomerStatus Status)
{
    public string? ExternalId { get; init; }
    public string? KeyCardId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime UpdatedAt { get; init; } = DateTime.Now;
};

public record PigPen(Guid Id, Guid CustomerId, string PenCode, int PigQty,
    DateTime StartDate, DateTime? EndDate, DateTime? EstimatedHarvestDate,
    decimal FeedCost, decimal Investment, decimal ProfitLoss, 
    PigPenType Type, DateTime CreatedAt, DateTime UpdatedAt)
{
    public string Name => $"Pen {PenCode}";
    public string Code => PenCode;
    public int CurrentPigCount => PigQty;
    public int MaxCapacity => (int)(PigQty * 1.2); // 20% buffer for capacity
};

public record FeedItem(Guid Id, Guid PigPenId, string FeedType, decimal QuantityKg, decimal PricePerKg, decimal Cost, DateTime Date)
{
    public string? ExternalReference { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime UpdatedAt { get; init; } = DateTime.Now;
};

public class Feed
{
    public Guid Id { get; set; }
    public Guid PigPenId { get; set; }
    public string ProductType { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime FeedDate { get; set; }
    public string? Notes { get; set; }
    public string? ExternalReference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public record Deposit(Guid Id, Guid PigPenId, decimal Amount, DateTime Date, string? Remark);

public record HarvestResult(Guid Id, Guid PigPenId, DateTime HarvestDate, int PigCount, decimal AvgWeight, decimal MinWeight, decimal MaxWeight, decimal TotalWeight, decimal SalePricePerKg, decimal Revenue);

public record PigPenSummary(Guid PigPenId, decimal TotalFeedCost, decimal TotalDeposit, decimal Investment, decimal ProfitLoss, decimal NetBalance);

public record DashboardOverview(
    int TotalActivePigPens,
    int TotalPigs,
    int TotalPigsCash,
    int TotalPigsProject,
    decimal TotalInvestment,
    decimal TotalInvestmentCash,
    decimal TotalInvestmentProject,
    decimal TotalProfitLoss,
    decimal TotalProfitLossCash,
    decimal TotalProfitLossProject,
    List<CustomerPigPenStats> CustomerStats
);

public record CustomerPigPenStats(
    Guid CustomerId,
    string CustomerName,
    CustomerStatus CustomerStatus,
    int PigPenCount,
    int TotalPigs,
    decimal TotalInvestment,
    decimal TotalProfitLoss
);
