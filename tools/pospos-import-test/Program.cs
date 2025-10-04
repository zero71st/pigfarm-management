using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PigFarmManagement.Server.Infrastructure.Extensions;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;
using PigFarmManagement.Shared.Models;
using PigFarmManagement.Shared.Contracts;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                        // Use same DB file as the server (repo-root pigfarm.db)
                        var dbPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "pigfarm.db"));
                        services.AddDbContext<PigFarmManagement.Server.Infrastructure.Data.PigFarmDbContext>(options =>
                            options.UseSqlite($"Data Source={dbPath}"));

                // Register application services (repositories, feed import, etc.)
                services.AddApplicationServices();
            })
            .Build();

        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var importService = services.GetRequiredService<IFeedImportService>();
        var efFeedRepo = services.GetRequiredService<PigFarmManagement.Server.Infrastructure.Data.Repositories.IFeedRepository>();

        // Load sample JSON
        var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "sample-pospos.json");
        if (!File.Exists(jsonPath)) jsonPath = Path.Combine(AppContext.BaseDirectory, "sample-pospos.json");
        var json = File.ReadAllText(jsonPath);

    // Parse transactions and seed a customer + pig pen so import has a target
    var transactions = System.Text.Json.JsonSerializer.Deserialize<List<PigFarmManagement.Shared.Models.PosPosFeedTransaction>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (transactions == null || transactions.Count == 0)
        {
            Console.WriteLine("No transactions found in sample JSON.");
            return 1;
        }

        // Seed or reuse a customer and pigPen in the DB to receive imported feeds
        var context = services.GetRequiredService<PigFarmManagement.Server.Infrastructure.Data.PigFarmDbContext>();
        // Ensure migrations are applied to this DB (creates tables if they're missing)
        await context.Database.MigrateAsync();
        var buyerCode = transactions[0].BuyerDetail?.Code ?? "M000001";

        // Upsert customer by Code
        var customerEntity = await context.Customers.FirstOrDefaultAsync(c => c.Code == buyerCode);
        if (customerEntity == null)
        {
            customerEntity = new PigFarmManagement.Server.Infrastructure.Data.Entities.CustomerEntity
            {
                Id = Guid.NewGuid(),
                Code = buyerCode,
                FirstName = transactions[0].BuyerDetail?.FirstName ?? "Test",
                LastName = transactions[0].BuyerDetail?.LastName ?? "User",
                Status = PigFarmManagement.Shared.Models.CustomerStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Customers.Add(customerEntity);
            await context.SaveChangesAsync();
        }

        // Upsert pig pen by PenCode for this customer
        var penCode = "TEST-PEN-001";
        var pigPenEntity = await context.PigPens.FirstOrDefaultAsync(p => p.PenCode == penCode && p.CustomerId == customerEntity.Id);
        if (pigPenEntity == null)
        {
            pigPenEntity = new PigFarmManagement.Server.Infrastructure.Data.Entities.PigPenEntity
            {
                Id = Guid.NewGuid(),
                CustomerId = customerEntity.Id,
                PenCode = penCode,
                PigQty = 20,
                RegisterDate = DateTime.UtcNow.AddDays(-30),
                FeedCost = 0,
                Investment = 0,
                ProfitLoss = 0,
                Type = PigFarmManagement.Shared.Models.PigPenType.Cash,
                DepositPerPig = 1000m,
                IsCalculationLocked = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.PigPens.Add(pigPenEntity);
            await context.SaveChangesAsync();
        }

        // Remove any existing feeds for this invoice to force a fresh import
        var invoiceNum = transactions[0].Code;
        if (!string.IsNullOrWhiteSpace(invoiceNum))
        {
            var existingFeeds = await context.Feeds.Where(f => f.TransactionCode == invoiceNum).ToListAsync();
            if (existingFeeds.Any())
            {
                context.Feeds.RemoveRange(existingFeeds);
                await context.SaveChangesAsync();
                Console.WriteLine($"Removed {existingFeeds.Count} existing feed(s) for invoice {invoiceNum} to force re-import.");
            }
        }

        Console.WriteLine("Running import from sample JSON...");
        var result = await importService.ImportPosPosFeedForPigPenAsync(pigPenEntity.Id, transactions);

        Console.WriteLine($"Import result: Total={result.TotalTransactions}, Successful={result.SuccessfulImports}, Failed={result.FailedImports}");
        if (result.Errors.Any())
        {
            Console.WriteLine("Errors:");
            foreach (var e in result.Errors) Console.WriteLine(" - " + e);
        }

        Console.WriteLine("Saved feeds (last 20):");
        var all = (await efFeedRepo.GetAllAsync()).OrderByDescending(f => f.CreatedAt).Take(20);
        foreach (var f in all)
        {
            Console.WriteLine($"Id={f.Id}, Transaction={f.TransactionCode}, InvoiceRef={f.InvoiceReferenceCode}, ProductCode={f.ProductCode}, ExternalCode={f.ExternalProductCode}, Qty={f.Quantity}, UnitPrice={f.UnitPrice}, Total={f.TotalPriceIncludeDiscount}, CostDiscountPrice={f.CostDiscountPrice}, Unmapped={f.UnmappedProduct}");
        }

        Console.WriteLine("Done.");
        return 0;
    }
}
