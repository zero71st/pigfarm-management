using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Shared.Contracts;

/// <summary>
/// Service contracts for feed import functionality
/// Responsibility: Define the contract for external feed data integration services
/// </summary>

public interface IFeedImportService
{
    /// <summary>
    /// Import feed data from POSPOS transactions
    /// </summary>
    Task<FeedImportResult> ImportPosPosFeedDataAsync(List<PosPosFeedTransaction> transactions);
    
    /// <summary>
    /// Import feed data from JSON content
    /// </summary>
    Task<FeedImportResult> ImportFromJsonAsync(string jsonContent);
    
    /// <summary>
    /// Get mock POSPOS feed data for testing
    /// </summary>
    Task<List<PosPosFeedTransaction>> GetMockPosPosFeedDataAsync();
    
    /// <summary>
    /// Import POSPOS feed data for a specific pig pen
    /// </summary>
    Task<FeedImportResult> ImportPosPosFeedForPigPenAsync(Guid pigPenId, List<PosPosFeedTransaction> transactions);
    
    /// <summary>
    /// Get POSPOS feed data by customer code
    /// </summary>
    Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerCodeAsync(string customerCode);
}
