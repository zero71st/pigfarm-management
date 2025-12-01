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

    public async Task<(PigPen pigPen, int updatedAssignmentCount)> UpdateAsync(PigPen pigPen)
    {
        // T005: Load with formula assignments for recalculation
        var entity = await _context.PigPens
            .Include(p => p.Customer)
            .Include(p => p.FormulaAssignments)
            .FirstOrDefaultAsync(p => p.Id == pigPen.Id);

        if (entity == null)
            throw new ArgumentException($"PigPen with ID {pigPen.Id} not found");

        // Track old PigQty to detect changes
        var oldPigQty = entity.PigQty;
        var updatedAssignmentCount = 0;

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

        // T005: Recalculate formula assignments if PigQty changed
        // This must happen BEFORE replacing assignments, so we modify the incoming pigPen model
        var updatedFormulaAssignments = new List<PigPenFormulaAssignment>();
        
        if (oldPigQty != pigPen.PigQty)
        {
            foreach (var assignment in pigPen.FormulaAssignments.Where(a => a.IsActive && !a.IsLocked))
            {
                // T006: Use _context directly to load source formula
                var feedFormula = await _context.FeedFormulas.FindAsync(assignment.OriginalFormulaId);
                
                // T011: Error handling for missing formulas
                if (feedFormula == null)
                {
                    // Skip this assignment if formula not found, keep original
                    updatedFormulaAssignments.Add(assignment);
                    continue;
                }

                // Create updated assignment with recalculated values
                var updatedAssignment = assignment with
                {
                    AssignedPigQuantity = pigPen.PigQty,
                    AssignedBagPerPig = feedFormula.ConsumeRate ?? 0,
                    AssignedTotalBags = Math.Ceiling((feedFormula.ConsumeRate ?? 0) * pigPen.PigQty)
                };
                
                updatedFormulaAssignments.Add(updatedAssignment);
                updatedAssignmentCount++;
            }
            
            // Add inactive/locked assignments unchanged
            updatedFormulaAssignments.AddRange(pigPen.FormulaAssignments.Where(a => !a.IsActive || a.IsLocked));
        }
        else
        {
            // No PigQty change, use original assignments
            updatedFormulaAssignments.AddRange(pigPen.FormulaAssignments);
        }

        // Update formula assignments
        // Remove existing assignments
        var existingAssignments = _context.PigPenFormulaAssignments.Where(fa => fa.PigPenId == pigPen.Id);
        _context.PigPenFormulaAssignments.RemoveRange(existingAssignments);

        // Add new/updated assignments
        foreach (var assignment in updatedFormulaAssignments)
        {
            var assignmentEntity = PigPenFormulaAssignmentEntity.FromModel(assignment);
            _context.PigPenFormulaAssignments.Add(assignmentEntity);
        }

        // T010: SaveChangesAsync wraps all assignment updates in single transaction (EF default behavior)
        await _context.SaveChangesAsync();
        
        // T008: Return tuple with updated model and count
        return (entity.ToModel(), updatedAssignmentCount);
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
