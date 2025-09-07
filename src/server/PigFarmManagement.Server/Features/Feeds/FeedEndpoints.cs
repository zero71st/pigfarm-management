using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.Feeds;

public static class FeedEndpoints
{
    public static IEndpointRouteBuilder MapFeedEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/pigpens").WithTags("Feeds");

        group.MapGet("/{pigPenId:guid}/feeds", GetFeedsByPigPen)
            .WithName("GetFeedsByPigPen");

        group.MapPost("/{pigPenId:guid}/feeds", AddFeedToPigPen)
            .WithName("AddFeedToPigPen");

        group.MapDelete("/feeds/{id:guid}", DeleteFeed)
            .WithName("DeleteFeed");

        return builder;
    }

    private static async Task<IResult> GetFeedsByPigPen(Guid pigPenId, IFeedService feedService)
    {
        try
        {
            var feeds = await feedService.GetFeedsByPigPenIdAsync(pigPenId);
            return Results.Ok(feeds);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving feeds: {ex.Message}");
        }
    }

    private static async Task<IResult> AddFeedToPigPen(Guid pigPenId, FeedCreateDto dto, IFeedService feedService)
    {
        try
        {
            var feedItem = await feedService.AddFeedToPigPenAsync(pigPenId, dto);
            return Results.Ok(feedItem);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error adding feed: {ex.Message}");
        }
    }

    private static async Task<IResult> DeleteFeed(Guid id, IFeedService feedService)
    {
        try
        {
            await feedService.DeleteFeedAsync(id);
            return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error deleting feed: {ex.Message}");
        }
    }
}
