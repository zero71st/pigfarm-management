using System.Text.Json;
using PigFarmManagement.Shared.Models;
using PigFarmManagement.Shared.Contracts;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;
using PigFarmManagement.Server.Services.ExternalServices;

namespace PigFarmManagement.Server.Services;

public class FeedImportService : IFeedImportService
{
    private readonly IPigPenRepository _pigPenRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IFeedRepository _feedRepository;
    private readonly Infrastructure.Data.Repositories.IFeedRepository _efFeedRepository;
    private readonly IPosposTransactionClient _posposTrasactionClient;

    // Keep legacy in-memory data for mock POSPOS transactions
    private readonly List<PigPen> _pigPens = new();
    private readonly List<Customer> _customers = new();

    public FeedImportService(
        IPigPenRepository pigPenRepository,
        ICustomerRepository customerRepository,
        IFeedRepository feedRepository,
        Infrastructure.Data.Repositories.IFeedRepository efFeedRepository,
        IPosposTransactionClient posposTransactionClient)
    {
        _pigPenRepository = pigPenRepository;
        _customerRepository = customerRepository;
        _feedRepository = feedRepository;
        _efFeedRepository = efFeedRepository;
        _posposTrasactionClient = posposTransactionClient;
        
        InitializeData(); // Still needed for mock data
    }

    public async Task<FeedImportResult> ImportPosPosFeedDataAsync(List<PosPosFeedTransaction> transactions)
    {
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
            }
            catch (Exception ex)
            {
                result.FailedImports++;
                result.Errors.Add($"Transaction {transaction.Code}: {ex.Message}");
            }
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
        var transactions = await _posposTrasactionClient.GetTransactionsByDateRangeAsync(fromDate, toDate);
        return transactions
            .Where(t => t.Timestamp >= fromDate && t.Timestamp <= toDate)
            .ToList();
    }

    public async Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerAndDateRangeAsync(string customerCode, DateTime fromDate, DateTime toDate)
    {
        var transactions = await _posposTrasactionClient.GetTransactionsByDateRangeAsync(fromDate, toDate);
        return transactions
            .Where(t => t.BuyerDetail != null && (
                t.BuyerDetail.Code.Equals(customerCode, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(t.BuyerDetail.KeyCardId) && t.BuyerDetail.KeyCardId.Equals(customerCode, StringComparison.OrdinalIgnoreCase))
            ) && t.Timestamp >= fromDate && t.Timestamp <= toDate)
            .ToList();
    }

    public async Task<List<PosPosFeedTransaction>> GetAllPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var transactions = await _posposTrasactionClient.GetTransactionsByDateRangeAsync(fromDate, toDate);
        return transactions
            .Where(t => t.Timestamp >= fromDate && t.Timestamp <= toDate)
            .ToList();
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
                InvoiceNumber = $"DEMO-{DateTime.Now:yyyyMMdd}-001",
                Quantity = 50,
                UnitPrice = 580,
                TotalPrice = 29000,
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
                await _feedRepository.CreateAsync(feed);
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
        // Idempotency: skip processing if a feed with the same InvoiceNumber already exists globally
        if (!string.IsNullOrWhiteSpace(transaction.Code))
        {
            var exists = await _efFeedRepository.ExistsByInvoiceNumberAsync(transaction.Code);
            if (exists)
            {
                result.FailedImports++;
                result.Errors.Add($"Transaction {transaction.Code} skipped: invoice already imported");
                return;
            }
        }

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

            var feed = new Feed
            {
                Id = Guid.NewGuid(),
                PigPenId = pigPen.Id,
                ProductType = "อาหารสัตว์",
                ProductCode = normalizedCode,
                ProductName = normalizedName,
                InvoiceNumber = transaction.Code,
                InvoiceReferenceCode = transaction.InvoiceReference?.Code,
                Quantity = bags,
                UnitPrice = unitPricePerBag,
                TotalPrice = orderItem.TotalPriceIncludeDiscount,
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
        }
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