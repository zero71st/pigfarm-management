using PigFarmManagement.Shared.Models;
using PigFarmManagement.Client.Features.Feeds.Services;
using System.Net.Http.Json;

namespace PigFarmManagement.Client.Features.PigPens.Services;

public interface IPigPenService
{
    // Pig Pen CRUD
    Task<List<PigPen>> GetPigPensAsync();
    Task<PigPen?> GetPigPenByIdAsync(Guid id);
    Task<PigPenSummary?> GetPigPenSummaryAsync(Guid id);
    Task<PigPen> CreatePigPenAsync(PigPenCreateDto dto);
    Task<PigPen> UpdatePigPenAsync(Guid id, PigPenUpdateDto dto);
    Task<bool> DeletePigPenAsync(Guid id);
    Task<PigPen> ForceClosePigPenAsync(PigPenForceCloseRequest request);

    // Feed Items
    Task<List<FeedItem>> GetFeedItemsAsync(Guid pigPenId);
    Task<FeedItem> AddFeedItemAsync(Guid pigPenId, FeedCreateDto dto);
    Task<bool> DeleteFeedItemAsync(Guid pigPenId, Guid feedItemId);

    // Deposits
    Task<List<Deposit>> GetDepositsAsync(Guid pigPenId);
    Task<Deposit> AddDepositAsync(Guid pigPenId, DepositCreateDto dto);
    Task<Deposit> UpdateDepositAsync(Guid pigPenId, Guid depositId, DepositUpdateDto dto);
    Task<bool> DeleteDepositAsync(Guid pigPenId, Guid depositId);

    // Harvest Results
    Task<List<HarvestResult>> GetHarvestResultsAsync(Guid pigPenId);
    Task<HarvestResult> AddHarvestResultAsync(Guid pigPenId, HarvestCreateDto dto);
    Task<HarvestResult> UpdateHarvestResultAsync(Guid pigPenId, Guid harvestId, HarvestUpdateDto dto);
    Task<bool> DeleteHarvestResultAsync(Guid pigPenId, Guid harvestId);

    // Formula Assignments
    Task<List<PigPenFormulaAssignment>> GetFormulaAssignmentsAsync(Guid pigPenId);
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

    public async Task<PigPen> UpdatePigPenAsync(Guid id, PigPenUpdateDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/pigpens/{id}", dto);
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

    public async Task<bool> DeleteFeedItemAsync(Guid pigPenId, Guid feedItemId)
    {
        var response = await _httpClient.DeleteAsync($"api/pigpens/{pigPenId}/feeds/{feedItemId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<Deposit> AddDepositAsync(Guid pigPenId, DepositCreateDto deposit)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/pigpens/{pigPenId}/deposits", deposit);
        response.EnsureSuccessStatusCode();
        var createdDeposit = await response.Content.ReadFromJsonAsync<Deposit>();
        return createdDeposit!;
    }

    public async Task<Deposit> UpdateDepositAsync(Guid pigPenId, Guid depositId, DepositUpdateDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/pigpens/{pigPenId}/deposits/{depositId}", dto);
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

    public async Task<HarvestResult> UpdateHarvestResultAsync(Guid pigPenId, Guid harvestId, HarvestUpdateDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/pigpens/{pigPenId}/harvests/{harvestId}", dto);
        response.EnsureSuccessStatusCode();
        var updatedHarvest = await response.Content.ReadFromJsonAsync<HarvestResult>();
        return updatedHarvest!;
    }

    public async Task<bool> DeleteHarvestResultAsync(Guid pigPenId, Guid harvestId)
    {
        var response = await _httpClient.DeleteAsync($"api/pigpens/{pigPenId}/harvests/{harvestId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<PigPen> ForceClosePigPenAsync(PigPenForceCloseRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/pigpens/{request.PigPenId}/force-close", request);
        response.EnsureSuccessStatusCode();
        var closedPigPen = await response.Content.ReadFromJsonAsync<PigPen>();
        return closedPigPen!;
    }

    public async Task<List<PigPenFormulaAssignment>> GetFormulaAssignmentsAsync(Guid pigPenId)
    {
        var assignments = await _httpClient.GetFromJsonAsync<List<PigPenFormulaAssignment>>($"api/pigpens/{pigPenId}/formula-assignments");
        return assignments ?? new List<PigPenFormulaAssignment>();
    }
}
