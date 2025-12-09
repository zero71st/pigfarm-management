namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Analytics, summary, and statistical models
/// Responsibility: Define aggregated data structures for reporting and dashboard purposes
/// </summary>

public record PigPenSummary(Guid PigPenId, decimal TotalFeedCost, decimal TotalDeposit, 
    decimal Investment, decimal ProfitLoss)
{
    // Analytics calculations
    public decimal CostEfficiencyRatio => TotalDeposit != 0 ? TotalFeedCost / TotalDeposit : 0;
    public bool IsProfitable => ProfitLoss > 0;
    public string PerformanceCategory => ProfitLoss switch
    {
        > 1000 => "Excellent",
        > 0 => "Good", 
        > -500 => "Average",
        _ => "Needs Attention"
    };
};

public record DashboardOverview(
    int TotalActivePigPens,
    int TotalActiveCustomers,
    int TotalPigs,
    int TotalPigsCash,
    int TotalPigsProject,
    decimal TotalCost,
    decimal TotalDeposit,
    decimal TotalPriceIncludeDiscount,
    decimal TotalCostCash,
    decimal TotalDepositCash,
    decimal TotalPriceIncludeDiscountCash,
    decimal TotalCostProject,
    decimal TotalDepositProject,
    decimal TotalPriceIncludeDiscountProject,
    List<CustomerPigPenStats> CustomerStats,
    List<CustomerPigPenStats> CustomerStatsCash)
{
    // Financial metrics - NEW
    public decimal TotalCustomerCapital => TotalDeposit;
    public decimal TotalOwnerCapital => TotalCost;
    public decimal TotalProfit => TotalPriceIncludeDiscount - TotalCost;
    public decimal TotalInvestment => TotalOwnerCapital + TotalCustomerCapital + TotalProfit;
    
    // Cash section metrics - NEW
    public decimal TotalCustomerCapitalCash => TotalDepositCash;
    public decimal TotalOwnerCapitalCash => TotalCostCash;
    public decimal TotalProfitCash => TotalPriceIncludeDiscountCash - TotalCostCash;
    public decimal TotalInvestmentCash => TotalOwnerCapitalCash + TotalCustomerCapitalCash + TotalProfitCash;
    
    // Project section metrics - NEW
    public decimal TotalCustomerCapitalProject => TotalDepositProject;
    public decimal TotalOwnerCapitalProject => TotalCostProject;
    public decimal TotalProfitProject => TotalPriceIncludeDiscountProject - TotalCostProject;
    public decimal TotalInvestmentProject => TotalOwnerCapitalProject + TotalCustomerCapitalProject + TotalProfitProject;
    
    // Dashboard analytics (updated to use new formulas)
    public decimal OverallROI => TotalInvestment != 0 ? (TotalProfit / TotalInvestment) * 100 : 0;
    public decimal CashROI => TotalInvestmentCash != 0 ? (TotalProfitCash / TotalInvestmentCash) * 100 : 0;
    public decimal ProjectROI => TotalInvestmentProject != 0 ? (TotalProfitProject / TotalInvestmentProject) * 100 : 0;
    public decimal AveragePigsPerPen => TotalActivePigPens != 0 ? (decimal)TotalPigs / TotalActivePigPens : 0;
    public string BusinessHealthStatus => TotalProfit switch
    {
        > 10000 => "Excellent",
        > 0 => "Healthy",
        > -5000 => "Concerning", 
        _ => "Critical"
    };
};

public record CustomerPigPenStats(
    Guid CustomerId,
    string CustomerName,
    CustomerStatus CustomerStatus,
    int PigPenCount,
    int TotalPigs,
    decimal TotalCost,
    decimal TotalDeposit,
    decimal TotalPriceIncludeDiscount)
{
    // Financial metrics - NEW formulas
    public decimal TotalCustomerCapital => TotalDeposit;
    public decimal TotalOwnerCapital => TotalCost;
    public decimal TotalProfitLoss => TotalPriceIncludeDiscount - TotalCost;
    public decimal TotalInvestment => TotalOwnerCapital + TotalCustomerCapital + TotalProfitLoss;
    
    // Customer analytics (updated to use new formulas)
    public decimal CustomerROI => TotalInvestment != 0 ? (TotalProfitLoss / TotalInvestment) * 100 : 0;
    public decimal AveragePigsPerPen => PigPenCount != 0 ? (decimal)TotalPigs / PigPenCount : 0;
    public bool IsTopPerformer => TotalProfitLoss > 5000 && CustomerROI > 15;
    public string PerformanceRating => (CustomerROI, TotalProfitLoss) switch
    {
        ( > 20, > 5000) => "A+",
        ( > 15, > 3000) => "A",
        ( > 10, > 1000) => "B",
        ( > 5, > 0) => "C",
        _ => "D"
    };
};

// Import and Integration Analytics
public class FeedImportResult
{
    public int TotalTransactions { get; set; }
    public int TotalFeedItems { get; set; }
    public int SuccessfulImports { get; set; }
    public int FailedImports { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ImportedFeedSummary> ImportedFeeds { get; set; } = new();
    
    // Import analytics
    public decimal SuccessRate => TotalTransactions > 0 ? (decimal)SuccessfulImports / TotalTransactions * 100 : 0;
    public decimal ErrorRate => TotalTransactions > 0 ? (decimal)FailedImports / TotalTransactions * 100 : 0;
    public decimal TotalImportValue => ImportedFeeds.Sum(f => f.TotalAmount);
    public bool IsSuccessfulImport => SuccessRate >= 90;
    public string ImportStatus => SuccessRate switch
    {
        >= 100 => "Perfect",
        >= 90 => "Excellent", 
        >= 80 => "Good",
        >= 70 => "Warning",
        _ => "Failed"
    };
}

public class ImportedFeedSummary
{
    public string InvoiceCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string PigPenCode { get; set; } = "";
    public int FeedItemsCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime ImportDate { get; set; }
    
    // Summary analytics
    public decimal AverageFeedItemValue => FeedItemsCount > 0 ? TotalAmount / FeedItemsCount : 0;
    public bool IsLargeOrder => FeedItemsCount > 10 || TotalAmount > 5000;
    public string OrderSize => (FeedItemsCount, TotalAmount) switch
    {
        (> 20, > 10000) => "Extra Large",
        (> 10, > 5000) => "Large",
        (> 5, > 2000) => "Medium",
        _ => "Small"
    };
};
