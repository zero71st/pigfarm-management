using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Client.Features.Feeds.Services;

public interface IFeedService
{
    Task<List<FeedItem>> GetFeedsByPigPenIdAsync(Guid pigPenId);
    Task<FeedItem> AddFeedToPigPenAsync(Guid pigPenId, FeedCreateDto dto);
    Task<bool> DeleteFeedAsync(Guid pigPenId, Guid feedItemId);
}
