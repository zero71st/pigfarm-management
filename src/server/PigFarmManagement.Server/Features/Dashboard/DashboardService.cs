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

        return new PigPenSummary(pigPenId, totalFeed, totalDeposit, investment, profitLoss);
    }

    public async Task<DashboardOverview> GetDashboardOverviewAsync()
    {
        // Get all data
        var pigPens = await _pigPenService.GetAllPigPensAsync();
        var customers = await _customerService.GetAllCustomersAsync();

        // Filter ACTIVE pig pens only (ActHarvestDate == null or in future)
        // This is the key change: only show active pens in dashboard
        var activePigPens = pigPens.Where(p => p.IsActive).ToList();

        // Group by pig pen type (both must be active)
        var cashPigPens = activePigPens.Where(p => p.Type == PigPenType.Cash).ToList();
        var projectPigPens = activePigPens.Where(p => p.Type == PigPenType.Project).ToList();

        // Calculate activity metrics
        int totalActivePigPens = activePigPens.Count;
        int totalPigs = activePigPens.Sum(p => p.PigQty);
        int totalPigsCash = cashPigPens.Sum(p => p.PigQty);
        int totalPigsProject = projectPigPens.Sum(p => p.PigQty);

        // Get active customers (customers with at least 1 active pig pen)
        var activeCustomerIds = activePigPens.Select(p => p.CustomerId).Distinct().ToHashSet();
        int totalActiveCustomers = customers.Count(c => activeCustomerIds.Contains(c.Id));

        // Calculate financial metrics for all active pens
        var (totalCost, totalDeposit, totalPriceIncludeDiscount) = 
            await CalculateFinancialMetricsAsync(activePigPens);
        
        // Calculate financial metrics for Cash pens
        var (totalCostCash, totalDepositCash, totalPriceIncludeDiscountCash) = 
            await CalculateFinancialMetricsAsync(cashPigPens);
        
        // Calculate financial metrics for Project pens
        var (totalCostProject, totalDepositProject, totalPriceIncludeDiscountProject) = 
            await CalculateFinancialMetricsAsync(projectPigPens);

        // Calculate customer statistics (only customers with active PROJECT pens)
        var customerStats = new List<CustomerPigPenStats>();
        foreach (var customer in customers)
        {
            var customerPigPens = activePigPens
                .Where(p => p.CustomerId == customer.Id && p.Type == PigPenType.Project)
                .ToList();
            if (customerPigPens.Count > 0) // Only include if has active project pens
            {
                var (customerCost, customerDeposit, customerPrice) = 
                    await CalculateFinancialMetricsAsync(customerPigPens);

                customerStats.Add(new CustomerPigPenStats(
                    customer.Id,
                    customer.DisplayName,
                    customer.Status,
                    customerPigPens.Count,
                    customerPigPens.Sum(p => p.PigQty),
                    customerCost,
                    customerDeposit,
                    customerPrice
                ));
            }
        }

        // Calculate customer statistics for CASH pens (ordered by revenue descending)
        var customerStatsCash = new List<CustomerPigPenStats>();
        foreach (var customer in customers)
        {
            var customerCashPens = activePigPens
                .Where(p => p.CustomerId == customer.Id && p.Type == PigPenType.Cash)
                .ToList();
            if (customerCashPens.Count > 0) // Only include if has active cash pens
            {
                var (customerCost, customerDeposit, customerPrice) = 
                    await CalculateFinancialMetricsAsync(customerCashPens);

                customerStatsCash.Add(new CustomerPigPenStats(
                    customer.Id,
                    customer.DisplayName,
                    customer.Status,
                    customerCashPens.Count,
                    customerCashPens.Sum(p => p.PigQty),
                    customerCost,
                    customerDeposit,
                    customerPrice
                ));
            }
        }
        // Order by revenue (TotalPriceIncludeDiscount) descending
        customerStatsCash = customerStatsCash.OrderByDescending(c => c.TotalPriceIncludeDiscount).ToList();

        return new DashboardOverview(
            totalActivePigPens,
            totalActiveCustomers,
            totalPigs,
            totalPigsCash,
            totalPigsProject,
            totalCost,
            totalDeposit,
            totalPriceIncludeDiscount,
            totalCostCash,
            totalDepositCash,
            totalPriceIncludeDiscountCash,
            totalCostProject,
            totalDepositProject,
            totalPriceIncludeDiscountProject,
            customerStats,
            customerStatsCash
        );
    }

    private async Task<(decimal totalCost, decimal totalDeposit, decimal totalPriceIncludeDiscount)> 
        CalculateFinancialMetricsAsync(List<PigPen> pigPens)
    {
        if (!pigPens.Any()) return (0, 0, 0);

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

        // Get all harvests for these pig pens
        var allHarvests = new List<HarvestResult>();
        foreach (var pigPenId in pigPenIds)
        {
            var harvests = await _harvestRepository.GetByPigPenIdAsync(pigPenId);
            allHarvests.AddRange(harvests);
        }

        var totalCost = allFeeds.Sum(f => (f.FeedCost ?? 0) * f.Quantity);
        var totalDeposit = allDeposits.Sum(d => d.Amount);
        var totalPriceIncludeDiscount = allFeeds.Sum(f => f.Cost); // Cost parameter = TotalPriceIncludeDiscount from entity
        
        return (totalCost, totalDeposit, totalPriceIncludeDiscount);
    }
}
