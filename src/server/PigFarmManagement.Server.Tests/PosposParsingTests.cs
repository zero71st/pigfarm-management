using System.Text.Json;
using PigFarmManagement.Shared.Models;
using Xunit;

namespace PigFarmManagement.Server.Tests;

public class PosposParsingTests
{
    [Theory]
    [InlineData("{ \"stock\": 5, \"price\": 12.5, \"special_price\": 10, \"total_price_include_discount\": 50 }")]
    [InlineData("{ \"stock\": \"5\", \"price\": \"12.5\", \"special_price\": \"10\", \"total_price_include_discount\": \"50\" }")]
    [InlineData("{ \"stock\": \" 5 \", \"price\": \" 12.50 \", \"special_price\": \"10\", \"total_price_include_discount\": \"50\" }")]
    public void PosposFeedItem_TolerantParsing_Works(string json)
    {
        var options = new JsonSerializerOptions();
        options.PropertyNameCaseInsensitive = true;

        var item = JsonSerializer.Deserialize<PosPosFeedItem>(json, options);

        Assert.NotNull(item);
        Assert.Equal(5m, item!.Stock);
        Assert.Equal(12.5m, item.Price);
        Assert.Equal(50m, item.TotalPriceIncludeDiscount);
    }

    [Theory]
    [InlineData("{ \"stock\": \"non-numeric\", \"price\": \"bad\", \"special_price\": \"-\", \"total_price_include_discount\": \"x\" }")]
    public void PosposFeedItem_TolerantParsing_FallsBackToZero(string json)
    {
        var options = new JsonSerializerOptions();
        options.PropertyNameCaseInsensitive = true;

        var item = JsonSerializer.Deserialize<PosPosFeedItem>(json, options);

        Assert.NotNull(item);
        Assert.Equal(0m, item!.Stock);
        Assert.Equal(0m, item.Price);
        Assert.Equal(0m, item.TotalPriceIncludeDiscount);
    }
}
