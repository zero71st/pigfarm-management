using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Infrastructure.Data;
using PigFarmManagement.Server.Infrastructure.Data.Entities;
using PigFarmManagement.Server.Services.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace PigFarmManagement.Server.Features.FeedFormulas;

// DTOs for FeedFormula CRUD operations
public record FeedFormulaCreateDto(string Code, string Name, string CategoryName, string Brand, decimal ConsumeRate, decimal Cost, string UnitName);
public record FeedFormulaUpdateDto(string Code, string Name, string CategoryName, string Brand, decimal ConsumeRate, decimal Cost, string UnitName);

// DTO for import results
public record ImportResult(
    int SuccessCount, 
    int ErrorCount, 
    int SkippedCount, 
    List<string> Errors,
    List<string> ImportedCodes);

public interface IFeedFormulaService
{
    Task<IEnumerable<FeedFormula>> GetAllFeedFormulasAsync();
    Task<FeedFormula?> GetFeedFormulaByIdAsync(Guid id);
    Task<FeedFormula> CreateFeedFormulaAsync(FeedFormulaCreateDto dto);
    Task<FeedFormula> UpdateFeedFormulaAsync(Guid id, FeedFormulaUpdateDto dto);
    Task<bool> DeleteFeedFormulaAsync(Guid id);
    Task<bool> ExistsAsync(string code);
    Task<ImportResult> ImportProductsFromPosposAsync();
    Task<IEnumerable<PigFarmManagement.Server.Services.ExternalServices.PosposProductDto>> GetPosposProductsAsync();
    Task<ImportResult> ImportSelectedProductsFromPosposAsync(IEnumerable<string> productCodes);
}

public class FeedFormulaService : IFeedFormulaService
{
    private readonly PigFarmDbContext _context;
    private readonly IPosposProductClient _posposProductClient;
    private readonly ILogger<FeedFormulaService> _logger;

    public FeedFormulaService(
        PigFarmDbContext context, 
        IPosposProductClient posposProductClient,
        ILogger<FeedFormulaService> logger)
    {
        _context = context;
        _posposProductClient = posposProductClient;
        _logger = logger;
    }

    public async Task<IEnumerable<FeedFormula>> GetAllFeedFormulasAsync()
    {
        var entities = await _context.FeedFormulas
            .OrderBy(f => f.Name)
            .ToListAsync();
        
        return entities.Select(e => e.ToModel());
    }

    public async Task<FeedFormula?> GetFeedFormulaByIdAsync(Guid id)
    {
        var entity = await _context.FeedFormulas.FindAsync(id);
        return entity?.ToModel();
    }

    public async Task<FeedFormula> CreateFeedFormulaAsync(FeedFormulaCreateDto dto)
    {
        var now = DateTime.UtcNow;
        var entity = new FeedFormulaEntity
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Name = dto.Name,
            CategoryName = dto.CategoryName,
            Brand = dto.Brand,
            ConsumeRate = dto.ConsumeRate,
            Cost = dto.Cost,
            UnitName = dto.UnitName,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.FeedFormulas.Add(entity);
        await _context.SaveChangesAsync();

        return entity.ToModel();
    }

