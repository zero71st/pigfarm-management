using PigFarmManagement.Shared.Models;
using Microsoft.EntityFrameworkCore;

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

        group.MapPost("/{id:guid}/regenerate-assignments", RegenerateFormulaAssignments)
            .WithName("RegenerateFormulaAssignments");

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

    private static async Task<IResult> UpdatePigPen(Guid id, PigPen pigPen, IPigPenService pigPenService)
    {
        try
        {
            // Ensure the ID in the URL matches the pig pen ID
            if (id != pigPen.Id)
            {
                return Results.BadRequest("Pig pen ID mismatch");
            }

            var updatedPigPen = await pigPenService.UpdatePigPenAsync(pigPen);
            return Results.Ok(updatedPigPen);
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
    private static async Task<IResult> CreateDeposit(Guid id, DepositCreateDto dto, IPigPenDetailService pigPenDetailService)
    {
        try
        {
            var deposit = await pigPenDetailService.CreateDepositAsync(id, dto);
            return Results.Created($"/api/pigpens/{id}/deposits/{deposit.Id}", deposit);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error creating deposit: {ex.Message}");
        }
    }

    private static async Task<IResult> UpdateDeposit(Guid id, Guid depositId, DepositUpdateDto dto, IPigPenDetailService pigPenDetailService)
    {
        try
        {
            var updatedDeposit = await pigPenDetailService.UpdateDepositAsync(id, depositId, dto);
            return Results.Ok(updatedDeposit);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error updating deposit: {ex.Message}");
        }
    }

    private static async Task<IResult> DeleteDeposit(Guid id, Guid depositId, IPigPenDetailService pigPenDetailService)
    {
        try
        {
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
    private static async Task<IResult> CreateHarvest(Guid id, HarvestCreateDto dto, IPigPenDetailService pigPenDetailService)
    {
        try
        {
            var harvest = await pigPenDetailService.CreateHarvestAsync(id, dto);
            return Results.Created($"/api/pigpens/{id}/harvests/{harvest.Id}", harvest);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error creating harvest: {ex.Message}");
        }
    }

    private static async Task<IResult> UpdateHarvest(Guid id, Guid harvestId, HarvestUpdateDto dto, IPigPenDetailService pigPenDetailService)
    {
        try
        {
            var updatedHarvest = await pigPenDetailService.UpdateHarvestAsync(id, harvestId, dto);
            return Results.Ok(updatedHarvest);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error updating harvest: {ex.Message}");
        }
    }

    private static async Task<IResult> DeleteHarvest(Guid id, Guid harvestId, IPigPenDetailService pigPenDetailService)
    {
        try
        {
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

    private static async Task<IResult> ForceClosePigPen(Guid id, IPigPenService pigPenService)
    {
        try
        {
            var pigPen = await pigPenService.GetPigPenByIdAsync(id);
            if (pigPen == null)
            {
                return Results.NotFound("Pig pen not found");
            }

            // Check if pig pen is already closed
            if (pigPen.ActHarvestDate.HasValue)
            {
                return Results.BadRequest("Pig pen is already closed");
            }

            // Force close the pig pen by setting actual harvest date to today
            var closedPigPen = pigPen with 
            { 
                ActHarvestDate = DateTime.Today,
                IsCalculationLocked = true,
                UpdatedAt = DateTime.UtcNow
            };

            var updatedPigPen = await pigPenService.UpdatePigPenAsync(closedPigPen);
            return Results.Ok(updatedPigPen);
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
}
