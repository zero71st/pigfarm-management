using System.Text.Json;
using PigFarmManagement.Shared.Models;
using PigFarmManagement.Shared.Domain.External;

namespace PigFarmManagement.Client.Features.FeedFormulas.Services;

public interface IFeedFormulaService
{
    Task<IEnumerable<FeedFormulaDto>> GetAllFeedFormulasAsync();
    Task<FeedFormulaDto?> GetFeedFormulaByIdAsync(Guid id);
    Task<FeedFormulaDto> CreateFeedFormulaAsync(FeedFormulaCreateDto feedFormula);
    Task<FeedFormulaDto> UpdateFeedFormulaAsync(Guid id, FeedFormulaUpdateDto feedFormula);
    Task<bool> DeleteFeedFormulaAsync(Guid id);
    Task<bool> ExistsAsync(string productCode);

    Task<ImportResultDto> ImportSelectedFromPosposAsync(List<string> productCodes);
    Task<List<PosposProduct>> SearchPosposProductsAsync(string q);
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

    public async Task<IEnumerable<FeedFormulaDto>> GetAllFeedFormulasAsync()
    {
        var response = await _httpClient.GetAsync("api/feed-formulas");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<IEnumerable<FeedFormulaDto>>(json, _jsonOptions) ?? [];
    }

    public async Task<FeedFormulaDto?> GetFeedFormulaByIdAsync(Guid id)
    {
        var response = await _httpClient.GetAsync($"api/feed-formulas/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
            
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FeedFormulaDto>(json, _jsonOptions);
    }

    public async Task<FeedFormulaDto> CreateFeedFormulaAsync(FeedFormulaCreateDto feedFormula)
    {
        var json = JsonSerializer.Serialize(feedFormula, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("api/feed-formulas", content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FeedFormulaDto>(responseJson, _jsonOptions)!;
    }

    public async Task<FeedFormulaDto> UpdateFeedFormulaAsync(Guid id, FeedFormulaUpdateDto feedFormula)
    {
        var json = JsonSerializer.Serialize(feedFormula, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PutAsync($"api/feed-formulas/{id}", content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FeedFormulaDto>(responseJson, _jsonOptions)!;
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

    public async Task<ImportResultDto> ImportSelectedFromPosposAsync(List<string> productCodes)
    {
        var request = new { ProductCodes = productCodes };
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("api/feed-formulas/import-selected", content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ImportResultDto>(responseJson, _jsonOptions)!;
    }

    public async Task<List<PosposProduct>> SearchPosposProductsAsync(string q)
    {
        var response = await _httpClient.GetAsync($"api/products/search?q={Uri.EscapeDataString(q)}");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<IEnumerable<PosposProduct>>(json, _jsonOptions) ?? [];
        return products.ToList();
    }


}
