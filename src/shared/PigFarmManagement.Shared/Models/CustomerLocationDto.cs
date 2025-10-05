namespace PigFarmManagement.Shared.Models;

/// <summary>
/// DTO for customer location data used in Google Maps integration
/// </summary>
public class CustomerLocationDto
{
    public Guid CustomerId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Address { get; set; }
    public DateTime UpdatedAt { get; set; }
}