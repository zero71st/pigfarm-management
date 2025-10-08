using System.Net.Http.Json;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Client.Features.Feeds.Services;

public interface IFeedImportService
{
    Task<FeedImportResult> ImportPosPosFeedDataAsync(List<PosPosTransaction> transactions);
    Task<FeedImportResult> ImportFromJsonAsync(string jsonContent);
    // Mock-specific methods removed. Client should use date-range or JSON import flows.
    Task<FeedImportResult> ImportPosPosFeedForPigPenAsync(Guid pigPenId, List<PosPosTransaction> transactions);
    Task<List<PosPosTransaction>> GetPosPosFeedByCustomerCodeAsync(string customerCode);
    Task<List<PosPosTransaction>> GetPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<List<PosPosTransaction>> GetPosPosFeedByCustomerAndDateRangeAsync(string customerCode, DateTime fromDate, DateTime toDate);
    Task<List<PosPosTransaction>> GetAllPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<FeedImportResult> ImportPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate);
}
