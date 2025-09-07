namespace PigFarmManagement.Shared.Models;

public enum CustomerType
{
    Cash,
    Project
}

public record Customer(Guid Id, string Code, string Name, CustomerType Type);

public record PigPen(Guid Id, Guid CustomerId, string PenCode, int PigQty,
    DateTime StartDate, DateTime? EndDate, DateTime? EstimatedHarvestDate,
    decimal FeedCost, decimal Investment, decimal ProfitLoss)
{
    public string Name => $"Pen {PenCode}";
    public string Code => PenCode;
    public int CurrentPigCount => PigQty;
    public int MaxCapacity => (int)(PigQty * 1.2); // 20% buffer for capacity
};

public record FeedItem(Guid Id, Guid PigPenId, string FeedType, decimal QuantityKg, decimal PricePerKg, decimal Cost, DateTime Date);

public record Deposit(Guid Id, Guid PigPenId, decimal Amount, DateTime Date, string? Remark);

public record HarvestResult(Guid Id, Guid PigPenId, DateTime HarvestDate, int PigCount, decimal AvgWeight, decimal MinWeight, decimal MaxWeight, decimal TotalWeight, decimal SalePricePerKg, decimal Revenue);

public record PigPenSummary(Guid PigPenId, decimal TotalFeedCost, decimal TotalDeposit, decimal Investment, decimal ProfitLoss, decimal NetBalance);

public record DashboardOverview(
    int TotalActivePigPens,
    int TotalPigsCash,
    int TotalPigsProject,
    decimal TotalInvestmentCash,
    decimal TotalInvestmentProject,
    decimal TotalProfitLossCash,
    decimal TotalProfitLossProject,
    List<CustomerPigPenStats> CustomerStats
);

public record CustomerPigPenStats(
    Guid CustomerId,
    string CustomerName,
    CustomerType CustomerType,
    int PigPenCount,
    int TotalPigs,
    decimal TotalInvestment,
    decimal TotalProfitLoss
);
