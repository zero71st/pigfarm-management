namespace PigFarmManagement.Shared.Models;

/// <summary>
/// DTO for customer deletion requests with validation and audit tracking
/// </summary>
public class CustomerDeletionRequest
{
    public Guid CustomerId { get; set; }
    public bool ForceDelete { get; set; } = false; // Hard delete override
    public string? Reason { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}