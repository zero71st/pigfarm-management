using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Shared.Services;

public interface IFeedImportService
{
    Task<FeedImportResult> ImportPosPosFeedDataAsync(List<PosPosFeedTransaction> transactions);
    Task<FeedImportResult> ImportFromJsonAsync(string jsonContent);
    Task<List<PosPosFeedTransaction>> GetMockPosPosFeedDataAsync();
    Task<FeedImportResult> ImportPosPosFeedForPigPenAsync(Guid pigPenId, List<PosPosFeedTransaction> transactions);
    Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerCodeAsync(string customerCode);
}
