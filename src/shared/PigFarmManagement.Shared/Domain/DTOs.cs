namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Data Transfer Objects for external system integration
/// Responsibility: Handle data contracts with external systems (APIs, imports, etc.)
/// </summary>

public class Feed
{
    public Guid Id { get; set; }
    public Guid PigPenId { get; set; }
    public string ProductType { get; set; } = "";
    public string ProductCode { get; set; } = ""; // Add product code field
    public string ProductName { get; set; } = ""; // Add product name field
    public string InvoiceNumber { get; set; } = ""; // Add invoice number field
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime FeedDate { get; set; }
    public string? Notes { get; set; }
    public string? ExternalReference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
    // Helper methods for external integration
    public void RecalculateTotalPrice()
    {
        TotalPrice = Quantity * UnitPrice;
    }
    
    public bool IsValidForImport()
    {
        return !string.IsNullOrWhiteSpace(ProductType) && 
               Quantity > 0 && 
               UnitPrice > 0;
    }
}
