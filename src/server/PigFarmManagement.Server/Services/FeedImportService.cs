using System.Text.Json;
using Microsoft.Extensions.Logging;
using PigFarmManagement.Shared.Models;
using PigFarmManagement.Shared.Contracts;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;
using PigFarmManagement.Server.Services.ExternalServices;

namespace PigFarmManagement.Server.Services;

public class FeedImportService : IFeedImportService
{
    private readonly IPigPenRepository _pigPenRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly Infrastructure.Data.Repositories.IFeedRepository _efFeedRepository;
    private readonly IPosposTransactionClient _posposTrasactionClient;
    private readonly PigFarmManagement.Server.Features.FeedFormulas.IFeedFormulaService _feedFormulaService;
    private readonly ILogger<FeedImportService> _logger;

    // Keep legacy in-memory data for mock POSPOS transactions
    private readonly List<PigPen> _pigPens = new();
    private readonly List<Customer> _customers = new();

    public FeedImportService(
        IPigPenRepository pigPenRepository,
        ICustomerRepository customerRepository,
        Infrastructure.Data.Repositories.IFeedRepository efFeedRepository,
        IPosposTransactionClient posposTransactionClient,
        PigFarmManagement.Server.Features.FeedFormulas.IFeedFormulaService feedFormulaService,
        ILogger<FeedImportService> logger)
    {
        _pigPenRepository = pigPenRepository;
        _customerRepository = customerRepository;
        _efFeedRepository = efFeedRepository;
        _posposTrasactionClient = posposTransactionClient;
        _feedFormulaService = feedFormulaService;
        _logger = logger;

        InitializeData(); // Still needed for mock data
    }

    public async Task<FeedImportResult> ImportPosPosFeedDataAsync(List<PosPosFeedTransaction> transactions)
    {
        _logger.LogInformation("Starting POSPOS feed import with {TransactionCount} transactions", transactions.Count);
        
        var result = new FeedImportResult
        {
            TotalTransactions = transactions.Count
        };

        foreach (var transaction in transactions)
        {
            try
            {
                await ProcessTransactionAsync(transaction, result);
                result.SuccessfulImports++;
                
                _logger.LogDebug("Successfully processed transaction {TransactionCode} with {ItemCount} items", 
                    transaction.Code, transaction.OrderList?.Count ?? 0);
            }
            catch (Exception ex)
            {
                result.FailedImports++;
                result.Errors.Add($"Transaction {transaction.Code}: {ex.Message}");
                _logger.LogError(ex, "Failed to process transaction {TransactionCode}", transaction.Code);
            }
        }

        // Log structured import summary
        var totalExpense = result.TotalImportValue;
        _logger.LogInformation("POSPOS feed import completed: {SuccessfulImports} successful, {FailedImports} failed, " +
                              "TotalValue: {TotalExpense:C}, ErrorRate: {ErrorRate:F1}%",
                              result.SuccessfulImports, result.FailedImports, totalExpense, result.ErrorRate);

        if (result.Errors.Any())
        {
            _logger.LogWarning("Import completed with {ErrorCount} errors: {Errors}", 
                              result.Errors.Count, string.Join("; ", result.Errors));
        }

        return result;
    }

    public async Task<FeedImportResult> ImportFromJsonAsync(string jsonContent)
    {
        try
        {
            var transactions = JsonSerializer.Deserialize<List<PosPosFeedTransaction>>(jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (transactions == null)
            {
                return new FeedImportResult
                {
                    FailedImports = 1,
                    Errors = new() { "Failed to parse JSON content" }
                };
            }

            return await ImportPosPosFeedDataAsync(transactions);
        }
        catch (Exception ex)
        {
            return new FeedImportResult
            {
                FailedImports = 1,
                Errors = new() { $"JSON parsing error: {ex.Message}" }
            };
        }
    }

    public async Task<FeedImportResult> ImportPosPosFeedForPigPenAsync(Guid pigPenId, List<PosPosFeedTransaction> transactions)
    {
        var result = new FeedImportResult
        {
            TotalTransactions = transactions.Count
        };

        // Find the specific pig pen using the repository
        var pigPen = await _pigPenRepository.GetByIdAsync(pigPenId);
        if (pigPen == null)
        {
            result.FailedImports = transactions.Count;
            result.Errors.Add($"Pig pen with ID {pigPenId} not found");
            return result;
        }

        foreach (var transaction in transactions)
        {
            try
            {
                await ProcessTransactionForPigPenAsync(transaction, pigPen, result);
                result.SuccessfulImports++;
            }
            catch (Exception ex)
            {
                result.FailedImports++;
                result.Errors.Add($"Transaction {transaction.Code}: {ex.Message}");
            }
        }

        return result;
    }

    // Mock endpoints removed. Use ImportFromJsonAsync or the POSPOS date-range endpoints for testing.

    public async Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerCodeAsync(string customerCode)
    {
        // Default to last 30 days for customer-only queries
        var to = DateTime.UtcNow;
        var from = to.AddDays(-30);
        var transactions = await _posposTrasactionClient.GetTransactionsByDateRangeAsync(from, to);
        return transactions
            .Where(t => t.BuyerDetail != null && (
                t.BuyerDetail.Code.Equals(customerCode, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(t.BuyerDetail.KeyCardId) && t.BuyerDetail.KeyCardId.Equals(customerCode, StringComparison.OrdinalIgnoreCase))
            ))
            .ToList();
    }

    public async Task<List<PosPosFeedTransaction>> GetPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        return await _posposTrasactionClient.GetTransactionsByDateRangeAsync(fromDate, toDate);
    }

