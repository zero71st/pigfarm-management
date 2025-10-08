using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Services;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;

namespace PigFarmManagement.Server.Features.Customers;

public interface ICustomerService
{
    Task<List<Customer>> GetAllCustomersAsync();
    Task<Customer?> GetCustomerByIdAsync(Guid id);
    Task<Customer> CreateCustomerAsync(CustomerCreateDto dto);
    Task<Customer> UpdateCustomerAsync(Guid id, CustomerUpdateDto dto);
    Task<bool> DeleteCustomerAsync(Guid id);
    
    // Location management
    Task<CustomerLocationDto?> GetCustomerLocationAsync(Guid customerId);
    Task<CustomerLocationDto> UpdateCustomerLocationAsync(CustomerLocationDto location);
    Task DeleteCustomerLocationAsync(Guid customerId);
    
    // Deletion validation
    Task<CustomerDeletionValidation> ValidateCustomerDeletionAsync(Guid customerId);
    Task<Customer> SoftDeleteCustomerAsync(CustomerDeletionRequest request);
    Task HardDeleteCustomerAsync(CustomerDeletionRequest request);
}

public class CustomerService : ICustomerService
{
    private readonly Infrastructure.Data.Repositories.ICustomerRepository _customerRepository;
    private readonly ICustomerLocationService _locationService;
    private readonly ICustomerDeletionService _deletionService;

    public CustomerService(
        Infrastructure.Data.Repositories.ICustomerRepository customerRepository,
        ICustomerLocationService locationService,
        ICustomerDeletionService deletionService)
    {
        _customerRepository = customerRepository;
        _locationService = locationService;
        _deletionService = deletionService;
    }

    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        var customers = await _customerRepository.GetAllAsync();
        return customers.ToList();
    }

    public async Task<Customer?> GetCustomerByIdAsync(Guid id)
    {
        return await _customerRepository.GetByIdAsync(id);
    }

    public async Task<Customer> CreateCustomerAsync(CustomerCreateDto dto)
    {
        // Business logic: Check if customer code already exists
        var existingCustomer = await _customerRepository.GetByCodeAsync(dto.Code);
        if (existingCustomer != null)
        {
            throw new InvalidOperationException("Customer code already exists");
        }

        return await _customerRepository.CreateAsync(dto);
    }

    public async Task<Customer> UpdateCustomerAsync(Guid id, CustomerUpdateDto dto)
    {
        // Business logic: Check if customer exists
        var existingCustomer = await _customerRepository.GetByIdIncludingDeletedAsync(id);
        if (existingCustomer == null)
        {
            throw new InvalidOperationException("Customer not found");
        }

        // Business logic: Check if customer code already exists for another customer
        if (dto.Code != null)
        {
            var codeConflict = await _customerRepository.GetByCodeAsync(dto.Code);
            if (codeConflict != null && codeConflict.Id != id)
            {
                throw new InvalidOperationException("Customer code already exists");
            }
        }

        return await _customerRepository.UpdateAsync(id, dto);
    }

    public async Task<bool> DeleteCustomerAsync(Guid id)
    {
        // Use soft delete through the deletion service
        var request = new CustomerDeletionRequest
        {
            CustomerId = id,
            ForceDelete = false,
            DeletedBy = "System",
            Reason = "Legacy delete operation"
        };

        await _deletionService.SoftDeleteCustomerAsync(request);
        return true;
    }

    // Location management methods
    public async Task<CustomerLocationDto?> GetCustomerLocationAsync(Guid customerId)
    {
        return await _locationService.GetLocationAsync(customerId);
    }

    public async Task<CustomerLocationDto> UpdateCustomerLocationAsync(CustomerLocationDto location)
    {
        return await _locationService.UpdateLocationAsync(location);
    }

    public async Task DeleteCustomerLocationAsync(Guid customerId)
    {
        await _locationService.DeleteLocationAsync(customerId);
    }

    // Deletion validation methods
    public async Task<CustomerDeletionValidation> ValidateCustomerDeletionAsync(Guid customerId)
    {
        return await _deletionService.ValidateCustomerDeletionAsync(customerId);
    }

    public async Task<Customer> SoftDeleteCustomerAsync(CustomerDeletionRequest request)
    {
        return await _deletionService.SoftDeleteCustomerAsync(request);
    }

    public async Task HardDeleteCustomerAsync(CustomerDeletionRequest request)
    {
        await _deletionService.HardDeleteCustomerAsync(request);
    }
}
