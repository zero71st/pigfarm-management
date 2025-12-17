using Microsoft.EntityFrameworkCore;
using PigFarmManagement.Server.Infrastructure.Data.Entities;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Repositories;

public class DepositRepository : IDepositRepository
{
    private readonly PigFarmDbContext _context;

    public DepositRepository(PigFarmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Deposit>> GetAllAsync()
    {
        var entities = await _context.Deposits.ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<Deposit?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Deposits.FindAsync(id);
        return entity?.ToModel();
    }

    public async Task<IEnumerable<Deposit>> GetByPigPenIdAsync(Guid pigPenId)
    {
        var entities = await _context.Deposits
            .Where(d => d.PigPenId == pigPenId)
            .OrderByDescending(d => d.Date)
            .ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<IEnumerable<Deposit>> GetByPigPenIdsAsync(IEnumerable<Guid> pigPenIds)
    {
        // Batched query for dashboard aggregation
        var penIdList = pigPenIds.ToList();
        if (!penIdList.Any())
            return Enumerable.Empty<Deposit>();

        var entities = await _context.Deposits
            .AsNoTracking()
            .Where(d => penIdList.Contains(d.PigPenId))
            .ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<Deposit> CreateAsync(Deposit deposit)
    {
        var entity = DepositEntity.FromModel(deposit);
        _context.Deposits.Add(entity);
        await _context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<Deposit> UpdateAsync(Deposit deposit)
    {
        var entity = await _context.Deposits.FindAsync(deposit.Id);
        if (entity == null)
            throw new ArgumentException($"Deposit with ID {deposit.Id} not found");

        entity.PigPenId = deposit.PigPenId;
        entity.Date = deposit.Date;
        entity.Amount = deposit.Amount;
        entity.Remark = deposit.Remark;

        await _context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Deposits.FindAsync(id);
        if (entity != null)
        {
            _context.Deposits.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
