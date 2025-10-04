using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PigFarmManagement.Server.Infrastructure.Data;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;
using PigFarmManagement.Shared.Models;
using Xunit;

namespace PigFarmManagement.Server.Tests;

public class FeedRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly FeedRepository _repository;

    public FeedRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        var logger = new LoggerFactory().CreateLogger<FeedRepository>();
        _repository = new FeedRepository(_context, logger);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistCostDiscountPrice()
    {
        // Arrange
        var feed = new Feed
        {
            FeedCode = "F001",
            FeedName = "Test Feed",
            PigPenId = 1,
            FeedType = "Starter",
            FeedDate = DateTime.Now,
            Quantity = 100,
            UnitPrice = 50.00m,
            TotalPrice = 5000.00m,
            Cost = 45.00m,
            CostDiscountPrice = 5.00m, // This should be persisted
            PriceIncludeDiscount = 45.00m,
            Sys_TotalPriceIncludeDiscount = 4500.00m,
            TotalPriceIncludeDiscount = 4500.00m,
            Pos_TotalPriceIncludeDiscount = 4500.00m
        };

        // Act
        var result = await _repository.CreateAsync(feed);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5.00m, result.CostDiscountPrice);
        
        // Verify in database
        var dbFeed = await _context.Feeds.FirstOrDefaultAsync(f => f.FeedCode == "F001");
        Assert.NotNull(dbFeed);
        Assert.Equal(5.00m, dbFeed.CostDiscountPrice);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistCostDiscountPrice()
    {
        // Arrange - Create initial feed
        var feed = new Feed
        {
            FeedCode = "F002",
            FeedName = "Test Feed 2",
            PigPenId = 1,
            FeedType = "Grower",
            FeedDate = DateTime.Now,
            Quantity = 100,
            UnitPrice = 50.00m,
            TotalPrice = 5000.00m,
            Cost = 45.00m,
            CostDiscountPrice = 3.00m,
            PriceIncludeDiscount = 47.00m,
            Sys_TotalPriceIncludeDiscount = 4700.00m,
            TotalPriceIncludeDiscount = 4700.00m,
            Pos_TotalPriceIncludeDiscount = 4700.00m
        };

        var created = await _repository.CreateAsync(feed);
        
        // Act - Update with new CostDiscountPrice
        created.CostDiscountPrice = 8.00m;
        created.PriceIncludeDiscount = 42.00m;
        created.Sys_TotalPriceIncludeDiscount = 4200.00m;
        created.TotalPriceIncludeDiscount = 4200.00m;
        created.Pos_TotalPriceIncludeDiscount = 4200.00m;
        
        var updated = await _repository.UpdateAsync(created);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(8.00m, updated.CostDiscountPrice);
        
        // Verify in database
        var dbFeed = await _context.Feeds.FirstOrDefaultAsync(f => f.FeedCode == "F002");
        Assert.NotNull(dbFeed);
        Assert.Equal(8.00m, dbFeed.CostDiscountPrice);
        Assert.Equal(42.00m, dbFeed.PriceIncludeDiscount);
        Assert.Equal(4200.00m, dbFeed.Sys_TotalPriceIncludeDiscount);
        Assert.Equal(4200.00m, dbFeed.TotalPriceIncludeDiscount);
        Assert.Equal(4200.00m, dbFeed.Pos_TotalPriceIncludeDiscount);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}