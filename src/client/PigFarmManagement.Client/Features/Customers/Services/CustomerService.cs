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
}
