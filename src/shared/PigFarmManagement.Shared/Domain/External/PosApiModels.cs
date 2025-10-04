using System.Text.Json.Serialization;
using System.Text.Json;
using System.Globalization;

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

    // Some POSPOS feeds use a different property name; accept both JSON shapes.
    [JsonPropertyName("invoice_reference")]
    public PosPosInvoiceReference? InvoiceReferenceAlias
    {
        get => InvoiceReference;
        set
        {
            if (value != null)
                InvoiceReference = value;
        }
    }
    
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

    // Convenience accessor: prefer BuyerDetail.Code, fall back to KeyCardId
    public string CustomerCode => !string.IsNullOrWhiteSpace(BuyerDetail?.Code) ? BuyerDetail.Code : (BuyerDetail?.KeyCardId ?? string.Empty);
}

public class PosPosFeedItem
{
    [JsonPropertyName("stock")]
    [JsonConverter(typeof(TolerantDecimalConverter))]
    // Use decimal to be tolerant of upstream numeric formats (ints, floats, numeric-strings)
    public decimal Stock { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("price")]
    [JsonConverter(typeof(TolerantDecimalConverter))]
    public decimal Price { get; set; }
    
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";
    
    [JsonPropertyName("total_price_include_discount")]
    [JsonConverter(typeof(TolerantDecimalConverter))]
    public decimal TotalPriceIncludeDiscount { get; set; }
    
    [JsonPropertyName("cost_discount_price")]
    [JsonConverter(typeof(TolerantDecimalConverter))]
    public decimal CostDiscountPrice { get; set; }
    
    [JsonPropertyName("note_in_order")]
    public List<string> NoteInOrder { get; set; } = new();
    
    // Business logic for feed item  
    public string NotesText => string.Join(", ", NoteInOrder);
}

/// <summary>
/// JsonConverter that accepts numbers or numeric-strings and parses to decimal.
/// Falls back to 0 when parsing fails.
/// </summary>
public class TolerantDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var d))
        {
            return d;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
            // Try trimmed version
            if (!string.IsNullOrWhiteSpace(s) && decimal.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }
            return 0m;
        }

        // For other token types, try to read as string then parse; otherwise return 0
        try
        {
            var s = reader.GetString();
            if (!string.IsNullOrWhiteSpace(s) && decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }
        catch { }

        return 0m;
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
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
