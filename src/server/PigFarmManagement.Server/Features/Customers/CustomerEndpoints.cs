using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Features.Customers;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/customers").WithTags("Customers");

        group.MapGet("/", GetAllCustomers)
            .WithName("GetAllCustomers");

        group.MapGet("/{id:guid}", GetCustomerById)
            .WithName("GetCustomerById");

        group.MapPost("/", CreateCustomer)
            .WithName("CreateCustomer");

        group.MapPut("/{id:guid}", UpdateCustomer)
            .WithName("UpdateCustomer");

        group.MapDelete("/{id:guid}", DeleteCustomer)
            .WithName("DeleteCustomer");

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

    private static async Task<IResult> CreateCustomer(Customer customer, ICustomerService customerService)
    {
        try
        {
            var createdCustomer = await customerService.CreateCustomerAsync(customer);
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

    private static async Task<IResult> UpdateCustomer(Guid id, Customer customer, ICustomerService customerService)
    {
        try
        {
            // Ensure the ID in the URL matches the customer ID
            if (id != customer.Id)
            {
                return Results.BadRequest("Customer ID mismatch");
            }

            var updatedCustomer = await customerService.UpdateCustomerAsync(customer);
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
}
