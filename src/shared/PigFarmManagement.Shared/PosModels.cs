using System.Text.Json.Serialization;

namespace PigFarmManagement.Shared.Models;

// POSPOS API Data Models
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
}

public class PosPosInvoiceReference
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";
}

// Import Result Models
public class FeedImportResult
{
    public int TotalTransactions { get; set; }
    public int TotalFeedItems { get; set; }
    public int SuccessfulImports { get; set; }
    public int FailedImports { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ImportedFeedSummary> ImportedFeeds { get; set; } = new();
}

public class ImportedFeedSummary
{
    public string InvoiceCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string PigPenCode { get; set; } = "";
    public int FeedItemsCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime ImportDate { get; set; }
}
