using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Services.ExternalServices;

public interface IPosposTransactionClient
{
    /// <summary>
    /// Fetch transactions within a date range by iterating pages until no more results.
    /// </summary>
    Task<List<PosPosTransaction>> GetTransactionsByDateRangeAsync(DateTime from, DateTime to, int pageSize = 300);
}