using Microsoft.EntityFrameworkCore;
using PigFarmManagement.Server.Infrastructure.Data.Entities;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Repositories;

public class HarvestRepository : IHarvestRepository
{
    private readonly PigFarmDbContext _context;

    public HarvestRepository(PigFarmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<HarvestResult>> GetAllAsync()
    {
        var entities = await _context.Harvests.ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<HarvestResult?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Harvests.FindAsync(id);
        return entity?.ToModel();
    }

    public async Task<IEnumerable<HarvestResult>> GetByPigPenIdAsync(Guid pigPenId)
    {
        var entities = await _context.Harvests
            .Where(h => h.PigPenId == pigPenId)
            .OrderByDescending(h => h.HarvestDate)
            .ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<HarvestResult> CreateAsync(HarvestResult harvest)
    {
        var entity = HarvestEntity.FromModel(harvest);
        _context.Harvests.Add(entity);
        await _context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<HarvestResult> UpdateAsync(HarvestResult harvest)
    {
        var entity = await _context.Harvests.FindAsync(harvest.Id);
        if (entity == null)
            throw new ArgumentException($"Harvest with ID {harvest.Id} not found");

        entity.PigPenId = harvest.PigPenId;
        entity.HarvestDate = harvest.HarvestDate;
        entity.PigCount = harvest.PigCount;
        entity.AvgWeight = harvest.AvgWeight;
        entity.MinWeight = harvest.MinWeight;
        entity.MaxWeight = harvest.MaxWeight;
        entity.TotalWeight = harvest.TotalWeight;
        entity.SalePricePerKg = harvest.SalePricePerKg;
        entity.Revenue = harvest.Revenue;

        await _context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Harvests.FindAsync(id);
        if (entity != null)
        {
            _context.Harvests.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
