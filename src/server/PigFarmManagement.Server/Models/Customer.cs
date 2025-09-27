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
    public string Name { get; set; } = string.Empty;
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ExternalId { get; set; }
    public string? KeyCardId { get; set; }
    public string? Address { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Sex { get; set; }
    public string? Zipcode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
