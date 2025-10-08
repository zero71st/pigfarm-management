using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PigFarmManagement.Server.Infrastructure.Data;
using PigFarmManagement.Server.Infrastructure.Data.Entities;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.FeedFormulas;

/// <summary>
/// Service for validating and maintaining the unified PigPenFormulaAssignment system
/// Since the system has been migrated to use PigPenFormulaAssignment exclusively,
/// this service focuses on validation and maintenance rather than data migration.
/// </summary>
public class FormulaMigrationService
{
    private readonly PigFarmDbContext _context;
    private readonly IFeedFormulaService _feedFormulaService;

    public FormulaMigrationService(PigFarmDbContext context, IFeedFormulaService feedFormulaService)
    {
        _context = context;
        _feedFormulaService = feedFormulaService;
    }

    /// <summary>
    /// Validates that the unified PigPenFormulaAssignment system is working correctly
    /// </summary>
    public async Task<SystemValidationResult> ValidateUnifiedSystemAsync()
    {
        var result = new SystemValidationResult();

        try
        {
            // Check that all pig pens have formula assignments where expected
            var totalPigPens = await _context.PigPens.CountAsync();
            var pigPensWithAssignments = await _context.PigPens
                .CountAsync(p => p.FormulaAssignments.Any());

            // Check for locked pig pens that have locked assignments
            var lockedPigPens = await _context.PigPens
                .CountAsync(p => p.IsCalculationLocked);

            var lockedPigPensWithLockedAssignments = await _context.PigPens
                .CountAsync(p => p.IsCalculationLocked &&
                               p.FormulaAssignments.Any(fa => fa.IsLocked));

            // Check for active pig pens with active assignments
            var activePigPens = await _context.PigPens
                .CountAsync(p => !p.IsCalculationLocked);

            var activePigPensWithActiveAssignments = await _context.PigPens
                .CountAsync(p => !p.IsCalculationLocked &&
                               p.FormulaAssignments.Any(fa => fa.IsActive));

            result.TotalPigPens = totalPigPens;
            result.PigPensWithAssignments = pigPensWithAssignments;
            result.LockedPigPens = lockedPigPens;
            result.LockedPigPensWithLockedAssignments = lockedPigPensWithLockedAssignments;
            result.ActivePigPens = activePigPens;
            result.ActivePigPensWithActiveAssignments = activePigPensWithActiveAssignments;

            // System is valid if:
            // 1. All pig pens have at least one formula assignment
            // 2. All locked pig pens have at least one locked assignment
            // 3. All active pig pens have at least one active assignment (or no assignments if newly created)
            result.IsValid = totalPigPens == pigPensWithAssignments &&
                           lockedPigPens == lockedPigPensWithLockedAssignments;

            result.ValidationMessages = new List<string>();

            if (totalPigPens != pigPensWithAssignments)
            {
                result.ValidationMessages.Add($"{totalPigPens - pigPensWithAssignments} pig pens have no formula assignments");
            }

            if (lockedPigPens != lockedPigPensWithLockedAssignments)
            {
                result.ValidationMessages.Add($"{lockedPigPens - lockedPigPensWithLockedAssignments} locked pig pens have no locked assignments");
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = ex.Message;
            result.ValidationMessages = new List<string> { $"Validation error: {ex.Message}" };
        }

        return result;
    }

    /// <summary>
    /// Repairs any inconsistencies in the unified system
    /// </summary>
    public async Task<SystemRepairResult> RepairSystemAsync()
    {
        var result = new SystemRepairResult();

        try
        {
            // Find locked pig pens without locked assignments
            var lockedPigPensWithoutLockedAssignments = await _context.PigPens
                .Where(p => p.IsCalculationLocked && !p.FormulaAssignments.Any(fa => fa.IsLocked))
                .ToListAsync();

            foreach (var pigPen in lockedPigPensWithoutLockedAssignments)
            {
                // Create a default locked assignment for locked pig pens without any
                // This is a safety measure - in practice, assignments should be created during force-close
                var defaultAssignment = new PigPenFormulaAssignmentEntity
                {
                    Id = Guid.NewGuid(),
                    PigPenId = pigPen.Id,
                    OriginalFormulaId = Guid.Empty, // No specific formula
                    ProductCode = "DEFAULT",
                    ProductName = "Default Formula (System Generated)",
                    Brand = pigPen.SelectedBrand ?? "Unknown",
                    Stage = null,
                    AssignedPigQuantity = pigPen.PigQty,
                    AssignedBagPerPig = 0, // Unknown
                    AssignedTotalBags = 0, // Unknown
                    AssignedAt = pigPen.CreatedAt,
                    EffectiveUntil = null,
                    IsActive = false,
                    IsLocked = true,
                    LockReason = "SystemRepair",
                    LockedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.PigPenFormulaAssignments.Add(defaultAssignment);
                result.RepairsPerformed++;
            }

            await _context.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Gets system statistics for monitoring
    /// </summary>
    public async Task<SystemStatistics> GetSystemStatisticsAsync()
    {
        var stats = new SystemStatistics();

        stats.TotalPigPens = await _context.PigPens.CountAsync();
        stats.TotalFormulaAssignments = await _context.PigPenFormulaAssignments.CountAsync();
        stats.LockedPigPens = await _context.PigPens.CountAsync(p => p.IsCalculationLocked);
        stats.ActivePigPens = await _context.PigPens.CountAsync(p => !p.IsCalculationLocked);
        stats.LockedAssignments = await _context.PigPenFormulaAssignments.CountAsync(fa => fa.IsLocked);
        stats.ActiveAssignments = await _context.PigPenFormulaAssignments.CountAsync(fa => fa.IsActive);

        // Calculate averages
        if (stats.TotalPigPens > 0)
        {
            stats.AverageAssignmentsPerPigPen = (double)stats.TotalFormulaAssignments / stats.TotalPigPens;
        }

        return stats;
    }
}

/// <summary>
/// Result of system validation
/// </summary>
public class SystemValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalPigPens { get; set; }
    public int PigPensWithAssignments { get; set; }
    public int LockedPigPens { get; set; }
    public int LockedPigPensWithLockedAssignments { get; set; }
    public int ActivePigPens { get; set; }
    public int ActivePigPensWithActiveAssignments { get; set; }
    public List<string>? ValidationMessages { get; set; }
}

/// <summary>
/// Result of system repair operation
/// </summary>
public class SystemRepairResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int RepairsPerformed { get; set; }
}

/// <summary>
/// System statistics for monitoring
/// </summary>
public class SystemStatistics
{
    public int TotalPigPens { get; set; }
    public int TotalFormulaAssignments { get; set; }
    public int LockedPigPens { get; set; }
    public int ActivePigPens { get; set; }
    public int LockedAssignments { get; set; }
    public int ActiveAssignments { get; set; }
    public double AverageAssignmentsPerPigPen { get; set; }
}