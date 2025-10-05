using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Client.Services;

/// <summary>
/// Client service for managing customer location data via API calls
/// </summary>
public interface ICustomerLocationService
{
    Task<CustomerLocationDto?> GetCustomerLocationAsync(Guid customerId);
    Task<CustomerLocationDto> UpdateCustomerLocationAsync(CustomerLocationDto location);
    Task DeleteCustomerLocationAsync(Guid customerId);
    bool ValidateLocation(decimal latitude, decimal longitude);
}

public class CustomerLocationService : ICustomerLocationService
{
    private readonly HttpClient _httpClient;

    public CustomerLocationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CustomerLocationDto?> GetCustomerLocationAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/customers/{customerId}/location");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CustomerLocationDto>();
            }
            
            // Return null if not found (404) or other client errors
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            return null;
        }
        catch (HttpRequestException)
        {
            // Handle network issues gracefully
            return null;
        }
    }

    public async Task<CustomerLocationDto> UpdateCustomerLocationAsync(CustomerLocationDto location)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/customers/{location.CustomerId}/location", location);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<CustomerLocationDto>();
        return result ?? throw new InvalidOperationException("Failed to deserialize updated location");
    }

    public async Task DeleteCustomerLocationAsync(Guid customerId)
    {
        var response = await _httpClient.DeleteAsync($"api/customers/{customerId}/location");
        response.EnsureSuccessStatusCode();
    }

    public bool ValidateLocation(decimal latitude, decimal longitude)
    {
        // Client-side validation for location coordinates
        if (latitude < -90 || latitude > 90)
        {
            return false;
        }
        
        if (longitude < -180 || longitude > 180)
        {
            return false;
        }
        
        return true;
    }
}