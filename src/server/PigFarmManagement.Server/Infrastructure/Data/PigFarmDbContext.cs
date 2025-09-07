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
        var customer1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var customer2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var customer3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var pigPen1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var pigPen2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var pigPen3Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var pigPen4Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var pigPen5Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        var now = DateTime.UtcNow;

        // Seed Customers
        modelBuilder.Entity<CustomerEntity>().HasData(
            new CustomerEntity
            {
                Id = customer1Id,
                Code = "CUST-001",
                Name = "John Farm",
                Type = CustomerType.Project,
                CreatedAt = now.AddDays(-60),
                UpdatedAt = now.AddDays(-60)
            },
            new CustomerEntity
            {
                Id = customer2Id,
                Code = "CUST-002",
                Name = "Mary Farm",
                Type = CustomerType.Cash,
                CreatedAt = now.AddDays(-50),
                UpdatedAt = now.AddDays(-50)
            },
            new CustomerEntity
            {
                Id = customer3Id,
                Code = "CUST-003",
                Name = "Somchai",
                Type = CustomerType.Project,
                CreatedAt = now.AddDays(-40),
                UpdatedAt = now.AddDays(-40)
            }
        );

        // Seed PigPens
        modelBuilder.Entity<PigPenEntity>().HasData(
            new PigPenEntity
            {
                Id = pigPen1Id,
                CustomerId = customer1Id,
                PenCode = "P001",
                PigQty = 25,
                StartDate = now.AddDays(-30),
                EndDate = null,
                EstimatedHarvestDate = now.AddDays(60),
                FeedCost = 1250.00m,
                Investment = 12500.00m,
                ProfitLoss = -2500.00m,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-1)
            },
            new PigPenEntity
            {
                Id = pigPen2Id,
                CustomerId = customer1Id,
                PenCode = "P002",
                PigQty = 18,
                StartDate = now.AddDays(-45),
                EndDate = null,
                EstimatedHarvestDate = now.AddDays(45),
                FeedCost = 980.00m,
                Investment = 9000.00m,
                ProfitLoss = -1800.00m,
                CreatedAt = now.AddDays(-45),
                UpdatedAt = now.AddDays(-2)
            },
            new PigPenEntity
            {
                Id = pigPen3Id,
                CustomerId = customer2Id,
                PenCode = "P003",
                PigQty = 30,
                StartDate = now.AddDays(-20),
                EndDate = null,
                EstimatedHarvestDate = now.AddDays(70),
                FeedCost = 1680.00m,
                Investment = 15000.00m,
                ProfitLoss = -3200.00m,
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now.AddDays(-1)
            },
            new PigPenEntity
            {
                Id = pigPen4Id,
                CustomerId = customer3Id,
                PenCode = "P004",
                PigQty = 22,
                StartDate = now.AddDays(-35),
                EndDate = null,
                EstimatedHarvestDate = now.AddDays(55),
                FeedCost = 1210.00m,
                Investment = 11000.00m,
                ProfitLoss = -2200.00m,
                CreatedAt = now.AddDays(-35),
                UpdatedAt = now.AddDays(-3)
            },
            new PigPenEntity
            {
                Id = pigPen5Id,
                CustomerId = customer2Id,
                PenCode = "P005",
                PigQty = 15,
                StartDate = now.AddDays(-10),
                EndDate = null,
                EstimatedHarvestDate = now.AddDays(80),
                FeedCost = 750.00m,
                Investment = 7500.00m,
                ProfitLoss = -1500.00m,
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-1)
            }
        );
    }
}
