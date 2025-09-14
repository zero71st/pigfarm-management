using PigFarmManagement.Shared.Models;
using PigFarmManagement.Client.Features.Feeds.Services;
using System.Net.Http.Json;

namespace PigFarmManagement.Client.Features.PigPens.Services;

public interface IPigPenService
{
    Task<List<PigPen>> GetPigPensAsync();
    Task<PigPen?> GetPigPenByIdAsync(Guid id);
    Task<PigPenSummary?> GetPigPenSummaryAsync(Guid id);
    Task<PigPen> CreatePigPenAsync(PigPenCreateDto pigPen);
    Task<PigPen> UpdatePigPenAsync(PigPen pigPen);
    Task<bool> DeletePigPenAsync(Guid id);
    Task<List<FeedItem>> GetFeedItemsAsync(Guid pigPenId);
    Task<List<Deposit>> GetDepositsAsync(Guid pigPenId);
    Task<List<HarvestResult>> GetHarvestResultsAsync(Guid pigPenId);
    Task<FeedItem> AddFeedItemAsync(Guid pigPenId, FeedCreateDto feedItem);
    Task<Deposit> AddDepositAsync(Guid pigPenId, DepositCreateDto deposit);
    Task<Deposit> UpdateDepositAsync(Deposit deposit);
    Task<bool> DeleteDepositAsync(Guid pigPenId, Guid depositId);
    Task<HarvestResult> AddHarvestResultAsync(Guid pigPenId, HarvestCreateDto harvest);
}

public class PigPenService : IPigPenService
{
    private readonly HttpClient _httpClient;

    public PigPenService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<PigPen>> GetPigPensAsync()
    {
        var pigPens = await _httpClient.GetFromJsonAsync<List<PigPen>>("api/pigpens");
        return pigPens ?? new List<PigPen>();
    }

    public async Task<PigPen?> GetPigPenByIdAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<PigPen>($"api/pigpens/{id}");
    }

    public async Task<PigPenSummary?> GetPigPenSummaryAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<PigPenSummary>($"api/pigpens/{id}/summary");
    }

    public async Task<PigPen> CreatePigPenAsync(PigPenCreateDto pigPen)
    {
        var response = await _httpClient.PostAsJsonAsync("api/pigpens", pigPen);
        response.EnsureSuccessStatusCode();
        var createdPigPen = await response.Content.ReadFromJsonAsync<PigPen>();
        return createdPigPen!;
    }

    public async Task<PigPen> UpdatePigPenAsync(PigPen pigPen)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/pigpens/{pigPen.Id}", pigPen);
        response.EnsureSuccessStatusCode();
        var updatedPigPen = await response.Content.ReadFromJsonAsync<PigPen>();
        return updatedPigPen!;
    }

    public async Task<bool> DeletePigPenAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/pigpens/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<FeedItem>> GetFeedItemsAsync(Guid pigPenId)
    {
        var feedItems = await _httpClient.GetFromJsonAsync<List<FeedItem>>($"api/pigpens/{pigPenId}/feeds");
        return feedItems ?? new List<FeedItem>();
    }

    public async Task<List<Deposit>> GetDepositsAsync(Guid pigPenId)
    {
        var deposits = await _httpClient.GetFromJsonAsync<List<Deposit>>($"api/pigpens/{pigPenId}/deposits");
        return deposits ?? new List<Deposit>();
    }

    public async Task<List<HarvestResult>> GetHarvestResultsAsync(Guid pigPenId)
    {
        var harvests = await _httpClient.GetFromJsonAsync<List<HarvestResult>>($"api/pigpens/{pigPenId}/harvests");
        return harvests ?? new List<HarvestResult>();
    }

    public async Task<FeedItem> AddFeedItemAsync(Guid pigPenId, FeedCreateDto feedItem)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/pigpens/{pigPenId}/feeds", feedItem);
        response.EnsureSuccessStatusCode();
        var createdFeedItem = await response.Content.ReadFromJsonAsync<FeedItem>();
        return createdFeedItem!;
    }

    public async Task<Deposit> AddDepositAsync(Guid pigPenId, DepositCreateDto deposit)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/pigpens/{pigPenId}/deposits", deposit);
        response.EnsureSuccessStatusCode();
        var createdDeposit = await response.Content.ReadFromJsonAsync<Deposit>();
        return createdDeposit!;
    }

    public async Task<Deposit> UpdateDepositAsync(Deposit deposit)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/pigpens/{deposit.PigPenId}/deposits/{deposit.Id}", deposit);
        response.EnsureSuccessStatusCode();
        var updatedDeposit = await response.Content.ReadFromJsonAsync<Deposit>();
        return updatedDeposit!;
    }

    public async Task<bool> DeleteDepositAsync(Guid pigPenId, Guid depositId)
    {
        var response = await _httpClient.DeleteAsync($"api/pigpens/{pigPenId}/deposits/{depositId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<HarvestResult> AddHarvestResultAsync(Guid pigPenId, HarvestCreateDto harvest)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/pigpens/{pigPenId}/harvests", harvest);
        response.EnsureSuccessStatusCode();
        var createdHarvest = await response.Content.ReadFromJsonAsync<HarvestResult>();
        return createdHarvest!;
    }
}

// DTOs that should be moved to shared project later
public record PigPenCreateDto(Guid CustomerId, string PenCode, int PigQty, DateTime RegisterDate, DateTime? ActHarvestDate, DateTime? EstimatedHarvestDate, PigPenType Type, Guid? FeedFormulaId, string? SelectedBrand, decimal DepositPerPig = 1500m);
public record DepositCreateDto(decimal Amount, DateTime Date, string? Remark);
public record HarvestCreateDto(DateTime HarvestDate, int PigCount, decimal AvgWeight, decimal MinWeight, decimal MaxWeight, decimal SalePricePerKg);
