using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;

namespace PigFarmManagement.Server.Services;

/// <summary>
/// Service for managing customer location data and Google Maps integration
/// </summary>
public interface ICustomerLocationService
{
    Task<CustomerLocationDto?> GetLocationAsync(Guid customerId);
    Task<CustomerLocationDto> UpdateLocationAsync(CustomerLocationDto location);
    Task DeleteLocationAsync(Guid customerId);
    Task<bool> ValidateCoordinatesAsync(decimal latitude, decimal longitude);
}

public class CustomerLocationService : ICustomerLocationService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<CustomerLocationService> _logger;

    public CustomerLocationService(
        ICustomerRepository customerRepository, 
        ILogger<CustomerLocationService> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<CustomerLocationDto?> GetLocationAsync(Guid customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer == null || !customer.HasLocation)
        {
            return null;
        }

        return new CustomerLocationDto
        {
            CustomerId = customer.Id,
            Latitude = customer.Latitude!.Value,
            Longitude = customer.Longitude!.Value,
            Address = customer.Address,
            UpdatedAt = customer.UpdatedAt
        };
    }

    public async Task<CustomerLocationDto> UpdateLocationAsync(CustomerLocationDto location)
    {
        if (!await ValidateCoordinatesAsync(location.Latitude, location.Longitude))
        {
            throw new ArgumentException("Invalid coordinates provided");
        }

        var customer = await _customerRepository.GetByIdAsync(location.CustomerId);
        if (customer == null)
        {
            throw new ArgumentException($"Customer with ID {location.CustomerId} not found");
        }

        var updatedCustomer = customer with
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Address = location.Address,
            UpdatedAt = DateTime.UtcNow
        };

        await _customerRepository.UpdateAsync(updatedCustomer);
        
        _logger.LogInformation("Updated location for customer {CustomerId}", location.CustomerId);

        return new CustomerLocationDto
        {
            CustomerId = updatedCustomer.Id,
            Latitude = updatedCustomer.Latitude!.Value,
            Longitude = updatedCustomer.Longitude!.Value,
            Address = updatedCustomer.Address,
            UpdatedAt = updatedCustomer.UpdatedAt
        };
    }

    public async Task DeleteLocationAsync(Guid customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer == null)
        {
            throw new ArgumentException($"Customer with ID {customerId} not found");
        }

        var updatedCustomer = customer with
        {
            Latitude = null,
            Longitude = null,
            UpdatedAt = DateTime.UtcNow
        };

        await _customerRepository.UpdateAsync(updatedCustomer);
        
        _logger.LogInformation("Deleted location for customer {CustomerId}", customerId);
    }

    public async Task<bool> ValidateCoordinatesAsync(decimal latitude, decimal longitude)
    {
        // Validate latitude range (-90 to 90)
        if (latitude < -90 || latitude > 90)
        {
            return false;
        }

        // Validate longitude range (-180 to 180)
        if (longitude < -180 || longitude > 180)
        {
            return false;
        }

        return await Task.FromResult(true);
    }
}