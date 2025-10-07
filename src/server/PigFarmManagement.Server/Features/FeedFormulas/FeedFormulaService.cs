using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Infrastructure.Data;
using PigFarmManagement.Server.Infrastructure.Data.Entities;
using PigFarmManagement.Server.Services.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PigFarmManagement.Server.Services;

namespace PigFarmManagement.Server.Features.FeedFormulas;

public interface IFeedFormulaService
{
    Task<IEnumerable<FeedFormula>> GetAllFeedFormulasAsync();
    Task<FeedFormula?> GetFeedFormulaByIdAsync(Guid id);
    Task<FeedFormula> CreateFeedFormulaAsync(FeedFormulaCreateDto dto);
    Task<FeedFormula> UpdateFeedFormulaAsync(Guid id, FeedFormulaUpdateDto dto);
    Task<bool> DeleteFeedFormulaAsync(Guid id);
    Task<bool> ExistsAsync(string code);
    Task<ImportResultDto> ImportProductsFromPosposAsync();
    Task<IEnumerable<PosposProductDto>> GetPosposProductsAsync();
    Task<ImportResultDto> ImportSelectedProductsFromPosposAsync(IEnumerable<string> productCodes);
    Task<PigFarmManagement.Shared.Models.ImportResult> ImportProductsByIdsAsync(ImportRequest request);
    // New DTO-returning methods for formula system operations
    Task<PigFarmManagement.Shared.Models.FormulaSystemValidationDto> ValidateFormulaSystemAsync();
    Task<PigFarmManagement.Shared.Models.FormulaSystemRepairDto> RepairFormulaSystemAsync();
    Task<PigFarmManagement.Shared.Models.FormulaSystemStatsDto> GetFormulaSystemStatsAsync();
}

