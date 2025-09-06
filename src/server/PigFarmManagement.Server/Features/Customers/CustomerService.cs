using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.Customers;

public interface ICustomerService
{
    Task<List<Customer>> GetAllCustomersAsync();
    Task<Customer?> GetCustomerByIdAsync(Guid id);
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task<Customer> UpdateCustomerAsync(Customer customer);
    Task<bool> DeleteCustomerAsync(Guid id);
}

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        return await _customerRepository.GetAllAsync();
    }

    public async Task<Customer?> GetCustomerByIdAsync(Guid id)
    {
        return await _customerRepository.GetByIdAsync(id);
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        // Business logic: Check if customer code already exists
        if (await _customerRepository.ExistsWithCodeAsync(customer.Code))
        {
            throw new InvalidOperationException("Customer code already exists");
        }

        return await _customerRepository.CreateAsync(customer);
    }

    public async Task<Customer> UpdateCustomerAsync(Customer customer)
    {
        // Business logic: Check if customer exists
        var existingCustomer = await _customerRepository.GetByIdAsync(customer.Id);
        if (existingCustomer == null)
        {
            throw new InvalidOperationException("Customer not found");
        }

        // Business logic: Check if customer code already exists for another customer
        if (await _customerRepository.ExistsWithCodeAsync(customer.Code, customer.Id))
        {
            throw new InvalidOperationException("Customer code already exists");
        }

        return await _customerRepository.UpdateAsync(customer);
    }

    public async Task<bool> DeleteCustomerAsync(Guid id)
    {
        // Business logic: Check if customer exists
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer == null)
        {
            throw new InvalidOperationException("Customer not found");
        }

        // Business logic: Check if customer has associated pig pens
        if (await _customerRepository.HasAssociatedPigPensAsync(id))
        {
            throw new InvalidOperationException("Cannot delete customer with associated pig pens");
        }

        return await _customerRepository.DeleteAsync(id);
    }
}
