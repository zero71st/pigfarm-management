using System.Text.Json;
using PigFarmManagement.Shared.Models;
using PigFarmManagement.Shared.Contracts;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;

namespace PigFarmManagement.Server.Services;

public class FeedImportService : IFeedImportService
{
    private readonly IPigPenRepository _pigPenRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IFeedRepository _feedRepository;

    // Keep legacy in-memory data for mock POSPOS transactions
    private readonly List<PigPen> _pigPens = new();
    private readonly List<Customer> _customers = new();

    public FeedImportService(
        IPigPenRepository pigPenRepository,
        ICustomerRepository customerRepository,
        IFeedRepository feedRepository)
    {
        _pigPenRepository = pigPenRepository;
        _customerRepository = customerRepository;
        _feedRepository = feedRepository;
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

    public async Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerCodeAsync(string customerCode)
    {
        var allTransactions = await GetMockPosPosFeedDataAsync();
        return allTransactions
            .Where(t => t.BuyerDetail.Code.Equals(customerCode, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.Timestamp)
            .ToList();
    }

    public async Task<List<PosPosFeedTransaction>> GetPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var allTransactions = await GetMockPosPosFeedDataAsync();
        return allTransactions
            .Where(t => t.Timestamp >= fromDate && t.Timestamp <= toDate)
            .OrderByDescending(t => t.Timestamp)
            .ToList();
    }

    public async Task<List<PosPosFeedTransaction>> GetPosPosFeedByCustomerAndDateRangeAsync(string customerCode, DateTime fromDate, DateTime toDate)
    {
        var allTransactions = await GetMockPosPosFeedDataAsync();
        return allTransactions
            .Where(t => t.BuyerDetail.Code.Equals(customerCode, StringComparison.OrdinalIgnoreCase) &&
                       t.Timestamp >= fromDate && t.Timestamp <= toDate)
            .OrderByDescending(t => t.Timestamp)
            .ToList();
    }

    public async Task<FeedImportResult> ImportPosPosFeedByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var transactions = await GetPosPosFeedByDateRangeAsync(fromDate, toDate);
        return await ImportPosPosFeedDataAsync(transactions);
    }

