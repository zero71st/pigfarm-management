using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Entities;

public class PigPenEntity
{
    public Guid Id { get; set; }
    
    public Guid CustomerId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string PenCode { get; set; } = string.Empty;
    
    public int PigQty { get; set; }
    
    public DateTime RegisterDate { get; set; }
    public DateTime? ActHarvestDate { get; set; }
    public DateTime? EstimatedHarvestDate { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal FeedCost { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Investment { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal ProfitLoss { get; set; }
    
    public PigPenType Type { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal DepositPerPig { get; set; } = 1500m; // Default value
    
    public string? SelectedBrand { get; set; }
    
    public bool IsCalculationLocked { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    [ForeignKey("CustomerId")]
    public virtual CustomerEntity Customer { get; set; } = null!;
    
    public virtual ICollection<PigPenFormulaAssignmentEntity> FormulaAssignments { get; set; } = new List<PigPenFormulaAssignmentEntity>();
    
    public virtual ICollection<FeedEntity> Feeds { get; set; } = new List<FeedEntity>();
    public virtual ICollection<DepositEntity> Deposits { get; set; } = new List<DepositEntity>();
    public virtual ICollection<HarvestEntity> Harvests { get; set; } = new List<HarvestEntity>();
    
    // Convert to shared model
    public PigPen ToModel()
    {
        return new PigPen(
            Id, 
            CustomerId, 
            PenCode, 
            PigQty, 
            RegisterDate, 
            ActHarvestDate, 
            EstimatedHarvestDate, 
            FeedCost, 
            Investment, 
            ProfitLoss, 
            Type,
            DepositPerPig,
            CreatedAt, 
            UpdatedAt
        )
        {
            SelectedBrand = SelectedBrand,
            FormulaAssignments = FormulaAssignments.Select(fa => fa.ToModel()).ToList(),
            IsCalculationLocked = IsCalculationLocked
        };
    }
    
    // Create from shared model
    public static PigPenEntity FromModel(PigPen pigPen)
    {
        return new PigPenEntity
        {
            Id = pigPen.Id,
            CustomerId = pigPen.CustomerId,
            PenCode = pigPen.PenCode,
            PigQty = pigPen.PigQty,
            RegisterDate = pigPen.RegisterDate,
            ActHarvestDate = pigPen.ActHarvestDate,
            EstimatedHarvestDate = pigPen.EstimatedHarvestDate,
            FeedCost = pigPen.FeedCost,
            Investment = pigPen.Investment,
            ProfitLoss = pigPen.ProfitLoss,
            Type = pigPen.Type,
            DepositPerPig = pigPen.DepositPerPig,
            SelectedBrand = pigPen.SelectedBrand,
            IsCalculationLocked = pigPen.IsCalculationLocked,
            CreatedAt = pigPen.CreatedAt,
            UpdatedAt = pigPen.UpdatedAt,
            FormulaAssignments = pigPen.FormulaAssignments.Select(fa => PigPenFormulaAssignmentEntity.FromModel(fa)).ToList()
        };
    }
}
