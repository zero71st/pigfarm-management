namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Application constants and configuration values
/// Responsibility: Define system-wide constants and default values
/// </summary>
public static class PigFarmConstants
{
    /// <summary>
    /// Default deposit amount per pig in Thai Baht
    /// </summary>
    public const decimal DEFAULT_DEPOSIT_PER_PIG = 1500m;
    
    /// <summary>
    /// Deposit completion thresholds for status calculation
    /// </summary>
    public static class DepositStatus
    {
        public const decimal COMPLETE_THRESHOLD = 1.0m; // 100%
        public const decimal PARTIAL_THRESHOLD = 0.5m;  // 50%
    }
    
    /// <summary>
    /// Currency formatting for Thai Baht
    /// </summary>
    public static class Currency
    {
        public const string THAI_BAHT_SYMBOL = "฿";
        public const string THAI_BAHT_FORMAT = "N2";
    }
}

/// <summary>
/// Extension methods for deposit calculations
/// </summary>
public static class DepositCalculationExtensions
{
    /// <summary>
    /// Calculate expected total deposit amount for a pig pen
    /// </summary>
    public static decimal CalculateExpectedDepositAmount(this PigPen pigPen)
    {
        return pigPen.PigQty * pigPen.DepositPerPig;
    }
    
    /// <summary>
    /// Calculate total current deposits for a pig pen
    /// </summary>
    public static decimal CalculateTotalDeposits(this IEnumerable<Deposit> deposits)
    {
        return deposits.Sum(d => d.Amount);
    }
    
    /// <summary>
    /// Calculate remaining deposit amount needed
    /// </summary>
    public static decimal CalculateRemainingDeposit(this PigPen pigPen, IEnumerable<Deposit> deposits)
    {
        var expectedAmount = pigPen.CalculateExpectedDepositAmount();
        var currentTotal = deposits.CalculateTotalDeposits();
        return Math.Max(0, expectedAmount - currentTotal);
    }
    
    /// <summary>
    /// Calculate deposit completion percentage
    /// </summary>
    public static decimal CalculateDepositCompletionPercentage(this PigPen pigPen, IEnumerable<Deposit> deposits)
    {
        var expectedAmount = pigPen.CalculateExpectedDepositAmount();
        if (expectedAmount == 0) return 0;
        
        var currentTotal = deposits.CalculateTotalDeposits();
        return Math.Min(1.0m, currentTotal / expectedAmount);
    }
    
    /// <summary>
    /// Get deposit status for UI display
    /// </summary>
    public static DepositCompletionStatus GetDepositStatus(this PigPen pigPen, IEnumerable<Deposit> deposits)
    {
        var completionPercentage = pigPen.CalculateDepositCompletionPercentage(deposits);
        
        return completionPercentage switch
        {
            >= PigFarmConstants.DepositStatus.COMPLETE_THRESHOLD => DepositCompletionStatus.Complete,
            >= PigFarmConstants.DepositStatus.PARTIAL_THRESHOLD => DepositCompletionStatus.Partial,
            > 0 => DepositCompletionStatus.Started,
            _ => DepositCompletionStatus.None
        };
    }
    
    /// <summary>
    /// Format Thai Baht amount for display
    /// </summary>
    public static string FormatThaiBaht(this decimal amount)
    {
        return $"{PigFarmConstants.Currency.THAI_BAHT_SYMBOL}{amount.ToString(PigFarmConstants.Currency.THAI_BAHT_FORMAT)}";
    }
}