    public async Task<List<PosPosFeedTransaction>> GetMockPosPosFeedDataAsync()
    {
        // Create PosPosFeedItems from products.json data with random stock
        var random = new Random();
        var posPosFeedItems = CreateMockFeedItemsFromProducts(random);

        // Create mock transactions using the products data
        var mockTransactions = new List<PosPosFeedTransaction>
        {
            new()
            {
                Id = "66fc65e4a2e61a30b0dcb111",
                Code = "INV-2024-001",
                Timestamp = DateTime.Today.AddHours(-2), // Today, 2 hours ago
                OrderList = new()
                {
                    posPosFeedItems[0], // เจ็ท 105 หมูเล็ก 6-15 กก.
                    posPosFeedItems[1]  // เจ็ท 108 หมูนม 15-25 กก.
                },
                BuyerDetail = new()
                {
                    Code = "CUST-001",
                    FirstName = "John",
                    LastName = "Farmer",
                    KeyCardId = "CARD-12345"
                },
                InvoiceReference = new()
                {
                    Code = "REF-2024-001"
                },
                SubTotal = posPosFeedItems[0].TotalPriceIncludeDiscount + posPosFeedItems[1].TotalPriceIncludeDiscount,
                GrandTotal = posPosFeedItems[0].TotalPriceIncludeDiscount + posPosFeedItems[1].TotalPriceIncludeDiscount,
                Status = "completed"
            },
            new()
            {
                Id = "66fc65e4a2e61a30b0dcb222",
                Code = "INV-2024-002", 
                Timestamp = DateTime.Today.AddDays(-1).AddHours(-1), // Yesterday, 1 hour ago from midnight
                OrderList = new()
                {
                    posPosFeedItems[2] // เจ็ท 110 หมู 25-40 กก.
                },
                BuyerDetail = new()
                {
                    Code = "CUST-002",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    KeyCardId = "CARD-67890"
                },
                InvoiceReference = new()
                {
                    Code = "REF-2024-002"
                },
                SubTotal = posPosFeedItems[2].TotalPriceIncludeDiscount,
                GrandTotal = posPosFeedItems[2].TotalPriceIncludeDiscount,
                Status = "completed"
            },
            new()
            {
                Id = "66fc65e4a2e61a30b0dcb333",
                Code = "INV-2024-003", 
                Timestamp = DateTime.Today.AddDays(-3).AddHours(10), // 3 days ago, 10 AM
                OrderList = new()
                {
                    posPosFeedItems[3] // เจ็ท 111 หมู 0-15 กก.
                },
                BuyerDetail = new()
                {
                    Code = "CUST-001",
                    FirstName = "John",
                    LastName = "Farmer",
                    KeyCardId = "CARD-12345"
                },
                InvoiceReference = new()
                {
                    Code = "REF-2024-003"
                },
                SubTotal = posPosFeedItems[3].TotalPriceIncludeDiscount,
                GrandTotal = posPosFeedItems[3].TotalPriceIncludeDiscount,
                Status = "completed"
            },
            new()
            {
                Id = "66fc65e4a2e61a30b0dcb444",
                Code = "INV-2024-004", 
                Timestamp = DateTime.Today.AddDays(-5).AddHours(14), // 5 days ago, 2 PM
                OrderList = new()
                {
                    posPosFeedItems[4], // เจ็ท 120 หมู 40-60 กก.
                    posPosFeedItems[5]  // เจ็ท 130 หมู 60-90 กก.
                },
                BuyerDetail = new()
                {
                    Code = "CUST-002",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    KeyCardId = "CARD-67890"
                },
                InvoiceReference = new()
                {
                    Code = "REF-2024-004"
                },
                SubTotal = posPosFeedItems[4].TotalPriceIncludeDiscount + posPosFeedItems[5].TotalPriceIncludeDiscount,
                GrandTotal = posPosFeedItems[4].TotalPriceIncludeDiscount + posPosFeedItems[5].TotalPriceIncludeDiscount,
                Status = "completed"
            },
            new()
            {
                Id = "66fc65e4a2e61a30b0dcb555",
                Code = "INV-2024-005", 
                Timestamp = DateTime.Today.AddDays(-7).AddHours(9), // 7 days ago, 9 AM
                OrderList = new()
                {
                    posPosFeedItems[6] // เจ็ท 153 หมู 90 กก. ขึ้นไป
                },
                BuyerDetail = new()
                {
                    Code = "CUST-003",
                    FirstName = "Mike",
                    LastName = "Green",
                    KeyCardId = "CARD-99999"
                },
                InvoiceReference = new()
                {
                    Code = "REF-2024-005"
                },
                SubTotal = posPosFeedItems[6].TotalPriceIncludeDiscount,
                GrandTotal = posPosFeedItems[6].TotalPriceIncludeDiscount,
                Status = "completed"
            },
            new()
            {
                Id = "66fc65e4a2e61a30b0dcb666",
                Code = "INV-2024-006", 
                Timestamp = DateTime.Today.AddDays(-10).AddHours(16), // 10 days ago, 4 PM
                OrderList = new()
                {
                    posPosFeedItems[0], // เจ็ท 105 หมูเล็ก 6-15 กก.
                    posPosFeedItems[2], // เจ็ท 110 หมู 25-40 กก.
                    posPosFeedItems[4]  // เจ็ท 120 หมู 40-60 กก.
                },
                BuyerDetail = new()
                {
                    Code = "CUST-001",
                    FirstName = "John",
                    LastName = "Farmer",
                    KeyCardId = "CARD-12345"
                },
                InvoiceReference = new()
                {
                    Code = "REF-2024-006"
                },
                SubTotal = posPosFeedItems[0].TotalPriceIncludeDiscount + posPosFeedItems[2].TotalPriceIncludeDiscount + posPosFeedItems[4].TotalPriceIncludeDiscount,
                GrandTotal = posPosFeedItems[0].TotalPriceIncludeDiscount + posPosFeedItems[2].TotalPriceIncludeDiscount + posPosFeedItems[4].TotalPriceIncludeDiscount,
                Status = "completed"
            }
        };

        return await Task.FromResult(mockTransactions);
    }

