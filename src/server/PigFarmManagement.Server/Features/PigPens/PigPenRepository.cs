using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Infrastructure.Data;

namespace PigFarmManagement.Server.Features.PigPens;

public interface IPigPenRepository
{
    Task<List<PigPen>> GetAllAsync();
    Task<PigPen?> GetByIdAsync(Guid id);
    Task<PigPen> CreateAsync(PigPen pigPen);
    Task<PigPen> UpdateAsync(PigPen pigPen);
    Task<bool> DeleteAsync(Guid id);
    Task<List<PigPen>> GetByCustomerIdAsync(Guid customerId);
}

public class PigPenRepository : IPigPenRepository
{
    private readonly InMemoryDataStore _dataStore;

    public PigPenRepository(InMemoryDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Task<List<PigPen>> GetAllAsync()
    {
        return Task.FromResult(_dataStore.PigPens.ToList());
    }

    public Task<PigPen?> GetByIdAsync(Guid id)
    {
        var pigPen = _dataStore.PigPens.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(pigPen);
    }

    public Task<PigPen> CreateAsync(PigPen pigPen)
    {
        _dataStore.PigPens.Add(pigPen);
        return Task.FromResult(pigPen);
    }

    public Task<PigPen> UpdateAsync(PigPen pigPen)
    {
        var existingPigPen = _dataStore.PigPens.FirstOrDefault(p => p.Id == pigPen.Id);
        if (existingPigPen != null)
        {
            _dataStore.PigPens.Remove(existingPigPen);
        }
        _dataStore.PigPens.Add(pigPen);
        return Task.FromResult(pigPen);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var pigPen = _dataStore.PigPens.FirstOrDefault(p => p.Id == id);
        if (pigPen != null)
        {
            _dataStore.PigPens.Remove(pigPen);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<List<PigPen>> GetByCustomerIdAsync(Guid customerId)
    {
        var pigPens = _dataStore.PigPens.Where(p => p.CustomerId == customerId).ToList();
        return Task.FromResult(pigPens);
    }
}
