using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Features.PigPens;

namespace PigFarmManagement.Server.Features.Feeds;

public interface IFeedService
{
    Task<List<FeedItem>> GetFeedsByPigPenIdAsync(Guid pigPenId);
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
        return await _feedRepository.GetByPigPenIdAsync(pigPenId);
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

        return await _feedRepository.CreateAsync(feedItem);
    }

    public async Task<bool> DeleteFeedAsync(Guid id)
    {
        var feedItem = await _feedRepository.GetByIdAsync(id);
        if (feedItem == null)
        {
            throw new InvalidOperationException("Feed item not found");
        }

        return await _feedRepository.DeleteAsync(id);
    }
}