    /// <summary>
    /// Creates mock PosPosFeedItem objects based on products.json data with random stock values
    /// </summary>
    private List<PosPosFeedItem> CreateMockFeedItemsFromProducts(Random random)
    {
        // Products data from products.json
        var products = new[]
        {
            new { code = "PK64000158", name = "เจ็ท 105 หมูเล็ก 6-15 กก.", price = "฿850.00", cost = "฿755.00" },
            new { code = "PK64000159", name = "เจ็ท 108 หมูนม 15-25 กก.", price = "฿750.00", cost = "฿650.00" },
            new { code = "PK64000160", name = "เจ็ท 110 หมู 25-40 กก.", price = "฿690.00", cost = "฿595.00" },
            new { code = "PK64000170", name = "เจ็ท 111 หมู 0-15 กก.", price = "฿775.00", cost = "฿738.30" },
            new { code = "PK64000161", name = "เจ็ท 120 หมู 40-60 กก.", price = "฿630.00", cost = "฿533.00" },
            new { code = "PK64000162", name = "เจ็ท 130 หมู 60-90 กก.", price = "฿598.00", cost = "฿505.00" },
            new { code = "PK64000163", name = "เจ็ท 153 หมู 90 กก. ขึ้นไป", price = "฿545.00", cost = "฿485.00" }
        };

        var feedItems = new List<PosPosFeedItem>();

        foreach (var product in products)
        {
            // Parse price by removing ฿ symbol and converting to decimal
            var price = decimal.Parse(product.price.Replace("฿", "").Replace(",", ""));
            var cost = decimal.Parse(product.cost.Replace("฿", "").Replace(",", ""));
            
            // Generate random stock between 10 and 100
            var stock = random.Next(10, 101);
            
            // Calculate special price with some random discount (5-15% off)
            var discountPercent = random.Next(5, 16) / 100.0m;
            var specialPrice = Math.Round(price * (1 - discountPercent), 2);

            var feedItem = new PosPosFeedItem
            {
                Stock = stock,
                Name = product.name,
                Price = price,
                SpecialPrice = specialPrice,
                Code = product.code,
                TotalPriceIncludeDiscount = specialPrice,
                NoteInOrder = new() { "อาหารสัตว์คุณภาพสูง", "สำหรับหมูในช่วงอายุที่กำหนด" }
            };

            feedItems.Add(feedItem);
        }

        return feedItems;
    }

    private async Task ProcessTransactionAsync(PosPosFeedTransaction transaction, FeedImportResult result)
    {
        // Find or create customer based on buyer detail
        var customer = FindOrCreateCustomer(transaction.BuyerDetail);
        
        // Find pig pen for this customer (for demo, we'll use the first available)
        var pigPen = _pigPens.FirstOrDefault(p => p.CustomerId == customer.Id) 
                    ?? _pigPens.FirstOrDefault(); // fallback

        if (pigPen == null)
        {
            throw new Exception($"No pig pen found for customer {customer.Name}");
        }

        var importedSummary = new ImportedFeedSummary
        {
            InvoiceCode = transaction.Code,
            CustomerName = $"{transaction.BuyerDetail.FirstName} {transaction.BuyerDetail.LastName}",
            PigPenCode = pigPen.Code,
            FeedItemsCount = transaction.OrderList.Count,
            TotalAmount = transaction.GrandTotal,
            ImportDate = DateTime.Now
        };

        // Process each feed item in the order
        foreach (var item in transaction.OrderList)
        {
            var feed = new Feed
            {
                Id = Guid.NewGuid(),
                PigPenId = pigPen.Id,
                ProductType = "อาหารสัตว์", // Use generic Thai category
                ProductCode = item.Code, // Store the actual product code
                ProductName = item.Name, // Store the actual product name
                InvoiceNumber = transaction.Code, // Store the invoice number
                Quantity = item.Stock,
                UnitPrice = item.SpecialPrice > 0 ? item.SpecialPrice : item.Price,
                TotalPrice = item.TotalPriceIncludeDiscount,
                FeedDate = transaction.Timestamp,
                Notes = string.Join(", ", item.NoteInOrder),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                ExternalReference = $"POSPOS-{transaction.Code}-{item.Code}"
            };

            await _feedRepository.CreateAsync(feed);
            result.TotalFeedItems++;
        }

        result.ImportedFeeds.Add(importedSummary);
    }

