using System.ComponentModel.DataAnnotations;

namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Product-related DTOs for product search and import functionality
/// </summary>

/// <summary>
/// DTO for product search request
/// </summary>
public class ProductSearchDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Search query cannot be empty")]
    public string Query { get; set; } = string.Empty;
}

/// <summary>
/// DTO for product search results
/// </summary>
public class ProductDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Cost { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateTime? LastUpdated { get; set; }
}

/// <summary>
/// DTO for product import request
/// </summary>
public class ProductImportDto
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one product ID must be provided")]
    public List<Guid> ProductIds { get; set; } = new();
    
    public bool OverwriteExisting { get; set; } = true;
    public string? ImportNotes { get; set; }
}

/// <summary>
/// DTO for individual product import result
/// </summary>
public class ProductImportItemDto
{
    public Guid? ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "Created", "Updated", "Failed", "Skipped"
    public string? Message { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for product import summary
/// </summary>
public class ProductImportSummaryDto
{
    public int TotalRequested { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public DateTime ImportStarted { get; set; }
    public DateTime ImportCompleted { get; set; }
    public TimeSpan Duration => ImportCompleted - ImportStarted;
    public decimal SuccessRate => TotalRequested > 0 ? (decimal)(Created + Updated) / TotalRequested * 100 : 0;
}

/// <summary>
/// DTO for complete product import result
/// </summary>
public class ProductImportResultDto
{
    public ProductImportSummaryDto Summary { get; set; } = new();
    public List<ProductImportItemDto> Items { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool IsSuccessful => Summary.Failed == 0 && Errors.Count == 0;
}