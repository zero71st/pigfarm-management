using System;

namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Request for creating a new feed item
/// </summary>
public record FeedCreateDto(
    string FeedType,
    decimal QuantityKg, 
    decimal PricePerKg,
    DateTime Date,
    string? Notes = null
);

/// <summary>
/// Request for updating an existing feed item
/// </summary>
public record FeedUpdateDto(
    string? FeedType = null,
    decimal? QuantityKg = null,
    decimal? PricePerKg = null, 
    DateTime? Date = null,
    string? Notes = null
);

/// <summary>
/// Feed item data transfer object
/// </summary>
public class FeedDto
{
    public Guid Id { get; set; }
    public Guid PigPenId { get; set; }
    public string FeedType { get; set; } = string.Empty;
    public decimal QuantityKg { get; set; }
    public decimal PricePerKg { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Legacy properties for backwards compatibility
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
}

/// <summary>
/// Feed import result data transfer object
/// </summary>
public class FeedImportResultDto
{
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int SkippedCount { get; set; }
    public int TotalProcessed { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> SuccessMessages { get; set; } = new();
    public List<string> SkippedItems { get; set; } = new();
    public DateTime ImportDate { get; set; }
    public string ImportSource { get; set; } = string.Empty;
}