    private async Task ProcessTransactionForPigPenAsync(PosPosFeedTransaction transaction, PigPen pigPen, FeedImportResult result)
    {
        var importedSummary = new ImportedFeedSummary
        {
            InvoiceCode = transaction.Code,
            CustomerName = $"{transaction.BuyerDetail.FirstName} {transaction.BuyerDetail.LastName}",
            PigPenCode = pigPen.PenCode,
            FeedItemsCount = transaction.OrderList.Count,
            TotalAmount = transaction.GrandTotal,
            ImportDate = DateTime.Now
        };

        // Process each feed item in the order for this specific pig pen
        foreach (var item in transaction.OrderList)
        {
            var feed = new Feed
            {
                Id = Guid.NewGuid(),
                PigPenId = pigPen.Id,
                ProductType = "อาหารสัตว์", // Use generic Thai category
                ProductCode = item.Code, // Store the actual product code
                ProductName = item.Name, // Store the actual product name
                InvoiceNumber = transaction.Code, // Store the invoice number
                Quantity = item.Stock,
                UnitPrice = item.SpecialPrice > 0 ? item.SpecialPrice : item.Price,
                TotalPrice = item.TotalPriceIncludeDiscount,
                FeedDate = transaction.Timestamp,
                Notes = string.Join(", ", item.NoteInOrder),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                ExternalReference = $"POSPOS-{transaction.Code}-{item.Code}"
            };

            await _feedRepository.CreateAsync(feed);
            result.TotalFeedItems++;
        }

        result.ImportedFeeds.Add(importedSummary);
    }

