using System;
using System.Text.Json.Serialization;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Models
{
    // Internal server-side Customer model extended to include common POSPOS fields
    public class Customer
    {
        // Core internal identifier (existing)
        public string Id { get; set; } = Guid.NewGuid().ToString();

    // Indexed / search fields
    public string Code { get; set; } = string.Empty; // maps to POSPOS code / key_card_id
    // Split into FirstName/LastName per POSPOS integration
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    // Friendly display computed property
    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName) ? Code : $"{FirstName} {LastName} ({Code})";
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ExternalId { get; set; }
    public string? KeyCardId { get; set; }
    public string? Address { get; set; }
    // FirstName/LastName defined above
    public string? Sex { get; set; }
    public string? Zipcode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
