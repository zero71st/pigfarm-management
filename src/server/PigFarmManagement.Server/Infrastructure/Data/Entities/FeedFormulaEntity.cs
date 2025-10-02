using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Entities;

public class FeedFormulaEntity
{
    public Guid Id { get; set; }
    
    // POSPOS fields
    public Guid? ExternalId { get; set; } // from POSPOS _id
    
    [MaxLength(50)]
    public string? Code { get; set; } // POSPOS code, primary product code
    
    [MaxLength(200)]
    public string? Name { get; set; } // POSPOS name
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Cost { get; set; } // POSPOS cost, used for profit calculations
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? ConsumeRate { get; set; } // user input, e.g., 0.5 per pig
    
    [MaxLength(100)]
    public string? CategoryName { get; set; } // POSPOS category.name
    
    [MaxLength(100)]
    public string? Brand { get; set; } // User-defined brand, different from CategoryName
    
    [MaxLength(50)]
    public string? UnitName { get; set; } // POSPOS unit.name
    
    public DateTime? LastUpdate { get; set; } // POSPOS lastupdate
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Model conversion methods
    public FeedFormula ToModel()
    {
        return new FeedFormula(
            Id: Id,
            ExternalId: ExternalId,
            Code: Code,
            Name: Name,
            Cost: Cost,
            ConsumeRate: ConsumeRate,
            CategoryName: CategoryName,
            Brand: Brand,
            UnitName: UnitName,
            LastUpdate: LastUpdate,
            CreatedAt: CreatedAt,
            UpdatedAt: UpdatedAt
        );
    }

    public static FeedFormulaEntity FromModel(FeedFormula feedFormula)
    {
        return new FeedFormulaEntity
        {
            Id = feedFormula.Id,
            ExternalId = feedFormula.ExternalId,
            Code = feedFormula.Code,
            Name = feedFormula.Name,
            Cost = feedFormula.Cost,
            ConsumeRate = feedFormula.ConsumeRate,
            CategoryName = feedFormula.CategoryName,
            Brand = feedFormula.Brand,
            UnitName = feedFormula.UnitName,
            LastUpdate = feedFormula.LastUpdate,
            CreatedAt = feedFormula.CreatedAt,
            UpdatedAt = feedFormula.UpdatedAt
        };
    }
}
