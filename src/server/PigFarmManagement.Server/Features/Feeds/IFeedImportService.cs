using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.Feeds;

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
    
    // Note: mock-specific methods were removed. Use live POSPOS flows or ImportFromJsonAsync for testing.
    
    /// <summary>
    /// Import POSPOS feed data for a specific pig pen
    /// </summary>
    Task<FeedImportResult> ImportPosPosFeedForPigPenAsync(Guid pigPenId, List<PosPosFeedTransaction> transactions);
    
    /// <summary>
    /// Get POSPOS feed data by customer code
    /// </summary>
    Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerCodeAsync(string customerCode);
    
    /// <summary>
    /// Get POSPOS feed data by date range
    /// </summary>
    Task<List<PosPosFeedTransaction>> GetPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate);
    
    /// <summary>
    /// Get POSPOS feed data by customer code and date range
    /// </summary>
    Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerAndDateRangeAsync(string customerCode, DateTime fromDate, DateTime toDate);
    
    /// <summary>
    /// Get all POSPOS feed data by date range (without customer filtering)
    /// </summary>
    Task<List<PosPosFeedTransaction>> GetAllPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate);
    
    /// <summary>
    /// Import POSPOS feed data by date range
    /// </summary>
    Task<FeedImportResult> ImportPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate);
    
    /// <summary>
    /// Create demo feeds with complete product information for testing
    /// </summary>
}