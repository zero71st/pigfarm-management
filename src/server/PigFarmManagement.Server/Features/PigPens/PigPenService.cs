using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Features.FeedFormulas;
using System.Linq;

namespace PigFarmManagement.Server.Features.PigPens;

public interface IPigPenService
{
    Task<List<PigPen>> GetAllPigPensAsync();
    Task<PigPen?> GetPigPenByIdAsync(Guid id);
    Task<PigPen> CreatePigPenAsync(PigPenCreateDto dto);
    Task<PigPen> UpdatePigPenAsync(PigPen pigPen);
    Task<bool> DeletePigPenAsync(Guid id);
    Task<List<PigPen>> GetPigPensByCustomerIdAsync(Guid customerId);
    Task<PigPen> ForceClosePigPenAsync(Guid id);
    Task<List<PigPenFormulaAssignment>> GetFormulaAssignmentsAsync(Guid pigPenId);
    Task<List<PigPenFormulaAssignment>> RegenerateFormulaAssignmentsAsync(Guid pigPenId);
}

public class PigPenService : IPigPenService
{
    private readonly IPigPenRepository _pigPenRepository;
    private readonly IFeedFormulaService _feedFormulaService;

    public PigPenService(IPigPenRepository pigPenRepository, IFeedFormulaService feedFormulaService)
    {
        _pigPenRepository = pigPenRepository;
        _feedFormulaService = feedFormulaService;
    }

    public async Task<List<PigPen>> GetAllPigPensAsync()
    {
        var pigPens = await _pigPenRepository.GetAllAsync();
        return pigPens.OrderByDescending(p => p.UpdatedAt).ToList();
    }

    public async Task<PigPen?> GetPigPenByIdAsync(Guid id)
    {
        return await _pigPenRepository.GetByIdAsync(id);
    }

    public async Task<PigPen> CreatePigPenAsync(PigPenCreateDto dto)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var pigPen = new PigPen(
            id, 
            dto.CustomerId, 
            dto.PenCode, 
            dto.PigQty, 
            dto.RegisterDate, 
            dto.ActHarvestDate, 
            dto.EstimatedHarvestDate, 
            0, 0, 0, // Initial values for FeedCost, Investment, ProfitLoss
            dto.Type, // PigPenType
            dto.DepositPerPig, // DepositPerPig from DTO
            now, // CreatedAt
            now) // UpdatedAt
        {
            SelectedBrand = dto.SelectedBrand, // Store the selected brand
            Note = dto.Note // Store the note
        };

        // If a brand is selected, automatically assign all formulas for that brand
        if (!string.IsNullOrEmpty(dto.SelectedBrand))
        {
            var formulaAssignments = await CreateAutomaticFormulaAssignments(id, dto.SelectedBrand, dto.PigQty);
            pigPen = pigPen with { FormulaAssignments = formulaAssignments };
        }

