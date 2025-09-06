using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Features.PigPens;
using PigFarmManagement.Server.Features.Feeds;
using PigFarmManagement.Server.Infrastructure.Data;

namespace PigFarmManagement.Server.Features.Dashboard;

public interface IDashboardService
{
    Task<PigPenSummary> GetPigPenSummaryAsync(Guid pigPenId);
}

public class DashboardService : IDashboardService
{
    private readonly IPigPenService _pigPenService;
    private readonly IFeedService _feedService;
    private readonly InMemoryDataStore _dataStore; // For deposits and harvests

    public DashboardService(
        IPigPenService pigPenService, 
        IFeedService feedService, 
        InMemoryDataStore dataStore)
    {
        _pigPenService = pigPenService;
        _feedService = feedService;
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
}
