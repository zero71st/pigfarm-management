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
            feed.ProductName, // FeedType from ProductName
            feed.ProductCode, // ProductCode
            feed.ProductName, // ProductName
            feed.TransactionCode, // TransactionCode
            feed.InvoiceReferenceCode, // InvoiceReferenceCode
            feed.Quantity,    // Quantity represents number of bags
            feed.UnitPrice,   // UnitPrice represents price per bag
            feed.TotalPriceIncludeDiscount,  // Cost from TotalPriceIncludeDiscount
            feed.FeedDate     // Date from FeedDate
        )
        {
            ExternalReference = feed.ExternalReference,
            ExternalProductCode = feed.ExternalProductCode,
            ExternalProductName = feed.ExternalProductName,
            InvoiceReferenceCode = feed.InvoiceReferenceCode,
            UnmappedProduct = feed.UnmappedProduct,
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
            ProductCode = feedItem.ProductCode, // Add ProductCode mapping
            ProductName = feedItem.ProductName, // Add ProductName mapping
            TransactionCode = feedItem.TransactionCode, // Add TransactionCode mapping
            Quantity = (int)feedItem.Quantity, // Quantity stored as bags (int)
            UnitPrice = feedItem.PricePerBag,
            TotalPriceIncludeDiscount = feedItem.Cost,
            FeedDate = feedItem.Date,
            ExternalReference = feedItem.ExternalReference,
            ExternalProductCode = feedItem.ExternalProductCode,
            ExternalProductName = feedItem.ExternalProductName,
            InvoiceReferenceCode = feedItem.InvoiceReferenceCode,
            UnmappedProduct = feedItem.UnmappedProduct,
            Notes = feedItem.Notes,
            CreatedAt = feedItem.CreatedAt,
            UpdatedAt = feedItem.UpdatedAt
        };
    }
}
