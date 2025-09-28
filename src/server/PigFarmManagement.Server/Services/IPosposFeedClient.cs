using System.Collections.Generic;
using System.Threading.Tasks;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Services
{
    public interface IPosposFeedClient
    {
        /// <summary>
        /// Fetch transactions from POSPOS for the given date range (inclusive).
        /// </summary>
        Task<IEnumerable<PosPosFeedTransaction>> GetTransactionsAsync(string? start = null, string? end = null, int page = 1, int limit = 200);
    }
}
