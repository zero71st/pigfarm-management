using System.Text.Json.Serialization;

namespace PigFarmManagement.Server.Models
{
    // Lightweight DTO representing the external POSPOS customer shape.
    // Use explicit JsonPropertyName attributes to match the upstream API fields.
    public class PosposMember
    {
        [JsonPropertyName("_id")] // POSPOS sometimes uses _id
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("code")] public string Code { get; set; } = string.Empty;

        // Provide both common variants via case-insensitive deserialization in client.
        [JsonPropertyName("firstname")] public string FirstName { get; set; } = string.Empty;
        [JsonPropertyName("lastname")] public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("phonenumber")] public string PhoneNumber { get; set; } = string.Empty;
        [JsonPropertyName("phone")] public string Phone { get; set; } = string.Empty; // fallback

        [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
        [JsonPropertyName("address")] public string Address { get; set; } = string.Empty;

        [JsonPropertyName("key_card_id")] public string KeyCardId { get; set; } = string.Empty;
        [JsonPropertyName("zipcode")] public string Zipcode { get; set; } = string.Empty;
        [JsonPropertyName("sex")] public string Sex { get; set; } = string.Empty;

        [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = string.Empty;
    }
}
