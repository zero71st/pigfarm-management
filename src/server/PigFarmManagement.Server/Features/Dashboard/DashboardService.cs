using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Features.PigPens;
using PigFarmManagement.Server.Features.Feeds;
using PigFarmManagement.Server.Features.Customers;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace PigFarmManagement.Server.Features.Dashboard;

public interface IDashboardService
{
    Task<DashboardOverview> GetDashboardOverviewAsync();
}

public class DashboardService : IDashboardService
{
    private readonly IPigPenService _pigPenService;
    private readonly IFeedService _feedService;
    private readonly ICustomerService _customerService;
    private readonly IDepositRepository _depositRepository;
    private readonly IHarvestRepository _harvestRepository;
    private readonly IMemoryCache _cache;
    private const string CacheKeyDashboardOverview = "dashboard_overview";

    public DashboardService(
        IPigPenService pigPenService, 
        IFeedService feedService,
        ICustomerService customerService,
        IDepositRepository depositRepository,
        IHarvestRepository harvestRepository,
        IMemoryCache cache)
    {
        _pigPenService = pigPenService;
        _feedService = feedService;
        _customerService = customerService;
        _depositRepository = depositRepository;
        _harvestRepository = harvestRepository;
        _cache = cache;
    }

    // Dashboard-level pig pen summary removed; use PigPen detail endpoints instead.

    public async Task<DashboardOverview> GetDashboardOverviewAsync()
    {
        // Check cache first; return cached result if available (30-second TTL)
        if (_cache.TryGetValue(CacheKeyDashboardOverview, out DashboardOverview? cachedOverview))
        {
            return cachedOverview!;
        }

        // Get all data (active pig pens only)
        var activePigPens = await _pigPenService.GetActivePigPensAsync();
        var customers = await _customerService.GetActiveCustomersAsync();

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

        var overview = new DashboardOverview(
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

        // Cache result for 30 seconds to reduce DB load on repeated requests
        _cache.Set(CacheKeyDashboardOverview, overview, TimeSpan.FromSeconds(30));

        return overview;
    }

    private async Task<(decimal totalCost, decimal totalDeposit, decimal totalPriceIncludeDiscount)> 
        CalculateFinancialMetricsAsync(List<PigPen> pigPens)
    {
        if (!pigPens.Any()) return (0, 0, 0);

        var pigPenIds = pigPens.Select(p => p.Id).ToList();
        
        // Batched queries (single query per collection instead of per-pen loop)
        var allFeeds = await _feedService.GetFeedsByPigPenIdsAsync(pigPenIds);
        var allDeposits = await _depositRepository.GetByPigPenIdsAsync(pigPenIds);
        var allHarvests = await _harvestRepository.GetByPigPenIdsAsync(pigPenIds);

        var totalCost = allFeeds.Sum(f => (f.FeedCost ?? 0) * f.Quantity);
        var totalDeposit = allDeposits.Sum(d => d.Amount);
        var totalPriceIncludeDiscount = allFeeds.Sum(f => f.Cost); // Cost parameter = TotalPriceIncludeDiscount from entity
        
        return (totalCost, totalDeposit, totalPriceIncludeDiscount);
    }
}