    public async Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerAndDateRangeAsync(string customerCode, DateTime fromDate, DateTime toDate)
    {
        var transactions = await _posposTrasactionClient.GetTransactionsByDateRangeAsync(fromDate, toDate);
        return transactions
            .Where(t => t.BuyerDetail != null && (
                t.BuyerDetail.Code.Equals(customerCode, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(t.BuyerDetail.KeyCardId) && t.BuyerDetail.KeyCardId.Equals(customerCode, StringComparison.OrdinalIgnoreCase))
            ))
            .ToList();
    }

    public async Task<List<PosPosFeedTransaction>> GetAllPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        return await _posposTrasactionClient.GetTransactionsByDateRangeAsync(fromDate, toDate);
    }

    public async Task<FeedImportResult> ImportPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var transactions = await GetPosPosFeedByDateRangeAsync(fromDate, toDate);
        return await ImportPosPosFeedDataAsync(transactions);
    }

    public async Task<FeedImportResult> CreateDemoFeedsWithProductInfoAsync(Guid pigPenId)
    {
        var pigPen = await _pigPenRepository.GetByIdAsync(pigPenId);
        if (pigPen == null)
        {
            return new FeedImportResult
            {
                FailedImports = 1,
                Errors = new() { $"Pig pen with ID {pigPenId} not found" }
            };
        }

        var demoFeeds = new List<Feed>
        {
            new Feed
            {
                Id = Guid.NewGuid(),
                PigPenId = pigPenId,
                ProductType = "อาหารสัตว์",
                ProductCode = "PK64000161",
                ProductName = "เจ็ท 120 หมู 40-60 กก.",
                TransactionCode = $"DEMO-{DateTime.Now:yyyyMMdd}-001",
                Quantity = 50,
                UnitPrice = 580,
                TotalPriceIncludeDiscount = 29000,
                FeedDate = DateTime.UtcNow.AddDays(-1),
                ExternalReference = "DEMO-FEED-001",
                Notes = "Demo feed for testing",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var result = new FeedImportResult
        {
            TotalFeedItems = demoFeeds.Count
        };

        foreach (var feed in demoFeeds)
        {
            try
            {
                await _efFeedRepository.CreateAsync(feed);
                result.SuccessfulImports++;
            }
            catch (Exception ex)
            {
                result.FailedImports++;
                result.Errors.Add($"Failed to create demo feed: {ex.Message}");
            }
        }

        return result;
    }

    private void InitializeData()
    {
        // Legacy initialization for mock data - keeping for compatibility
        // This will be removed once all data comes from repositories
    }
    // Legacy initialization for mock data - kept only as a historical placeholder.
    private async Task ProcessTransactionAsync(PosPosFeedTransaction transaction, FeedImportResult result)
    {
        // Find or create customer based on buyer detail
        var customer = FindOrCreateCustomer(transaction.BuyerDetail);

        // Find pig pen for this customer (for demo, we'll use the first available)
        var pigPen = _pigPens.FirstOrDefault(p => p.CustomerId == customer.Id)
                    ?? _pigPens.FirstOrDefault(); // fallback

        if (pigPen == null)
        {
            throw new Exception($"No pig pen found for customer {customer.DisplayName}");
        }

        await ProcessTransactionForPigPenAsync(transaction, pigPen, result);
    }

    private async Task ProcessTransactionForPigPenAsync(PosPosFeedTransaction transaction, PigPen pigPen, FeedImportResult result)
    {
        _logger.LogDebug("Processing transaction {TransactionCode} for pig pen {PigPenId}", transaction.Code, pigPen.Id);
        
        // Idempotency: skip processing if a feed with the same InvoiceNumber already exists globally
        if (!string.IsNullOrWhiteSpace(transaction.Code))
        {
            var exists = await _efFeedRepository.ExistsByInvoiceNumberAsync(transaction.Code);
            if (exists)
            {
                result.FailedImports++;
                result.Errors.Add($"Transaction {transaction.Code} skipped: invoice already imported");
                _logger.LogWarning("Skipping duplicate transaction {TransactionCode}", transaction.Code);
                return;
            }
        }

        // Preload feed formulas to avoid repeated DB calls
        var formulas = (await _feedFormulaService.GetAllFeedFormulasAsync()).ToList();
        var formulaByCode = formulas
            .Where(f => !string.IsNullOrWhiteSpace(f.Code))
            .ToDictionary(f => f.Code!, StringComparer.OrdinalIgnoreCase);

        var processedItems = 0;
        var formulaMatchCount = 0;
        var totalValue = 0m;

        foreach (var orderItem in transaction.OrderList)
        {
            // stock is decimal representing number of bags. Coerce to integer bag count.
            var bags = (int)Math.Round(orderItem.Stock, MidpointRounding.AwayFromZero);

            // UnitPrice is price-per-bag (POSPOS price is per-bag)
            var unitPricePerBag = orderItem.Price;

            // Determine product mapping: prefer exact code match, fallback to exact name match
            var normalizedCode = (orderItem.Code ?? string.Empty).Trim();
            var normalizedName = (orderItem.Name ?? string.Empty).Trim();

            // NOTE: repository for product catalog is not available; rely on ProductCode/Name being stored on Feed

            // Try to find a feed formula by product code
            decimal? formulaCost = null;
            if (!string.IsNullOrWhiteSpace(normalizedCode) && formulaByCode.TryGetValue(normalizedCode, out var ff))
            {
                formulaCost = ff.Cost;
                formulaMatchCount++;
                _logger.LogDebug("Found formula cost {Cost:C} for product {ProductCode}", formulaCost, normalizedCode);
            }

            // Use CostDiscountPrice directly from POSPOS (no internal calculation)
            var costDiscountPrice = orderItem.CostDiscountPrice;
            
            _logger.LogDebug("Processing order item {ProductCode}: Price={Price:C}, Stock={Stock}, CostDiscountPrice={CostDiscountPrice:C}",
                normalizedCode, orderItem.Price, orderItem.Stock, costDiscountPrice);

            var priceIncludeDiscount = unitPricePerBag - costDiscountPrice;

            var feed = new Feed
            {
                Id = Guid.NewGuid(),
                PigPenId = pigPen.Id,
                ProductType = "อาหารสัตว์",
                ProductCode = normalizedCode,
                ProductName = normalizedName,
                TransactionCode = transaction.Code,
                InvoiceReferenceCode = transaction.InvoiceReference?.Code,
                Quantity = bags,
                UnitPrice = unitPricePerBag,
                Cost = formulaCost,
                CostDiscountPrice = costDiscountPrice,
                PriceIncludeDiscount = priceIncludeDiscount,
                Sys_TotalPriceIncludeDiscount = priceIncludeDiscount * bags,
                TotalPriceIncludeDiscount = orderItem.TotalPriceIncludeDiscount,
                Pos_TotalPriceIncludeDiscount = orderItem.TotalPriceIncludeDiscount,
                FeedDate = transaction.Timestamp,
                ExternalReference = $"POSPOS-{transaction.Code}-{orderItem.Code}",
                Notes = $"Imported from POSPOS transaction {transaction.Code}",
                ExternalProductCode = normalizedCode,
                ExternalProductName = normalizedName,
                UnmappedProduct = true, // default to true until a product mapping routine updates this
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // If we had product lookup logic, we would set UnmappedProduct=false and set ProductCode/ProductName accordingly.

            await _efFeedRepository.CreateAsync(feed);

            result.TotalFeedItems++;
            processedItems++;
            totalValue += orderItem.TotalPriceIncludeDiscount;
        }
        
        _logger.LogInformation("Transaction {TransactionCode} processed: {ProcessedItems} items, {FormulaMatches} formula matches, total value {TotalValue:C}",
            transaction.Code, processedItems, formulaMatchCount, totalValue);
    }

    private Customer FindOrCreateCustomer(PosPosBuyerDetail buyerDetail)
    {
        var existingCustomer = _customers.FirstOrDefault(c =>
            c.Code.Equals(buyerDetail.Code, StringComparison.OrdinalIgnoreCase));

        if (existingCustomer != null)
        {
            return existingCustomer;
        }

        // Create new customer
        var newCustomer = new Customer(
            Guid.NewGuid(),
            buyerDetail.Code,
            CustomerStatus.Active)
        {
            FirstName = buyerDetail.FirstName,
            LastName = buyerDetail.LastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _customers.Add(newCustomer);
        return newCustomer;
    }
}