using System.Text.Json;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Client.Features.FeedFormulas.Services;

// DTOs for client-side operations
public record FeedFormulaCreateDto(string Code, string Name, string CategoryName, string Brand, decimal ConsumeRate, decimal Cost, string UnitName);
public record FeedFormulaUpdateDto(string Code, string Name, string CategoryName, string Brand, decimal ConsumeRate, decimal Cost, string UnitName);

// POSPOS Import DTO
public class ImportResultDto
{
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> ImportedCodes { get; set; } = new();
}

public class FeedFormulaDto
{
    public Guid Id { get; set; }
    
    // New POSPOS fields
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? CategoryName { get; set; }
    public string? Brand { get; set; }
    public decimal ConsumeRate { get; set; }
    public decimal Cost { get; set; }
    public string? UnitName { get; set; }
    
    // Legacy properties for backwards compatibility
    public string ProductCode => Code ?? string.Empty;
    public string ProductName => Name ?? string.Empty;
    public decimal BagPerPig => ConsumeRate;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ConsumptionRate { get; set; } = string.Empty;
    public string BrandDisplayName { get; set; } = string.Empty;
}

public interface IFeedFormulaService
{
    Task<IEnumerable<FeedFormulaDto>> GetAllFeedFormulasAsync();
    Task<FeedFormulaDto?> GetFeedFormulaByIdAsync(Guid id);
    Task<FeedFormulaDto> CreateFeedFormulaAsync(FeedFormulaCreateDto feedFormula);
    Task<FeedFormulaDto> UpdateFeedFormulaAsync(Guid id, FeedFormulaUpdateDto feedFormula);
    Task<bool> DeleteFeedFormulaAsync(Guid id);
    Task<bool> ExistsAsync(string productCode);
    Task<ImportResultDto> ImportFromPosposAsync();
    Task<IEnumerable<PosposProductDto>> GetPosposProductsAsync();
    Task<ImportResultDto> ImportSelectedFromPosposAsync(List<string> productCodes);
    Task<List<PosposProductDto>> SearchPosposProductsAsync(string q);
    Task<PigFarmManagement.Shared.Models.ImportResult> ImportSelectedFromPosposAsync(List<Guid> productIds);
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

    public async Task<ImportResultDto> ImportFromPosposAsync()
    {
        var response = await _httpClient.PostAsync("api/feed-formulas/import", null);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ImportResultDto>(json, _jsonOptions)!;
    }

    public async Task<IEnumerable<PosposProductDto>> GetPosposProductsAsync()
    {
        var response = await _httpClient.GetAsync("api/feed-formulas/pospos-products");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<IEnumerable<PosposProductDto>>(json, _jsonOptions) ?? [];
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

    public async Task<List<PosposProductDto>> SearchPosposProductsAsync(string q)
    {
        var response = await _httpClient.GetAsync($"api/products/search?q={Uri.EscapeDataString(q)}");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<IEnumerable<PosposProductDto>>(json, _jsonOptions) ?? [];
        return products.ToList();
    }

    public async Task<PigFarmManagement.Shared.Models.ImportResult> ImportSelectedFromPosposAsync(List<Guid> productIds)
    {
        var request = new ImportRequest { ProductIds = productIds };
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("api/products/import", content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PigFarmManagement.Shared.Models.ImportResult>(responseJson, _jsonOptions)!;
    }
}
