namespace PigFarmManagement.Shared.Models;

/// <summary>
/// DTO for customer deletion validation results to prevent invalid deletions
/// </summary>
public class CustomerDeletionValidation
{
    public bool CanDelete { get; set; }
    public List<string> BlockingReasons { get; set; } = new();
    public int ActivePigPenCount { get; set; }
    public int ActiveTransactionCount { get; set; }
    public bool HasRecentActivity { get; set; }
}