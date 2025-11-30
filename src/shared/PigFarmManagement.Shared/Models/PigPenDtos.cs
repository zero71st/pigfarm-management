using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Request for creating a new pig pen
/// </summary>
public record PigPenCreateDto(
    Guid CustomerId,
    string PenCode,
    int PigQty,
    DateTime RegisterDate,
    DateTime? ActHarvestDate,
    DateTime? EstimatedHarvestDate,
    PigPenType Type,
    string? SelectedBrand,
    decimal DepositPerPig = 1500m,
    string? Note = null
);

/// <summary>
/// Request for updating an existing pig pen
/// </summary>
public record PigPenUpdateDto(
    string? PenCode = null,
    int? PigQty = null,
    DateTime? RegisterDate = null,
    DateTime? ActHarvestDate = null,
    DateTime? EstimatedHarvestDate = null,
    PigPenType? Type = null,
    string? SelectedBrand = null,
    decimal? DepositPerPig = null,
    string? Note = null
);

/// <summary>
/// Request for creating a deposit
/// </summary>
public record DepositCreateDto(
    decimal Amount,
    DateTime Date,
    string? Remark
);

/// <summary>
/// Request for updating a deposit
/// </summary>
public record DepositUpdateDto(
    decimal Amount,
    DateTime Date,
    string? Remark
);

/// <summary>
/// Request for creating a harvest result
/// </summary>
public record HarvestCreateDto(
    DateTime HarvestDate,
    int PigCount,
    decimal AvgWeight,
    decimal TotalWeight,
    decimal SalePricePerKg,
    decimal Revenue
);

/// <summary>
/// Request for updating a harvest result
/// </summary>
public record HarvestUpdateDto(
    DateTime HarvestDate,
    int PigCount,
    decimal AvgWeight,
    decimal TotalWeight,
    decimal SalePricePerKg,
    decimal Revenue
);

/// <summary>
/// Request for force closing a pig pen
/// </summary>
public record PigPenForceCloseRequest(
    Guid PigPenId,
    string Reason,
    bool ConfirmForceClose = false
);

/// <summary>
/// Response for deleting an invoice by reference code
/// </summary>
public record DeleteInvoiceResponse(
    int DeletedCount,
    string InvoiceReferenceCode,
    string Message
);