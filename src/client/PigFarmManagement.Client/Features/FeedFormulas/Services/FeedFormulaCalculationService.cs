using System.Text.Json;

namespace PigFarmManagement.Client.Features.FeedFormulas.Services;

// Request and Response DTOs for client
public record FeedCalculationRequest(Guid FeedFormulaId, int PigCount, decimal? BagPrice = null);

public class FeedFormulaWithCalculationResponse
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
    public decimal TotalBagsRequired { get; set; }
    public int PigCount { get; set; }
}

public class FeedCalculationResponse
{
    public Guid FeedFormulaId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int PigCount { get; set; }
    public decimal BagPerPig { get; set; }
    public decimal TotalBagsRequired { get; set; }
    public decimal? BagPrice { get; set; }
    public decimal? TotalCost { get; set; }
    public DateTime CalculationDate { get; set; }
}

public interface IFeedFormulaCalculationService
{
    Task<IEnumerable<FeedFormulaWithCalculationResponse>> GetFeedFormulasByBrandAsync(string brand, int? pigCount = null);
    Task<FeedCalculationResponse> CalculateFeedRequirementsAsync(FeedCalculationRequest request);
}

public class FeedFormulaCalculationService : IFeedFormulaCalculationService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public FeedFormulaCalculationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<IEnumerable<FeedFormulaWithCalculationResponse>> GetFeedFormulasByBrandAsync(string brand, int? pigCount = null)
    {
        var url = $"api/feed-formulas/by-brand/{Uri.EscapeDataString(brand)}";
        if (pigCount.HasValue)
        {
            url += $"?pigCount={pigCount.Value}";
        }

        Console.WriteLine($"[FeedFormulaCalculationService] Calling URL: {_httpClient.BaseAddress}{url}");
        Console.WriteLine($"[FeedFormulaCalculationService] Escaped brand: {Uri.EscapeDataString(brand)}");

        try
        {
            var response = await _httpClient.GetAsync(url);
            Console.WriteLine($"[FeedFormulaCalculationService] Response status: {response.StatusCode}");
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[FeedFormulaCalculationService] Response JSON: {json}");
            
            var result = JsonSerializer.Deserialize<IEnumerable<FeedFormulaWithCalculationResponse>>(json, _jsonOptions) ?? [];
            Console.WriteLine($"[FeedFormulaCalculationService] Deserialized {result.Count()} items");
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FeedFormulaCalculationService] Error: {ex.Message}");
            Console.WriteLine($"[FeedFormulaCalculationService] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<FeedCalculationResponse> CalculateFeedRequirementsAsync(FeedCalculationRequest request)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("api/feed-formulas/calculate-requirements", content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FeedCalculationResponse>(responseJson, _jsonOptions)!;
    }
}
