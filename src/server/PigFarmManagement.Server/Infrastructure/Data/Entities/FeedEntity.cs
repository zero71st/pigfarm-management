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
    public string TransactionCode { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Cost { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? CostDiscountPrice { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? PriceIncludeDiscount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Sys_TotalPriceIncludeDiscount { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPriceIncludeDiscount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Pos_TotalPriceIncludeDiscount { get; set; }
    
    public DateTime FeedDate { get; set; }
    
    public string? ExternalReference { get; set; }
    public string? ExternalProductCode { get; set; }
    public string? ExternalProductName { get; set; }
    public string? InvoiceReferenceCode { get; set; }
    public bool UnmappedProduct { get; set; }
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
            TransactionCode = TransactionCode,
            Quantity = Quantity,
            UnitPrice = UnitPrice,
            Cost = Cost,
            CostDiscountPrice = CostDiscountPrice,
            PriceIncludeDiscount = PriceIncludeDiscount,
            Sys_TotalPriceIncludeDiscount = Sys_TotalPriceIncludeDiscount,
            TotalPriceIncludeDiscount = TotalPriceIncludeDiscount,
            Pos_TotalPriceIncludeDiscount = Pos_TotalPriceIncludeDiscount,
            FeedDate = FeedDate,
            ExternalReference = ExternalReference,
            ExternalProductCode = ExternalProductCode,
            ExternalProductName = ExternalProductName,
            InvoiceReferenceCode = InvoiceReferenceCode,
            UnmappedProduct = UnmappedProduct,
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
            TransactionCode = feed.TransactionCode,
            Quantity = feed.Quantity,
            UnitPrice = feed.UnitPrice,
            Cost = feed.Cost,
            CostDiscountPrice = feed.CostDiscountPrice,
            PriceIncludeDiscount = feed.PriceIncludeDiscount,
            Sys_TotalPriceIncludeDiscount = feed.Sys_TotalPriceIncludeDiscount,
            TotalPriceIncludeDiscount = feed.TotalPriceIncludeDiscount,
            Pos_TotalPriceIncludeDiscount = feed.Pos_TotalPriceIncludeDiscount,
            FeedDate = feed.FeedDate,
            ExternalReference = feed.ExternalReference,
            ExternalProductCode = feed.ExternalProductCode,
            ExternalProductName = feed.ExternalProductName,
            InvoiceReferenceCode = feed.InvoiceReferenceCode,
            UnmappedProduct = feed.UnmappedProduct,
            Notes = feed.Notes,
            CreatedAt = feed.CreatedAt,
            UpdatedAt = feed.UpdatedAt
        };
    }
}
