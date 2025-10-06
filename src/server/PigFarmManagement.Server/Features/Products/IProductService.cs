using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.Products;

/// <summary>
/// Interface for product-related operations
/// Handles product search and import functionality from external POSPOS system
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Search for products by code or name
    /// </summary>
    /// <param name="searchDto">Search criteria</param>
    /// <returns>List of matching products</returns>
    Task<IEnumerable<ProductDto>> SearchProductsAsync(ProductSearchDto searchDto);
    
    /// <summary>
    /// Import selected products from external POSPOS system
    /// </summary>
    /// <param name="importDto">Import request with product IDs</param>
    /// <returns>Import result with summary and details</returns>
    Task<ProductImportResultDto> ImportProductsAsync(ProductImportDto importDto);
    
    /// <summary>
    /// Get all available products from external POSPOS system
    /// </summary>
    /// <returns>List of all products</returns>
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();
    
    /// <summary>
    /// Get a specific product by code from external POSPOS system
    /// </summary>
    /// <param name="code">Product code</param>
    /// <returns>Product if found, null otherwise</returns>
    Task<ProductDto?> GetProductByCodeAsync(string code);
}