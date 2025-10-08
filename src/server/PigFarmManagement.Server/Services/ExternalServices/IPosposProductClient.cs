using System.Collections.Generic;
using System.Threading.Tasks;
using PigFarmManagement.Shared.Models;
using PigFarmManagement.Shared.Domain.External;

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
        Task<List<PosposProduct>> GetAllProductsAsync();

        /// <summary>
        /// Fetches a single product by code from the POSPOS API.
        /// </summary>
        /// <param name="code">The product code (e.g., "PK64000158")</param>
        /// <returns>Product DTO or null if not found</returns>
        Task<PosposProduct?> GetProductByCodeAsync(string code);
    }

    /// <summary>
    /// Response wrapper from POSPOS API.
    /// </summary>
    public class PosposProductResponse
    {
        public int Success { get; set; }
        public List<PosposProduct> Data { get; set; } = new();
    }
}
