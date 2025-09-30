using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Services.ExternalServices;

public interface IPosposTransactionClient
{
    /// <summary>
    /// Fetch a single page of transactions from POSPOS. The returned tuple contains the transactions and a boolean flag indicating whether there are more pages available.
    /// </summary>
    Task<(List<PosPosFeedTransaction> Transactions, bool HasMore)> GetTransactionsPageAsync(int page = 1, int limit = 100);

    /// <summary>
    /// Fetch transactions within a date range by iterating pages until no more results.
    /// </summary>
    Task<List<PosPosFeedTransaction>> GetTransactionsByDateRangeAsync(DateTime from, DateTime to, int pageSize = 100);
}