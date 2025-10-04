using System;
using Microsoft.EntityFrameworkCore;
using PigFarmManagement.Server.Infrastructure.Data;

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlite("Data Source=../../../pigfarm.db")
    .Options;

using var context = new ApplicationDbContext(options);

var feeds = await context.Feeds
    .Select(f => new { 
        f.Id, 
        f.FeedCode, 
        f.CostDiscountPrice, 
        f.PriceIncludeDiscount,
        f.Pos_TotalPriceIncludeDiscount 
    })
    .Take(5)
    .ToListAsync();

Console.WriteLine("Current Feed Data:");
Console.WriteLine("Id\tFeedCode\tCostDiscountPrice\tPriceIncludeDiscount\tPos_TotalPriceIncludeDiscount");

foreach (var feed in feeds)
{
    Console.WriteLine($"{feed.Id}\t{feed.FeedCode}\t{feed.CostDiscountPrice:C}\t{feed.PriceIncludeDiscount:C}\t{feed.Pos_TotalPriceIncludeDiscount:C}");
}

Console.WriteLine($"\nTotal feeds: {await context.Feeds.CountAsync()}");