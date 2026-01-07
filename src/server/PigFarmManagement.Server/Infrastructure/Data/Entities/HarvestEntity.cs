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

    private static DateTime AsUnspecifiedDate(DateTime value)
        => DateTime.SpecifyKind(value.Date, DateTimeKind.Unspecified);

    // Workaround for storing date-only values in PostgreSQL timestamptz:
    // store as UTC noon to prevent timezone conversions from shifting the calendar day.
    private static DateTime ToUtcNoon(DateTime value)
    {
        var date = value.Date;
        return new DateTime(date.Year, date.Month, date.Day, 12, 0, 0, DateTimeKind.Utc);
    }
    
    // Convert to shared model
    public HarvestResult ToModel()
    {
        return new HarvestResult(
            Id, 
            PigPenId, 
            AsUnspecifiedDate(HarvestDate), 
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
        // Date-only field stored in PostgreSQL timestamptz: store as UTC noon to avoid day shifting.
        var harvestDate = ToUtcNoon(harvest.HarvestDate);

        return new HarvestEntity
        {
            Id = harvest.Id,
            PigPenId = harvest.PigPenId,
            HarvestDate = harvestDate,
            PigCount = harvest.PigCount,
            AvgWeight = harvest.AvgWeight,
            TotalWeight = harvest.TotalWeight,
            SalePricePerKg = harvest.SalePricePerKg,
            Revenue = harvest.Revenue
        };
    }
}
