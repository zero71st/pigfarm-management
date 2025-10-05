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
            .Include(p => p.FormulaAssignments)
            .ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<PigPen?> GetByIdAsync(Guid id)
    {
        var entity = await _context.PigPens
            .Include(p => p.Customer)
            .Include(p => p.FormulaAssignments)
            .FirstOrDefaultAsync(p => p.Id == id);
        return entity?.ToModel();
    }

    public async Task<IEnumerable<PigPen>> GetByCustomerIdAsync(Guid customerId)
    {
        var entities = await _context.PigPens
            .Include(p => p.Customer)
            .Include(p => p.FormulaAssignments)
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
        entity.DepositPerPig = pigPen.DepositPerPig;
        entity.SelectedBrand = pigPen.SelectedBrand;
        entity.Note = pigPen.Note;
        entity.IsCalculationLocked = pigPen.IsCalculationLocked;
        entity.UpdatedAt = DateTime.UtcNow;

        // Update formula assignments
        // Remove existing assignments
        var existingAssignments = _context.PigPenFormulaAssignments.Where(fa => fa.PigPenId == pigPen.Id);
        _context.PigPenFormulaAssignments.RemoveRange(existingAssignments);

        // Add new assignments
        foreach (var assignment in pigPen.FormulaAssignments)
        {
            var assignmentEntity = PigPenFormulaAssignmentEntity.FromModel(assignment);
            _context.PigPenFormulaAssignments.Add(assignmentEntity);
        }

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
