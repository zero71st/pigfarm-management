using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;

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
    private readonly Infrastructure.Data.Repositories.ICustomerRepository _efCustomerRepository;
    private readonly Infrastructure.Data.Repositories.IPigPenRepository _efPigPenRepository;

    public CustomerRepository(
        Infrastructure.Data.Repositories.ICustomerRepository efCustomerRepository,
        Infrastructure.Data.Repositories.IPigPenRepository efPigPenRepository)
    {
        _efCustomerRepository = efCustomerRepository;
        _efPigPenRepository = efPigPenRepository;
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        var customers = await _efCustomerRepository.GetAllAsync();
        return customers.ToList();
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        return await _efCustomerRepository.GetByIdAsync(id);
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        return await _efCustomerRepository.CreateAsync(customer);
    }

    public async Task<Customer> UpdateAsync(Customer customer)
    {
        return await _efCustomerRepository.UpdateAsync(customer);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _efCustomerRepository.DeleteAsync(id);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExistsWithCodeAsync(string code, Guid? excludeId = null)
    {
        var customer = await _efCustomerRepository.GetByCodeAsync(code);
        return customer != null && (excludeId == null || customer.Id != excludeId);
    }

    public async Task<bool> HasAssociatedPigPensAsync(Guid customerId)
    {
        var pigPens = await _efPigPenRepository.GetByCustomerIdAsync(customerId);
        return pigPens.Any();
    }
}
