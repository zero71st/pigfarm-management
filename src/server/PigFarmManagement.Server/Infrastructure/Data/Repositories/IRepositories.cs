using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Repositories;

public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer?> GetByCodeAsync(string code);
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
    Task DeleteAsync(Guid id);
}

public interface IPigPenRepository
{
    Task<IEnumerable<PigPen>> GetAllAsync();
    Task<PigPen?> GetByIdAsync(Guid id);
    Task<IEnumerable<PigPen>> GetByCustomerIdAsync(Guid customerId);
    Task<PigPen?> GetByPenCodeAsync(string penCode);
    Task<PigPen> CreateAsync(PigPen pigPen);
    Task<PigPen> UpdateAsync(PigPen pigPen);
    Task DeleteAsync(Guid id);
}

public interface IFeedRepository
{
    Task<IEnumerable<Feed>> GetAllAsync();
    Task<Feed?> GetByIdAsync(Guid id);
    Task<IEnumerable<Feed>> GetByPigPenIdAsync(Guid pigPenId);
    Task<Feed> CreateAsync(Feed feed);
    Task<Feed> UpdateAsync(Feed feed);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Feed>> CreateManyAsync(IEnumerable<Feed> feeds);
    Task<bool> ExistsByInvoiceNumberAsync(string invoiceNumber);
}

public interface IDepositRepository
{
    Task<IEnumerable<Deposit>> GetAllAsync();
    Task<Deposit?> GetByIdAsync(Guid id);
    Task<IEnumerable<Deposit>> GetByPigPenIdAsync(Guid pigPenId);
    Task<Deposit> CreateAsync(Deposit deposit);
    Task<Deposit> UpdateAsync(Deposit deposit);
    Task DeleteAsync(Guid id);
}

public interface IHarvestRepository
{
    Task<IEnumerable<HarvestResult>> GetAllAsync();
    Task<HarvestResult?> GetByIdAsync(Guid id);
    Task<IEnumerable<HarvestResult>> GetByPigPenIdAsync(Guid pigPenId);
    Task<HarvestResult> CreateAsync(HarvestResult harvest);
    Task<HarvestResult> UpdateAsync(HarvestResult harvest);
    Task DeleteAsync(Guid id);
}
