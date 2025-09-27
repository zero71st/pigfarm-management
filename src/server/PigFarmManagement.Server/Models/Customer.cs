using System;
using System.Text.Json.Serialization;

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
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // Additional POSPOS fields (kept optional)
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Sex { get; set; } = string.Empty;
        public string KeyCardId { get; set; } = string.Empty;
        public string Zipcode { get; set; } = string.Empty;

        // Raw external payload (JSON) for full-fidelity storage if desired
        public string? RawPosposJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
