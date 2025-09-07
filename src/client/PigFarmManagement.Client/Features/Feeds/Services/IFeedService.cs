using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Client.Features.Feeds.Services;

public record FeedCreateDto(string FeedType, decimal QuantityKg, decimal PricePerKg, DateTime Date);

public interface IFeedService
{
    Task<List<FeedItem>> GetFeedsByPigPenIdAsync(Guid pigPenId);
    Task<FeedItem> AddFeedToPigPenAsync(Guid pigPenId, FeedCreateDto dto);
    Task<bool> DeleteFeedAsync(Guid id);
}
