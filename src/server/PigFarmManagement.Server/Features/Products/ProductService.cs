using PigFarmManagement.Shared.Models;
using PigFarmManagement.Server.Services.ExternalServices;
using PigFarmManagement.Server.Features.FeedFormulas;

namespace PigFarmManagement.Server.Features.Products;

/// <summary>
/// Service for product-related operations
/// Handles product search and import functionality from external POSPOS system
/// </summary>
public class ProductService : IProductService
{
    private readonly IPosposProductClient _posposClient;
    private readonly IFeedFormulaService _feedFormulaService;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IPosposProductClient posposClient,
        IFeedFormulaService feedFormulaService,
        ILogger<ProductService> logger)
    {
        _posposClient = posposClient;
        _feedFormulaService = feedFormulaService;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(ProductSearchDto searchDto)
    {
        if (string.IsNullOrWhiteSpace(searchDto.Query))
        {
            throw new ArgumentException("Search query cannot be empty", nameof(searchDto));
        }

        _logger.LogInformation("Searching products with query: {Query}", searchDto.Query);

        try
        {
            // Fetch all products from POSPOS
            var allProducts = await _posposClient.GetAllProductsAsync();
            
            // Search for partial matches in both code and name
            var results = allProducts
                .Where(p => 
                    (p.Code?.Contains(searchDto.Query, StringComparison.OrdinalIgnoreCase) == true) ||
                    (p.Name?.Contains(searchDto.Query, StringComparison.OrdinalIgnoreCase) == true))
                .Select(MapPosposProductToDto)
                .ToList();
                
            _logger.LogInformation("Search for '{Query}' returned {Count} results", searchDto.Query, results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with query: {Query}", searchDto.Query);
            throw;
        }
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

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        _logger.LogInformation("Fetching all products from POSPOS");

        try
        {
            var posposProducts = await _posposClient.GetAllProductsAsync();
            var products = posposProducts.Select(MapPosposProductToDto).ToList();
            
            _logger.LogInformation("Retrieved {Count} products from POSPOS", products.Count);
            
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all products from POSPOS");
            throw;
        }
    }

    public async Task<ProductDto?> GetProductByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Product code cannot be empty", nameof(code));
        }

        _logger.LogInformation("Fetching product by code: {Code}", code);

        try
        {
            var posposProduct = await _posposClient.GetProductByCodeAsync(code);
            
            if (posposProduct == null)
            {
                _logger.LogWarning("Product with code {Code} not found", code);
                return null;
            }

            var product = MapPosposProductToDto(posposProduct);
            _logger.LogInformation("Found product: {Code} - {Name}", product.Code, product.Name);
            
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product by code: {Code}", code);
            throw;
        }
    }

    /// <summary>
    /// Maps a PosposProductDto to ProductDto
    /// </summary>
    private static ProductDto MapPosposProductToDto(PosposProductDto posposProduct)
    {
        return new ProductDto
        {
            Id = posposProduct.Id,
            Code = posposProduct.Code,
            Name = posposProduct.Name,
            Cost = posposProduct.Cost,
            IsActive = true, // Assume all POSPOS products are active
            LastUpdated = DateTime.UtcNow
        };
    }
}