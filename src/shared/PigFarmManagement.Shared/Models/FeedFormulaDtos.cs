namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Request for creating a new feed formula
/// </summary>
public record FeedFormulaCreateDto(
    string Code,
    string Name,
    string CategoryName,
    string Brand,
    decimal ConsumeRate,
    decimal Cost,
    string UnitName
);

/// <summary>
/// Request for updating an existing feed formula
/// </summary>
public record FeedFormulaUpdateDto(
    string Code,
    string Name,
    string CategoryName,
    string Brand,
    decimal ConsumeRate,
    decimal Cost,
    string UnitName
);

/// <summary>
/// Request for calculating feed requirements
/// </summary>
public record FeedCalculationRequest(
    Guid FeedFormulaId,
    int PigCount,
    decimal? BagPrice = null
);

/// <summary>
/// Feed formula data transfer object
/// </summary>
public class FeedFormulaDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? CategoryName { get; set; }
    public string? Brand { get; set; }
    public decimal ConsumeRate { get; set; }
    public decimal Cost { get; set; }
    public string? UnitName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ConsumptionRate { get; set; } = string.Empty;
    
    // Legacy properties for backwards compatibility
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal BagPerPig { get; set; }
    public string BrandDisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Feed formula with calculation data
/// </summary>
public class FeedFormulaWithCalculationDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal ConsumeRate { get; set; }
    public decimal Cost { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public decimal TotalBagsRequired { get; set; }
    public int PigCount { get; set; }
    
    // Legacy properties for backwards compatibility
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal BagPerPig { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ConsumptionRate { get; set; } = string.Empty;
    public string BrandDisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Feed calculation result data
/// </summary>
public class FeedCalculationDto
{
    public Guid FeedFormulaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int PigCount { get; set; }
    public decimal ConsumeRate { get; set; }
    public decimal TotalBagsRequired { get; set; }
    public decimal? BagPrice { get; set; }
    public decimal? TotalCost { get; set; }
    public DateTime CalculationDate { get; set; }
    
    // Legacy properties for backwards compatibility
    public string ProductName => Name ?? string.Empty;
    public decimal BagPerPig => ConsumeRate;
    public string Brand { get; set; } = string.Empty;
}

/// <summary>
/// Import operation result
/// </summary>
public class ImportResultDto
{
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> ImportedCodes { get; set; } = new();
}