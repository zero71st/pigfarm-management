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
            .Include(p => p.Harvests)
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
        // PostgreSQL timestamp with time zone requires UTC-normalized DateTime
        entity.RegisterDate = DateTime.SpecifyKind(pigPen.RegisterDate, DateTimeKind.Utc);
        entity.ActHarvestDate = pigPen.ActHarvestDate.HasValue 
            ? DateTime.SpecifyKind(pigPen.ActHarvestDate.Value, DateTimeKind.Utc) 
            : null;
        entity.EstimatedHarvestDate = pigPen.EstimatedHarvestDate.HasValue 
            ? DateTime.SpecifyKind(pigPen.EstimatedHarvestDate.Value, DateTimeKind.Utc) 
            : null;
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

        // Update feed item costs from current formula costs (if formulas exist)
        var feedItems = await _context.Feeds
            .Where(f => f.PigPenId == pigPen.Id)
            .ToListAsync();
        
        foreach (var feedItem in feedItems)
        {
            // Try to find matching formula by product code
            var formula = await _context.FeedFormulas
                .FirstOrDefaultAsync(ff => ff.Code == feedItem.ProductCode);
            
            if (formula != null && formula.Cost.HasValue && feedItem.Cost != formula.Cost.Value)
            {
                // Update feed item cost to match current formula cost
                feedItem.Cost = formula.Cost.Value;
                feedItem.UpdatedAt = DateTime.UtcNow;
            }
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

    public async Task<PigPen> ForceCloseAsync(Guid pigPenId)
    {
        var entity = await _context.PigPens
            .Include(p => p.FormulaAssignments)
            .FirstOrDefaultAsync(p => p.Id == pigPenId);

        if (entity == null)
            throw new ArgumentException($"PigPen with ID {pigPenId} not found");

        // Lock existing active assignments in-place to avoid remove/add cycles
        var now = DateTime.UtcNow;
        foreach (var assignment in entity.FormulaAssignments.Where(a => a.IsActive))
        {
            assignment.IsActive = false;
            assignment.IsLocked = true;
            assignment.LockReason = "ForceClosed";
            assignment.LockedAt = now;
            assignment.UpdatedAt = now;
        }

        // Set harvest date and lock calculations
        // Normalize to UTC for PostgreSQL compatibility
        entity.ActHarvestDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);
        entity.IsCalculationLocked = true;
        entity.UpdatedAt = now;

        await _context.SaveChangesAsync();

        // Reload assignments to ensure navigation is in sync
        await _context.Entry(entity).Collection(e => e.FormulaAssignments).LoadAsync();

        return entity.ToModel();
    }

    public async Task<PigPen> ReopenAsync(Guid pigPenId)
    {
        var entity = await _context.PigPens
            .Include(p => p.FormulaAssignments)
            .FirstOrDefaultAsync(p => p.Id == pigPenId);

        if (entity == null)
            throw new ArgumentException($"PigPen with ID {pigPenId} not found");

        if (!entity.IsCalculationLocked)
            throw new ArgumentException($"PigPen with ID {pigPenId} is not closed, cannot reopen");

        // Clear the force-close state
        var now = DateTime.UtcNow;
        
        // Clear harvest date and unlock calculations
        entity.ActHarvestDate = null;
        entity.IsCalculationLocked = false;
        entity.UpdatedAt = now;

        // Note: We don't unlock formula assignments here because:
        // 1. They were locked when force-closed and should remain locked
        // 2. Users can regenerate assignments if needed
        // 3. This preserves the historical state of what was force-closed

        await _context.SaveChangesAsync();

        // Reload assignments to ensure navigation is in sync
        await _context.Entry(entity).Collection(e => e.FormulaAssignments).LoadAsync();

        return entity.ToModel();
    }

    public async Task UpdateTimestampAsync(Guid pigPenId)
    {
        var entity = await _context.PigPens.FindAsync(pigPenId);
        if (entity == null)
            return;

        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
