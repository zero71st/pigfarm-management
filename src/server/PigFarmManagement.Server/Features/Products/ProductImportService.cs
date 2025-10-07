using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Features.FeedFormulas;

namespace PigFarmManagement.Server.Features.Products;

/// <summary>
/// Service for product import operations from external POSPOS system
/// Handles import functionality separated from basic product operations
/// </summary>
public class ProductImportService : IProductImportService
{
    private readonly IFeedFormulaService _feedFormulaService;
    private readonly ILogger<ProductImportService> _logger;

    public ProductImportService(
        IFeedFormulaService feedFormulaService,
        ILogger<ProductImportService> logger)
    {
        _feedFormulaService = feedFormulaService;
        _logger = logger;
    }

    public async Task<ProductImportResultDto> ImportProductsAsync(ProductImportDto importDto)
    {
        if (importDto.ProductIds == null || !importDto.ProductIds.Any())
        {
            throw new ArgumentException("ProductIds cannot be empty", nameof(importDto));
        }

        _logger.LogInformation("Starting import of {Count} products", importDto.ProductIds.Count);

        var startTime = DateTime.UtcNow;
        var result = new ProductImportResultDto
        {
            Summary = new ProductImportSummaryDto
            {
                TotalRequested = importDto.ProductIds.Count,
                ImportStarted = startTime
            }
        };

        try
        {
            // Convert to legacy ImportRequest format for FeedFormulaService compatibility
            var legacyRequest = new ImportRequest
            {
                ProductIds = importDto.ProductIds
            };

            // Use existing FeedFormulaService import functionality
            var legacyResult = await _feedFormulaService.ImportProductsByIdsAsync(legacyRequest);
            
            // Map legacy result to new DTO format
            result.Summary.Created = legacyResult.Summary.Created;
            result.Summary.Updated = legacyResult.Summary.Updated;
            result.Summary.Failed = legacyResult.Summary.Failed;
            result.Summary.ImportCompleted = DateTime.UtcNow;

            // Map legacy items to new DTO format
            result.Items = legacyResult.Items.Select(item => new ProductImportItemDto
            {
                ProductId = item.ProductId,
                Status = item.Status,
                Message = item.Message,
                ProcessedAt = DateTime.UtcNow
            }).ToList();

            _logger.LogInformation(
                "Product import completed: {Created} created, {Updated} updated, {Failed} failed", 
                result.Summary.Created, result.Summary.Updated, result.Summary.Failed);

            return result;
        }
        catch (Exception ex)
        {
            result.Summary.ImportCompleted = DateTime.UtcNow;
            result.Summary.Failed = importDto.ProductIds.Count;
            result.Errors.Add($"Import failed: {ex.Message}");
            
            _logger.LogError(ex, "Error importing {Count} products", importDto.ProductIds.Count);
            throw;
        }
    }
}