public class FeedFormulaService : IFeedFormulaService
{
    private readonly PigFarmDbContext _context;
    private readonly IPosposProductClient _posposProductClient;
    private readonly ILogger<FeedFormulaService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public FeedFormulaService(
        PigFarmDbContext context,
        IPosposProductClient posposProductClient,
        ILogger<FeedFormulaService> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _posposProductClient = posposProductClient;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    // removed old overload that accepted FormulaMigrationService to avoid circular DI

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
    public async Task<ImportResultDto> ImportProductsFromPosposAsync()
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
                return new ImportResultDto 
                { 
                    SuccessCount = 0, 
                    ErrorCount = 0, 
                    SkippedCount = 0, 
                    Errors = new List<string> { "No products returned from POSPOS API" }, 
                    ImportedCodes = new List<string>() 
                };
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

            return new ImportResultDto
            {
                SuccessCount = successCount,
                ErrorCount = errorCount,
                SkippedCount = skippedCount,
                Errors = errors,
                ImportedCodes = importedCodes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during POSPOS product import");
            errors.Add($"Fatal error: {ex.Message}");
            return new ImportResultDto
            {
                SuccessCount = successCount,
                ErrorCount = errorCount + 1,
                SkippedCount = skippedCount,
                Errors = errors,
                ImportedCodes = importedCodes
            };
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

    public async Task<IEnumerable<PosposProductDto>> GetPosposProductsAsync()
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

    public async Task<ImportResultDto> ImportSelectedProductsFromPosposAsync(IEnumerable<string> productCodes)
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
                return new ImportResultDto 
                { 
                    SuccessCount = 0, 
                    ErrorCount = 0, 
                    SkippedCount = 0, 
                    Errors = new List<string> { "No matching products found for the selected codes" }, 
                    ImportedCodes = new List<string>() 
                };
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

            return new ImportResultDto
            {
                SuccessCount = successCount,
                ErrorCount = errorCount,
                SkippedCount = skippedCount,
                Errors = errors,
                ImportedCodes = importedCodes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during selective POSPOS product import");
            errors.Add($"Fatal error: {ex.Message}");
            return new ImportResultDto
            {
                SuccessCount = successCount,
                ErrorCount = errorCount + 1,
                SkippedCount = skippedCount,
                Errors = errors,
                ImportedCodes = importedCodes
            };
        }
    }

    /// <summary>
    /// Import products by their POSPOS IDs with upsert behavior (FR-011).
    /// Updates existing FeedFormula records and creates new ones.
    /// </summary>
    /// <param name="request">Import request containing product IDs</param>
    /// <returns>Import result with per-item status</returns>
    public async Task<PigFarmManagement.Shared.Models.ImportResult> ImportProductsByIdsAsync(ImportRequest request)
    {
        _logger.LogInformation("Starting import of {Count} products by IDs", request.ProductIds.Count);

        var items = new List<ImportItemResult>();
        int created = 0, updated = 0, failed = 0;

        try
        {
            // Fetch all products from POSPOS to get their details
            var allPosposProducts = await _posposProductClient.GetAllProductsAsync();
            
            // Create a mapping from generated GUIDs back to POSPOS products
            var posposProductMap = new Dictionary<Guid, PosposProductDto>();
            foreach (var product in allPosposProducts)
            {
                if (!string.IsNullOrWhiteSpace(product.Id))
                {
                    var guid = GenerateGuidFromObjectId(product.Id);
                    posposProductMap[guid] = product;
                }
            }

            // Get existing FeedFormula records to check for updates
            var existingFormulas = await _context.FeedFormulas
                .Where(f => f.ExternalId.HasValue)
                .ToListAsync();
            
            var existingFormulaMap = existingFormulas
                .Where(f => f.ExternalId.HasValue)
                .ToDictionary(f => f.ExternalId!.Value, f => f);

            foreach (var productId in request.ProductIds)
            {
                try
                {
                    // Find the POSPOS product for this ID
                    if (!posposProductMap.TryGetValue(productId, out var posposProduct))
                    {
                        failed++;
                        items.Add(new ImportItemResult
                        {
                            ProductId = productId,
                            Status = "Failed",
                            Message = "Product not found in POSPOS"
                        });
                        continue;
                    }

                    // Validate product has required fields
                    if (string.IsNullOrWhiteSpace(posposProduct.Code))
                    {
                        failed++;
                        items.Add(new ImportItemResult
                        {
                            ProductId = productId,
                            Status = "Failed", 
                            Message = "Product has no code"
                        });
                        continue;
                    }

                    // Check if this product already exists
                    if (existingFormulaMap.TryGetValue(productId, out var existingFormula))
                    {
                        // Update existing record
                        var mappedDto = MapPosposProductToFeedFormula(posposProduct);
                        
                        existingFormula.Code = mappedDto.Code;
                        existingFormula.Name = mappedDto.Name;
                        existingFormula.Cost = mappedDto.Cost;
                        existingFormula.CategoryName = mappedDto.CategoryName;
                        existingFormula.UnitName = mappedDto.UnitName;
                        existingFormula.LastUpdate = posposProduct.LastUpdate;
                        existingFormula.UpdatedAt = DateTime.UtcNow;

                        updated++;
                        items.Add(new ImportItemResult
                        {
                            ProductId = productId,
                            Status = "Updated",
                            Message = $"Updated feed formula: {posposProduct.Code}"
                        });
                    }
                    else
                    {
                        // Create new record
                        var mappedDto = MapPosposProductToFeedFormula(posposProduct);
                        var now = DateTime.UtcNow;
                        
                        var entity = new FeedFormulaEntity
                        {
                            Id = Guid.NewGuid(),
                            ExternalId = productId,
                            Code = mappedDto.Code,
                            Name = mappedDto.Name,
                            Cost = mappedDto.Cost,
                            CategoryName = mappedDto.CategoryName,
                            Brand = mappedDto.Brand,
                            UnitName = mappedDto.UnitName,
                            ConsumeRate = mappedDto.ConsumeRate,
                            LastUpdate = posposProduct.LastUpdate,
                            CreatedAt = now,
                            UpdatedAt = now
                        };

                        _context.FeedFormulas.Add(entity);
                        created++;
                        items.Add(new ImportItemResult
                        {
                            ProductId = productId,
                            Status = "Created",
                            Message = $"Created feed formula: {posposProduct.Code}"
                        });
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    items.Add(new ImportItemResult
                    {
                        ProductId = productId,
                        Status = "Failed",
                        Message = $"Error: {ex.Message}"
                    });
                    _logger.LogError(ex, "Error importing product {ProductId}", productId);
                }
            }

            // Save all changes in a single transaction
            if (created > 0 || updated > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Product import completed: {Created} created, {Updated} updated, {Failed} failed", 
                    created, updated, failed);
            }

            return new PigFarmManagement.Shared.Models.ImportResult
            {
                Summary = new ImportSummary { Created = created, Updated = updated, Failed = failed },
                Items = items
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during product import");
            return new PigFarmManagement.Shared.Models.ImportResult
            {
                Summary = new ImportSummary { Created = created, Updated = updated, Failed = failed + 1 },
                Items = items.Concat(new[] { 
                    new ImportItemResult 
                    { 
                        ProductId = Guid.Empty, 
                        Status = "Failed", 
                        Message = $"Fatal error: {ex.Message}" 
                    } 
                }).ToList()
            };
        }
    }

    public async Task<PigFarmManagement.Shared.Models.FormulaSystemValidationDto> ValidateFormulaSystemAsync()
    {
        var migrationService = _serviceProvider.GetService<FormulaMigrationService>();
        if (migrationService == null)
            throw new InvalidOperationException("FormulaMigrationService is not available");

    var result = await migrationService.ValidateUnifiedSystemAsync();
        return new PigFarmManagement.Shared.Models.FormulaSystemValidationDto
        {
            IsValid = result.IsValid,
            ErrorMessage = result.ErrorMessage,
            TotalPigPens = result.TotalPigPens,
            PigPensWithAssignments = result.PigPensWithAssignments,
            LockedPigPens = result.LockedPigPens,
            LockedPigPensWithLockedAssignments = result.LockedPigPensWithLockedAssignments,
            ActivePigPens = result.ActivePigPens,
            ActivePigPensWithActiveAssignments = result.ActivePigPensWithActiveAssignments,
            ValidationMessages = result.ValidationMessages,
            ValidationTimestamp = DateTime.UtcNow
        };
    }

    public async Task<PigFarmManagement.Shared.Models.FormulaSystemRepairDto> RepairFormulaSystemAsync()
    {
        var migrationService = _serviceProvider.GetService<FormulaMigrationService>();
        if (migrationService == null)
            throw new InvalidOperationException("FormulaMigrationService is not available");

    var result = await migrationService.RepairSystemAsync();
        return new PigFarmManagement.Shared.Models.FormulaSystemRepairDto
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            RepairsPerformed = result.RepairsPerformed,
            RepairTimestamp = DateTime.UtcNow
        };
    }

    public async Task<PigFarmManagement.Shared.Models.FormulaSystemStatsDto> GetFormulaSystemStatsAsync()
    {
        var migrationService = _serviceProvider.GetService<FormulaMigrationService>();
        if (migrationService == null)
            throw new InvalidOperationException("FormulaMigrationService is not available");

    var stats = await migrationService.GetSystemStatisticsAsync();
        return new PigFarmManagement.Shared.Models.FormulaSystemStatsDto
        {
            TotalPigPens = stats.TotalPigPens,
            ActivePigPens = stats.ActivePigPens,
            ClosedPigPens = stats.LockedPigPens,
            TotalAssignments = stats.TotalFormulaAssignments,
            ActiveAssignments = stats.ActiveAssignments,
            LockedAssignments = stats.LockedAssignments,
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Maps a PosposProductDto to FeedFormulaCreateDto for import operations.
    /// Includes product code normalization, name mapping, cost handling, and unit/category mapping.
    /// </summary>
    /// <param name="posposProduct">The POSPOS product to map</param>
    /// <returns>FeedFormulaCreateDto ready for persistence</returns>
    private static FeedFormulaCreateDto MapPosposProductToFeedFormula(PosposProductDto posposProduct)
    {
        // Normalize product code: trim and convert to uppercase
        var normalizedCode = posposProduct.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        
        // Handle name mapping with fallback
        var name = posposProduct.Name?.Trim() ?? normalizedCode;
        
        // Map cost with fallback to 0 if null
        var cost = posposProduct.Cost ?? 0m;
        
        // Map category name with fallback
        var categoryName = posposProduct.Category?.Name?.Trim() ?? "Unknown";
        
        // Map unit name with fallback
        var unitName = posposProduct.Unit?.Name?.Trim() ?? "Unknown";
        
        // Use empty string for brand as POSPOS doesn't provide this field
        var brand = string.Empty;
        
        // Default consume rate for imported products
        var consumeRate = 1.0m;

        return new FeedFormulaCreateDto(
            Code: normalizedCode,
            Name: name,
            CategoryName: categoryName,
            Brand: brand,
            ConsumeRate: consumeRate,
            Cost: cost,
            UnitName: unitName
        );
    }

}
