using Microsoft.EntityFrameworkCore;
using PigFarmManagement.Server.Infrastructure.Data.Entities;
using PigFarmManagement.Shared.Models;

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

    public async Task<Feed?> FindByExternalReferenceAsync(string externalReference)
    {
        if (string.IsNullOrWhiteSpace(externalReference)) return null;
        var entity = await _context.Feeds.FirstOrDefaultAsync(f => f.ExternalReference == externalReference);
        return entity?.ToModel();
    }

    public async Task<Feed> CreateAsync(Feed feed)
    {
        var entity = FeedEntity.FromModel(feed);
        _context.Feeds.Add(entity);
        await _context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<Feed> CreateIfNotExistsAsync(Feed feed)
    {
        if (string.IsNullOrWhiteSpace(feed.ExternalReference))
        {
            // Fallback to normal create when no external reference provided
            return await CreateAsync(feed);
        }

        // Try to insert; if a unique constraint violation occurs, return the existing record
        var entity = FeedEntity.FromModel(feed);
        _context.Feeds.Add(entity);
        try
        {
            await _context.SaveChangesAsync();
            return entity.ToModel();
        }
        catch (DbUpdateException)
        {
            // Likely unique constraint violation on ExternalReference. Attempt to return existing.
            var existing = await _context.Feeds.FirstOrDefaultAsync(f => f.ExternalReference == feed.ExternalReference);
            if (existing != null) return existing.ToModel();
            // If we can't find it, rethrow to signal unexpected failure
            throw;
        }
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
        entity.Quantity = feed.Quantity;
        entity.UnitPrice = feed.UnitPrice;
        entity.TotalPrice = feed.TotalPrice;
        entity.ExternalReference = feed.ExternalReference;
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
}
