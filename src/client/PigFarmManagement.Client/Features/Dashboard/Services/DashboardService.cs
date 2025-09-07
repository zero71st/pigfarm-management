using System.Net.Http.Json;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Client.Features.Dashboard.Services;

public class DashboardService : IDashboardService
{
    private readonly HttpClient _httpClient;

    public DashboardService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DashboardOverview> GetDashboardOverviewAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<DashboardOverview>("/api/dashboard/overview");
        return response ?? throw new InvalidOperationException("Failed to retrieve dashboard overview");
    }

    public async Task<PigPenSummary> GetPigPenSummaryAsync(Guid pigPenId)
    {
        var response = await _httpClient.GetFromJsonAsync<PigPenSummary>($"/api/dashboard/pigpens/{pigPenId}/summary");
        return response ?? throw new InvalidOperationException("Failed to retrieve pig pen summary");
    }
}