        return await _pigPenRepository.CreateAsync(pigPen);
    }

    private async Task<List<PigPenFormulaAssignment>> CreateAutomaticFormulaAssignments(Guid pigPenId, string brand, int pigQty)
    {
        try
        {
            // Get all feed formulas
            var allFormulas = await _feedFormulaService.GetAllFeedFormulasAsync();
            
            // Filter formulas by brand field (the brand selected by user)
            var brandFormulas = allFormulas
                .Where(f => f.Brand != null && f.Brand.Equals(brand, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!brandFormulas.Any())
            {
                // If no formulas found by brand, try category name as fallback
                brandFormulas = allFormulas
                    .Where(f => f.CategoryName != null && f.CategoryName.Equals(brand, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!brandFormulas.Any())
                return new List<PigPenFormulaAssignment>();

            // Create assignments for all formulas that match the brand
            var assignments = new List<PigPenFormulaAssignment>();
            var now = DateTime.UtcNow;
            
            foreach (var formula in brandFormulas)
            {
                var assignment = new PigPenFormulaAssignment(
                    Id: Guid.NewGuid(),
                    PigPenId: pigPenId,
                    OriginalFormulaId: formula.Id,
                    ProductCode: formula.Code ?? string.Empty,
                    ProductName: formula.Name ?? string.Empty,
                    Brand: formula.Brand ?? string.Empty,
                    Stage: null, // Single formula, no stage
                    AssignedPigQuantity: pigQty,
                    AssignedBagPerPig: formula.ConsumeRate ?? 0,
                    AssignedTotalBags: (formula.ConsumeRate ?? 0) * pigQty,
                    AssignedAt: now,
                    EffectiveUntil: null, // Always effective
                    IsActive: true,
                    IsLocked: false,
                    LockReason: null,
                    LockedAt: null
                );
                assignments.Add(assignment);
            }

            return assignments;
        }
        catch
        {
            // If automatic assignment fails, return empty list (pig pen still created without formulas)
            return new List<PigPenFormulaAssignment>();
        }
    }

    public async Task<PigPen> UpdatePigPenAsync(PigPen pigPen)
    {
        var existingPigPen = await _pigPenRepository.GetByIdAsync(pigPen.Id);
        if (existingPigPen == null)
        {
            throw new InvalidOperationException("Pig pen not found");
        }

        // Update the UpdatedAt timestamp while preserving CreatedAt
        var updatedPigPen = pigPen with { 
            CreatedAt = existingPigPen.CreatedAt, 
            UpdatedAt = DateTime.UtcNow 
        };

        return await _pigPenRepository.UpdateAsync(updatedPigPen);
    }

    public async Task<bool> DeletePigPenAsync(Guid id)
    {
        var pigPen = await _pigPenRepository.GetByIdAsync(id);
        if (pigPen == null)
        {
            throw new InvalidOperationException("Pig pen not found");
        }

        return await _pigPenRepository.DeleteAsync(id);
    }

    public async Task<List<PigPen>> GetPigPensByCustomerIdAsync(Guid customerId)
    {
        return await _pigPenRepository.GetByCustomerIdAsync(customerId);
    }

    public async Task<PigPen> ForceClosePigPenAsync(Guid id)
    {
        var pigPen = await _pigPenRepository.GetByIdAsync(id);
        if (pigPen == null)
        {
            throw new InvalidOperationException("Pig pen not found");
        }

        // Create locked formula assignments for historical data integrity
        var lockedAssignments = new List<PigPenFormulaAssignment>();

        // Handle existing formula assignments (new system)
        foreach (var assignment in pigPen.FormulaAssignments.Where(a => a.IsActive))
        {
            var lockedAssignment = assignment with {
                IsActive = false,
                IsLocked = true,
                LockReason = "ForceClosed",
                LockedAt = DateTime.UtcNow
            };
            lockedAssignments.Add(lockedAssignment);
        }

        // Combine existing assignments with new locked ones
        var allAssignments = pigPen.FormulaAssignments
            .Where(a => !a.IsActive) // Keep inactive ones
            .Concat(lockedAssignments) // Add newly locked ones
            .ToList();

        // Force close by setting actual harvest date to today and lock calculations
        var forceClosedPigPen = pigPen with 
        { 
            ActHarvestDate = DateTime.Today,
            FormulaAssignments = allAssignments,
            IsCalculationLocked = true,
            UpdatedAt = DateTime.UtcNow
        };

        return await _pigPenRepository.UpdateAsync(forceClosedPigPen);
    }

    public async Task<List<PigPenFormulaAssignment>> GetFormulaAssignmentsAsync(Guid pigPenId)
    {
        var pigPen = await _pigPenRepository.GetByIdAsync(pigPenId);
        if (pigPen == null)
        {
            throw new InvalidOperationException("Pig pen not found");
        }

        return pigPen.FormulaAssignments.OrderByDescending(a => a.AssignedAt).ToList();
    }

    public async Task<List<PigPenFormulaAssignment>> RegenerateFormulaAssignmentsAsync(Guid pigPenId)
    {
        var pigPen = await _pigPenRepository.GetByIdAsync(pigPenId);
        if (pigPen == null)
        {
            throw new InvalidOperationException("Pig pen not found");
        }

        // Clear existing assignments
        pigPen.FormulaAssignments.Clear();

        // Regenerate assignments based on the pig pen's selected brand
        if (!string.IsNullOrEmpty(pigPen.SelectedBrand))
        {
            await CreateAutomaticFormulaAssignments(pigPen.Id, pigPen.SelectedBrand, pigPen.PigQty);
            await _pigPenRepository.UpdateAsync(pigPen);
        }

        return pigPen.FormulaAssignments.OrderByDescending(a => a.AssignedAt).ToList();
    }
}
