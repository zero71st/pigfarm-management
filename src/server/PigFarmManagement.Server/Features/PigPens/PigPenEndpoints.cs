using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.PigPens;

public static class PigPenEndpoints
{
    public static IEndpointRouteBuilder MapPigPenEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/pigpens").WithTags("PigPens");

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
}
