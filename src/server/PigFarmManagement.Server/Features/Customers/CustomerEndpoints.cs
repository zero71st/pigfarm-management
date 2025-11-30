using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.Customers;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/customers").WithTags("Customers");

        // TODO: Add security authorization policies once security middleware is enabled (Feature 010)
        // For minimal APIs, use: .RequireAuthorization("PolicyName") or metadata attributes

        group.MapGet("/", GetAllCustomers)
            .WithName("GetAllCustomers")
            .RequireAuthorization(); // Requires "read:customers" permission

        group.MapGet("/{id:guid}", GetCustomerById)
            .WithName("GetCustomerById")
            .RequireAuthorization(); // Requires "read:customers" permission

        group.MapPost("/", CreateCustomer)
            .WithName("CreateCustomer")
            .RequireAuthorization(); // Requires "write:customers" permission

        group.MapPut("/{id:guid}", UpdateCustomer)
            .WithName("UpdateCustomer")
            .RequireAuthorization(); // Requires "write:customers" permission

        group.MapDelete("/{id:guid}", DeleteCustomer)
            .WithName("DeleteCustomer")
            .RequireAuthorization(); // Requires "delete:customers" permission

        // T018: Customer deletion validation endpoint
        group.MapPost("/{id:guid}/validate-deletion", ValidateCustomerDeletion)
            .WithName("ValidateCustomerDeletion")
            .RequireAuthorization();

        // T019: Customer deletion endpoint (soft/hard)
        group.MapPost("/{id:guid}/soft-delete", SoftDeleteCustomer)
            .WithName("SoftDeleteCustomer")
            .RequireAuthorization();
            
        group.MapPost("/{id:guid}/hard-delete", HardDeleteCustomer)
            .WithName("HardDeleteCustomer")
            .RequireAuthorization();

        // T020: Customer location update endpoint
        group.MapGet("/{id:guid}/location", GetCustomerLocation)
            .WithName("GetCustomerLocation")
            .RequireAuthorization();
            
        group.MapPut("/{id:guid}/location", UpdateCustomerLocation)
            .WithName("UpdateCustomerLocation")
            .RequireAuthorization();
            
        group.MapDelete("/{id:guid}/location", DeleteCustomerLocation)
            .WithName("DeleteCustomerLocation")
            .RequireAuthorization();

        // Admin delete all customers endpoint
        group.MapDelete("/admin/delete-all", DeleteAllCustomers)
            .WithName("DeleteAllCustomers")
            .WithSummary("Delete all customers (Admin only)")
            .RequireAuthorization();

        return builder;
    }

    private static async Task<IResult> GetAllCustomers(ICustomerService customerService)
    {
        try
        {
            var customers = await customerService.GetAllCustomersAsync();
            return Results.Ok(customers);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving customers: {ex.Message}");
        }
    }

    private static async Task<IResult> GetCustomerById(Guid id, ICustomerService customerService)
    {
        try
        {
            var customer = await customerService.GetCustomerByIdAsync(id);
            return customer == null ? Results.NotFound() : Results.Ok(customer);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving customer: {ex.Message}");
        }
    }

    private static async Task<IResult> CreateCustomer(CustomerCreateDto dto, ICustomerService customerService)
    {
        try
        {
            var createdCustomer = await customerService.CreateCustomerAsync(dto);
            return Results.Created($"/api/customers/{createdCustomer.Id}", createdCustomer);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error creating customer: {ex.Message}");
        }
    }

    private static async Task<IResult> UpdateCustomer(Guid id, CustomerUpdateDto dto, ICustomerService customerService)
    {
        try
        {
            var updatedCustomer = await customerService.UpdateCustomerAsync(id, dto);
            return Results.Ok(updatedCustomer);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error updating customer: {ex.Message}");
        }
    }

    private static async Task<IResult> DeleteCustomer(Guid id, ICustomerService customerService)
    {
        try
        {
            await customerService.DeleteCustomerAsync(id);
            return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error deleting customer: {ex.Message}");
        }
    }

    // T018: Customer deletion validation endpoint
    private static async Task<IResult> ValidateCustomerDeletion(Guid id, ICustomerService customerService)
    {
        try
        {
            var validation = await customerService.ValidateCustomerDeletionAsync(id);
            return Results.Ok(validation);
        }
        catch (ArgumentException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error validating customer deletion: {ex.Message}");
        }
    }

    // T019: Customer deletion endpoints (soft/hard)
    private static async Task<IResult> SoftDeleteCustomer(Guid id, ICustomerService customerService, CustomerDeletionRequest request)
    {
        try
        {
            // Ensure the ID in the request matches the route parameter
            request.CustomerId = id;
            var customer = await customerService.SoftDeleteCustomerAsync(request);
            return Results.Ok(customer);
        }
        catch (ArgumentException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error soft deleting customer: {ex.Message}");
        }
    }

    private static async Task<IResult> HardDeleteCustomer(Guid id, ICustomerService customerService, CustomerDeletionRequest request)
    {
        try
        {
            // Ensure the ID in the request matches the route parameter
            request.CustomerId = id;
            await customerService.HardDeleteCustomerAsync(request);
            return Results.Ok(new { message = "Customer permanently deleted", deletedId = id });
        }
        catch (ArgumentException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error hard deleting customer: {ex.Message}");
        }
    }

    // T020: Customer location endpoints
    private static async Task<IResult> GetCustomerLocation(Guid id, ICustomerService customerService)
    {
        try
        {
            var location = await customerService.GetCustomerLocationAsync(id);
            return location == null ? Results.NotFound() : Results.Ok(location);
        }
        catch (ArgumentException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving customer location: {ex.Message}");
        }
    }

    private static async Task<IResult> UpdateCustomerLocation(Guid id, ICustomerService customerService, CustomerLocationDto location)
    {
        try
        {
            // Ensure the ID in the location matches the route parameter
            location.CustomerId = id;
            var updatedLocation = await customerService.UpdateCustomerLocationAsync(location);
            return Results.Ok(updatedLocation);
        }
        catch (ArgumentException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error updating customer location: {ex.Message}");
        }
    }

    private static async Task<IResult> DeleteCustomerLocation(Guid id, ICustomerService customerService)
    {
        try
        {
            await customerService.DeleteCustomerLocationAsync(id);
            return Results.Ok(new { message = "Customer location deleted", customerId = id });
        }
        catch (ArgumentException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error deleting customer location: {ex.Message}");
        }
    }

    // Admin delete all customers endpoint
    private static async Task<IResult> DeleteAllCustomers(ICustomerService customerService)
    {
        try
        {
            var deletedCount = await customerService.DeleteAllCustomersAsync();
            return Results.Ok(new { message = $"All {deletedCount} customers deleted successfully", deletedCount });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error deleting all customers: {ex.Message}");
        }
    }
}
