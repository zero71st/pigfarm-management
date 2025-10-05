namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Extension methods for converting between Customer domain models and DTOs
/// </summary>
public static class CustomerExtensions
{
    /// <summary>
    /// Converts a Customer domain model to a CustomerCreateDto
    /// </summary>
    public static CustomerCreateDto ToCreateDto(this Customer customer)
    {
        return new CustomerCreateDto(
            Code: customer.Code,
            Status: customer.Status,
            FirstName: customer.FirstName,
            LastName: customer.LastName,
            Phone: customer.Phone,
            Email: customer.Email,
            Address: customer.Address,
            ExternalId: customer.ExternalId,
            KeyCardId: customer.KeyCardId,
            Sex: customer.Sex,
            Zipcode: customer.Zipcode,
            Latitude: customer.Latitude,
            Longitude: customer.Longitude
        );
    }

    /// <summary>
    /// Converts a Customer domain model to a CustomerUpdateDto
    /// </summary>
    public static CustomerUpdateDto ToUpdateDto(this Customer customer)
    {
        return new CustomerUpdateDto(
            Code: customer.Code,
            Status: customer.Status,
            FirstName: customer.FirstName,
            LastName: customer.LastName,
            Phone: customer.Phone,
            Email: customer.Email,
            Address: customer.Address,
            ExternalId: customer.ExternalId,
            KeyCardId: customer.KeyCardId,
            Sex: customer.Sex,
            Zipcode: customer.Zipcode,
            Latitude: customer.Latitude,
            Longitude: customer.Longitude
        );
    }
}