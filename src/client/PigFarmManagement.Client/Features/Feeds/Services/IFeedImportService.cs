using System.Net.Http.Json;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Client.Features.Feeds.Services;

public interface IFeedImportService
{
    Task<FeedImportResult> ImportPosPosFeedDataAsync(List<PosPosFeedTransaction> transactions);
    Task<FeedImportResult> ImportFromJsonAsync(string jsonContent);
    // Mock-specific methods removed. Client should use date-range or JSON import flows.
    Task<FeedImportResult> ImportPosPosFeedForPigPenAsync(Guid pigPenId, List<PosPosFeedTransaction> transactions);
    Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerCodeAsync(string customerCode);
    Task<List<PosPosFeedTransaction>> GetPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerAndDateRangeAsync(string customerCode, DateTime fromDate, DateTime toDate);
    Task<FeedImportResult> ImportPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate);
}

public class FeedImportService : IFeedImportService
{
    private readonly HttpClient _httpClient;

    public FeedImportService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FeedImportResult> ImportPosPosFeedDataAsync(List<PosPosFeedTransaction> transactions)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/feeds/import/pospos", transactions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FeedImportResult>() ?? new FeedImportResult();
    }

    public async Task<FeedImportResult> ImportFromJsonAsync(string jsonContent)
    {
        var request = new { JsonContent = jsonContent };
        var response = await _httpClient.PostAsJsonAsync("/api/feeds/import/pospos/json", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FeedImportResult>() ?? new FeedImportResult();
    }

    // Mock methods removed. Use date-range endpoints or ImportFromJsonAsync for testing / replay.

    public async Task<FeedImportResult> ImportPosPosFeedForPigPenAsync(Guid pigPenId, List<PosPosFeedTransaction> transactions)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/feeds/import/pospos/pigpen/{pigPenId}", transactions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FeedImportResult>() ?? new FeedImportResult();
    }

    public async Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerCodeAsync(string customerCode)
    {
        var response = await _httpClient.GetFromJsonAsync<List<PosPosFeedTransaction>>($"/api/feeds/import/pospos/customer/{customerCode}");
        return response ?? new List<PosPosFeedTransaction>();
    }

    public async Task<List<PosPosFeedTransaction>> GetPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var response = await _httpClient.GetFromJsonAsync<List<PosPosFeedTransaction>>(
            $"/api/feeds/import/pospos/daterange?fromDate={fromDate:yyyy-MM-ddTHH:mm:ss}&toDate={toDate:yyyy-MM-ddTHH:mm:ss}");
        return response ?? new List<PosPosFeedTransaction>();
    }

    public async Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerAndDateRangeAsync(string customerCode, DateTime fromDate, DateTime toDate)
    {
        var response = await _httpClient.GetFromJsonAsync<List<PosPosFeedTransaction>>(
            $"/api/feeds/import/pospos/customer/{customerCode}/daterange?fromDate={fromDate:yyyy-MM-ddTHH:mm:ss}&toDate={toDate:yyyy-MM-ddTHH:mm:ss}");
        return response ?? new List<PosPosFeedTransaction>();
    }

    public async Task<FeedImportResult> ImportPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var request = new { FromDate = fromDate, ToDate = toDate };
        var response = await _httpClient.PostAsJsonAsync("/api/feeds/import/pospos/daterange/import", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FeedImportResult>() ?? new FeedImportResult();
    }
}
