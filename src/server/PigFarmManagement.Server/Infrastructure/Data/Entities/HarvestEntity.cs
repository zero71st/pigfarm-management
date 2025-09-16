using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Entities;

public class HarvestEntity
{
    public Guid Id { get; set; }
    
    public Guid PigPenId { get; set; }
    
    public DateTime HarvestDate { get; set; }
    
    public int PigCount { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal AvgWeight { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalWeight { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal SalePricePerKg { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Revenue { get; set; }
    
    // Navigation Properties
    [ForeignKey("PigPenId")]
    public virtual PigPenEntity PigPen { get; set; } = null!;
    
    // Convert to shared model
    public HarvestResult ToModel()
    {
        return new HarvestResult(
            Id, 
            PigPenId, 
            HarvestDate, 
            PigCount, 
            AvgWeight, 
            TotalWeight, 
            SalePricePerKg, 
            Revenue
        );
    }
    
    // Create from shared model
    public static HarvestEntity FromModel(HarvestResult harvest)
    {
        return new HarvestEntity
        {
            Id = harvest.Id,
            PigPenId = harvest.PigPenId,
            HarvestDate = harvest.HarvestDate,
            PigCount = harvest.PigCount,
            AvgWeight = harvest.AvgWeight,
            TotalWeight = harvest.TotalWeight,
            SalePricePerKg = harvest.SalePricePerKg,
            Revenue = harvest.Revenue
        };
    }
}
