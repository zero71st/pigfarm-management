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
    
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? EstimatedHarvestDate { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal FeedCost { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Investment { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal ProfitLoss { get; set; }
    
    public PigPenType Type { get; set; }
    
    public Guid? FeedFormulaId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    [ForeignKey("CustomerId")]
    public virtual CustomerEntity Customer { get; set; } = null!;
    
    [ForeignKey("FeedFormulaId")]
    public virtual FeedFormulaEntity? FeedFormula { get; set; }
    
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
            StartDate, 
            EndDate, 
            EstimatedHarvestDate, 
            FeedCost, 
            Investment, 
            ProfitLoss, 
            Type,
            FeedFormulaId,
            CreatedAt, 
            UpdatedAt
        );
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
            StartDate = pigPen.StartDate,
            EndDate = pigPen.EndDate,
            EstimatedHarvestDate = pigPen.EstimatedHarvestDate,
            FeedCost = pigPen.FeedCost,
            Investment = pigPen.Investment,
            ProfitLoss = pigPen.ProfitLoss,
            Type = pigPen.Type,
            FeedFormulaId = pigPen.FeedFormulaId,
            CreatedAt = pigPen.CreatedAt,
            UpdatedAt = pigPen.UpdatedAt
        };
    }
}
