using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Infrastructure.Data;
using PigFarmManagement.Server.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace PigFarmManagement.Server.Features.FeedFormulas;

// DTOs for FeedFormula CRUD operations
public record FeedFormulaCreateDto(string ProductCode, string ProductName, string Brand, decimal BagPerPig);
public record FeedFormulaUpdateDto(string ProductCode, string ProductName, string Brand, decimal BagPerPig);

public interface IFeedFormulaService
{
    Task<IEnumerable<FeedFormula>> GetAllFeedFormulasAsync();
    Task<FeedFormula?> GetFeedFormulaByIdAsync(Guid id);
    Task<FeedFormula> CreateFeedFormulaAsync(FeedFormulaCreateDto dto);
    Task<FeedFormula> UpdateFeedFormulaAsync(Guid id, FeedFormulaUpdateDto dto);
    Task<bool> DeleteFeedFormulaAsync(Guid id);
    Task<bool> ExistsAsync(string productCode);
}

public class FeedFormulaService : IFeedFormulaService
{
    private readonly PigFarmDbContext _context;

    public FeedFormulaService(PigFarmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FeedFormula>> GetAllFeedFormulasAsync()
    {
        var entities = await _context.FeedFormulas
            .OrderBy(f => f.ProductName)
            .ToListAsync();
        
        return entities.Select(e => e.ToModel());
    }

    public async Task<FeedFormula?> GetFeedFormulaByIdAsync(Guid id)
    {
        var entity = await _context.FeedFormulas.FindAsync(id);
        return entity?.ToModel();
    }

    public async Task<FeedFormula> CreateFeedFormulaAsync(FeedFormulaCreateDto dto)
    {
        var now = DateTime.UtcNow;
        var entity = new FeedFormulaEntity
        {
            Id = Guid.NewGuid(),
            ProductCode = dto.ProductCode,
            ProductName = dto.ProductName,
            Brand = dto.Brand,
            BagPerPig = dto.BagPerPig,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.FeedFormulas.Add(entity);
        await _context.SaveChangesAsync();

        return entity.ToModel();
    }

    public async Task<FeedFormula> UpdateFeedFormulaAsync(Guid id, FeedFormulaUpdateDto dto)
    {
        var entity = await _context.FeedFormulas.FindAsync(id);
        if (entity == null)
            throw new InvalidOperationException($"Feed formula with ID {id} not found");

        entity.ProductCode = dto.ProductCode;
        entity.ProductName = dto.ProductName;
        entity.Brand = dto.Brand;
        entity.BagPerPig = dto.BagPerPig;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<bool> DeleteFeedFormulaAsync(Guid id)
    {
        var entity = await _context.FeedFormulas.FindAsync(id);
        if (entity == null)
            return false;

        _context.FeedFormulas.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string productCode)
    {
        return await _context.FeedFormulas
            .AnyAsync(f => f.ProductCode == productCode);
    }
}
