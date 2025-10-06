using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.Feeds;

public static class FeedEndpoints
{
    public static IEndpointRouteBuilder MapFeedEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/pigpens").WithTags("Feeds");

        group.MapPost("/{pigPenId:guid}/feeds", AddFeedToPigPen)
            .WithName("AddFeedToPigPen")
            .WithSummary("Add a new feed item to a pig pen")
            .Accepts<FeedCreateDto>("application/json")
            .Produces<FeedItem>();

        group.MapDelete("/{pigPenId:guid}/feeds/{feedItemId:guid}", DeleteFeed)
            .WithName("DeleteFeed")
            .WithSummary("Delete a feed item from a pig pen")
            .Produces(204);

        return builder;
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

    private static async Task<IResult> DeleteFeed(Guid pigPenId, Guid feedItemId, IFeedService feedService)
    {
        try
        {
            var deleted = await feedService.DeleteFeedAsync(feedItemId);
            if (!deleted)
            {
                return Results.NotFound($"Feed item with ID {feedItemId} not found");
            }
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error deleting feed: {ex.Message}");
        }
    }
}
