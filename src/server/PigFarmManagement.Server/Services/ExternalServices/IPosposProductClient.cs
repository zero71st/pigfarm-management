using System.Collections.Generic;
using System.Threading.Tasks;

namespace PigFarmManagement.Server.Services.ExternalServices
{
    /// <summary>
    /// Low-level HTTP client for POSPOS Product API communication.
    /// Handles rate limiting, authentication, and error handling.
    /// </summary>
    public interface IPosposProductClient
    {
        /// <summary>
        /// Fetches all products from the POSPOS API.
        /// </summary>
        /// <returns>List of product DTOs from POSPOS</returns>
        Task<List<PosposProductDto>> GetAllProductsAsync();

        /// <summary>
        /// Fetches a single product by code from the POSPOS API.
        /// </summary>
        /// <param name="code">The product code (e.g., "PK64000158")</param>
        /// <returns>Product DTO or null if not found</returns>
        Task<PosposProductDto?> GetProductByCodeAsync(string code);
    }

    /// <summary>
    /// DTO representing a product from POSPOS API response.
    /// Matches the structure from data-model.md.
    /// </summary>
    public class PosposProductDto
    {
        public string Id { get; set; } = string.Empty; // _id from POSPOS
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal? Cost { get; set; }
        public PosposCategoryDto? Category { get; set; }
        public PosposUnitDto? Unit { get; set; }
        public DateTime? LastUpdate { get; set; } // lastupdate from POSPOS
    }

    public class PosposCategoryDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class PosposUnitDto
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response wrapper from POSPOS API.
    /// </summary>
    public class PosposProductResponse
    {
        public int Success { get; set; }
        public List<PosposProductDto> Data { get; set; } = new();
    }
}
