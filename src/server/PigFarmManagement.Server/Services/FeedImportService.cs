using System.Text.Json;
using PigFarmManagement.Shared.Models;
using PigFarmManagement.Shared.Contracts;

namespace PigFarmManagement.Server.Services;

public class FeedImportService : IFeedImportService
{
    private readonly List<PigPen> _pigPens = new();
    private readonly List<Customer> _customers = new();
    private readonly List<Feed> _feeds = new();

    public FeedImportService()
    {
        InitializeData();
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

        // Find the specific pig pen
        var pigPen = _pigPens.FirstOrDefault(p => p.Id == pigPenId);
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

    public async Task<List<PosPosFeedTransaction>> GetMockPosPosFeedDataAsync()
    {
        // Mock data based on the POSPOS structure you provided
        var mockTransactions = new List<PosPosFeedTransaction>
        {
            new()
            {
                Id = "66fc65e4a2e61a30b0dcb111",
                Code = "INV-2024-001",
                Timestamp = DateTime.Now.AddHours(-2),
                OrderList = new()
                {
                    new PosPosFeedItem
                    {
                        Stock = 50,
                        Name = "Premium Pig Feed 20kg",
                        Price = 850.00m,
                        SpecialPrice = 800.00m,
                        Code = "PF-20KG-001",
                        TotalPriceIncludeDiscount = 800.00m,
                        NoteInOrder = new() { "High protein content", "For growing pigs" }
                    },
                    new PosPosFeedItem
                    {
                        Stock = 25,
                        Name = "Vitamin Supplement Mix",
                        Price = 320.00m,
                        SpecialPrice = 300.00m,
                        Code = "VS-MIX-002",
                        TotalPriceIncludeDiscount = 300.00m,
                        NoteInOrder = new() { "Essential vitamins", "Mix with main feed" }
                    }
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
                SubTotal = 1100.00m,
                GrandTotal = 1100.00m,
                Status = "completed"
            },
            new()
            {
                Id = "66fc65e4a2e61a30b0dcb222",
                Code = "INV-2024-002", 
                Timestamp = DateTime.Now.AddHours(-1),
                OrderList = new()
                {
                    new PosPosFeedItem
                    {
                        Stock = 100,
                        Name = "Starter Feed for Piglets",
                        Price = 950.00m,
                        SpecialPrice = 900.00m,
                        Code = "SF-PIG-003",
                        TotalPriceIncludeDiscount = 900.00m,
                        NoteInOrder = new() { "For 3-8 weeks old piglets", "Easy to digest" }
                    }
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
                SubTotal = 900.00m,
                GrandTotal = 900.00m,
                Status = "completed"
            },
            new()
            {
                Id = "66fc65e4a2e61a30b0dcb333",
                Code = "INV-2024-003", 
                Timestamp = DateTime.Now.AddHours(-3),
                OrderList = new()
                {
                    new PosPosFeedItem
                    {
                        Stock = 30,
                        Name = "Finisher Feed Premium",
                        Price = 780.00m,
                        SpecialPrice = 750.00m,
                        Code = "FF-PREM-004",
                        TotalPriceIncludeDiscount = 750.00m,
                        NoteInOrder = new() { "For final growth phase", "High energy content" }
                    }
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
                SubTotal = 750.00m,
                GrandTotal = 750.00m,
                Status = "completed"
            },
            new()
            {
                Id = "66fc65e4a2e61a30b0dcb444",
                Code = "INV-2024-004", 
                Timestamp = DateTime.Now.AddMinutes(-30),
                OrderList = new()
                {
                    new PosPosFeedItem
                    {
                        Stock = 40,
                        Name = "Grower Feed Standard",
                        Price = 680.00m,
                        SpecialPrice = 650.00m,
                        Code = "GF-STD-005",
                        TotalPriceIncludeDiscount = 650.00m,
                        NoteInOrder = new() { "For medium-sized pigs", "Balanced nutrition" }
                    },
                    new PosPosFeedItem
                    {
                        Stock = 15,
                        Name = "Mineral Supplement",
                        Price = 280.00m,
                        SpecialPrice = 260.00m,
                        Code = "MS-MIX-006",
                        TotalPriceIncludeDiscount = 260.00m,
                        NoteInOrder = new() { "Essential minerals", "Add to feed" }
                    }
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
                SubTotal = 910.00m,
                GrandTotal = 910.00m,
                Status = "completed"
            }
        };

        return await Task.FromResult(mockTransactions);
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
                ProductType = MapProductNameToType(item.Name),
                Quantity = item.Stock,
                UnitPrice = item.SpecialPrice > 0 ? item.SpecialPrice : item.Price,
                TotalPrice = item.TotalPriceIncludeDiscount,
                FeedDate = transaction.Timestamp,
                Notes = string.Join(", ", item.NoteInOrder),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                ExternalReference = $"POSPOS-{transaction.Code}-{item.Code}"
            };

            _feeds.Add(feed);
            result.TotalFeedItems++;
        }

        result.ImportedFeeds.Add(importedSummary);
        await Task.CompletedTask;
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
                ProductType = MapProductNameToType(item.Name),
                Quantity = item.Stock,
                UnitPrice = item.SpecialPrice > 0 ? item.SpecialPrice : item.Price,
                TotalPrice = item.TotalPriceIncludeDiscount,
                FeedDate = transaction.Timestamp,
                Notes = string.Join(", ", item.NoteInOrder),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                ExternalReference = $"POSPOS-{transaction.Code}-{item.Code}"
            };

            _feeds.Add(feed);
            result.TotalFeedItems++;
        }

        result.ImportedFeeds.Add(importedSummary);
        await Task.CompletedTask;
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
                DateTime.Now.AddDays(-10),
                DateTime.Now.AddDays(-1)
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
                DateTime.Now.AddDays(-8),
                DateTime.Now.AddDays(-2)
            )
        });
    }
}
