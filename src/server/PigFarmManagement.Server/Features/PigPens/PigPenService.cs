using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.PigPens;

public record PigPenCreateDto(Guid CustomerId, string PenCode, int PigQty, DateTime StartDate, DateTime? EndDate, DateTime? EstimatedHarvestDate);

public interface IPigPenService
{
    Task<List<PigPen>> GetAllPigPensAsync();
    Task<PigPen?> GetPigPenByIdAsync(Guid id);
    Task<PigPen> CreatePigPenAsync(PigPenCreateDto dto);
    Task<PigPen> UpdatePigPenAsync(PigPen pigPen);
    Task<bool> DeletePigPenAsync(Guid id);
    Task<List<PigPen>> GetPigPensByCustomerIdAsync(Guid customerId);
}

public class PigPenService : IPigPenService
{
    private readonly IPigPenRepository _pigPenRepository;

    public PigPenService(IPigPenRepository pigPenRepository)
    {
        _pigPenRepository = pigPenRepository;
    }

    public async Task<List<PigPen>> GetAllPigPensAsync()
    {
        return await _pigPenRepository.GetAllAsync();
    }

    public async Task<PigPen?> GetPigPenByIdAsync(Guid id)
    {
        return await _pigPenRepository.GetByIdAsync(id);
    }

    public async Task<PigPen> CreatePigPenAsync(PigPenCreateDto dto)
    {
        var id = Guid.NewGuid();
        var pigPen = new PigPen(
            id, 
            dto.CustomerId, 
            dto.PenCode, 
            dto.PigQty, 
            dto.StartDate, 
            dto.EndDate, 
            dto.EstimatedHarvestDate, 
            0, 0, 0); // Initial values for currentQty, harvested, deaths

        return await _pigPenRepository.CreateAsync(pigPen);
    }

    public async Task<PigPen> UpdatePigPenAsync(PigPen pigPen)
    {
        var existingPigPen = await _pigPenRepository.GetByIdAsync(pigPen.Id);
        if (existingPigPen == null)
        {
            throw new InvalidOperationException("Pig pen not found");
        }

        return await _pigPenRepository.UpdateAsync(pigPen);
    }

    public async Task<bool> DeletePigPenAsync(Guid id)
    {
        var pigPen = await _pigPenRepository.GetByIdAsync(id);
        if (pigPen == null)
        {
            throw new InvalidOperationException("Pig pen not found");
        }

        return await _pigPenRepository.DeleteAsync(id);
    }

    public async Task<List<PigPen>> GetPigPensByCustomerIdAsync(Guid customerId)
    {
        return await _pigPenRepository.GetByCustomerIdAsync(customerId);
    }
}
