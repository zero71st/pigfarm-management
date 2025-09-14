using Microsoft.EntityFrameworkCore;
using PigFarmManagement.Server.Infrastructure.Data.Entities;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Repositories;

public class PigPenRepository : IPigPenRepository
{
    private readonly PigFarmDbContext _context;

    public PigPenRepository(PigFarmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PigPen>> GetAllAsync()
    {
        var entities = await _context.PigPens
            .Include(p => p.Customer)
            .ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<PigPen?> GetByIdAsync(Guid id)
    {
        var entity = await _context.PigPens
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == id);
        return entity?.ToModel();
    }

    public async Task<IEnumerable<PigPen>> GetByCustomerIdAsync(Guid customerId)
    {
        var entities = await _context.PigPens
            .Include(p => p.Customer)
            .Where(p => p.CustomerId == customerId)
            .ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<PigPen?> GetByPenCodeAsync(string penCode)
    {
        var entity = await _context.PigPens
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.PenCode == penCode);
        return entity?.ToModel();
    }

    public async Task<PigPen> CreateAsync(PigPen pigPen)
    {
        var entity = PigPenEntity.FromModel(pigPen);
        _context.PigPens.Add(entity);
        await _context.SaveChangesAsync();

        // Reload with customer data
        await _context.Entry(entity).Reference(p => p.Customer).LoadAsync();
        return entity.ToModel();
    }

    public async Task<PigPen> UpdateAsync(PigPen pigPen)
    {
        var entity = await _context.PigPens
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == pigPen.Id);

        if (entity == null)
            throw new ArgumentException($"PigPen with ID {pigPen.Id} not found");

        entity.CustomerId = pigPen.CustomerId;
        entity.PenCode = pigPen.PenCode;
        entity.PigQty = pigPen.PigQty;
        entity.RegisterDate = pigPen.RegisterDate;
        entity.ActHarvestDate = pigPen.ActHarvestDate;
        entity.EstimatedHarvestDate = pigPen.EstimatedHarvestDate;
        entity.FeedCost = pigPen.FeedCost;
        entity.Investment = pigPen.Investment;
        entity.ProfitLoss = pigPen.ProfitLoss;
        entity.Type = pigPen.Type;
        entity.FeedFormulaId = pigPen.FeedFormulaId;
        entity.DepositPerPig = pigPen.DepositPerPig;
        entity.SelectedBrand = pigPen.SelectedBrand;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.PigPens.FindAsync(id);
        if (entity != null)
        {
            _context.PigPens.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
