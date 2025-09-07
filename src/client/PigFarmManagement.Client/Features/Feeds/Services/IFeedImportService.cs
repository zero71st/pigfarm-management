using System.Net.Http.Json;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Client.Features.Feeds.Services;

public interface IFeedImportService
{
    Task<FeedImportResult> ImportPosPosFeedDataAsync(List<PosPosFeedTransaction> transactions);
    Task<FeedImportResult> ImportFromJsonAsync(string jsonContent);
    Task<List<PosPosFeedTransaction>> GetMockPosPosFeedDataAsync();
    Task<FeedImportResult> ImportMockPosPosFeedDataAsync();
    Task<FeedImportResult> ImportPosPosFeedForPigPenAsync(Guid pigPenId, List<PosPosFeedTransaction> transactions);
    Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerCodeAsync(string customerCode);
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

    public async Task<List<PosPosFeedTransaction>> GetMockPosPosFeedDataAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<PosPosFeedTransaction>>("/api/feeds/import/pospos/mock");
        return response ?? new List<PosPosFeedTransaction>();
    }

    public async Task<FeedImportResult> ImportMockPosPosFeedDataAsync()
    {
        var response = await _httpClient.PostAsync("/api/feeds/import/pospos/mock/import", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FeedImportResult>() ?? new FeedImportResult();
    }

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
}
