using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;

namespace PigFarmManagement.Server.Features.Feeds;

public interface IFeedRepository
{
    Task<List<FeedItem>> GetByPigPenIdAsync(Guid pigPenId);
    Task<FeedItem> CreateAsync(FeedItem feedItem);
    Task<FeedItem?> GetByIdAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}

public class FeedRepository : IFeedRepository
{
    private readonly Infrastructure.Data.Repositories.IFeedRepository _efFeedRepository;

    public FeedRepository(Infrastructure.Data.Repositories.IFeedRepository efFeedRepository)
    {
        _efFeedRepository = efFeedRepository;
    }

    public async Task<List<FeedItem>> GetByPigPenIdAsync(Guid pigPenId)
    {
        var feeds = await _efFeedRepository.GetByPigPenIdAsync(pigPenId);
        return feeds.Select(ConvertToFeedItem).ToList();
    }

    public async Task<FeedItem> CreateAsync(FeedItem feedItem)
    {
        var feed = ConvertToFeed(feedItem);
        var createdFeed = await _efFeedRepository.CreateAsync(feed);
        return ConvertToFeedItem(createdFeed);
    }

    public async Task<FeedItem?> GetByIdAsync(Guid id)
    {
        var feed = await _efFeedRepository.GetByIdAsync(id);
        return feed != null ? ConvertToFeedItem(feed) : null;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _efFeedRepository.DeleteAsync(id);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static FeedItem ConvertToFeedItem(Feed feed)
    {
        return new FeedItem(
            feed.Id,
            feed.PigPenId,
            feed.ProductType, // FeedType from ProductType
            feed.Quantity,    // QuantityKg from Quantity (assuming same unit)
            feed.UnitPrice,   // PricePerKg from UnitPrice
            feed.TotalPrice,  // Cost from TotalPrice
            feed.FeedDate     // Date from FeedDate
        )
        {
            ExternalReference = feed.ExternalReference,
            Notes = feed.Notes,
            CreatedAt = feed.CreatedAt,
            UpdatedAt = feed.UpdatedAt
        };
    }

    private static Feed ConvertToFeed(FeedItem feedItem)
    {
        return new Feed
        {
            Id = feedItem.Id,
            PigPenId = feedItem.PigPenId,
            ProductType = feedItem.FeedType,
            Quantity = (int)feedItem.QuantityKg, // Convert decimal to int
            UnitPrice = feedItem.PricePerKg,
            TotalPrice = feedItem.Cost,
            FeedDate = feedItem.Date,
            ExternalReference = feedItem.ExternalReference,
            Notes = feedItem.Notes,
            CreatedAt = feedItem.CreatedAt,
            UpdatedAt = feedItem.UpdatedAt
        };
    }
}
