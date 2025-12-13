using Microsoft.EntityFrameworkCore;
using PigFarmManagement.Server.Infrastructure.Data.Entities;
using PigFarmManagement.Shared.Models;
using PigFarmManagement.Shared.DTOs;

namespace PigFarmManagement.Server.Infrastructure.Data.Repositories;

public class FeedRepository : IFeedRepository
{
    private readonly PigFarmDbContext _context;

    public FeedRepository(PigFarmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Feed>> GetAllAsync()
    {
        var entities = await _context.Feeds.ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<Feed?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Feeds.FindAsync(id);
        return entity?.ToModel();
    }

    public async Task<IEnumerable<Feed>> GetByPigPenIdAsync(Guid pigPenId)
    {
        var entities = await _context.Feeds
            .Where(f => f.PigPenId == pigPenId)
            .OrderByDescending(f => f.FeedDate)
            .ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<Feed> CreateAsync(Feed feed)
    {
        var entity = FeedEntity.FromModel(feed);
        _context.Feeds.Add(entity);
        await _context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<IEnumerable<Feed>> CreateManyAsync(IEnumerable<Feed> feeds)
    {
        var entities = feeds.Select(FeedEntity.FromModel).ToList();
        _context.Feeds.AddRange(entities);
        await _context.SaveChangesAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<Feed> UpdateAsync(Feed feed)
    {
        var entity = await _context.Feeds.FindAsync(feed.Id);
        if (entity == null)
            throw new ArgumentException($"Feed with ID {feed.Id} not found");

        entity.PigPenId = feed.PigPenId;
        entity.FeedDate = feed.FeedDate;
        entity.ProductType = feed.ProductType;
        entity.ProductCode = feed.ProductCode;
        entity.ProductName = feed.ProductName;
        entity.TransactionCode = feed.TransactionCode;
        entity.InvoiceReferenceCode = feed.InvoiceReferenceCode;
        entity.Quantity = feed.Quantity;
        entity.UnitPrice = feed.UnitPrice;
        entity.Cost = feed.Cost;
        entity.CostDiscountPrice = feed.CostDiscountPrice;
        entity.PriceIncludeDiscount = feed.PriceIncludeDiscount;
        entity.Sys_TotalPriceIncludeDiscount = feed.Sys_TotalPriceIncludeDiscount;
        entity.TotalPriceIncludeDiscount = feed.TotalPriceIncludeDiscount;
        entity.Pos_TotalPriceIncludeDiscount = feed.Pos_TotalPriceIncludeDiscount;
        entity.ExternalReference = feed.ExternalReference;
        entity.ExternalProductCode = feed.ExternalProductCode;
        entity.ExternalProductName = feed.ExternalProductName;
        entity.UnmappedProduct = feed.UnmappedProduct;
        entity.Notes = feed.Notes;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Feeds.FindAsync(id);
        if (entity != null)
        {
            _context.Feeds.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByInvoiceNumberAsync(string invoiceNumber)
    {
        // Note: Method name kept for backward compatibility, but checks TransactionCode field
        if (string.IsNullOrWhiteSpace(invoiceNumber)) return false;
        return await _context.Feeds.AnyAsync(f => f.TransactionCode == invoiceNumber);
    }

    public async Task<int> DeleteByInvoiceReferenceAsync(Guid pigPenId, string invoiceReferenceCode)
    {
        if (string.IsNullOrWhiteSpace(invoiceReferenceCode))
            return 0;

        var feedsToDelete = await _context.Feeds
            .Where(f => f.PigPenId == pigPenId && f.InvoiceReferenceCode == invoiceReferenceCode)
            .ToListAsync();

        if (!feedsToDelete.Any())
            return 0;

        _context.Feeds.RemoveRange(feedsToDelete);
        await _context.SaveChangesAsync();

        return feedsToDelete.Count;
    }

    public async Task<DateTime?> GetLastImportDateAsync(Guid pigPenId)
    {
        var lastFeed = await _context.Feeds
            .Where(f => f.PigPenId == pigPenId)
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefaultAsync();

        return lastFeed?.CreatedAt;
    }

    /// <summary>
    /// Get feed progress for all pig pens: accumulated bags, expected bags, and progress percent.
    /// </summary>
    public async Task<List<FeedProgressDto>> GetFeedProgressAsync()
    {
        // Get accumulated bags per pig pen (sum of all feed quantities, excluding service charges)
        var accumulatedByPen = await _context.Feeds
            .Where(f => !f.ProductName.Contains("ค่าขนส่ง"))  // Exclude service charge items
            .GroupBy(f => f.PigPenId)
            .Select(g => new
            {
                PigPenId = g.Key,
                AccumulatedBags = g.Sum(f => f.Quantity),
                LastImportDate = g.Max(f => f.CreatedAt)
            })
            .ToListAsync();

        // Get expected bags per pig pen (sum of all active formula assignment targets)
        var expectedByPen = await _context.PigPenFormulaAssignments
            .Where(fa => fa.IsActive && !fa.IsLocked)
            .GroupBy(fa => fa.PigPenId)
            .Select(g => new
            {
                PigPenId = g.Key,
                ExpectedBags = (int)g.Sum(fa => fa.AssignedTotalBags)
            })
            .ToListAsync();

        // Combine and calculate progress
        var expectedDict = expectedByPen.ToDictionary(x => x.PigPenId, x => x.ExpectedBags);
        
        return accumulatedByPen.Select(a => new FeedProgressDto(
            a.PigPenId,
            a.AccumulatedBags,
            expectedDict.TryGetValue(a.PigPenId, out var expected) ? expected : 0,
            expectedDict.TryGetValue(a.PigPenId, out var exp) && exp > 0 
                ? Math.Round((double)a.AccumulatedBags / exp * 100.0, 1)
                : 0.0,
            a.LastImportDate
        )).ToList();
    }
}
