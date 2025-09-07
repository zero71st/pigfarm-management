using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Features.PigPens;
using PigFarmManagement.Server.Features.Feeds;
using PigFarmManagement.Server.Features.Customers;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;

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
    private readonly IDepositRepository _depositRepository;
    private readonly IHarvestRepository _harvestRepository;

    public DashboardService(
        IPigPenService pigPenService, 
        IFeedService feedService,
        ICustomerService customerService,
        IDepositRepository depositRepository,
        IHarvestRepository harvestRepository)
    {
        _pigPenService = pigPenService;
        _feedService = feedService;
        _customerService = customerService;
        _depositRepository = depositRepository;
        _harvestRepository = harvestRepository;
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
        var deposits = await _depositRepository.GetByPigPenIdAsync(pigPenId);
        var harvests = await _harvestRepository.GetByPigPenIdAsync(pigPenId);

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
        var allDeposits = await _depositRepository.GetAllAsync();
        var allHarvests = await _harvestRepository.GetAllAsync();

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
        decimal totalInvestmentCash = await CalculateTotalInvestmentAsync(cashPigPens);
        decimal totalInvestmentProject = await CalculateTotalInvestmentAsync(projectPigPens);
        decimal totalProfitLossCash = await CalculateTotalProfitLossAsync(cashPigPens);
        decimal totalProfitLossProject = await CalculateTotalProfitLossAsync(projectPigPens);

        // Calculate customer statistics
        var customerStats = new List<CustomerPigPenStats>();
        foreach (var customer in customers)
        {
            var customerPigPens = activePigPens.Where(p => p.CustomerId == customer.Id).ToList();
            if (customerPigPens.Count > 0)
            {
                var customerInvestment = await CalculateTotalInvestmentAsync(customerPigPens);
                var customerProfitLoss = await CalculateTotalProfitLossAsync(customerPigPens);

                customerStats.Add(new CustomerPigPenStats(
                    customer.Id,
                    customer.Name,
                    customer.Type,
                    customerPigPens.Count,
                    customerPigPens.Sum(p => p.PigQty),
                    customerInvestment,
                    customerProfitLoss
                ));
            }
        }

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

    private async Task<decimal> CalculateTotalInvestmentAsync(List<PigPen> pigPens)
    {
        if (!pigPens.Any()) return 0;

        var pigPenIds = pigPens.Select(p => p.Id).ToHashSet();
        
        // Get all feeds for these pig pens
        var allFeeds = new List<FeedItem>();
        foreach (var pigPenId in pigPenIds)
        {
            var feeds = await _feedService.GetFeedsByPigPenIdAsync(pigPenId);
            allFeeds.AddRange(feeds);
        }

        // Get all deposits for these pig pens
        var allDeposits = new List<Deposit>();
        foreach (var pigPenId in pigPenIds)
        {
            var deposits = await _depositRepository.GetByPigPenIdAsync(pigPenId);
            allDeposits.AddRange(deposits);
        }

        var totalFeed = allFeeds.Sum(f => f.Cost);
        var totalDeposit = allDeposits.Sum(d => d.Amount);
        return totalFeed - totalDeposit;
    }

    private async Task<decimal> CalculateTotalProfitLossAsync(List<PigPen> pigPens)
    {
        if (!pigPens.Any()) return 0;

        var pigPenIds = pigPens.Select(p => p.Id).ToHashSet();
        
        // Get all feeds for these pig pens
        var allFeeds = new List<FeedItem>();
        foreach (var pigPenId in pigPenIds)
        {
            var feeds = await _feedService.GetFeedsByPigPenIdAsync(pigPenId);
            allFeeds.AddRange(feeds);
        }

        // Get all harvests for these pig pens
        var allHarvests = new List<HarvestResult>();
        foreach (var pigPenId in pigPenIds)
        {
            var harvests = await _harvestRepository.GetByPigPenIdAsync(pigPenId);
            allHarvests.AddRange(harvests);
        }

        var totalFeed = allFeeds.Sum(f => f.Cost);
        var totalRevenue = allHarvests.Sum(h => h.Revenue);
        return totalRevenue - totalFeed;
    }
}
