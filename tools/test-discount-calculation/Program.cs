using System;
using System.Text.Json;
using PigFarmManagement.Shared.Models;

// Test reading CostDiscountPrice directly from POSPOS data
var testItem = new PosPosFeedItem
{
    Stock = 5,
    Price = 580,
    Code = "TEST001",
    Name = "Test Feed",
    CostDiscountPrice = 30, // This comes directly from POSPOS API
    TotalPriceIncludeDiscount = 2900
};

Console.WriteLine("Testing CostDiscountPrice from POSPOS:");
Console.WriteLine($"Stock (Quantity): {testItem.Stock}");
Console.WriteLine($"CostDiscountPrice (from POSPOS): {testItem.CostDiscountPrice:C}");
Console.WriteLine($"Price: {testItem.Price:C}");

// Test JSON deserialization with the new field
var json = """
{
  "stock": 5,
  "price": 580,
  "code": "TEST001",
  "name": "Test Feed",
  "cost_discount_price": 30,
  "total_price_include_discount": 2900,
  "note_in_order": []
}
""";

try
{
    var deserializedItem = JsonSerializer.Deserialize<PosPosFeedItem>(json, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    Console.WriteLine("\nTesting JSON deserialization:");
    Console.WriteLine($"Deserialized CostDiscountPrice: {deserializedItem?.CostDiscountPrice:C}");
    Console.WriteLine($"This value comes directly from POSPOS API, no internal calculation");
}
catch (Exception ex)
{
    Console.WriteLine($"JSON deserialization failed: {ex.Message}");
}