    private Customer FindOrCreateCustomer(PosPosBuyerDetail buyerDetail)
    {
        var existingCustomer = _customers.FirstOrDefault(c => 
            c.ExternalId == buyerDetail.Code || 
            c.KeyCardId == buyerDetail.KeyCardId);

        if (existingCustomer != null)
        {
            return existingCustomer;
        }

        // Create new customer
        var newCustomer = new Customer(
            Guid.NewGuid(),
            buyerDetail.Code,
            $"{buyerDetail.FirstName} {buyerDetail.LastName}",
            CustomerStatus.Active)
        {
            ExternalId = buyerDetail.Code,
            KeyCardId = buyerDetail.KeyCardId,
            Phone = "",
            Email = "",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _customers.Add(newCustomer);
        return newCustomer;
    }

    private string MapProductNameToType(string productName)
    {
        productName = productName.ToLower();
        
        if (productName.Contains("starter") || productName.Contains("piglet"))
            return "Starter Feed";
        if (productName.Contains("grower") || productName.Contains("growing"))
            return "Grower Feed";
        if (productName.Contains("finisher"))
            return "Finisher Feed";
        if (productName.Contains("vitamin") || productName.Contains("supplement"))
            return "Supplement";
        if (productName.Contains("premium"))
            return "Premium Feed";
        
        return "Standard Feed";
    }

    private void InitializeData()
    {
        // Initialize with some sample data
        var customer1 = new Customer(
            Guid.NewGuid(),
            "CUST-001",
            "John Farmer",
            CustomerStatus.Active)
        {
            Phone = "123-456-7890",
            Email = "john@example.com",
            ExternalId = "CUST-001",
            KeyCardId = "CARD-12345"
        };

        var customer2 = new Customer(
            Guid.NewGuid(),
            "CUST-002",
            "Sarah Johnson",
            CustomerStatus.Active)
        {
            Phone = "098-765-4321",
            Email = "sarah@example.com",
            ExternalId = "CUST-002",
            KeyCardId = "CARD-67890"
        };

        _customers.AddRange(new[] { customer1, customer2 });

        _pigPens.AddRange(new[]
        {
            new PigPen(
                Guid.NewGuid(),
                customer1.Id,
                "PEN-001",
                45,
                DateTime.Now.AddDays(-10),
                null,
                DateTime.Now.AddDays(60),
                1200.00m,
                5000.00m,
                -800.00m,
                PigPenType.Cash,
                null, // FeedFormulaId
                DateTime.Now.AddDays(-10), // CreatedAt
                DateTime.Now.AddDays(-1) // UpdatedAt
            ),
            new PigPen(
                Guid.NewGuid(),
                customer2.Id,
                "PEN-002",
                38,
                DateTime.Now.AddDays(-8),
                null,
                DateTime.Now.AddDays(65),
                980.00m,
                4200.00m,
                -650.00m,
                PigPenType.Project,
                null, // FeedFormulaId
                DateTime.Now.AddDays(-8), // CreatedAt
                DateTime.Now.AddDays(-2) // UpdatedAt
            )
        });
    }

    /// <summary>
    /// Creates hardcoded demo feed records with complete product information
    /// Call this method to add sample feeds to demonstrate the product display feature
    /// </summary>
    public async Task<FeedImportResult> CreateDemoFeedsWithProductInfoAsync(Guid pigPenId)
    {
        var result = new FeedImportResult { TotalTransactions = 1 };

        try
        {
            // Create hardcoded feed records with Thai product information
            var demoFeeds = new[]
            {
                new Feed
                {
                    Id = Guid.NewGuid(),
                    PigPenId = pigPenId,
                    ProductType = "อาหารสัตว์",
                    ProductCode = "PK64000158",
                    ProductName = "เจ็ท 105 หมูเล็ก 6-15 กก.",
                    InvoiceNumber = "INV-2024-001",
                    Quantity = 50,
                    UnitPrice = 755.00m,
                    TotalPrice = 37750.00m,
                    FeedDate = DateTime.Now.AddDays(-2),
                    ExternalReference = "POSPOS-INV-2024-001-PK64000158",
                    Notes = "อาหารสัตว์คุณภาพสูง, สำหรับหมูในช่วงอายุที่กำหนด",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new Feed
                {
                    Id = Guid.NewGuid(),
                    PigPenId = pigPenId,
                    ProductType = "อาหารสัตว์",
                    ProductCode = "PK64000159",
                    ProductName = "เจ็ท 108 หมูนม 15-25 กก.",
                    InvoiceNumber = "INV-2024-002",
                    Quantity = 75,
                    UnitPrice = 650.00m,
                    TotalPrice = 48750.00m,
                    FeedDate = DateTime.Now.AddDays(-5),
                    ExternalReference = "POSPOS-INV-2024-002-PK64000159",
                    Notes = "อาหารสัตว์สำหรับหมูนม",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new Feed
                {
                    Id = Guid.NewGuid(),
                    PigPenId = pigPenId,
                    ProductType = "อาหารสัตว์",
                    ProductCode = "PK64000160",
                    ProductName = "เจ็ท 110 หมู 25-40 กก.",
                    InvoiceNumber = "INV-2024-003",
                    Quantity = 100,
                    UnitPrice = 595.00m,
                    TotalPrice = 59500.00m,
                    FeedDate = DateTime.Now.AddDays(-7),
                    ExternalReference = "POSPOS-INV-2024-003-PK64000160",
                    Notes = "อาหารสัตว์สำหรับหมูโต",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            };

            foreach (var feed in demoFeeds)
            {
                await _feedRepository.CreateAsync(feed);
                result.TotalFeedItems++;
            }

            result.SuccessfulImports = 1;
            result.ImportedFeeds.Add(new ImportedFeedSummary
            {
                InvoiceCode = "DEMO-BATCH",
                CustomerName = "Demo Customer",
                PigPenCode = "DEMO-PEN",
                FeedItemsCount = demoFeeds.Length,
                TotalAmount = demoFeeds.Sum(f => f.TotalPrice),
                ImportDate = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            result.FailedImports = 1;
            result.Errors.Add($"Demo feed creation error: {ex.Message}");
        }

        return result;
    }
}
