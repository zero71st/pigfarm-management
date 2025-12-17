using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.Dashboard;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/dashboard").WithTags("Dashboard").RequireAuthorization();

        group.MapGet("/overview", GetDashboardOverview)
            .WithName("GetDashboardOverview");

        // Removed dashboard-level pig pen summary endpoint (use /api/pigpens/{id}/summary)

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

    // pig-pen summary endpoint removed from dashboard endpoints
}
