using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Features.FeedProgress;

namespace PigFarmManagement.Server.Features.FeedProgress;

public static class FeedProgressEndpoints
{
    public static IEndpointRouteBuilder MapFeedProgressEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/pigpens/{pigPenId:guid}/feed-progress")
            .WithTags("Feed Progress")
            .RequireAuthorization();

        group.MapGet("/summary", GetFeedProgressSummary)
            .WithName("GetFeedProgressSummary")
            .WithSummary("Get feed progress summary for a pig pen")
            .WithDescription("Returns feed formula requirements vs actual feed consumption with progress indicators");

        group.MapGet("/usage-history", GetFeedUsageHistory)
            .WithName("GetFeedUsageHistory")
            .WithSummary("Get feed bag usage history for a pig pen")
            .WithDescription("Returns recent feed consumption history converted to bag units");

        return builder;
    }

    private static async Task<IResult> GetFeedProgressSummary(
        Guid pigPenId, 
        IFeedProgressService feedProgressService)
    {
        try
        {
            var summary = await feedProgressService.GetFeedProgressAsync(pigPenId);
            return Results.Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error getting feed progress: {ex.Message}");
        }
    }

    private static async Task<IResult> GetFeedUsageHistory(
        Guid pigPenId, 
        IFeedProgressService feedProgressService)
    {
        try
        {
            var history = await feedProgressService.GetFeedBagUsageHistoryAsync(pigPenId);
            return Results.Ok(history);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error getting feed usage history: {ex.Message}");
        }
    }
}