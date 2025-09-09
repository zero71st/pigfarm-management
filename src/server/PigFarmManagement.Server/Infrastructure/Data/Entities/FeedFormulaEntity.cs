using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Entities;

public class FeedFormulaEntity
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string ProductCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal BagPerPig { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Model conversion methods
    public FeedFormula ToModel()
    {
        return new FeedFormula(
            Id: Id,
            ProductCode: ProductCode,
            ProductName: ProductName,
            Brand: Brand,
            BagPerPig: BagPerPig,
            CreatedAt: CreatedAt,
            UpdatedAt: UpdatedAt
        );
    }

    public static FeedFormulaEntity FromModel(FeedFormula feedFormula)
    {
        return new FeedFormulaEntity
        {
            Id = feedFormula.Id,
            ProductCode = feedFormula.ProductCode,
            ProductName = feedFormula.ProductName,
            Brand = feedFormula.Brand,
            BagPerPig = feedFormula.BagPerPig,
            CreatedAt = feedFormula.CreatedAt,
            UpdatedAt = feedFormula.UpdatedAt
        };
    }
}
