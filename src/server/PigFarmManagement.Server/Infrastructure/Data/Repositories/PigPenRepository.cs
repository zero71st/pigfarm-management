using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PigFarmManagement.Server.Infrastructure.Data.Entities;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Repositories;

public class PigPenRepository : IPigPenRepository
{
    private readonly PigFarmDbContext _context;
    private readonly ILogger<PigPenRepository> _logger;

    public PigPenRepository(PigFarmDbContext context, ILogger<PigPenRepository> logger)
    {
        _context = context;
        _logger = logger;
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

    public async Task<IEnumerable<PigPen>> GetActiveAsync()
    {
        // Dashboard queries - return only essential fields, no related collections
        var entities = await _context.PigPens
            .AsNoTracking()
            .Where(p => !p.IsCalculationLocked)
            .Select(p => new PigPenEntity
            {
                Id = p.Id,
                CustomerId = p.CustomerId,
                PenCode = p.PenCode,
                PigQty = p.PigQty,
                RegisterDate = p.RegisterDate,
                ActHarvestDate = p.ActHarvestDate,
                EstimatedHarvestDate = p.EstimatedHarvestDate,
                FeedCost = p.FeedCost,
                Investment = p.Investment,
                ProfitLoss = p.ProfitLoss,
                Type = p.Type,
                DepositPerPig = p.DepositPerPig,
                SelectedBrand = p.SelectedBrand,
                Note = p.Note,
                IsCalculationLocked = p.IsCalculationLocked,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
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

    public async Task<(PigPen pigPen, int updatedAssignmentCount)> UpdateAsync(PigPen pigPen, IEnumerable<string>? preserveProductCodes = null)
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

        // Detect used products from Feed history and merge with client preserve list
        var usedProductCodes = await _context.Feeds
            .Where(f => f.PigPenId == pigPen.Id && !string.IsNullOrEmpty(f.ProductCode))
            .Select(f => f.ProductCode!)
            .Distinct()
            .ToListAsync();

        var preservedCodes = preserveProductCodes?.ToHashSet(StringComparer.OrdinalIgnoreCase)
                             ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Auto-preserve products already used in feeds
        foreach (var code in usedProductCodes)
            preservedCodes.Add(code);

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
            // Get preserved assignments (keep their original pig quantity - already fed)
            var preservedAssignments = pigPen.FormulaAssignments
                .Where(a => a.IsActive && !a.IsLocked && preservedCodes.Contains(a.ProductCode))
                .ToList();

            // Get unlocked and non-preserved assignments for redistribution
            var unlocked = pigPen.FormulaAssignments
                .Where(a => a.IsActive && !a.IsLocked && !preservedCodes.Contains(a.ProductCode))
                .ToList();

            // Add preserved assignments unchanged (keep original AssignedPigQuantity, but sync bag-per-pig from formula)
            foreach (var assignment in preservedAssignments)
            {
                var feedFormula = await _context.FeedFormulas.FindAsync(assignment.OriginalFormulaId);
                var updatedAssignment = assignment with
                {
                    AssignedBagPerPig = feedFormula?.ConsumeRate ?? assignment.AssignedBagPerPig,
                    AssignedTotalBags = Math.Ceiling((feedFormula?.ConsumeRate ?? assignment.AssignedBagPerPig) * assignment.AssignedPigQuantity)
                };
                updatedFormulaAssignments.Add(updatedAssignment);
            }

            // Add locked assignments unchanged
            updatedFormulaAssignments.AddRange(pigPen.FormulaAssignments.Where(a => a.IsActive && a.IsLocked));

            // Redistribute unlocked/non-preserved assignments to new pig quantity
            if (unlocked.Any())
            {
                foreach (var assignment in unlocked)
                {
                    var feedFormula = await _context.FeedFormulas.FindAsync(assignment.OriginalFormulaId);
                    var bagPerPig = feedFormula?.ConsumeRate ?? assignment.AssignedBagPerPig;
                    
                    // Update to new pig quantity
                    var updatedAssignment = assignment with
                    {
                        AssignedPigQuantity = pigPen.PigQty,
                        AssignedBagPerPig = bagPerPig,
                        AssignedTotalBags = Math.Ceiling(bagPerPig * pigPen.PigQty)
                    };
                    updatedFormulaAssignments.Add(updatedAssignment);
                    updatedAssignmentCount++;
                }
            }

            // Add inactive assignments unchanged
            updatedFormulaAssignments.AddRange(pigPen.FormulaAssignments.Where(a => !a.IsActive));

            // Log the update
            _logger.LogInformation(
                "PigPen {PenId} qty changed {OldQty}->{NewQty}. preserved: {Preserved}. assignments updated: {Count}",
                pigPen.Id, oldPigQty, pigPen.PigQty, string.Join(',', preservedCodes), updatedAssignmentCount);
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

        // Lock calculations (do NOT modify ActHarvestDate on force-close)
        // Keep ActHarvestDate as-is to preserve scheduled/actual harvest dates
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

        // Clear the force-close state and restore user-edit rights
        var now = DateTime.UtcNow;
        
        // Do not touch ActHarvestDate here if it was intentionally set elsewhere
        entity.IsCalculationLocked = false;
        entity.UpdatedAt = now;

        // Unlock assignments that were locked by a ForceClose operation
        foreach (var assignment in entity.FormulaAssignments.Where(a => a.IsLocked && a.LockReason == "ForceClosed"))
        {
            assignment.IsLocked = false;
            assignment.IsActive = true;
            assignment.LockReason = null;
            assignment.LockedAt = null;
            assignment.UpdatedAt = now;
        }

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

    public async Task<List<ProductUsageDto>> GetUsedProductUsagesAsync(Guid pigPenId)
    {
        var usages = await _context.Feeds
            .Where(f => f.PigPenId == pigPenId && !string.IsNullOrEmpty(f.ProductCode))
            .GroupBy(f => f.ProductCode!)
            .Select(g => new ProductUsageDto(g.Key, g.Count()))
            .ToListAsync();
        return usages;
    }
}
