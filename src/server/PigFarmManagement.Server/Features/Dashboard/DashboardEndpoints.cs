using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.Dashboard;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/dashboard").WithTags("Dashboard").RequireAuthorization();

        group.MapGet("/overview", GetDashboardOverview)
            .WithName("GetDashboardOverview");

        group.MapGet("/pigpens/{id:guid}/summary", GetPigPenSummary)
            .WithName("GetPigPenSummary");

        return builder;
    }

    private static async Task<IResult> GetDashboardOverview(IDashboardService dashboardService)
    {
        try
        {
            var overview = await dashboardService.GetDashboardOverviewAsync();
            return Results.Ok(overview);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving dashboard overview: {ex.Message}");
        }
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
