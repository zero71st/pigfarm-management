using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PigFarmManagement.Server.Services;
using PigFarmManagement.Shared.Models;
using Xunit;

namespace PigFarmManagement.Server.Tests
{
    public class PosposInvoiceImporterTests
    {
        [Fact]
        public async Task ImportByDateRange_Delegates_To_FeedImportService()
        {
            // Arrange
            var feedTx = new PosPosFeedTransaction { Id = "t1", Code = "TR1", Timestamp = DateTime.UtcNow };
            var mockFeedClient = new Mock<IPosposFeedClient>();
            mockFeedClient.Setup(m => m.GetTransactionsAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new[] { feedTx });

            var fakeResult = new FeedImportResult { TotalTransactions = 1, TotalFeedItems = 0, SuccessfulImports = 1 };
            var mockFeedImportService = new Mock<PigFarmManagement.Shared.Contracts.IFeedImportService>();
            mockFeedImportService.Setup(m => m.ImportPosPosFeedDataAsync(It.IsAny<List<PosPosFeedTransaction>>())).ReturnsAsync(fakeResult);

            var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<PosposInvoiceImporter>();

            var importer = new PosposInvoiceImporter(mockFeedClient.Object, mockFeedImportService.Object, logger);

            // Act
            var res = await importer.ImportByDateRangeAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

            // Assert
            Assert.Equal(1, res.TotalTransactions);
            mockFeedClient.Verify(m => m.GetTransactionsAsync(It.IsAny<string?>(), It.IsAny<string?>(), 1, 200), Times.Once);
            mockFeedImportService.Verify(m => m.ImportPosPosFeedDataAsync(It.IsAny<List<PosPosFeedTransaction>>()), Times.Once);
        }
    }
}
