using PigFarmManagement.Shared.Domain;

namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Request for creating a new customer
/// </summary>
public record CustomerCreateDto(
    string Code,
    CustomerStatus Status = CustomerStatus.Active,
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? ExternalId = null,
    string? KeyCardId = null,
    string? Sex = null,
    string? Zipcode = null,
    decimal? Latitude = null,
    decimal? Longitude = null
);

/// <summary>
/// Request for updating an existing customer
/// </summary>
public record CustomerUpdateDto(
    string? Code = null,
    CustomerStatus? Status = null,
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? ExternalId = null,
    string? KeyCardId = null,
    string? Sex = null,
    string? Zipcode = null,
    decimal? Latitude = null,
    decimal? Longitude = null
);