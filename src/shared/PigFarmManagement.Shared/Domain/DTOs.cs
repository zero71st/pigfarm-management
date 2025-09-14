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
    public string InvoiceNumber { get; set; } = ""; // Add invoice number field
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime FeedDate { get; set; }
    public string? Notes { get; set; }
    public string? ExternalReference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
    // Helper methods for external integration
    public void RecalculateTotalPrice()
    {
        TotalPrice = Quantity * UnitPrice;
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
