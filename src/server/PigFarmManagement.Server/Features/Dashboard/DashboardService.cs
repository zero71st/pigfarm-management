using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Features.PigPens;
using PigFarmManagement.Server.Features.Feeds;
using PigFarmManagement.Server.Features.Customers;
using PigFarmManagement.Server.Infrastructure.Data;

namespace PigFarmManagement.Server.Features.Dashboard;

public interface IDashboardService
{
    Task<PigPenSummary> GetPigPenSummaryAsync(Guid pigPenId);
    Task<DashboardOverview> GetDashboardOverviewAsync();
}

public class DashboardService : IDashboardService
{
    private readonly IPigPenService _pigPenService;
    private readonly IFeedService _feedService;
    private readonly ICustomerService _customerService;
    private readonly InMemoryDataStore _dataStore; // For deposits and harvests

    public DashboardService(
        IPigPenService pigPenService, 
        IFeedService feedService,
        ICustomerService customerService,
        InMemoryDataStore dataStore)
    {
        _pigPenService = pigPenService;
        _feedService = feedService;
        _customerService = customerService;
        _dataStore = dataStore;
    }

    public async Task<PigPenSummary> GetPigPenSummaryAsync(Guid pigPenId)
    {
        // Verify pig pen exists
        var pigPen = await _pigPenService.GetPigPenByIdAsync(pigPenId);
        if (pigPen == null)
        {
            throw new InvalidOperationException("Pig pen not found");
        }

        // Get related data
        var feeds = await _feedService.GetFeedsByPigPenIdAsync(pigPenId);
        var deposits = _dataStore.Deposits.Where(d => d.PigPenId == pigPenId).ToList();
        var harvests = _dataStore.Harvests.Where(h => h.PigPenId == pigPenId).ToList();

        // Calculate summary
        decimal totalFeed = feeds.Sum(f => f.Cost);
        decimal totalDeposit = deposits.Sum(d => d.Amount);
        decimal revenue = harvests.Sum(h => h.Revenue);
        decimal investment = totalFeed - totalDeposit;
        decimal profitLoss = revenue - totalFeed;
        decimal net = revenue - totalFeed + totalDeposit;

        return new PigPenSummary(pigPenId, totalFeed, totalDeposit, investment, profitLoss, net);
    }

    public async Task<DashboardOverview> GetDashboardOverviewAsync()
    {
        // Get all data
        var pigPens = await _pigPenService.GetAllPigPensAsync();
        var customers = await _customerService.GetAllCustomersAsync();
        var feeds = _dataStore.Feeds.ToList(); // All feeds from data store
        var deposits = _dataStore.Deposits.ToList();
        var harvests = _dataStore.Harvests.ToList();

        // Filter active pig pens (those without end date or end date in future)
        var activePigPens = pigPens.Where(p => p.EndDate == null || p.EndDate > DateTime.Now).ToList();

        // Group by customer type
        var cashPigPens = activePigPens.Where(p => 
            customers.FirstOrDefault(c => c.Id == p.CustomerId)?.Type == CustomerType.Cash).ToList();
        var projectPigPens = activePigPens.Where(p => 
            customers.FirstOrDefault(c => c.Id == p.CustomerId)?.Type == CustomerType.Project).ToList();

        // Calculate totals
        int totalActivePigPens = activePigPens.Count;
        int totalPigsCash = cashPigPens.Sum(p => p.PigQty);
        int totalPigsProject = projectPigPens.Sum(p => p.PigQty);

        // Calculate financial metrics
        decimal totalInvestmentCash = CalculateTotalInvestment(cashPigPens, feeds, deposits);
        decimal totalInvestmentProject = CalculateTotalInvestment(projectPigPens, feeds, deposits);
        decimal totalProfitLossCash = CalculateTotalProfitLoss(cashPigPens, feeds, deposits, harvests);
        decimal totalProfitLossProject = CalculateTotalProfitLoss(projectPigPens, feeds, deposits, harvests);

        // Calculate customer statistics
        var customerStats = customers.Select(customer =>
        {
            var customerPigPens = activePigPens.Where(p => p.CustomerId == customer.Id).ToList();
            var customerInvestment = CalculateTotalInvestment(customerPigPens, feeds, deposits);
            var customerProfitLoss = CalculateTotalProfitLoss(customerPigPens, feeds, deposits, harvests);

            return new CustomerPigPenStats(
                customer.Id,
                customer.Name,
                customer.Type,
                customerPigPens.Count,
                customerPigPens.Sum(p => p.PigQty),
                customerInvestment,
                customerProfitLoss
            );
        }).Where(stats => stats.PigPenCount > 0).ToList();

        return new DashboardOverview(
            totalActivePigPens,
            totalPigsCash,
            totalPigsProject,
            totalInvestmentCash,
            totalInvestmentProject,
            totalProfitLossCash,
            totalProfitLossProject,
            customerStats
        );
    }

    private decimal CalculateTotalInvestment(List<PigPen> pigPens, List<FeedItem> feeds, List<Deposit> deposits)
    {
        var pigPenIds = pigPens.Select(p => p.Id).ToHashSet();
        var totalFeed = feeds.Where(f => pigPenIds.Contains(f.PigPenId)).Sum(f => f.Cost);
        var totalDeposit = deposits.Where(d => pigPenIds.Contains(d.PigPenId)).Sum(d => d.Amount);
        return totalFeed - totalDeposit;
    }

    private decimal CalculateTotalProfitLoss(List<PigPen> pigPens, List<FeedItem> feeds, List<Deposit> deposits, List<HarvestResult> harvests)
    {
        var pigPenIds = pigPens.Select(p => p.Id).ToHashSet();
        var totalFeed = feeds.Where(f => pigPenIds.Contains(f.PigPenId)).Sum(f => f.Cost);
        var totalRevenue = harvests.Where(h => pigPenIds.Contains(h.PigPenId)).Sum(h => h.Revenue);
        return totalRevenue - totalFeed;
    }
}
