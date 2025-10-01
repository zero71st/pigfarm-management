using System.Net.Http.Json;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Client.Features.Feeds.Services;

public interface IFeedImportService
{
    Task<FeedImportResult> ImportPosPosFeedDataAsync(List<PosPosFeedTransaction> transactions);
    Task<FeedImportResult> ImportFromJsonAsync(string jsonContent);
    // Mock-specific methods removed. Client should use date-range or JSON import flows.
    Task<FeedImportResult> ImportPosPosFeedForPigPenAsync(Guid pigPenId, List<PosPosFeedTransaction> transactions);
    Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerCodeAsync(string customerCode);
    Task<List<PosPosFeedTransaction>> GetPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerAndDateRangeAsync(string customerCode, DateTime fromDate, DateTime toDate);
    Task<List<PosPosFeedTransaction>> GetAllPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<FeedImportResult> ImportPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate);
}
