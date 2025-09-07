using System.Text.Json.Serialization;

namespace PigFarmManagement.Shared.Models;

/// <summary>
/// External API models for POSPOS system integration
/// Responsibility: Define data contracts for external POSPOS API communication
/// </summary>

public class PosPosFeedTransaction
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";
    
    [JsonPropertyName("order_list")]
    public List<PosPosFeedItem> OrderList { get; set; } = new();
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("buyer_detail")]
    public PosPosBuyerDetail BuyerDetail { get; set; } = new();
    
    [JsonPropertyName("reference_tax_invoice_abbreviate")]
    public PosPosInvoiceReference InvoiceReference { get; set; } = new();
    
    [JsonPropertyName("sub_total")]
    public decimal SubTotal { get; set; }
    
    [JsonPropertyName("grand_total")]
    public decimal GrandTotal { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
    
    // Helper methods for external integration
    public bool IsValidTransaction() => !string.IsNullOrWhiteSpace(Code) && OrderList.Any();
    public decimal TotalFeedCost => OrderList.Sum(item => item.TotalPriceIncludeDiscount);
    public string CustomerFullName => $"{BuyerDetail.FirstName} {BuyerDetail.LastName}".Trim();
}

public class PosPosFeedItem
{
    [JsonPropertyName("stock")]
    public int Stock { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    
    [JsonPropertyName("special_price")]
    public decimal SpecialPrice { get; set; }
    
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";
    
    [JsonPropertyName("total_price_include_discount")]
    public decimal TotalPriceIncludeDiscount { get; set; }
    
    [JsonPropertyName("note_in_order")]
    public List<string> NoteInOrder { get; set; } = new();
    
    // Business logic for feed item
    public bool HasDiscount => SpecialPrice < Price;
    public decimal DiscountAmount => Price - SpecialPrice;
    public string NotesText => string.Join(", ", NoteInOrder);
}

public class PosPosBuyerDetail
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";
    
    [JsonPropertyName("firstname")]
    public string FirstName { get; set; } = "";
    
    [JsonPropertyName("lastname")]
    public string LastName { get; set; } = "";
    
    [JsonPropertyName("key_card_id")]
    public string KeyCardId { get; set; } = "";
    
    // Helper properties
    public string FullName => $"{FirstName} {LastName}".Trim();
    public bool HasKeyCard => !string.IsNullOrWhiteSpace(KeyCardId);
}

public class PosPosInvoiceReference
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";
    
    // Validation
    public bool IsValid => !string.IsNullOrWhiteSpace(Code);
}
