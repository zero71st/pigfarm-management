namespace PigFarmManagement.Shared.DTOs;

/// <summary>
/// DTO for displaying grouped invoice information in the Invoice Management tab
/// </summary>
public record InvoiceGroupDto(
    string InvoiceReferenceCode,
    string TransactionCode,
    decimal TotalAmount,
    DateTime InvoiceDate,
    int ItemCount
);
