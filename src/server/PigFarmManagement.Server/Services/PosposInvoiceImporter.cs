using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Services
{
    public interface IPosposInvoiceImporter
    {
        Task<FeedImportResult> ImportByDateRangeAsync(DateTime from, DateTime to);
        Task<FeedImportResult> ImportByCustomerCodeAsync(string customerCode, DateTime? from = null, DateTime? to = null);
    }

    public class PosposInvoiceImporter : IPosposInvoiceImporter
    {
        private readonly IPosposFeedClient _feedClient;
        private readonly PigFarmManagement.Shared.Contracts.IFeedImportService _feedImportService;
        private readonly ILogger<PosposInvoiceImporter> _logger;

        public PosposInvoiceImporter(IPosposFeedClient feedClient, PigFarmManagement.Shared.Contracts.IFeedImportService feedImportService, ILogger<PosposInvoiceImporter> logger)
        {
            _feedClient = feedClient;
            _feedImportService = feedImportService;
            _logger = logger;
        }

        public async Task<FeedImportResult> ImportByDateRangeAsync(DateTime from, DateTime to)
        {
            var start = from.ToString("yyyy-MM-dd");
            var end = to.ToString("yyyy-MM-dd");
            var tx = (await _feedClient.GetTransactionsAsync(start, end)).ToList();
            _logger.LogInformation("Fetched {Count} transactions from POSPOS for {From} - {To}", tx.Count, start, end);
            return await _feedImportService.ImportPosPosFeedDataAsync(tx);
        }

        public async Task<FeedImportResult> ImportByCustomerCodeAsync(string customerCode, DateTime? from = null, DateTime? to = null)
        {
            IEnumerable<PosPosFeedTransaction> tx;
            if (from.HasValue && to.HasValue)
            {
                tx = (await _feedClient.GetTransactionsAsync(start: from.Value.ToString("yyyy-MM-dd"), end: to.Value.ToString("yyyy-MM-dd"))).Where(t => string.Equals(t.BuyerDetail.Code, customerCode, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                tx = (await _feedClient.GetTransactionsAsync()).Where(t => string.Equals(t.BuyerDetail.Code, customerCode, StringComparison.OrdinalIgnoreCase));
            }

            var list = tx.ToList();
            _logger.LogInformation("Fetched {Count} transactions for customer {CustomerCode}", list.Count, customerCode);
            return await _feedImportService.ImportPosPosFeedDataAsync(list);
        }
    }
}
