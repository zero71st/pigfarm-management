using PigFarmManagement.Shared.Models;
using System.Linq;

namespace PigFarmManagement.Server.Features.PigPens;

public record PigPenCreateDto(Guid CustomerId, string PenCode, int PigQty, DateTime RegisterDate, DateTime? ActHarvestDate, DateTime? EstimatedHarvestDate, PigPenType Type, Guid? FeedFormulaId, string? SelectedBrand, decimal DepositPerPig = 1500m);

public interface IPigPenService
{
    Task<List<PigPen>> GetAllPigPensAsync();
    Task<PigPen?> GetPigPenByIdAsync(Guid id);
    Task<PigPen> CreatePigPenAsync(PigPenCreateDto dto);
    Task<PigPen> UpdatePigPenAsync(PigPen pigPen);
    Task<bool> DeletePigPenAsync(Guid id);
    Task<List<PigPen>> GetPigPensByCustomerIdAsync(Guid customerId);
    Task<PigPen> ForceClosePigPenAsync(Guid id);
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
        var pigPens = await _pigPenRepository.GetAllAsync();
        return pigPens.OrderByDescending(p => p.UpdatedAt).ToList();
    }

    public async Task<PigPen?> GetPigPenByIdAsync(Guid id)
    {
        return await _pigPenRepository.GetByIdAsync(id);
    }

    public async Task<PigPen> CreatePigPenAsync(PigPenCreateDto dto)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var pigPen = new PigPen(
            id, 
            dto.CustomerId, 
            dto.PenCode, 
            dto.PigQty, 
            dto.RegisterDate, 
            dto.ActHarvestDate, 
            dto.EstimatedHarvestDate, 
            0, 0, 0, // Initial values for FeedCost, Investment, ProfitLoss
            dto.Type, // PigPenType
            dto.FeedFormulaId, // FeedFormulaId
            dto.DepositPerPig, // DepositPerPig from DTO
            now, // CreatedAt
            now) // UpdatedAt
        {
            SelectedBrand = dto.SelectedBrand // Store the selected brand
        };

        return await _pigPenRepository.CreateAsync(pigPen);
    }

    public async Task<PigPen> UpdatePigPenAsync(PigPen pigPen)
    {
        var existingPigPen = await _pigPenRepository.GetByIdAsync(pigPen.Id);
        if (existingPigPen == null)
        {
            throw new InvalidOperationException("Pig pen not found");
        }

        // Update the UpdatedAt timestamp while preserving CreatedAt
        var updatedPigPen = pigPen with { 
            CreatedAt = existingPigPen.CreatedAt, 
            UpdatedAt = DateTime.UtcNow 
        };

        return await _pigPenRepository.UpdateAsync(updatedPigPen);
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

    public async Task<PigPen> ForceClosePigPenAsync(Guid id)
    {
        var pigPen = await _pigPenRepository.GetByIdAsync(id);
        if (pigPen == null)
        {
            throw new InvalidOperationException("Pig pen not found");
        }

        // Force close by setting actual harvest date to today
        var forceClosedPigPen = pigPen with 
        { 
            ActHarvestDate = DateTime.Today,
            UpdatedAt = DateTime.UtcNow
        };

        return await _pigPenRepository.UpdateAsync(forceClosedPigPen);
    }
}
