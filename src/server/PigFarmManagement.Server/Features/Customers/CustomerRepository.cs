using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Infrastructure.Data;

namespace PigFarmManagement.Server.Features.Customers;

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsWithCodeAsync(string code, Guid? excludeId = null);
    Task<bool> HasAssociatedPigPensAsync(Guid customerId);
}

public class CustomerRepository : ICustomerRepository
{
    private readonly InMemoryDataStore _dataStore;

    public CustomerRepository(InMemoryDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Task<List<Customer>> GetAllAsync()
    {
        return Task.FromResult(_dataStore.Customers.ToList());
    }

    public Task<Customer?> GetByIdAsync(Guid id)
    {
        var customer = _dataStore.Customers.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(customer);
    }

    public Task<Customer> CreateAsync(Customer customer)
    {
        _dataStore.Customers.Add(customer);
        return Task.FromResult(customer);
    }

    public Task<Customer> UpdateAsync(Customer customer)
    {
        var existingCustomer = _dataStore.Customers.FirstOrDefault(c => c.Id == customer.Id);
        if (existingCustomer != null)
        {
            _dataStore.Customers.Remove(existingCustomer);
        }
        _dataStore.Customers.Add(customer);
        return Task.FromResult(customer);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var customer = _dataStore.Customers.FirstOrDefault(c => c.Id == id);
        if (customer != null)
        {
            _dataStore.Customers.Remove(customer);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> ExistsWithCodeAsync(string code, Guid? excludeId = null)
    {
        var exists = _dataStore.Customers.Any(c => c.Code == code && (excludeId == null || c.Id != excludeId));
        return Task.FromResult(exists);
    }

    public Task<bool> HasAssociatedPigPensAsync(Guid customerId)
    {
        var hasAssociation = _dataStore.PigPens.Any(p => p.CustomerId == customerId);
        return Task.FromResult(hasAssociation);
    }
}
