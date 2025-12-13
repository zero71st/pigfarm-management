using PigFarmManagement.Shared.Models;
using Microsoft.EntityFrameworkCore;
using PigFarmManagement.Server.Infrastructure.Data.Repositories;
using PigFarmManagement.Server.Infrastructure.Data;

namespace PigFarmManagement.Server.Features.PigPens;

public static class PigPenEndpoints
{
    public static IEndpointRouteBuilder MapPigPenEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/pigpens").WithTags("PigPens").RequireAuthorization();

        group.MapGet("/", GetAllPigPens)
            .WithName("GetAllPigPens");

        group.MapGet("/{id:guid}", GetPigPenById)
            .WithName("GetPigPenById");

        group.MapPost("/", CreatePigPen)
            .WithName("CreatePigPen");

        group.MapPut("/{id:guid}", UpdatePigPen)
            .WithName("UpdatePigPen");

        group.MapDelete("/{id:guid}", DeletePigPen)
            .WithName("DeletePigPen");

        // Pig pen detail endpoints
        group.MapGet("/{id:guid}/summary", GetPigPenSummary)
            .WithName("GetPigPenDetailSummary");

        group.MapGet("/{id:guid}/feeds", GetPigPenFeeds)
            .WithName("GetPigPenFeeds");

        group.MapGet("/{id:guid}/deposits", GetPigPenDeposits)
            .WithName("GetPigPenDeposits");

        group.MapGet("/{id:guid}/harvests", GetPigPenHarvests)
            .WithName("GetPigPenHarvests");

        group.MapGet("/{id:guid}/formula-assignments", GetPigPenFormulaAssignments)
            .WithName("GetPigPenFormulaAssignments");

        // CRUD for deposits
        group.MapPost("/{id:guid}/deposits", CreateDeposit)
            .WithName("CreateDeposit");

        group.MapPut("/{id:guid}/deposits/{depositId:guid}", UpdateDeposit)
            .WithName("UpdateDeposit");

        group.MapDelete("/{id:guid}/deposits/{depositId:guid}", DeleteDeposit)
            .WithName("DeleteDeposit");

        // CRUD for harvests
        group.MapPost("/{id:guid}/harvests", CreateHarvest)
            .WithName("CreateHarvest");

        group.MapPut("/{id:guid}/harvests/{harvestId:guid}", UpdateHarvest)
            .WithName("UpdateHarvest");

        group.MapDelete("/{id:guid}/harvests/{harvestId:guid}", DeleteHarvest)
            .WithName("DeleteHarvest");

        // Special actions
        group.MapPost("/{id:guid}/force-close", ForceClosePigPen)
            .WithName("ForceClosePigPen");

        group.MapPost("/{id:guid}/reopen", ReopenPigPen)
            .WithName("ReopenPigPen");

        group.MapPost("/{id:guid}/set-appointment", SetAppointment)
            .WithName("SetAppointment");

        group.MapPost("/{id:guid}/regenerate-assignments", RegenerateFormulaAssignments)
            .WithName("RegenerateFormulaAssignments");

        // Delete invoice by reference code
        group.MapDelete("/{pigPenId:guid}/invoices/{invoiceReferenceCode}", DeleteInvoiceByReference)
            .WithName("DeleteInvoiceByReference");

        // Get last feed import dates for all pig pens (batch)
        group.MapGet("/last-feed-imports", GetLastFeedImports)
            .WithName("GetLastFeedImports");

        // Get used product usages for a pig pen (for recalculation dialog)
        group.MapGet("/{id:guid}/used-product-usages", GetUsedProductUsages)
            .WithName("GetUsedProductUsages");

