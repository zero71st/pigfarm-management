using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PigFarmManagement.Shared.Models;
using PigFarmManagement.Shared.Contracts;
using PigFarmManagement.Server.Services;

namespace PigFarmManagement.Server.Features.Feeds;

public static class FeedImportEndpoints
{
    public static void MapFeedImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/feeds/import").WithTags("Feed Import");

        group.MapPost("/pospos", ImportPosPosFeedData)
            .WithName("ImportPosPosFeedData")
            .WithSummary("Import feed data from POSPOS transactions");

        group.MapPost("/pospos/json", ImportPosPosFeedFromJson)
            .WithName("ImportPosPosFeedFromJson")
            .WithSummary("Import feed data from POSPOS JSON string");

        group.MapPost("/pospos/pigpen/{pigPenId:guid}", ImportPosPosFeedForPigPen)
            .WithName("ImportPosPosFeedForPigPen")
            .WithSummary("Import POSPOS feed data for a specific pig pen");

        group.MapPost("/demo/pigpen/{pigPenId:guid}", CreateDemoFeedsForPigPen)
            .WithName("CreateDemoFeedsForPigPen")
            .WithSummary("Create demo feed records with complete product information for testing");

        group.MapGet("/pospos/customer/{customerCode}", GetPosPosFeedByCustomer)
            .WithName("GetPosPosFeedByCustomer")
            .WithSummary("Get POSPOS feed transactions by customer code");

        group.MapGet("/pospos/daterange", GetPosPosFeedByDateRange)
            .WithName("GetPosPosFeedByDateRange")
            .WithSummary("Get POSPOS feed transactions by date range");

        group.MapGet("/pospos/customer/{customerCode}/daterange", GetPosPosFeedByCustomerAndDateRange)
            .WithName("GetPosPosFeedByCustomerAndDateRange")
            .WithSummary("Get POSPOS feed transactions by customer code and date range");

        // Debug: raw fetch from POSPOS for a customer (no import)
        group.MapGet("/pospos/customer/{customerCode}/raw", GetRawPosPosByCustomer)
            .WithName("GetRawPosPosByCustomer")
            .WithSummary("Fetch raw POSPOS transactions for a customer (no import). Optional query params: from, to (ISO8601)");

        group.MapPost("/pospos/daterange/import", ImportPosPosFeedByDateRange)
            .WithName("ImportPosPosFeedByDateRange")
            .WithSummary("Import POSPOS feed data by date range");

        // Single-call endpoint: fetch from POSPOS by date range and import into the system
        group.MapPost("/pospos/fetch-and-import", FetchAndImportPosPosByDateRange)
            .WithName("FetchAndImportPosPosByDateRange")
            .WithSummary("Fetch POSPOS transactions from POSPOS by date range and import them as feeds");
    }

    private static async Task<IResult> ImportPosPosFeedData(
        [FromBody] List<PosPosFeedTransaction> transactions,
        IFeedImportService feedImportService)
    {
        try
        {
            var result = await feedImportService.ImportPosPosFeedDataAsync(transactions);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Import failed: {ex.Message}");
        }
    }

    private static async Task<IResult> ImportPosPosFeedFromJson(
        [FromBody] ImportJsonRequest request,
        IFeedImportService feedImportService)
    {
        try
        {
            var result = await feedImportService.ImportFromJsonAsync(request.JsonContent);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Import failed: {ex.Message}");
        }
    }

    // ...mock helper methods intentionally removed. Client should fetch directly from POSPOS.

    private static async Task<IResult> ImportPosPosFeedForPigPen(
        Guid pigPenId,
        [FromBody] List<PosPosFeedTransaction> transactions,
        IFeedImportService feedImportService)
    {
        try
        {
            var result = await feedImportService.ImportPosPosFeedForPigPenAsync(pigPenId, transactions);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Import failed: {ex.Message}");
        }
    }

    private static async Task<IResult> CreateDemoFeedsForPigPen(
        Guid pigPenId,
        IFeedImportService feedImportService)
    {
        try
        {
            var result = await feedImportService.CreateDemoFeedsWithProductInfoAsync(pigPenId);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Demo feed creation failed: {ex.Message}");
        }
    }

    private static async Task<IResult> GetPosPosFeedByCustomer(
        string customerCode,
        IFeedImportService feedImportService)
    {
        try
        {
            var transactions = await feedImportService.GetPosPosFeedByCustomerCodeAsync(customerCode);
            return Results.Ok(transactions);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get transactions: {ex.Message}");
        }
    }

    private static async Task<IResult> GetPosPosFeedByDateRange(
        DateTime fromDate,
        DateTime toDate,
        IFeedImportService feedImportService)
    {
        try
        {
            var transactions = await feedImportService.GetPosPosFeedByDateRangeAsync(fromDate, toDate);
            return Results.Ok(transactions);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get transactions: {ex.Message}");
        }
    }

    private static async Task<IResult> GetPosPosFeedByCustomerAndDateRange(
        string customerCode,
        DateTime fromDate,
        DateTime toDate,
        IFeedImportService feedImportService)
    {
        try
        {
            var transactions = await feedImportService.GetPosPosFeedByCustomerAndDateRangeAsync(customerCode, fromDate, toDate);
            return Results.Ok(transactions);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get transactions: {ex.Message}");
        }
    }

    private static async Task<IResult> ImportPosPosFeedByDateRange(
        [FromBody] DateRangeImportRequest request,
        IFeedImportService feedImportService)
    {
        try
        {
            var result = await feedImportService.ImportPosPosFeedByDateRangeAsync(request.FromDate, request.ToDate);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Import failed: {ex.Message}");
        }
    }

    private static async Task<IResult> GetRawPosPosByCustomer(
        string customerCode,
        [FromQuery] string? from,
        [FromQuery] string? to,
        IPosposFeedClient posposFeedClient)
    {
        try
        {
            DateTime fromDt = DateTime.UtcNow.AddDays(-90);
            DateTime toDt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var f)) fromDt = f;
            if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var t)) toDt = t;

            var transactions = await posposFeedClient.GetTransactionsByDateRangeAsync(fromDt, toDt);

            var filtered = transactions.Where(t => t.BuyerDetail != null && (
                t.BuyerDetail.Code.Equals(customerCode, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(t.BuyerDetail.KeyCardId) && t.BuyerDetail.KeyCardId.Equals(customerCode, StringComparison.OrdinalIgnoreCase))
            )).ToList();

            return Results.Ok(filtered);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to fetch raw POSPOS transactions: {ex.Message}");
        }
    }

    private static async Task<IResult> FetchAndImportPosPosByDateRange(
        [FromBody] DateRangeImportRequest request,
        IFeedImportService feedImportService,
        IPosposFeedClient posposFeedClient)
    {
        try
        {
            // Fetch transactions from POSPOS using the feed client
            var transactions = await posposFeedClient.GetTransactionsByDateRangeAsync(request.FromDate, request.ToDate);

            if (transactions == null || transactions.Count == 0)
            {
                return Results.Ok(new FeedImportResult
                {
                    TotalTransactions = 0,
                    SuccessfulImports = 0,
                    FailedImports = 0
                });
            }

            // Import fetched transactions
            var result = await feedImportService.ImportPosPosFeedDataAsync(transactions);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Fetch and import failed: {ex.Message}");
        }
    }
}

public record ImportJsonRequest(string JsonContent);
public record DateRangeImportRequest(DateTime FromDate, DateTime ToDate);
