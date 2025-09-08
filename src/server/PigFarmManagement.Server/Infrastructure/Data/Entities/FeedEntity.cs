using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Entities;

public class FeedEntity
{
    public Guid Id { get; set; }
    
    public Guid PigPenId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ProductType { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string ProductCode { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }
    
    public DateTime FeedDate { get; set; }
    
    public string? ExternalReference { get; set; }
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    [ForeignKey("PigPenId")]
    public virtual PigPenEntity PigPen { get; set; } = null!;
    
    // Convert to shared model
    public Feed ToModel()
    {
        return new Feed
        {
            Id = Id,
            PigPenId = PigPenId,
            ProductType = ProductType,
            ProductCode = ProductCode,
            ProductName = ProductName,
            InvoiceNumber = InvoiceNumber,
            Quantity = Quantity,
            UnitPrice = UnitPrice,
            TotalPrice = TotalPrice,
            FeedDate = FeedDate,
            ExternalReference = ExternalReference,
            Notes = Notes,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }
    
    // Create from shared model
    public static FeedEntity FromModel(Feed feed)
    {
        return new FeedEntity
        {
            Id = feed.Id,
            PigPenId = feed.PigPenId,
            ProductType = feed.ProductType,
            ProductCode = feed.ProductCode,
            ProductName = feed.ProductName,
            InvoiceNumber = feed.InvoiceNumber,
            Quantity = feed.Quantity,
            UnitPrice = feed.UnitPrice,
            TotalPrice = feed.TotalPrice,
            FeedDate = feed.FeedDate,
            ExternalReference = feed.ExternalReference,
            Notes = feed.Notes,
            CreatedAt = feed.CreatedAt,
            UpdatedAt = feed.UpdatedAt
        };
    }
}
