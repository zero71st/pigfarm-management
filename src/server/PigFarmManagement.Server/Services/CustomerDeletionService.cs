using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;

namespace PigFarmManagement.Server.Services;

/// <summary>
/// Service for managing customer deletion with business rule validation
/// </summary>
public interface ICustomerDeletionService
{
    Task<CustomerDeletionValidation> ValidateCustomerDeletionAsync(Guid customerId);
    Task<Customer> SoftDeleteCustomerAsync(CustomerDeletionRequest request);
    Task HardDeleteCustomerAsync(CustomerDeletionRequest request);
}

public class CustomerDeletionService : ICustomerDeletionService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPigPenRepository _pigPenRepository;
    private readonly ILogger<CustomerDeletionService> _logger;

    public CustomerDeletionService(
        ICustomerRepository customerRepository,
        IPigPenRepository pigPenRepository,
        ILogger<CustomerDeletionService> logger)
    {
        _customerRepository = customerRepository;
        _pigPenRepository = pigPenRepository;
        _logger = logger;
    }

    public async Task<CustomerDeletionValidation> ValidateCustomerDeletionAsync(Guid customerId)
    {
        var validation = new CustomerDeletionValidation();
        var blockingReasons = new List<string>();

        // Check for active pig pens
        var pigPens = await _pigPenRepository.GetByCustomerIdAsync(customerId);
        var activePigPens = pigPens.Where(p => p.ActHarvestDate == null).ToList();
        
        validation.ActivePigPenCount = activePigPens.Count;
        
        if (activePigPens.Any())
        {
            blockingReasons.Add($"Customer has {activePigPens.Count} active pig pen(s) without harvest dates");
        }

        // Check for recent activity (last 30 days)
        var recentDate = DateTime.UtcNow.AddDays(-30);
        var hasRecentPigPens = pigPens.Any(p => p.CreatedAt > recentDate || p.UpdatedAt > recentDate);
        
        validation.HasRecentActivity = hasRecentPigPens;
        
        if (hasRecentPigPens)
        {
            blockingReasons.Add("Customer has recent activity within the last 30 days");
        }

        // TODO: Add transaction count validation when transaction entity is implemented
        validation.ActiveTransactionCount = 0;

        validation.BlockingReasons = blockingReasons;
        validation.CanDelete = !blockingReasons.Any();

        return validation;
    }

    public async Task<Customer> SoftDeleteCustomerAsync(CustomerDeletionRequest request)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
        if (customer == null)
        {
            throw new ArgumentException($"Customer with ID {request.CustomerId} not found");
        }

        if (customer.IsDeleted)
        {
            throw new InvalidOperationException("Customer is already deleted");
        }

        var validation = await ValidateCustomerDeletionAsync(request.CustomerId);
        if (!validation.CanDelete && !request.ForceDelete)
        {
            throw new InvalidOperationException($"Cannot delete customer: {string.Join(", ", validation.BlockingReasons)}");
        }

        var deletedCustomer = customer with
        {
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = request.DeletedBy,
            UpdatedAt = DateTime.UtcNow
        };

        await _customerRepository.UpdateAsync(deletedCustomer);
        
        _logger.LogInformation("Soft deleted customer {CustomerId} by {DeletedBy}. Reason: {Reason}", 
            request.CustomerId, request.DeletedBy, request.Reason);

        return deletedCustomer;
    }

    public async Task HardDeleteCustomerAsync(CustomerDeletionRequest request)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
        if (customer == null)
        {
            throw new ArgumentException($"Customer with ID {request.CustomerId} not found");
        }

        var validation = await ValidateCustomerDeletionAsync(request.CustomerId);
        if (!validation.CanDelete && !request.ForceDelete)
        {
            throw new InvalidOperationException($"Cannot delete customer: {string.Join(", ", validation.BlockingReasons)}");
        }

        await _customerRepository.DeleteAsync(request.CustomerId);
        
        _logger.LogWarning("Hard deleted customer {CustomerId} by {DeletedBy}. Reason: {Reason}", 
            request.CustomerId, request.DeletedBy, request.Reason);
    }
}