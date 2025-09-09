using System.Text.Json;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Client.Features.FeedFormulas.Services;

// DTOs for client-side operations
public record FeedFormulaCreateDto(string ProductCode, string ProductName, string Brand, decimal BagPerPig);
public record FeedFormulaUpdateDto(string ProductCode, string ProductName, string Brand, decimal BagPerPig);

public class FeedFormulaResponse
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal BagPerPig { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ConsumptionRate { get; set; } = string.Empty;
    public string BrandDisplayName { get; set; } = string.Empty;
}

public interface IFeedFormulaService
{
    Task<IEnumerable<FeedFormulaResponse>> GetAllFeedFormulasAsync();
    Task<FeedFormulaResponse?> GetFeedFormulaByIdAsync(Guid id);
    Task<FeedFormulaResponse> CreateFeedFormulaAsync(FeedFormulaCreateDto feedFormula);
    Task<FeedFormulaResponse> UpdateFeedFormulaAsync(Guid id, FeedFormulaUpdateDto feedFormula);
    Task<bool> DeleteFeedFormulaAsync(Guid id);
    Task<bool> ExistsAsync(string productCode);
}

public class FeedFormulaService : IFeedFormulaService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public FeedFormulaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<IEnumerable<FeedFormulaResponse>> GetAllFeedFormulasAsync()
    {
        var response = await _httpClient.GetAsync("api/feed-formulas");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<IEnumerable<FeedFormulaResponse>>(json, _jsonOptions) ?? [];
    }

    public async Task<FeedFormulaResponse?> GetFeedFormulaByIdAsync(Guid id)
    {
        var response = await _httpClient.GetAsync($"api/feed-formulas/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
            
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FeedFormulaResponse>(json, _jsonOptions);
    }

    public async Task<FeedFormulaResponse> CreateFeedFormulaAsync(FeedFormulaCreateDto feedFormula)
    {
        var json = JsonSerializer.Serialize(feedFormula, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("api/feed-formulas", content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FeedFormulaResponse>(responseJson, _jsonOptions)!;
    }

    public async Task<FeedFormulaResponse> UpdateFeedFormulaAsync(Guid id, FeedFormulaUpdateDto feedFormula)
    {
        var json = JsonSerializer.Serialize(feedFormula, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PutAsync($"api/feed-formulas/{id}", content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FeedFormulaResponse>(responseJson, _jsonOptions)!;
    }

    public async Task<bool> DeleteFeedFormulaAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/feed-formulas/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return false;
            
        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<bool> ExistsAsync(string productCode)
    {
        var response = await _httpClient.GetAsync($"api/feed-formulas/exists/{productCode}");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<bool>(json, _jsonOptions);
    }
}
