using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;

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
    private readonly Infrastructure.Data.Repositories.IPigPenRepository _efPigPenRepository;

    public PigPenRepository(Infrastructure.Data.Repositories.IPigPenRepository efPigPenRepository)
    {
        _efPigPenRepository = efPigPenRepository;
    }

    public async Task<List<PigPen>> GetAllAsync()
    {
        var pigPens = await _efPigPenRepository.GetAllAsync();
        return pigPens.ToList();
    }

    public async Task<PigPen?> GetByIdAsync(Guid id)
    {
        return await _efPigPenRepository.GetByIdAsync(id);
    }

    public async Task<PigPen> CreateAsync(PigPen pigPen)
    {
        return await _efPigPenRepository.CreateAsync(pigPen);
    }

    public async Task<PigPen> UpdateAsync(PigPen pigPen)
    {
        return await _efPigPenRepository.UpdateAsync(pigPen);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _efPigPenRepository.DeleteAsync(id);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<PigPen>> GetByCustomerIdAsync(Guid customerId)
    {
        var pigPens = await _efPigPenRepository.GetByCustomerIdAsync(customerId);
        return pigPens.ToList();
    }
}
