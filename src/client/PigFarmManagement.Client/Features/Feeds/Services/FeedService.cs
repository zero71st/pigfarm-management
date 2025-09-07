using System.Net.Http.Json;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Client.Features.Feeds.Services;

public class FeedService : IFeedService
{
    private readonly HttpClient _httpClient;

    public FeedService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<FeedItem>> GetFeedsByPigPenIdAsync(Guid pigPenId)
    {
        var response = await _httpClient.GetFromJsonAsync<List<FeedItem>>($"/api/pigpens/{pigPenId}/feeds");
        return response ?? new List<FeedItem>();
    }

    public async Task<FeedItem> AddFeedToPigPenAsync(Guid pigPenId, FeedCreateDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/pigpens/{pigPenId}/feeds", dto);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<FeedItem>();
        return result ?? throw new InvalidOperationException("Failed to create feed");
    }

    public async Task<bool> DeleteFeedAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"/api/feeds/{id}");
        return response.IsSuccessStatusCode;
    }
}
