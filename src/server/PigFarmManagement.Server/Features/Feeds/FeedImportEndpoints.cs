using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PigFarmManagement.Shared.Models;
using PigFarmManagement.Shared.Contracts;

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

        group.MapGet("/pospos/mock", GetMockPosPosFeedData)
            .WithName("GetMockPosPosFeedData")
            .WithSummary("Get mock POSPOS feed data for testing");

        group.MapPost("/pospos/mock/import", ImportMockPosPosFeedData)
            .WithName("ImportMockPosPosFeedData")
            .WithSummary("Import mock POSPOS feed data for testing");

        group.MapPost("/pospos/pigpen/{pigPenId:guid}", ImportPosPosFeedForPigPen)
            .WithName("ImportPosPosFeedForPigPen")
            .WithSummary("Import POSPOS feed data for a specific pig pen");

        group.MapGet("/pospos/customer/{customerCode}", GetPosPosFeedByCustomer)
            .WithName("GetPosPosFeedByCustomer")
            .WithSummary("Get POSPOS feed transactions by customer code");
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

    private static async Task<IResult> GetMockPosPosFeedData(IFeedImportService feedImportService)
    {
        try
        {
            var mockData = await feedImportService.GetMockPosPosFeedDataAsync();
            return Results.Ok(mockData);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get mock data: {ex.Message}");
        }
    }

    private static async Task<IResult> ImportMockPosPosFeedData(IFeedImportService feedImportService)
    {
        try
        {
            var mockData = await feedImportService.GetMockPosPosFeedDataAsync();
            var result = await feedImportService.ImportPosPosFeedDataAsync(mockData);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Mock import failed: {ex.Message}");
        }
    }

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
}

public record ImportJsonRequest(string JsonContent);
