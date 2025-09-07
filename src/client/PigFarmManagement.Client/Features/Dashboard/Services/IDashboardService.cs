using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Client.Features.Dashboard.Services;

public interface IDashboardService
{
    Task<DashboardOverview> GetDashboardOverviewAsync();
    Task<PigPenSummary> GetPigPenSummaryAsync(Guid pigPenId);
}
