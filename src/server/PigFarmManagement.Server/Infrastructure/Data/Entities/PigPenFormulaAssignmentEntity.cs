using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Entities;

public class PigPenFormulaAssignmentEntity
{
    public Guid Id { get; set; }

    public Guid PigPenId { get; set; }

    public Guid OriginalFormulaId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Stage { get; set; } // "Starter", "Grower", "Finisher"

    public int AssignedPigQuantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AssignedBagPerPig { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AssignedTotalBags { get; set; }

    public DateTime AssignedAt { get; set; }
    public DateTime? EffectiveUntil { get; set; }

    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }

    [MaxLength(100)]
    public string? LockReason { get; set; }

    public DateTime? LockedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey("PigPenId")]
    public virtual PigPenEntity PigPen { get; set; } = null!;

    [ForeignKey("OriginalFormulaId")]
    public virtual FeedFormulaEntity OriginalFormula { get; set; } = null!;

    // Convert to shared model
    public PigPenFormulaAssignment ToModel()
    {
        return new PigPenFormulaAssignment(
            Id,
            PigPenId,
            OriginalFormulaId,
            ProductCode,
            ProductName,
            Brand,
            Stage,
            AssignedPigQuantity,
            AssignedBagPerPig,
            AssignedTotalBags,
            AssignedAt,
            EffectiveUntil,
            IsActive,
            IsLocked,
            LockReason,
            LockedAt
        );
    }

    // Create from shared model
    public static PigPenFormulaAssignmentEntity FromModel(PigPenFormulaAssignment model)
    {
        return new PigPenFormulaAssignmentEntity
        {
            Id = model.Id,
            PigPenId = model.PigPenId,
            OriginalFormulaId = model.OriginalFormulaId,
            ProductCode = model.ProductCode,
            ProductName = model.ProductName,
            Brand = model.Brand,
            Stage = model.Stage,
            AssignedPigQuantity = model.AssignedPigQuantity,
            AssignedBagPerPig = model.AssignedBagPerPig,
            AssignedTotalBags = model.AssignedTotalBags,
            AssignedAt = model.AssignedAt,
            EffectiveUntil = model.EffectiveUntil,
            IsActive = model.IsActive,
            IsLocked = model.IsLocked,
            LockReason = model.LockReason,
            LockedAt = model.LockedAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}