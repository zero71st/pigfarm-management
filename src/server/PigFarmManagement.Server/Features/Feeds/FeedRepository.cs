using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Infrastructure.Data;

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
    private readonly InMemoryDataStore _dataStore;

    public FeedRepository(InMemoryDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Task<List<FeedItem>> GetByPigPenIdAsync(Guid pigPenId)
    {
        var feeds = _dataStore.Feeds.Where(f => f.PigPenId == pigPenId).ToList();
        return Task.FromResult(feeds);
    }

    public Task<FeedItem> CreateAsync(FeedItem feedItem)
    {
        _dataStore.Feeds.Add(feedItem);
        return Task.FromResult(feedItem);
    }

    public Task<FeedItem?> GetByIdAsync(Guid id)
    {
        var feedItem = _dataStore.Feeds.FirstOrDefault(f => f.Id == id);
        return Task.FromResult(feedItem);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var feedItem = _dataStore.Feeds.FirstOrDefault(f => f.Id == id);
        if (feedItem != null)
        {
            _dataStore.Feeds.Remove(feedItem);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
