using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.Products;

/// <summary>
/// Interface for product import operations from external POSPOS system
/// Handles import functionality separated from basic product operations
/// </summary>
public interface IProductImportService
{
    /// <summary>
    /// Import selected products from external POSPOS system
    /// </summary>
    /// <param name="importDto">Import request with product IDs</param>
    /// <returns>Import result with summary and details</returns>
    Task<ProductImportResultDto> ImportProductsAsync(ProductImportDto importDto);
}