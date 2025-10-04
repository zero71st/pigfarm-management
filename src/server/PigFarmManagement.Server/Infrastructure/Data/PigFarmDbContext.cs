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
    public DbSet<FeedFormulaEntity> FeedFormulas { get; set; }
    public DbSet<PigPenFormulaAssignmentEntity> PigPenFormulaAssignments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer Configuration
        modelBuilder.Entity<CustomerEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<int>();
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
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.SelectedBrand).HasMaxLength(100);

            entity.HasOne(e => e.Customer)
                  .WithMany(e => e.PigPens)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // PigPenFormulaAssignment Configuration
        modelBuilder.Entity<PigPenFormulaAssignmentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Brand).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Stage).HasMaxLength(50);
            entity.Property(e => e.AssignedBagPerPig).HasColumnType("decimal(18,4)");
            entity.Property(e => e.AssignedTotalBags).HasColumnType("decimal(18,4)");
            entity.Property(e => e.LockReason).HasMaxLength(100);

            entity.HasOne(e => e.PigPen)
                  .WithMany(e => e.FormulaAssignments)
                  .HasForeignKey(e => e.PigPenId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.OriginalFormula)
                  .WithMany()
                  .HasForeignKey(e => e.OriginalFormulaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Feed Configuration
        modelBuilder.Entity<FeedEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalPriceIncludeDiscount).HasColumnType("decimal(18,2)");
            
            // New pricing fields configuration
            entity.Property(e => e.Cost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CostDiscountPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PriceIncludeDiscount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Sys_TotalPriceIncludeDiscount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Pos_TotalPriceIncludeDiscount).HasColumnType("decimal(18,2)");

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
            entity.Property(e => e.TotalWeight).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SalePricePerKg).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Revenue).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.PigPen)
                  .WithMany(e => e.Harvests)
                  .HasForeignKey(e => e.PigPenId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FeedFormula Configuration
        modelBuilder.Entity<FeedFormulaEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code);
            entity.HasIndex(e => e.ExternalId);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.UnitName).HasMaxLength(50);
            entity.Property(e => e.Cost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ConsumeRate).HasColumnType("decimal(18,2)");
        });

    // Seed Data
    // NOTE: Disabled automatic seed of mock/sample data so the system starts empty
    // and real POSPOS customers can be imported by the user.
    // SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;
        var random = new Random(42); // Fixed seed for consistent data

        // Generate 100 Customers
        var customers = new List<CustomerEntity>();
        var customerStatuses = Enum.GetValues<CustomerStatus>();
        
        for (int i = 1; i <= 100; i++)
        {
            var full = GetRandomCustomerName(i, random);
            var parts = full.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var first = parts.Length > 0 ? parts[0] : full;
            var last = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty;

            customers.Add(new CustomerEntity
            {
                Id = Guid.NewGuid(),
                Code = $"M{i:D6}",
                FirstName = first,
                LastName = last,
                Status = customerStatuses[random.Next(customerStatuses.Length)],
                CreatedAt = now.AddDays(-random.Next(1, 365)),
                UpdatedAt = now.AddDays(-random.Next(1, 30))
            });
        }
        modelBuilder.Entity<CustomerEntity>().HasData(customers);

        // Generate Feed Formulas first
        var feedFormulas = new List<FeedFormulaEntity>();
        var jetFeedFormulaIds = new List<Guid>();
        
        // เจ็ท feed formulas
        var jetFormulas = new[]
        {
            new { Code = "PK64000158", Name = "เจ็ท 105 หมูเล็ก 6-15 กก.", BagPerPig = 1.5m },
            new { Code = "PK64000159", Name = "เจ็ท 108 หมูนม 15-25 กก.", BagPerPig = 1.8m },
            new { Code = "PK64000160", Name = "เจ็ท 110 หมู 25-40 กก.", BagPerPig = 2.0m },
            new { Code = "PK64000161", Name = "เจ็ท 120 หมู 40-60 กก.", BagPerPig = 2.2m },
            new { Code = "PK64000162", Name = "เจ็ท 130 หมู 60-90 กก.", BagPerPig = 2.5m },
            new { Code = "PK64000163", Name = "เจ็ท 153 หมู 90 กก. ขึ้นไป", BagPerPig = 2.8m }
        };
        
        foreach (var formula in jetFormulas)
        {
            var id = Guid.NewGuid();
            jetFeedFormulaIds.Add(id);
            feedFormulas.Add(new FeedFormulaEntity
            {
                Id = id,
                Code = formula.Code,
                Name = formula.Name,
                CategoryName = "อาหารสัตว์",
                ConsumeRate = formula.BagPerPig,
                CreatedAt = now.AddDays(-365),
                UpdatedAt = now.AddDays(-30)
            });
        }
        
        // Other brands
        feedFormulas.AddRange(new[]
        {
            new FeedFormulaEntity
            {
                Id = Guid.NewGuid(),
                Code = "PURE001",
                Name = "เพียว บรีดเดอร์",
                CategoryName = "อาหารสัตว์",
                ConsumeRate = 2.8m,
                CreatedAt = now.AddDays(-365),
                UpdatedAt = now.AddDays(-30)
            },
            new FeedFormulaEntity
            {
                Id = Guid.NewGuid(),
                Code = "PURE002",
                Name = "เพียว พรีเมียม",
                CategoryName = "อาหารสัตว์",
                ConsumeRate = 1.8m,
                CreatedAt = now.AddDays(-365),
                UpdatedAt = now.AddDays(-30)
            },
            new FeedFormulaEntity
            {
                Id = Guid.NewGuid(),
                Code = "PURE003",
                Name = "เพียว ออร์แกนิก",
                CategoryName = "อาหารสัตว์",
                ConsumeRate = 2.5m,
                CreatedAt = now.AddDays(-365),
                UpdatedAt = now.AddDays(-30)
            }
        });

        // Generate 100 PigPens with เจ็ท as default brand
        var pigPens = new List<PigPenEntity>();
        var pigPenTypes = Enum.GetValues<PigPenType>();
        
        for (int i = 1; i <= 100; i++)
        {
            var customer = customers[random.Next(customers.Count)];
            var startDaysAgo = random.Next(1, 180);
            var pigQty = random.Next(10, 50);
            var feedCostPerPig = random.Next(30, 100);
            var investmentPerPig = random.Next(400, 800);
            
            // 80% use เจ็ท, 20% use other brands or no formula
            Guid? selectedFeedFormulaId = null;
            string selectedBrand = "เจ็ท";
            
            if (random.Next(0, 100) < 80) // 80% use เจ็ท
            {
                selectedFeedFormulaId = jetFeedFormulaIds[random.Next(jetFeedFormulaIds.Count)];
                selectedBrand = "เจ็ท";
            }
            else if (random.Next(0, 100) < 60) // 12% use เพียว
            {
                var pureFormulas = feedFormulas.Where(f => f.Code != null && f.Code.StartsWith("PURE")).ToList();
                if (pureFormulas.Any())
                {
                    selectedFeedFormulaId = pureFormulas[random.Next(pureFormulas.Count)].Id;
                    selectedBrand = "เพียว";
                }
            }
            // 8% have no formula assigned but still default to เจ็ท brand
            
            pigPens.Add(new PigPenEntity
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                PenCode = $"P{i:D3}",
                PigQty = pigQty,
                RegisterDate = now.AddDays(-startDaysAgo),
                ActHarvestDate = random.Next(0, 10) == 0 ? now.AddDays(-random.Next(1, startDaysAgo)) : null, // 10% chance of being completed
                EstimatedHarvestDate = now.AddDays(random.Next(30, 120)),
                FeedCost = pigQty * feedCostPerPig + random.Next(-500, 500),
                Investment = pigQty * investmentPerPig + random.Next(-2000, 2000),
                ProfitLoss = random.Next(-5000, 2000),
                Type = pigPenTypes[random.Next(pigPenTypes.Length)],
                SelectedBrand = selectedBrand,
                DepositPerPig = random.Next(0, 100) < 30 ? 1000m : 1500m, // 30% use 1000, 70% use 1500
                IsCalculationLocked = false, // Default to unlocked
                CreatedAt = now.AddDays(-startDaysAgo),
                UpdatedAt = now.AddDays(-random.Next(1, 30))
            });
        }
        modelBuilder.Entity<PigPenEntity>().HasData(pigPens);
        modelBuilder.Entity<FeedFormulaEntity>().HasData(feedFormulas);

        // Generate 100 Feeds with เจ็ท products
        var feeds = new List<FeedEntity>();
        var jetProducts = new[]
        {
            new { Code = "PK64000158", Name = "เจ็ท 105 หมูเล็ก 6-15 กก.", Type = "อาหารหมูเล็ก", BasePrice = 755m },
            new { Code = "PK64000159", Name = "เจ็ท 108 หมูนม 15-25 กก.", Type = "อาหารหมูนม", BasePrice = 650m },
            new { Code = "PK64000160", Name = "เจ็ท 110 หมู 25-40 กก.", Type = "อาหารหมูโต", BasePrice = 595m },
            new { Code = "PK64000161", Name = "เจ็ท 120 หมู 40-60 กก.", Type = "อาหารหมูโต", BasePrice = 580m },
            new { Code = "PK64000162", Name = "เจ็ท 130 หมู 60-90 กก.", Type = "อาหารหมูขุน", BasePrice = 565m },
            new { Code = "PK64000163", Name = "เจ็ท 153 หมู 90 กก. ขึ้นไป", Type = "อาหารหมูขุน", BasePrice = 550m }
        };
        
        for (int i = 1; i <= 100; i++)
        {
            var pigPen = pigPens[random.Next(pigPens.Count)];
            var quantity = random.Next(25, 200); // bags (25kg each)
            var product = jetProducts[random.Next(jetProducts.Length)];
            var priceVariation = random.Next(-50, 51); // ±50 baht price variation
            var unitPrice = product.BasePrice + priceVariation;
            
            feeds.Add(new FeedEntity
            {
                Id = Guid.NewGuid(),
                PigPenId = pigPen.Id,
                ProductType = "อาหารสัตว์",
                ProductCode = product.Code,
                ProductName = product.Name,
                TransactionCode = $"ST68{i:D6}",
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalPriceIncludeDiscount = quantity * unitPrice,
                FeedDate = now.AddDays(-random.Next(1, 90)),
                ExternalReference = $"POSPOS-ST68{i:D6}-{product.Code}",
                Notes = random.Next(0, 3) == 0 ? $"อาหารสัตว์คุณภาพสูง, {product.Type}" : null,
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
            var totalWeight = pigCount * avgWeight + random.Next(-50, 50);
            var salePricePerKg = random.Next(45, 75);
            
            harvests.Add(new HarvestEntity
            {
                Id = Guid.NewGuid(),
                PigPenId = pigPen.Id,
                HarvestDate = now.AddDays(-random.Next(1, 180)),
                PigCount = pigCount,
                AvgWeight = avgWeight,
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
