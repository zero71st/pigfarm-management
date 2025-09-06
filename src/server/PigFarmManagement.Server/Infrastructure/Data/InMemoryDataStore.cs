using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data;

public class InMemoryDataStore
{
    public List<Customer> Customers { get; }
    public List<PigPen> PigPens { get; }
    public List<FeedItem> Feeds { get; }
    public List<Deposit> Deposits { get; }
    public List<HarvestResult> Harvests { get; }

    public InMemoryDataStore()
    {
        // Initialize with sample data
        Customers = new List<Customer>
        {
            new(Guid.NewGuid(), "C001", "John Farm", CustomerType.Project),
            new(Guid.NewGuid(), "C002", "Mary Farm", CustomerType.Cash),
            new(Guid.NewGuid(), "C003", "Somchai", CustomerType.Project)
        };

        PigPens = new List<PigPen>
        {
            new(Guid.NewGuid(), Customers[0].Id, "P001", 25, DateTime.Now.AddDays(-30), null, DateTime.Now.AddDays(60), 25, 30, 0),
            new(Guid.NewGuid(), Customers[0].Id, "P002", 18, DateTime.Now.AddDays(-45), null, DateTime.Now.AddDays(45), 18, 25, 0),
            new(Guid.NewGuid(), Customers[1].Id, "P003", 30, DateTime.Now.AddDays(-20), null, DateTime.Now.AddDays(70), 30, 35, 0),
            new(Guid.NewGuid(), Customers[2].Id, "P004", 22, DateTime.Now.AddDays(-35), null, DateTime.Now.AddDays(55), 22, 30, 0),
            new(Guid.NewGuid(), Customers[1].Id, "P005", 15, DateTime.Now.AddDays(-10), null, DateTime.Now.AddDays(80), 15, 20, 0)
        };

        Feeds = new List<FeedItem>();
        Deposits = new List<Deposit>();
        Harvests = new List<HarvestResult>();
    }
}