    public async Task<FeedFormula> UpdateFeedFormulaAsync(Guid id, FeedFormulaUpdateDto dto)
    {
        var entity = await _context.FeedFormulas.FindAsync(id);
        if (entity == null)
            throw new InvalidOperationException($"Feed formula with ID {id} not found");

        entity.Code = dto.Code;
        entity.Name = dto.Name;
        entity.CategoryName = dto.CategoryName;
        entity.Brand = dto.Brand;
        entity.ConsumeRate = dto.ConsumeRate;
        entity.Cost = dto.Cost;
        entity.UnitName = dto.UnitName;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<bool> DeleteFeedFormulaAsync(Guid id)
    {
        var entity = await _context.FeedFormulas.FindAsync(id);
        if (entity == null)
            return false;

        _context.FeedFormulas.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string code)
    {
        return await _context.FeedFormulas
            .AnyAsync(f => f.Code == code);
    }
    public async Task<ImportResult> ImportProductsFromPosposAsync()
    {
        _logger.LogInformation("Starting POSPOS product import");

        var errors = new List<string>();
        var importedCodes = new List<string>();
        int successCount = 0;
        int errorCount = 0;
        int skippedCount = 0;

        try
        {
            // Fetch all products from POSPOS
            var posposProducts = await _posposProductClient.GetAllProductsAsync();
            _logger.LogInformation("Fetched {Count} products from POSPOS", posposProducts.Count);

            if (posposProducts.Count == 0)
            {
                _logger.LogWarning("No products returned from POSPOS API");
                return new ImportResult(0, 0, 0, new List<string> { "No products returned from POSPOS API" }, new List<string>());
            }

            // Get existing products to check for duplicates
            var existingCodes = await _context.FeedFormulas
                .Where(f => f.Code != null)
                .Select(f => f.Code!)
                .ToListAsync();

            var existingCodesSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

            foreach (var product in posposProducts)
            {
                try
                {
                    // Validate product has required fields
                    if (string.IsNullOrWhiteSpace(product.Code))
                    {
                        errorCount++;
                        errors.Add($"Product {product.Id} has no code, skipping");
                        continue;
                    }

                    // Check for duplicates by Code
                    if (existingCodesSet.Contains(product.Code))
                    {
                        skippedCount++;
                        _logger.LogDebug("Product {Code} already exists, skipping", product.Code);
                        continue;
                    }

                    // Parse POSPOS _id (MongoDB ObjectId) to Guid
                    Guid? externalId = null;
                    if (!string.IsNullOrWhiteSpace(product.Id))
                    {
                        // MongoDB ObjectId is 24 hex characters, we'll use it as a string representation
                        // For now, generate a deterministic Guid from the ObjectId string
                        externalId = GenerateGuidFromObjectId(product.Id);
                    }

                    // Transform POSPOS product to FeedFormula entity
                    var now = DateTime.UtcNow;
                    var entity = new FeedFormulaEntity
                    {
                        Id = Guid.NewGuid(),
                        ExternalId = externalId,
                        Code = product.Code,
                        Name = product.Name,
                        Cost = product.Cost,
                        CategoryName = product.Category?.Name,
                        Brand = null, // User will set this manually later
                        UnitName = product.Unit?.Name,
                        LastUpdate = product.LastUpdate,
                        ConsumeRate = null, // User input field, not from POSPOS
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    _context.FeedFormulas.Add(entity);
                    existingCodesSet.Add(product.Code); // Add to set to avoid duplicates in same batch
                    importedCodes.Add(product.Code);
                    successCount++;

                    _logger.LogDebug("Imported product {Code}: {Name}", product.Code, product.Name);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    var errorMsg = $"Error importing product {product.Code}: {ex.Message}";
                    errors.Add(errorMsg);
                    _logger.LogError(ex, "Error importing product {Code}", product.Code);
                }
            }

            // Save all changes in a single transaction
            if (successCount > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "POSPOS import completed: {Success} imported, {Skipped} skipped, {Errors} errors", 
                    successCount, skippedCount, errorCount);
            }

            return new ImportResult(successCount, errorCount, skippedCount, errors, importedCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during POSPOS product import");
            errors.Add($"Fatal error: {ex.Message}");
            return new ImportResult(successCount, errorCount + 1, skippedCount, errors, importedCodes);
        }
    }

    /// <summary>
    /// Generates a deterministic Guid from a MongoDB ObjectId string.
    /// Uses MD5 hash of the ObjectId to create a Guid.
    /// </summary>
    private static Guid GenerateGuidFromObjectId(string objectId)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(objectId));
        return new Guid(hash);
    }

