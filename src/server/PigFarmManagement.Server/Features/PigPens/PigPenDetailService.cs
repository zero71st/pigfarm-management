using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;

namespace PigFarmManagement.Server.Features.PigPens;

public record DepositCreateDto(decimal Amount, DateTime Date, string? Remark);
public record HarvestCreateDto(DateTime HarvestDate, int PigCount, decimal AvgWeight, decimal TotalWeight, decimal SalePricePerKg, decimal Revenue);

public interface IPigPenDetailService
{
    Task<PigPenSummary?> GetPigPenSummaryAsync(Guid pigPenId);
    Task<List<FeedItem>> GetPigPenFeedsAsync(Guid pigPenId);
    Task<List<Deposit>> GetPigPenDepositsAsync(Guid pigPenId);
    Task<List<HarvestResult>> GetPigPenHarvestsAsync(Guid pigPenId);
    
    // Deposit CRUD
    Task<Deposit> CreateDepositAsync(Guid pigPenId, DepositCreateDto dto);
    Task<Deposit> UpdateDepositAsync(Deposit deposit);
    Task DeleteDepositAsync(Guid depositId);
    
    // Harvest CRUD
    Task<HarvestResult> CreateHarvestAsync(Guid pigPenId, HarvestCreateDto dto);
    Task<HarvestResult> UpdateHarvestAsync(HarvestResult harvest);
    Task DeleteHarvestAsync(Guid harvestId);
}

public class PigPenDetailService : IPigPenDetailService
{
    private readonly IPigPenRepository _pigPenRepository;
    private readonly IFeedRepository _feedRepository;
    private readonly IDepositRepository _depositRepository;
    private readonly IHarvestRepository _harvestRepository;

    public PigPenDetailService(
        IPigPenRepository pigPenRepository,
        IFeedRepository feedRepository, 
        IDepositRepository depositRepository,
        IHarvestRepository harvestRepository)
    {
        _pigPenRepository = pigPenRepository;
        _feedRepository = feedRepository;
        _depositRepository = depositRepository;
        _harvestRepository = harvestRepository;
    }

    public async Task<PigPenSummary?> GetPigPenSummaryAsync(Guid pigPenId)
    {
        var pigPen = await _pigPenRepository.GetByIdAsync(pigPenId);
        if (pigPen == null) return null;

        var feeds = await _feedRepository.GetByPigPenIdAsync(pigPenId);
        var deposits = await _depositRepository.GetByPigPenIdAsync(pigPenId);
        var harvests = await _harvestRepository.GetByPigPenIdAsync(pigPenId);

        var totalFeedCost = feeds.Sum(f => f.TotalPrice);
        var totalDeposit = deposits.Sum(d => d.Amount);
        var totalRevenue = harvests.Sum(h => h.Revenue);
        var investment = pigPen.Investment;
        var profitLoss = totalRevenue - totalFeedCost - investment;
        var netBalance = totalDeposit - totalFeedCost;

        return new PigPenSummary(
            pigPenId,
            totalFeedCost,
            totalDeposit,
            investment,
            profitLoss,
            netBalance
        );
    }

    public async Task<List<FeedItem>> GetPigPenFeedsAsync(Guid pigPenId)
    {
        var feeds = await _feedRepository.GetByPigPenIdAsync(pigPenId);
        return feeds.Select(f => new FeedItem(
            f.Id, 
            f.PigPenId, 
            f.ProductName, // Use ProductName as FeedType to show actual product name
            f.ProductCode, // Pass the product code
            f.ProductName, // Pass the product name
            f.TransactionCode, // Pass the transaction code
            f.InvoiceReferenceCode, // Pass the invoice reference code
            f.Quantity * 25m, // Convert bags to kg (assuming 25kg per bag)
            f.UnitPrice / 25m, // Convert price per bag to price per kg
            f.TotalPrice, // Use TotalPrice (total cost of the bags)
            f.FeedDate
        )
        {
            ExternalReference = f.ExternalReference,
            Notes = f.Notes,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt
        }).ToList();
    }

    public async Task<List<Deposit>> GetPigPenDepositsAsync(Guid pigPenId)
    {
        var deposits = await _depositRepository.GetByPigPenIdAsync(pigPenId);
        return deposits.ToList();
    }

    public async Task<List<HarvestResult>> GetPigPenHarvestsAsync(Guid pigPenId)
    {
        var harvests = await _harvestRepository.GetByPigPenIdAsync(pigPenId);
        return harvests.ToList();
    }

    // Deposit CRUD
    public async Task<Deposit> CreateDepositAsync(Guid pigPenId, DepositCreateDto dto)
    {
        var pigPen = await _pigPenRepository.GetByIdAsync(pigPenId);
        if (pigPen == null)
        {
            throw new InvalidOperationException("Pig pen not found");
        }

        var deposit = new Deposit(
            Guid.NewGuid(),
            pigPenId,
            dto.Amount,
            dto.Date,
            dto.Remark
        );

        return await _depositRepository.CreateAsync(deposit);
    }

    public async Task<Deposit> UpdateDepositAsync(Deposit deposit)
    {
        var existingDeposit = await _depositRepository.GetByIdAsync(deposit.Id);
        if (existingDeposit == null)
        {
            throw new InvalidOperationException("Deposit not found");
        }

        return await _depositRepository.UpdateAsync(deposit);
    }

    public async Task DeleteDepositAsync(Guid depositId)
    {
        var deposit = await _depositRepository.GetByIdAsync(depositId);
        if (deposit == null)
        {
            throw new InvalidOperationException("Deposit not found");
        }

        await _depositRepository.DeleteAsync(depositId);
    }

    // Harvest CRUD
    public async Task<HarvestResult> CreateHarvestAsync(Guid pigPenId, HarvestCreateDto dto)
    {
        var pigPen = await _pigPenRepository.GetByIdAsync(pigPenId);
        if (pigPen == null)
        {
            throw new InvalidOperationException("Pig pen not found");
        }

        var harvest = new HarvestResult(
            Guid.NewGuid(),
            pigPenId,
            dto.HarvestDate,
            dto.PigCount,
            dto.AvgWeight,
            dto.TotalWeight,
            dto.SalePricePerKg,
            dto.Revenue
        );

        return await _harvestRepository.CreateAsync(harvest);
    }

    public async Task<HarvestResult> UpdateHarvestAsync(HarvestResult harvest)
    {
        var existingHarvest = await _harvestRepository.GetByIdAsync(harvest.Id);
        if (existingHarvest == null)
        {
            throw new InvalidOperationException("Harvest not found");
        }

        return await _harvestRepository.UpdateAsync(harvest);
    }

    public async Task DeleteHarvestAsync(Guid harvestId)
    {
        var harvest = await _harvestRepository.GetByIdAsync(harvestId);
        if (harvest == null)
        {
            throw new InvalidOperationException("Harvest not found");
        }

        await _harvestRepository.DeleteAsync(harvestId);
    }
}