        return builder;
    }

    private static async Task<IResult> GetAllPigPens(IPigPenService pigPenService)
    {
        try
        {
            var pigPens = await pigPenService.GetAllPigPensAsync();
            return Results.Ok(pigPens);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving pig pens: {ex.Message}");
        }
    }

    private static async Task<IResult> GetPigPenById(Guid id, IPigPenService pigPenService)
    {
        try
        {
            var pigPen = await pigPenService.GetPigPenByIdAsync(id);
            return pigPen == null ? Results.NotFound() : Results.Ok(pigPen);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving pig pen: {ex.Message}");
        }
    }

    private static async Task<IResult> CreatePigPen(PigPenCreateDto dto, IPigPenService pigPenService)
    {
        try
        {
            var createdPigPen = await pigPenService.CreatePigPenAsync(dto);
            return Results.Created($"/api/pigpens/{createdPigPen.Id}", createdPigPen);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (DbUpdateException dbEx) when (dbEx.InnerException != null && dbEx.InnerException.Message.Contains("duplicate key value"))
        {
            // PostgreSQL unique violation -> PenCode already exists
            return Results.Conflict(new { message = "A pig pen with the same PenCode already exists." });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error creating pig pen: {ex.Message}");
        }
    }

    private static async Task<IResult> UpdatePigPen(Guid id, PigPenUpdateDto dto, IPigPenService pigPenService, HttpContext context)
    {
        try
        {
            // Load existing pig pen
            var existing = await pigPenService.GetPigPenByIdAsync(id);
            if (existing == null)
            {
                return Results.NotFound();
            }

            // Merge fields from DTO (only update when DTO field is not null)
            var updatedPigPen = existing with
            {
                PenCode = dto.PenCode ?? existing.PenCode,
                PigQty = dto.PigQty ?? existing.PigQty,
                RegisterDate = dto.RegisterDate ?? existing.RegisterDate,
                ActHarvestDate = dto.ActHarvestDate ?? existing.ActHarvestDate,
                EstimatedHarvestDate = dto.EstimatedHarvestDate ?? existing.EstimatedHarvestDate,
                Type = dto.Type ?? existing.Type,
                SelectedBrand = dto.SelectedBrand ?? existing.SelectedBrand,
                DepositPerPig = dto.DepositPerPig ?? existing.DepositPerPig,
                Note = dto.Note ?? existing.Note,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            // T009: Extract user context for logging
            var userId = context.User.FindFirst("user_id")?.Value;
            
            var result = await pigPenService.UpdatePigPenAsync(updatedPigPen, userId, dto.PreserveProductCodes);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error updating pig pen: {ex.Message}");
        }
    }

    private static async Task<IResult> DeletePigPen(Guid id, IPigPenService pigPenService)
    {
        try
        {
            await pigPenService.DeletePigPenAsync(id);
            return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error deleting pig pen: {ex.Message}");
        }
    }

    // Pig pen detail endpoints
    private static async Task<IResult> GetPigPenSummary(Guid id, IPigPenDetailService pigPenDetailService)
    {
        try
        {
            var summary = await pigPenDetailService.GetPigPenSummaryAsync(id);
            return summary == null ? Results.NotFound() : Results.Ok(summary);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving pig pen summary: {ex.Message}");
        }
    }

    private static async Task<IResult> GetPigPenFeeds(Guid id, IPigPenDetailService pigPenDetailService)
    {
        try
        {
            var feeds = await pigPenDetailService.GetPigPenFeedsAsync(id);
            return Results.Ok(feeds);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving pig pen feeds: {ex.Message}");
        }
    }

    private static async Task<IResult> GetPigPenDeposits(Guid id, IPigPenDetailService pigPenDetailService)
    {
        try
        {
            var deposits = await pigPenDetailService.GetPigPenDepositsAsync(id);
            return Results.Ok(deposits);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving pig pen deposits: {ex.Message}");
        }
    }

    private static async Task<IResult> GetPigPenHarvests(Guid id, IPigPenDetailService pigPenDetailService)
    {
        try
        {
            var harvests = await pigPenDetailService.GetPigPenHarvestsAsync(id);
            return Results.Ok(harvests);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving pig pen harvests: {ex.Message}");
        }
    }

    // Deposit CRUD
    private static async Task<IResult> CreateDeposit(Guid id, DepositCreateDto dto, IPigPenDetailService pigPenDetailService, IPigPenService pigPenService, ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("CreateDeposit called for PigPen {PigPenId} with Amount={Amount}, Date={Date}",
                id, dto.Amount, dto.Date);
            // Guard: disallow mutations when pig pen is force-closed
            var pigPen = await pigPenService.GetPigPenByIdAsync(id);
            if (pigPen == null) return Results.NotFound("Pig pen not found");
            if (pigPen.IsCalculationLocked) return Results.BadRequest("Pig pen is force-closed; mutations are not allowed.");

            var deposit = await pigPenDetailService.CreateDepositAsync(id, dto);
            
            logger.LogInformation("Deposit created successfully with Id={DepositId}", deposit.Id);
            return Results.Created($"/api/pigpens/{id}/deposits/{deposit.Id}", deposit);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Business validation error creating deposit for PigPen {PigPenId}", id);
            return Results.BadRequest(new { error = ex.Message, type = "validation" });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
            logger.LogError(dbEx, "Database error creating deposit for PigPen {PigPenId}: {InnerMessage}", id, innerMessage);
            return Results.Problem(
                detail: $"Database error: {innerMessage}",
                title: "Database Error",
                statusCode: 500
            );
        }
        catch (Exception ex)
        {
            var fullError = ex.InnerException != null 
                ? $"{ex.Message} -> {ex.InnerException.Message}" 
                : ex.Message;
            logger.LogError(ex, "Unexpected error creating deposit for PigPen {PigPenId}: {FullError}", id, fullError);
            return Results.Problem(
                detail: fullError,
                title: "Error creating deposit",
                statusCode: 500
            );
        }
    }

    private static async Task<IResult> UpdateDeposit(Guid id, Guid depositId, DepositUpdateDto dto, IPigPenDetailService pigPenDetailService, IPigPenService pigPenService, ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("UpdateDeposit called for PigPen {PigPenId}, Deposit {DepositId} with Amount={Amount}, Date={Date}",
                id, depositId, dto.Amount, dto.Date);
            // Guard: disallow mutations when pig pen is force-closed
            var pigPen = await pigPenService.GetPigPenByIdAsync(id);
            if (pigPen == null) return Results.NotFound("Pig pen not found");
            if (pigPen.IsCalculationLocked) return Results.BadRequest("Pig pen is force-closed; mutations are not allowed.");

            var updatedDeposit = await pigPenDetailService.UpdateDepositAsync(id, depositId, dto);
            
            logger.LogInformation("Deposit {DepositId} updated successfully", depositId);
            return Results.Ok(updatedDeposit);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Business validation error updating deposit {DepositId} for PigPen {PigPenId}", depositId, id);
            return Results.BadRequest(new { error = ex.Message, type = "validation" });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
            logger.LogError(dbEx, "Database error updating deposit {DepositId} for PigPen {PigPenId}: {InnerMessage}", depositId, id, innerMessage);
            return Results.Problem(
                detail: $"Database error: {innerMessage}",
                title: "Database Error",
                statusCode: 500
            );
        }
        catch (Exception ex)
        {
            var fullError = ex.InnerException != null 
                ? $"{ex.Message} -> {ex.InnerException.Message}" 
                : ex.Message;
            logger.LogError(ex, "Unexpected error updating deposit {DepositId} for PigPen {PigPenId}: {FullError}", depositId, id, fullError);
            return Results.Problem(
                detail: fullError,
                title: "Error updating deposit",
                statusCode: 500
            );
        }
    }

    private static async Task<IResult> DeleteDeposit(Guid id, Guid depositId, IPigPenDetailService pigPenDetailService, IPigPenService pigPenService)
    {
        try
        {
            var pigPen = await pigPenService.GetPigPenByIdAsync(id);
            if (pigPen == null) return Results.NotFound("Pig pen not found");
            if (pigPen.IsCalculationLocked) return Results.BadRequest("Pig pen is force-closed; mutations are not allowed.");

            await pigPenDetailService.DeleteDepositAsync(id, depositId);
            return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error deleting deposit: {ex.Message}");
        }
    }

    // Harvest CRUD
    private static async Task<IResult> CreateHarvest(Guid id, HarvestCreateDto dto, IPigPenDetailService pigPenDetailService, IPigPenService pigPenService, ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("CreateHarvest called for PigPen {PigPenId} with Date={HarvestDate}, PigCount={PigCount}, TotalWeight={TotalWeight}, PricePerKg={PricePerKg}",
                id, dto.HarvestDate, dto.PigCount, dto.TotalWeight, dto.SalePricePerKg);
            // Guard: disallow mutations when pig pen is force-closed
            var pigPen = await pigPenService.GetPigPenByIdAsync(id);
            if (pigPen == null) return Results.NotFound("Pig pen not found");
            if (pigPen.IsCalculationLocked) return Results.BadRequest("Pig pen is force-closed; mutations are not allowed.");

            var harvest = await pigPenDetailService.CreateHarvestAsync(id, dto);
            
            logger.LogInformation("Harvest created successfully with Id={HarvestId}", harvest.Id);
            return Results.Created($"/api/pigpens/{id}/harvests/{harvest.Id}", harvest);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Business validation error creating harvest for PigPen {PigPenId}", id);
            return Results.BadRequest(new { error = ex.Message, type = "validation" });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
            logger.LogError(dbEx, "Database error creating harvest for PigPen {PigPenId}: {InnerMessage}", id, innerMessage);
            return Results.Problem(
                detail: $"Database error: {innerMessage}",
                title: "Database Error",
                statusCode: 500
            );
        }
        catch (Exception ex)
        {
            var fullError = ex.InnerException != null 
                ? $"{ex.Message} -> {ex.InnerException.Message}" 
                : ex.Message;
            logger.LogError(ex, "Unexpected error creating harvest for PigPen {PigPenId}: {FullError}", id, fullError);
            return Results.Problem(
                detail: fullError,
                title: "Error creating harvest",
                statusCode: 500
            );
        }
    }

    private static async Task<IResult> UpdateHarvest(Guid id, Guid harvestId, HarvestUpdateDto dto, IPigPenDetailService pigPenDetailService, IPigPenService pigPenService, ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("UpdateHarvest called for PigPen {PigPenId}, Harvest {HarvestId} with Date={HarvestDate}, PigCount={PigCount}, TotalWeight={TotalWeight}, PricePerKg={PricePerKg}",
                id, harvestId, dto.HarvestDate, dto.PigCount, dto.TotalWeight, dto.SalePricePerKg);
            // Guard: disallow mutations when pig pen is force-closed
            var pigPen = await pigPenService.GetPigPenByIdAsync(id);
            if (pigPen == null) return Results.NotFound("Pig pen not found");
            if (pigPen.IsCalculationLocked) return Results.BadRequest("Pig pen is force-closed; mutations are not allowed.");

            var updatedHarvest = await pigPenDetailService.UpdateHarvestAsync(id, harvestId, dto);
            
            logger.LogInformation("Harvest {HarvestId} updated successfully", harvestId);
            return Results.Ok(updatedHarvest);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Business validation error updating harvest {HarvestId} for PigPen {PigPenId}", harvestId, id);
            return Results.BadRequest(new { error = ex.Message, type = "validation" });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
            logger.LogError(dbEx, "Database error updating harvest {HarvestId} for PigPen {PigPenId}: {InnerMessage}", harvestId, id, innerMessage);
            return Results.Problem(
                detail: $"Database error: {innerMessage}",
                title: "Database Error",
                statusCode: 500
            );
        }
        catch (Exception ex)
        {
            var fullError = ex.InnerException != null 
                ? $"{ex.Message} -> {ex.InnerException.Message}" 
                : ex.Message;
            logger.LogError(ex, "Unexpected error updating harvest {HarvestId} for PigPen {PigPenId}: {FullError}", harvestId, id, fullError);
            return Results.Problem(
                detail: fullError,
                title: "Error updating harvest",
                statusCode: 500
            );
        }
    }

    private static async Task<IResult> DeleteHarvest(Guid id, Guid harvestId, IPigPenDetailService pigPenDetailService, IPigPenService pigPenService)
    {
        try
        {
            var pigPen = await pigPenService.GetPigPenByIdAsync(id);
            if (pigPen == null) return Results.NotFound("Pig pen not found");
            if (pigPen.IsCalculationLocked) return Results.BadRequest("Pig pen is force-closed; mutations are not allowed.");

            await pigPenDetailService.DeleteHarvestAsync(id, harvestId);
            return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error deleting harvest: {ex.Message}");
        }
    }

    private static async Task<IResult> GetPigPenFormulaAssignments(Guid id, IPigPenService pigPenService)
    {
        try
        {
            var assignments = await pigPenService.GetFormulaAssignmentsAsync(id);
            return Results.Ok(assignments);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving formula assignments: {ex.Message}");
        }
    }

    private static async Task<IResult> ForceClosePigPen(Guid id, PigPenForceCloseRequest request, IPigPenService pigPenService)
    {
        try
        {
            // Validate that the ID in the route matches the ID in the request body
            if (id != request.PigPenId)
            {
                return Results.BadRequest("Pig pen ID mismatch between route and request body");
            }

            var pigPen = await pigPenService.GetPigPenByIdAsync(id);
            if (pigPen == null)
            {
                return Results.NotFound("Pig pen not found");
            }

            // Force close the pig pen by calling the service method
            var closedPigPen = await pigPenService.ForceClosePigPenAsync(id);
            return Results.Ok(closedPigPen);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error force closing pig pen: {ex.Message}");
        }
    }

    private static async Task<IResult> ReopenPigPen(Guid id, IPigPenService pigPenService)
    {
        try
        {
            var pigPen = await pigPenService.GetPigPenByIdAsync(id);
            if (pigPen == null)
            {
                return Results.NotFound("Pig pen not found");
            }

            if (!pigPen.IsCalculationLocked)
            {
                return Results.BadRequest("Pig pen is not closed, cannot reopen");
            }

            // Reopen the pig pen by calling the service method
            var reopenedPigPen = await pigPenService.ReopenPigPenAsync(id);
            return Results.Ok(reopenedPigPen);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error reopening pig pen: {ex.Message}");
        }
    }

    private static async Task<IResult> SetAppointment(Guid id, SetAppointmentDto dto, IPigPenService pigPenService)
    {
        try
        {
            var pigPen = await pigPenService.GetPigPenByIdAsync(id);
            if (pigPen == null)
            {
                return Results.NotFound("Pig pen not found");
            }

            // Validate appointment date only if one is provided (must be after register date)
            if (dto.AppointmentDate.HasValue && dto.AppointmentDate.Value <= pigPen.RegisterDate)
            {
                return Results.BadRequest("Appointment date must be after the registration date");
            }

            // Set the appointment date (ActHarvestDate) - can be null to clear/cancel appointment
            var updatedPigPen = await pigPenService.SetAppointmentAsync(id, dto.AppointmentDate);
            return Results.Ok(updatedPigPen);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error setting appointment: {ex.Message}");
        }
    }

    private static async Task<IResult> RegenerateFormulaAssignments(Guid id, IPigPenService pigPenService)
    {
        try
        {
            var result = await pigPenService.RegenerateFormulaAssignmentsAsync(id);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error regenerating formula assignments: {ex.Message}");
        }
    }

    private static async Task<IResult> DeleteInvoiceByReference(
        Guid pigPenId, 
        string invoiceReferenceCode, 
        IFeedRepository feedRepository,
        IPigPenService pigPenService,
        ILogger<Program> logger)
    {
        try
        {
            // Guard: disallow mutations when pig pen is force-closed
            var pigPen = await pigPenService.GetPigPenByIdAsync(pigPenId);
            if (pigPen == null) return Results.NotFound(new { error = "Pig pen not found" });
            if (pigPen.IsCalculationLocked) return Results.BadRequest(new { error = "Pig pen is force-closed; mutations are not allowed." });
            // Validate invoice reference code
            if (string.IsNullOrWhiteSpace(invoiceReferenceCode))
            {
                return Results.BadRequest(new { error = "Invoice reference code cannot be null or empty" });
            }

            // Delete feeds with matching invoice reference code
            var deletedCount = await feedRepository.DeleteByInvoiceReferenceAsync(pigPenId, invoiceReferenceCode);

            // Return 404 if no items found
            if (deletedCount == 0)
            {
                return Results.NotFound(new 
                { 
                    error = $"No feed items found with invoice reference code '{invoiceReferenceCode}' for pig pen '{pigPenId}'" 
                });
            }

            // Log deletion
            logger.LogInformation(
                "Deleted {DeletedCount} feed items for invoice {InvoiceReferenceCode} in pig pen {PigPenId}",
                deletedCount, invoiceReferenceCode, pigPenId);

            // Return success response
            var response = new DeleteInvoiceResponse(
                deletedCount,
                invoiceReferenceCode,
                $"Successfully deleted {deletedCount} feed items for invoice {invoiceReferenceCode}"
            );

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting invoice {InvoiceReferenceCode} for pig pen {PigPenId}", 
                invoiceReferenceCode, pigPenId);
            return Results.Problem($"Error deleting invoice: {ex.Message}");
        }
    }

    private static async Task<IResult> GetLastFeedImports(PigFarmDbContext db)
    {
        try
        {
            var now = DateTime.UtcNow.Date;
            
            // Single query: GROUP BY PigPenId and get MAX(FeedDate) for each (invoice date from POSPOS)
            var results = await db.Feeds
                .GroupBy(f => f.PigPenId)
                .Select(g => new
                {
                    PigPenId = g.Key,
                    LastInvoiceDate = (DateTime?)g.Max(x => x.FeedDate)
                })
                .ToListAsync();

            // Map to DTOs with calculated days
            var dtos = results.Select(r => new LastFeedImportDateDto(
                r.PigPenId,
                r.LastInvoiceDate,
                r.LastInvoiceDate.HasValue ? (int?)(now - r.LastInvoiceDate.Value.Date).Days : null
            )).ToList();

            return Results.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving last feed imports: {ex.Message}");
        }
    }

    private static async Task<IResult> GetUsedProductUsages(Guid id, IPigPenService pigPenService)
    {
        try
        {
            var usages = await pigPenService.GetUsedProductUsagesAsync(id);
            return Results.Ok(usages);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving used product usages: {ex.Message}");
        }
    }
}