    public async Task<IEnumerable<PigFarmManagement.Server.Services.ExternalServices.PosposProductDto>> GetPosposProductsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching POSPOS products for selection");
            var products = await _posposProductClient.GetAllProductsAsync();
            _logger.LogInformation("Fetched {Count} products from POSPOS", products.Count);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching POSPOS products");
            throw;
        }
    }

    public async Task<ImportResult> ImportSelectedProductsFromPosposAsync(IEnumerable<string> productCodes)
    {
        _logger.LogInformation("Starting selective POSPOS product import for {Count} products", productCodes.Count());

        var errors = new List<string>();
        var importedCodes = new List<string>();
        int successCount = 0;
        int errorCount = 0;
        int skippedCount = 0;

        var productCodesSet = new HashSet<string>(productCodes, StringComparer.OrdinalIgnoreCase);

        try
        {
            // Fetch all products from POSPOS and filter by selected codes
            var allPosposProducts = await _posposProductClient.GetAllProductsAsync();
            var selectedProducts = allPosposProducts
                .Where(p => !string.IsNullOrWhiteSpace(p.Code) && productCodesSet.Contains(p.Code))
                .ToList();

            _logger.LogInformation("Found {Count} matching products from POSPOS", selectedProducts.Count);

            if (selectedProducts.Count == 0)
            {
                _logger.LogWarning("No matching products found for the selected codes");
                return new ImportResult(0, 0, 0, new List<string> { "No matching products found for the selected codes" }, new List<string>());
            }

            // Get existing products to check for duplicates
            var existingCodes = await _context.FeedFormulas
                .Where(f => f.Code != null)
                .Select(f => f.Code!)
                .ToListAsync();

            var existingCodesSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

            foreach (var product in selectedProducts)
            {
                try
                {
                    // Validate product has required fields
                    if (string.IsNullOrWhiteSpace(product.Code))
                    {
                        errorCount++;
                        errors.Add($"Product {product.Id} has no code, skipping");
                        continue;
                    }

                    // Check for duplicates by Code
                    if (existingCodesSet.Contains(product.Code))
                    {
                        skippedCount++;
                        _logger.LogDebug("Product {Code} already exists, skipping", product.Code);
                        continue;
                    }

                    // Parse POSPOS _id (MongoDB ObjectId) to Guid
                    Guid? externalId = null;
                    if (!string.IsNullOrWhiteSpace(product.Id))
                    {
                        // MongoDB ObjectId is 24 hex characters, we'll use it as a string representation
                        // For now, generate a deterministic Guid from the ObjectId string
                        externalId = GenerateGuidFromObjectId(product.Id);
                    }

                    // Transform POSPOS product to FeedFormula entity
                    var now = DateTime.UtcNow;
                    var entity = new FeedFormulaEntity
                    {
                        Id = Guid.NewGuid(),
                        ExternalId = externalId,
                        Code = product.Code,
                        Name = product.Name,
                        Cost = product.Cost,
                        CategoryName = product.Category?.Name,
                        Brand = null, // User will set this manually later
                        UnitName = product.Unit?.Name,
                        LastUpdate = product.LastUpdate,
                        ConsumeRate = null, // User input field, not from POSPOS
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    _context.FeedFormulas.Add(entity);
                    existingCodesSet.Add(product.Code); // Add to set to avoid duplicates in same batch
                    importedCodes.Add(product.Code);
                    successCount++;

                    _logger.LogDebug("Imported selected product {Code}: {Name}", product.Code, product.Name);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    var errorMsg = $"Error importing product {product.Code}: {ex.Message}";
                    errors.Add(errorMsg);
                    _logger.LogError(ex, "Error importing product {Code}", product.Code);
                }
            }

            // Save all changes in a single transaction
            if (successCount > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Selective POSPOS import completed: {Success} imported, {Skipped} skipped, {Errors} errors", 
                    successCount, skippedCount, errorCount);
            }

            return new ImportResult(successCount, errorCount, skippedCount, errors, importedCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during selective POSPOS product import");
            errors.Add($"Fatal error: {ex.Message}");
            return new ImportResult(successCount, errorCount + 1, skippedCount, errors, importedCodes);
        }
    }

}
