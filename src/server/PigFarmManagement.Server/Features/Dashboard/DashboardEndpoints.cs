using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.Dashboard;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/pigpens").WithTags("Dashboard");

        group.MapGet("/{id:guid}/summary", GetPigPenSummary)
            .WithName("GetPigPenSummary")
            .WithOpenApi();

        return builder;
    }

    private static async Task<IResult> GetPigPenSummary(Guid id, IDashboardService dashboardService)
    {
        try
        {
            var summary = await dashboardService.GetPigPenSummaryAsync(id);
            return Results.Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving pig pen summary: {ex.Message}");
        }
    }
}
