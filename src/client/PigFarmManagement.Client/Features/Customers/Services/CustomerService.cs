using PigFarmManagement.Shared.Models;
using System.Net.Http.Json;

namespace PigFarmManagement.Client.Features.Customers.Services;

public interface ICustomerService
{
    Task<List<Customer>> GetCustomersAsync();
    Task<Customer?> GetCustomerByIdAsync(Guid id);
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task<Customer> UpdateCustomerAsync(Customer customer);
    Task<bool> DeleteCustomerAsync(Guid id);
    
    // T022: Deletion methods
    Task<CustomerDeletionValidation> ValidateCustomerDeletionAsync(Guid customerId);
    Task<Customer> SoftDeleteCustomerAsync(CustomerDeletionRequest request);
    Task<bool> HardDeleteCustomerAsync(CustomerDeletionRequest request);
    
    // T023: Location methods
    Task<CustomerLocationDto?> GetCustomerLocationAsync(Guid customerId);
    Task<CustomerLocationDto> UpdateCustomerLocationAsync(CustomerLocationDto location);
    Task<bool> DeleteCustomerLocationAsync(Guid customerId);
}

public class CustomerService : ICustomerService
{
    private readonly HttpClient _httpClient;

    public CustomerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Customer>> GetCustomersAsync()
    {
        var customers = await _httpClient.GetFromJsonAsync<List<Customer>>("api/customers");
        return customers ?? new List<Customer>();
    }

    public async Task<Customer?> GetCustomerByIdAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<Customer>($"api/customers/{id}");
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        var response = await _httpClient.PostAsJsonAsync("api/customers", customer);
        response.EnsureSuccessStatusCode();
        var createdCustomer = await response.Content.ReadFromJsonAsync<Customer>();
        return createdCustomer ?? customer;
    }

    public async Task<Customer> UpdateCustomerAsync(Customer customer)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/customers/{customer.Id}", customer);
        response.EnsureSuccessStatusCode();
        var updatedCustomer = await response.Content.ReadFromJsonAsync<Customer>();
        return updatedCustomer ?? customer;
    }

    public async Task<bool> DeleteCustomerAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/customers/{id}");
        return response.IsSuccessStatusCode;
    }

    // T022: Deletion methods implementation
    public async Task<CustomerDeletionValidation> ValidateCustomerDeletionAsync(Guid customerId)
    {
        var response = await _httpClient.PostAsync($"api/customers/{customerId}/validate-deletion", null);
        response.EnsureSuccessStatusCode();
        
        var validation = await response.Content.ReadFromJsonAsync<CustomerDeletionValidation>();
        return validation ?? throw new InvalidOperationException("Failed to deserialize deletion validation");
    }

    public async Task<Customer> SoftDeleteCustomerAsync(CustomerDeletionRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/customers/{request.CustomerId}/soft-delete", request);
        response.EnsureSuccessStatusCode();
        
        var customer = await response.Content.ReadFromJsonAsync<Customer>();
        return customer ?? throw new InvalidOperationException("Failed to deserialize soft deleted customer");
    }

    public async Task<bool> HardDeleteCustomerAsync(CustomerDeletionRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/customers/{request.CustomerId}/hard-delete", request);
        return response.IsSuccessStatusCode;
    }

    // T023: Location methods implementation
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

    public async Task<bool> DeleteCustomerLocationAsync(Guid customerId)
    {
        var response = await _httpClient.DeleteAsync($"api/customers/{customerId}/location");
        return response.IsSuccessStatusCode;
    }
}
