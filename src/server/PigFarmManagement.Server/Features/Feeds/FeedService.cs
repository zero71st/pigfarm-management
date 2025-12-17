using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Features.PigPens;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;

namespace PigFarmManagement.Server.Features.Feeds;

public interface IFeedService
{
    Task<List<FeedItem>> GetFeedsByPigPenIdAsync(Guid pigPenId);
    Task<List<FeedItem>> GetFeedsByPigPenIdsAsync(IEnumerable<Guid> pigPenIds);
    Task<FeedItem> AddFeedToPigPenAsync(Guid pigPenId, FeedCreateDto dto);
    Task<bool> DeleteFeedAsync(Guid id);
}

public class FeedService : IFeedService
{
    private readonly IFeedRepository _feedRepository;
    private readonly IPigPenService _pigPenService;

    public FeedService(IFeedRepository feedRepository, IPigPenService pigPenService)
    {
        _feedRepository = feedRepository;
        _pigPenService = pigPenService;
    }

    public async Task<List<FeedItem>> GetFeedsByPigPenIdAsync(Guid pigPenId)
    {
        var feeds = await _feedRepository.GetByPigPenIdAsync(pigPenId);
        return feeds.Select(ConvertToFeedItem).ToList();
    }

    public async Task<List<FeedItem>> GetFeedsByPigPenIdsAsync(IEnumerable<Guid> pigPenIds)
    {
        // Batched query for multiple pig pens (used by dashboard aggregation)
        var feeds = await _feedRepository.GetByPigPenIdsAsync(pigPenIds);
        return feeds.Select(ConvertToFeedItem).ToList();
    }

    public async Task<FeedItem> AddFeedToPigPenAsync(Guid pigPenId, FeedCreateDto dto)
    {
        // Business logic: Verify pig pen exists
        var pigPen = await _pigPenService.GetPigPenByIdAsync(pigPenId);
        if (pigPen == null)
        {
            throw new InvalidOperationException("Pig pen not found");
        }

        // Business logic: Calculate total cost
        var totalCost = dto.QuantityKg * dto.PricePerKg;

        var feedItem = new FeedItem(
            Guid.NewGuid(),
            pigPenId,
            dto.FeedType,
            "", // ProductCode - empty for manually added feeds
            dto.FeedType, // ProductName - use FeedType as product name
            "", // TransactionCode - empty for manually added feeds
            null, // InvoiceReferenceCode - null for manually added feeds
            dto.QuantityKg,
            dto.PricePerKg,
            totalCost,
            dto.Date)
        {
            // For manually added feeds, new pricing fields are null since they don't come from POSPOS
            FeedCost = null,
            CostDiscountPrice = null,
            PriceIncludeDiscount = null,
            Sys_TotalPriceIncludeDiscount = null,
            Pos_TotalPriceIncludeDiscount = null
        };

        var createdFeed = await _feedRepository.CreateAsync(ConvertToFeed(feedItem));
        return ConvertToFeedItem(createdFeed);
    }

    public async Task<bool> DeleteFeedAsync(Guid id)
    {
        var feed = await _feedRepository.GetByIdAsync(id);
        if (feed == null)
        {
            throw new InvalidOperationException("Feed item not found");
        }

        try
        {
            await _feedRepository.DeleteAsync(id);
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
            UpdatedAt = feed.UpdatedAt,
            FeedCost = feed.Cost,
            CostDiscountPrice = feed.CostDiscountPrice,
            PriceIncludeDiscount = feed.PriceIncludeDiscount,
            Sys_TotalPriceIncludeDiscount = feed.Sys_TotalPriceIncludeDiscount,
            Pos_TotalPriceIncludeDiscount = feed.Pos_TotalPriceIncludeDiscount
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
