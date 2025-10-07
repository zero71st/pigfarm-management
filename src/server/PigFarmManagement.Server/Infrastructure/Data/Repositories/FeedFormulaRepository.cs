using Microsoft.EntityFrameworkCore;
using PigFarmManagement.Server.Infrastructure.Data.Entities;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Repositories;

public class FeedFormulaRepository : IFeedFormulaRepository
{
    private readonly PigFarmDbContext _context;

    public FeedFormulaRepository(PigFarmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FeedFormula>> GetAllAsync()
    {
        var entities = await _context.FeedFormulas
            .OrderBy(f => f.Name)
            .ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<FeedFormula?> GetByIdAsync(Guid id)
    {
        var entity = await _context.FeedFormulas.FindAsync(id);
        return entity?.ToModel();
    }

    public async Task<FeedFormula?> GetByCodeAsync(string code)
    {
        var entity = await _context.FeedFormulas
            .FirstOrDefaultAsync(f => f.Code == code);
        return entity?.ToModel();
    }

    public async Task<FeedFormula> CreateAsync(FeedFormulaCreateDto dto)
    {
        var now = DateTime.UtcNow;
        var entity = new FeedFormulaEntity
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Name = dto.Name,
            CategoryName = dto.CategoryName,
            Brand = dto.Brand,
            ConsumeRate = dto.ConsumeRate,
            Cost = dto.Cost,
            UnitName = dto.UnitName,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.FeedFormulas.Add(entity);
        await _context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<FeedFormula> UpdateAsync(Guid id, FeedFormulaUpdateDto dto)
    {
        var entity = await _context.FeedFormulas.FindAsync(id);
        if (entity == null)
            throw new InvalidOperationException($"Feed formula with ID {id} not found");

        entity.Code = dto.Code;
        entity.Name = dto.Name;
        entity.CategoryName = dto.CategoryName;
        entity.Brand = dto.Brand;
        entity.ConsumeRate = dto.ConsumeRate;
        entity.Cost = dto.Cost;
        entity.UnitName = dto.UnitName;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.FeedFormulas.FindAsync(id);
        if (entity == null)
            return false;

        _context.FeedFormulas.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string code)
    {
        return await _context.FeedFormulas
            .AnyAsync(f => f.Code == code);
    }

    public async Task<IEnumerable<FeedFormula>> CreateManyAsync(IEnumerable<FeedFormulaCreateDto> dtos)
    {
        var now = DateTime.UtcNow;
        var entities = dtos.Select(dto => new FeedFormulaEntity
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Name = dto.Name,
            CategoryName = dto.CategoryName,
            Brand = dto.Brand,
            ConsumeRate = dto.ConsumeRate,
            Cost = dto.Cost,
            UnitName = dto.UnitName,
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();

        _context.FeedFormulas.AddRange(entities);
        await _context.SaveChangesAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<IEnumerable<FeedFormula>> GetByCodesAsync(IEnumerable<string> codes)
    {
        var codeList = codes.ToList();
        var entities = await _context.FeedFormulas
            .Where(f => f.Code != null && codeList.Contains(f.Code))
            .ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<IEnumerable<FeedFormula>> GetByExternalIdsAsync(IEnumerable<Guid> externalIds)
    {
        var idList = externalIds.ToList();
        var entities = await _context.FeedFormulas
            .Where(f => f.ExternalId.HasValue && idList.Contains(f.ExternalId.Value))
            .ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<FeedFormula> UpsertByExternalIdAsync(Guid externalId, FeedFormulaCreateDto dto)
    {
        var existingEntity = await _context.FeedFormulas
            .FirstOrDefaultAsync(f => f.ExternalId == externalId);

        if (existingEntity != null)
        {
            // Update existing
            existingEntity.Code = dto.Code;
            existingEntity.Name = dto.Name;
            existingEntity.CategoryName = dto.CategoryName;
            existingEntity.Brand = dto.Brand;
            existingEntity.ConsumeRate = dto.ConsumeRate;
            existingEntity.Cost = dto.Cost;
            existingEntity.UnitName = dto.UnitName;
            existingEntity.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new
            var now = DateTime.UtcNow;
            existingEntity = new FeedFormulaEntity
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Code = dto.Code,
                Name = dto.Name,
                CategoryName = dto.CategoryName,
                Brand = dto.Brand,
                ConsumeRate = dto.ConsumeRate,
                Cost = dto.Cost,
                UnitName = dto.UnitName,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.FeedFormulas.Add(existingEntity);
        }

        await _context.SaveChangesAsync();
        return existingEntity.ToModel();
    }
}