using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Features.PigPens;
using PigFarmManagement.Server.Features.FeedFormulas;
using PigFarmManagement.Server.Features.Feeds;
using FeedProgressModel = PigFarmManagement.Shared.Models.FeedProgress;

namespace PigFarmManagement.Server.Features.FeedProgress;

public interface IFeedProgressService
{
    Task<FeedProgressSummary> GetFeedProgressAsync(Guid pigPenId);
    Task<List<FeedBagUsage>> GetFeedBagUsageHistoryAsync(Guid pigPenId);
}

public class FeedProgressService : IFeedProgressService
{
    private readonly IPigPenService _pigPenService;
    private readonly IFeedFormulaService _feedFormulaService;
    private readonly IFeedService _feedService;
    
    public FeedProgressService(
        IPigPenService pigPenService, 
        IFeedFormulaService feedFormulaService,
        IFeedService feedService)
    {
        _pigPenService = pigPenService;
        _feedFormulaService = feedFormulaService;
        _feedService = feedService;
    }

    public async Task<FeedProgressSummary> GetFeedProgressAsync(Guid pigPenId)
    {
        var pigPen = await _pigPenService.GetPigPenByIdAsync(pigPenId);
        if (pigPen == null)
            throw new InvalidOperationException("Pig pen not found");

        // Get ALL feed formulas for the pig pen's selected brand
        var feedFormulas = new List<FeedFormula>();
        FeedFormula? primaryFeedFormula = null;
        
        // Get active formula assignments for this pig pen
        var activeAssignments = pigPen.FormulaAssignments
            .Where(fa => fa.IsActive && !fa.IsLocked)
            .ToList();

        if (activeAssignments.Any())
        {
            // Get all feed formulas for the assigned formulas
            var allFormulas = await _feedFormulaService.GetAllFeedFormulasAsync();
            
            // Get the formulas referenced by active assignments
            var assignedFormulaIds = activeAssignments.Select(fa => fa.OriginalFormulaId).Distinct();
            feedFormulas = allFormulas
                .Where(f => assignedFormulaIds.Contains(f.Id))
                .ToList();
                
            // Use the first assignment as primary reference
            primaryFeedFormula = feedFormulas.FirstOrDefault(f => f.Id == activeAssignments.First().OriginalFormulaId);
        }
        else if (!string.IsNullOrEmpty(pigPen.SelectedBrand))
        {
            // Fallback: Get all formulas for the selected brand
            var allFormulas = await _feedFormulaService.GetAllFeedFormulasAsync();
            feedFormulas = allFormulas
                .Where(f => f.Brand.Equals(pigPen.SelectedBrand, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var feeds = await _feedService.GetFeedsByPigPenIdAsync(pigPenId);
        
        // Calculate feed progress using ALL formulas for the brand
        var progress = CalculateFeedProgress(pigPen, feedFormulas, feeds);
        
        // Get recent feed usage
        var recentFeeds = CalculateFeedBagUsage(feeds);

        return new FeedProgressSummary(
            pigPen.Id,
            pigPen.PenCode,
            pigPen.PigQty,
            primaryFeedFormula, // Keep primary formula for display reference
            progress,
            recentFeeds
        );
    }

    public async Task<List<FeedBagUsage>> GetFeedBagUsageHistoryAsync(Guid pigPenId)
    {
        var feeds = await _feedService.GetFeedsByPigPenIdAsync(pigPenId);
        return CalculateFeedBagUsage(feeds);
    }

    private FeedProgressModel CalculateFeedProgress(PigPen pigPen, List<FeedFormula> feedFormulas, List<FeedItem> feeds)
    {
        if (!feedFormulas.Any())
        {
            return new FeedProgressModel(
                RequiredBags: 0,
                ActualBags: 0,
                PercentageComplete: 0,
                IsOnTrack: true,
                IsOverFeeding: false,
                Status: "No feed formula assigned"
            );
        }

        // Calculate TOTAL required bags by summing ALL feed formulas for the brand
        var totalBagPerPig = feedFormulas.Sum(f => f.BagPerPig);
        var requiredBags = totalBagPerPig * pigPen.PigQty;
        
        // Calculate actual bags consumed
    // Quantity is stored as number of bags
    var actualBags = feeds.Sum(f => f.Quantity);
        
        // Calculate percentage
        var percentage = requiredBags > 0 ? (actualBags / requiredBags) * 100 : 0;
        
        // Determine status
        var isOnTrack = percentage >= 80 && percentage <= 110; // 80-110% is considered on track
        var isOverFeeding = percentage > 110;
        
        string status;
        if (percentage < 50)
            status = "Under-fed";
        else if (percentage < 80)
            status = "Below target";
        else if (percentage <= 110)
            status = "On track";
        else if (percentage <= 130)
            status = "Over-feeding";
        else
            status = "Severely over-feeding";

        return new FeedProgressModel(
            RequiredBags: requiredBags,
            ActualBags: actualBags,
            PercentageComplete: percentage,
            IsOnTrack: isOnTrack,
            IsOverFeeding: isOverFeeding,
            Status: status
        );
    }

    private List<FeedBagUsage> CalculateFeedBagUsage(List<FeedItem> feeds)
    {
        return feeds
            .OrderByDescending(f => f.Date)
            .Take(10) // Get last 10 feed records
            .Select(f => new FeedBagUsage(
                Date: f.Date,
                ProductName: f.ProductName,
                BagsUsed: f.Quantity,
                CostPerBag: f.PricePerBag,
                TotalCost: f.Cost
            ))
            .ToList();
    }
}