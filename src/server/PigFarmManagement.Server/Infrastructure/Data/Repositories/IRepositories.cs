using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Repositories;

public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<IEnumerable<Customer>> GetAllIncludingDeletedAsync();
    Task<IEnumerable<Customer>> GetDeletedAsync();
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer?> GetByIdIncludingDeletedAsync(Guid id);
    Task<Customer?> GetByCodeAsync(string code);
    Task<IEnumerable<Customer>> GetByExternalIdsAsync(IEnumerable<string> externalIds);
    Task<Customer> CreateAsync(CustomerCreateDto dto);
    Task<Customer> UpdateAsync(Guid id, CustomerUpdateDto dto);
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
    Task<bool> ExistsByInvoiceNumberAsync(string invoiceNumber); // Note: actually checks TransactionCode field
    Task<int> DeleteByInvoiceReferenceAsync(Guid pigPenId, string invoiceReferenceCode);
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

public interface IFeedFormulaRepository
{
    Task<IEnumerable<FeedFormula>> GetAllAsync();
    Task<FeedFormula?> GetByIdAsync(Guid id);
    Task<FeedFormula?> GetByCodeAsync(string code);
    Task<FeedFormula> CreateAsync(FeedFormulaCreateDto dto);
    Task<FeedFormula> UpdateAsync(Guid id, FeedFormulaUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(string code);
    Task<IEnumerable<FeedFormula>> CreateManyAsync(IEnumerable<FeedFormulaCreateDto> dtos);
    Task<IEnumerable<FeedFormula>> GetByCodesAsync(IEnumerable<string> codes);
    Task<IEnumerable<FeedFormula>> GetByExternalIdsAsync(IEnumerable<Guid> externalIds);
    Task<FeedFormula> UpsertByExternalIdAsync(Guid externalId, FeedFormulaCreateDto dto);
}
