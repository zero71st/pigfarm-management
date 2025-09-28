using System.Text.Json;
using PigFarmManagement.Shared.Models;

var samples = new[]
{
    // numeric tokens
    "{ \"stock\": 5, \"price\": 12.5, \"special_price\": 10, \"total_price_include_discount\": 50 }",
    // numeric-strings
    "{ \"stock\": \"5\", \"price\": \"12.5\", \"special_price\": \"10\", \"total_price_include_discount\": \"50\" }",
    // whitespace-wrapped strings
    "{ \"stock\": \" 5 \", \"price\": \" 12.50 \", \"special_price\": \"10\", \"total_price_include_discount\": \"50\" }",
    // bad data
    "{ \"stock\": \"non-numeric\", \"price\": \"bad\", \"special_price\": \"-\", \"total_price_include_discount\": \"x\" }"
};

var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

for (int i = 0; i < samples.Length; i++)
{
    Console.WriteLine($"Sample #{i+1}");
    var json = samples[i];
    try
    {
        var item = JsonSerializer.Deserialize<PosPosFeedItem>(json, options);
        if (item is null)
        {
            Console.WriteLine("  -> Deserialized null");
            continue;
        }
        Console.WriteLine($"  Stock: {item.Stock}");
        Console.WriteLine($"  Price: {item.Price}");
        Console.WriteLine($"  SpecialPrice: {item.SpecialPrice}");
        Console.WriteLine($"  TotalPriceIncludeDiscount: {item.TotalPriceIncludeDiscount}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  -> Exception: {ex.Message}");
    }
}

Console.WriteLine("Done");
