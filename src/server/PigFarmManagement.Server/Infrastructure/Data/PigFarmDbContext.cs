using Microsoft.EntityFrameworkCore;
using PigFarmManagement.Server.Infrastructure.Data.Entities;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data;

public class PigFarmDbContext : DbContext
{
    public PigFarmDbContext(DbContextOptions<PigFarmDbContext> options) : base(options)
    {
    }

    public DbSet<CustomerEntity> Customers { get; set; }
    public DbSet<PigPenEntity> PigPens { get; set; }
    public DbSet<FeedEntity> Feeds { get; set; }
    public DbSet<DepositEntity> Deposits { get; set; }
    public DbSet<HarvestEntity> Harvests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer Configuration
        modelBuilder.Entity<CustomerEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Type).HasConversion<int>();
        });

        // PigPen Configuration
        modelBuilder.Entity<PigPenEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PenCode).IsUnique();
            entity.Property(e => e.PenCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FeedCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Investment).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ProfitLoss).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Customer)
                  .WithMany(e => e.PigPens)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Feed Configuration
        modelBuilder.Entity<FeedEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.PigPen)
                  .WithMany(e => e.Feeds)
                  .HasForeignKey(e => e.PigPenId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Deposit Configuration
        modelBuilder.Entity<DepositEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.PigPen)
                  .WithMany(e => e.Deposits)
                  .HasForeignKey(e => e.PigPenId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Harvest Configuration
        modelBuilder.Entity<HarvestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AvgWeight).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MinWeight).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaxWeight).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalWeight).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SalePricePerKg).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Revenue).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.PigPen)
                  .WithMany(e => e.Harvests)
                  .HasForeignKey(e => e.PigPenId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed Data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;
        var random = new Random(42); // Fixed seed for consistent data

        // Generate 100 Customers
        var customers = new List<CustomerEntity>();
        var customerTypes = Enum.GetValues<CustomerType>();
        
        for (int i = 1; i <= 100; i++)
        {
            customers.Add(new CustomerEntity
            {
                Id = Guid.NewGuid(),
                Code = $"CUST-{i:D3}",
                Name = GetRandomCustomerName(i, random),
                Type = customerTypes[random.Next(customerTypes.Length)],
                CreatedAt = now.AddDays(-random.Next(1, 365)),
                UpdatedAt = now.AddDays(-random.Next(1, 30))
            });
        }
        modelBuilder.Entity<CustomerEntity>().HasData(customers);

        // Generate 100 PigPens
        var pigPens = new List<PigPenEntity>();
        for (int i = 1; i <= 100; i++)
        {
            var customer = customers[random.Next(customers.Count)];
            var startDaysAgo = random.Next(1, 180);
            var pigQty = random.Next(10, 50);
            var feedCostPerPig = random.Next(30, 100);
            var investmentPerPig = random.Next(400, 800);
            
            pigPens.Add(new PigPenEntity
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                PenCode = $"P{i:D3}",
                PigQty = pigQty,
                StartDate = now.AddDays(-startDaysAgo),
                EndDate = random.Next(0, 10) == 0 ? now.AddDays(-random.Next(1, startDaysAgo)) : null, // 10% chance of being completed
                EstimatedHarvestDate = now.AddDays(random.Next(30, 120)),
                FeedCost = pigQty * feedCostPerPig + random.Next(-500, 500),
                Investment = pigQty * investmentPerPig + random.Next(-2000, 2000),
                ProfitLoss = random.Next(-5000, 2000),
                CreatedAt = now.AddDays(-startDaysAgo),
                UpdatedAt = now.AddDays(-random.Next(1, 30))
            });
        }
        modelBuilder.Entity<PigPenEntity>().HasData(pigPens);

        // Generate 100 Feeds
        var feeds = new List<FeedEntity>();
        var productTypes = new[] { "Starter Feed", "Grower Feed", "Finisher Feed", "Sow Feed", "Premium Mix", "Organic Feed", "Vitamin Supplement" };
        
        for (int i = 1; i <= 100; i++)
        {
            var pigPen = pigPens[random.Next(pigPens.Count)];
            var quantity = random.Next(50, 500);
            var unitPrice = random.Next(15, 45);
            
            feeds.Add(new FeedEntity
            {
                Id = Guid.NewGuid(),
                PigPenId = pigPen.Id,
                ProductType = productTypes[random.Next(productTypes.Length)],
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalPrice = quantity * unitPrice,
                FeedDate = now.AddDays(-random.Next(1, 90)),
                ExternalReference = $"INV-{random.Next(10000, 99999)}",
                Notes = random.Next(0, 3) == 0 ? GetRandomFeedNote(random) : null,
                CreatedAt = now.AddDays(-random.Next(1, 90)),
                UpdatedAt = now.AddDays(-random.Next(1, 30))
            });
        }
        modelBuilder.Entity<FeedEntity>().HasData(feeds);

        // Generate 100 Deposits
        var deposits = new List<DepositEntity>();
        for (int i = 1; i <= 100; i++)
        {
            var pigPen = pigPens[random.Next(pigPens.Count)];
            
            deposits.Add(new DepositEntity
            {
                Id = Guid.NewGuid(),
                PigPenId = pigPen.Id,
                Amount = random.Next(1000, 10000),
                Date = now.AddDays(-random.Next(1, 120)),
                Remark = random.Next(0, 3) == 0 ? GetRandomDepositRemark(random) : null
            });
        }
        modelBuilder.Entity<DepositEntity>().HasData(deposits);

        // Generate 100 Harvests
        var harvests = new List<HarvestEntity>();
        for (int i = 1; i <= 100; i++)
        {
            var pigPen = pigPens[random.Next(pigPens.Count)];
            var pigCount = random.Next(5, pigPen.PigQty);
            var avgWeight = random.Next(80, 120);
            var minWeight = avgWeight - random.Next(10, 20);
            var maxWeight = avgWeight + random.Next(10, 20);
            var totalWeight = pigCount * avgWeight + random.Next(-50, 50);
            var salePricePerKg = random.Next(45, 75);
            
            harvests.Add(new HarvestEntity
            {
                Id = Guid.NewGuid(),
                PigPenId = pigPen.Id,
                HarvestDate = now.AddDays(-random.Next(1, 180)),
                PigCount = pigCount,
                AvgWeight = avgWeight,
                MinWeight = minWeight,
                MaxWeight = maxWeight,
                TotalWeight = totalWeight,
                SalePricePerKg = salePricePerKg,
                Revenue = totalWeight * salePricePerKg
            });
        }
        modelBuilder.Entity<HarvestEntity>().HasData(harvests);
    }

    private static string GetRandomCustomerName(int index, Random random)
    {
        var firstNames = new[] { "John", "Mary", "Somchai", "Siriporn", "David", "Sarah", "Michael", "Lisa", "Niran", "Ploy", "Robert", "Jennifer", "Suchart", "Malee", "James" };
        var lastNames = new[] { "Farm", "Ranch", "Agriculture", "Livestock", "Farming Co", "Pig Farm", "Swine Ranch", "Agricultural", "Livestock Ltd", "Farm Corp" };
        
        return $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]} {index}";
    }

    private static string GetRandomFeedNote(Random random)
    {
        var notes = new[] { 
            "High quality feed", 
            "Organic certified", 
            "Special diet supplement", 
            "Vitamin enriched", 
            "Bulk purchase discount",
            "Premium grade feed",
            "Locally sourced ingredients"
        };
        return notes[random.Next(notes.Length)];
    }

    private static string GetRandomDepositRemark(Random random)
    {
        var remarks = new[] { 
            "Initial investment", 
            "Additional funding", 
            "Monthly payment", 
            "Equipment purchase", 
            "Feed advance payment",
            "Maintenance fund",
            "Emergency fund"
        };
        return remarks[random.Next(remarks.Length)];
    }
}
