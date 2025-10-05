namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Data Transfer Objects for external system integration
/// Responsibility: Handle data contracts with external systems (APIs, imports, etc.)
/// </summary>

public class Feed
{
    public Guid Id { get; set; }
    public Guid PigPenId { get; set; }
    public string ProductType { get; set; } = "";
    public string ProductCode { get; set; } = ""; // Add product code field
    public string ProductName { get; set; } = ""; // Add product name field
    public string TransactionCode { get; set; } = ""; // Add transaction code field (formerly InvoiceNumber)
    public string? InvoiceReferenceCode { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    // Cost from FeedFormula (per bag) if available
    public decimal? Cost { get; set; }
    // Discount-like cost coming from the POSPOS order item (per bag)
    public decimal? CostDiscountPrice { get; set; }
    // Price per bag after including discount (UnitPrice - CostDiscountPrice)
    public decimal? PriceIncludeDiscount { get; set; }
    // System-calculated total price based on PriceIncludeDiscount * Quantity
    public decimal? Sys_TotalPriceIncludeDiscount { get; set; }
    public decimal TotalPriceIncludeDiscount { get; set; }
    // POS-provided total price preserved for comparison
    public decimal? Pos_TotalPriceIncludeDiscount { get; set; }
    public DateTime FeedDate { get; set; }
    public string? Notes { get; set; }
    public string? ExternalReference { get; set; }
    // New fields for external product mapping
    public string? ExternalProductCode { get; set; }
    public string? ExternalProductName { get; set; }
    public bool UnmappedProduct { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
    // Backward compatibility property
    [Obsolete("Use TransactionCode instead")]
    public string InvoiceNumber
    {
        get => TransactionCode;
        set => TransactionCode = value;
    }
    
    // Helper methods for external integration
    public void RecalculateTotalPrice()
    {
        TotalPriceIncludeDiscount = Quantity * UnitPrice;
    }
    
    public bool IsValidForImport()
    {
        return !string.IsNullOrWhiteSpace(ProductType) && 
               Quantity > 0 && 
               UnitPrice > 0;
    }
}

/// <summary>
/// DTO for deposit calculation information used in dialogs
/// </summary>
public record DepositCalculationInfo
{
    public int PigQuantity { get; init; }
    public decimal DepositPerPig { get; init; }
    public decimal ExpectedTotalDeposit { get; init; }
    public decimal CurrentTotalDeposits { get; init; }
    public decimal RemainingDeposit { get; init; }
    public decimal CompletionPercentage { get; init; }
    public DepositCompletionStatus Status { get; init; }
    
    // Display properties
    public string ExpectedTotalDepositFormatted => ExpectedTotalDeposit.FormatThaiBaht();
    public string CurrentTotalDepositsFormatted => CurrentTotalDeposits.FormatThaiBaht();
    public string RemainingDepositFormatted => RemainingDeposit.FormatThaiBaht();
    public string CompletionPercentageFormatted => $"{CompletionPercentage:P0}";
    public string DepositPerPigFormatted => DepositPerPig.FormatThaiBaht();
    
    /// <summary>
    /// Create deposit calculation info from pig pen and deposits
    /// </summary>
    public static DepositCalculationInfo Create(PigPen pigPen, IEnumerable<Deposit> deposits)
    {
        var expectedTotal = pigPen.CalculateExpectedDepositAmount();
        var currentTotal = deposits.CalculateTotalDeposits();
        var remaining = pigPen.CalculateRemainingDeposit(deposits);
        var completion = pigPen.CalculateDepositCompletionPercentage(deposits);
        var status = pigPen.GetDepositStatus(deposits);
        
        return new DepositCalculationInfo
        {
            PigQuantity = pigPen.PigQty,
            DepositPerPig = pigPen.DepositPerPig,
            ExpectedTotalDeposit = expectedTotal,
            CurrentTotalDeposits = currentTotal,
            RemainingDeposit = remaining,
            CompletionPercentage = completion,
            Status = status
        };
    }
}

/// <summary>
/// DTO for harvest calculation information used in dialogs and progress display
/// </summary>
public record HarvestCalculationInfo
{
    public int TotalPigQuantity { get; init; }
    public int HarvestedPigQuantity { get; init; }
    public int RemainingPigQuantity { get; init; }
    public decimal CompletionPercentage { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal AveragePricePerKg { get; init; }
    public HarvestCompletionStatus Status { get; init; }
    
    // Display properties
    public string TotalRevenueFormatted => TotalRevenue.FormatThaiBaht();
    public string AveragePricePerKgFormatted => AveragePricePerKg.FormatThaiBaht();
    public string CompletionPercentageFormatted => $"{CompletionPercentage:P0}";
    
    /// <summary>
    /// Create harvest calculation info from pig pen and harvest results
    /// </summary>
    public static HarvestCalculationInfo Create(PigPen pigPen, IEnumerable<HarvestResult> harvests)
    {
        var harvestList = harvests.ToList();
        var totalHarvested = harvestList.Sum(h => h.PigCount);
        var remaining = pigPen.PigQty - totalHarvested;
        var completionPercentage = pigPen.PigQty == 0 ? 0m : (decimal)totalHarvested / pigPen.PigQty;
        var totalRevenue = harvestList.Sum(h => h.Revenue);
        var avgPricePerKg = harvestList.Any() ? harvestList.Average(h => h.SalePricePerKg) : 0m;
        
        var status = completionPercentage switch
        {
            >= 1.0m => HarvestCompletionStatus.Complete,
            >= 0.5m => HarvestCompletionStatus.Partial,
            > 0m => HarvestCompletionStatus.Started,
            _ => HarvestCompletionStatus.None
        };
        
        return new HarvestCalculationInfo
        {
            TotalPigQuantity = pigPen.PigQty,
            HarvestedPigQuantity = totalHarvested,
            RemainingPigQuantity = Math.Max(0, remaining),
            CompletionPercentage = completionPercentage,
            TotalRevenue = totalRevenue,
            AveragePricePerKg = avgPricePerKg,
            Status = status
        };
    }
}

// New DTOs for feed progress visualization
public record FeedProgress(
    decimal RequiredBags,
    decimal ActualBags,
    decimal PercentageComplete,
    bool IsOnTrack,
    bool IsOverFeeding,
    string Status
);

public record FeedProgressSummary(
    Guid PigPenId,
    string PenCode,
    int PigCount,
    FeedFormula? FeedFormula,
    FeedProgress Progress,
    List<FeedBagUsage> RecentFeeds
);

public record FeedBagUsage(
    DateTime Date,
    string ProductName,
    decimal BagsUsed,
    decimal CostPerBag,
    decimal TotalCost
);

// DTOs for product import feature (spec 007)
public class ImportRequest
{
    public List<Guid> ProductIds { get; set; } = new();
}

public class ImportItemResult
{
    public Guid? ProductId { get; set; }
    public string Status { get; set; } = "";
    public string? Message { get; set; }
}

public class ImportResult
{
    public ImportSummary Summary { get; set; } = new();
    public List<ImportItemResult> Items { get; set; } = new();
}

public class ImportSummary
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Failed { get; set; }
}

// POSPOS external system DTOs (shared between client and server)
/// <summary>
/// DTO representing a product from POSPOS API response.
/// Used for product import and search functionality.
/// </summary>
public class PosposProductDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Cost { get; set; }
    public PosposCategoryDto? Category { get; set; }
    public PosposUnitDto? Unit { get; set; }
    public DateTime? LastUpdate { get; set; }
}

public class PosposCategoryDto
{
    public string Name { get; set; } = string.Empty;
}

public class PosposUnitDto
{
    public string Name { get; set; } = string.Empty;
}
