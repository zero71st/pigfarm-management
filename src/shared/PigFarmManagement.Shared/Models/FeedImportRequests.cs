using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Request for importing feed data from JSON content
/// </summary>
public record FeedImportJsonRequest(string JsonContent);

/// <summary>
/// Request for importing feed data by date range
/// </summary>
public record FeedImportDateRangeRequest(DateTime FromDate, DateTime ToDate);

/// <summary>
/// Request for importing feed data for a specific pig pen
/// </summary>
public record FeedImportForPigPenRequest(Guid PigPenId, List<PosPosTransaction> Transactions);

/// <summary>
/// Request for getting feed data by customer and date range
/// </summary>
public record FeedDataByCustomerDateRangeRequest(string CustomerCode, DateTime FromDate, DateTime ToDate);