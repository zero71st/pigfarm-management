using Microsoft.EntityFrameworkCore;
using PigFarmManagement.Server.Infrastructure.Data.Entities;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly PigFarmDbContext _context;

    public CustomerRepository(PigFarmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        var entities = await _context.Customers
            .Where(c => !c.IsDeleted)
            .ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<IEnumerable<Customer>> GetAllIncludingDeletedAsync()
    {
        var entities = await _context.Customers.ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<IEnumerable<Customer>> GetDeletedAsync()
    {
        var entities = await _context.Customers
            .Where(c => c.IsDeleted)
            .ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Customers
            .Where(c => c.Id == id && !c.IsDeleted)
            .FirstOrDefaultAsync();
        return entity?.ToModel();
    }

    public async Task<Customer?> GetByIdIncludingDeletedAsync(Guid id)
    {
        var entity = await _context.Customers.FindAsync(id);
        return entity?.ToModel();
    }

    public async Task<Customer?> GetByCodeAsync(string code)
    {
        var entity = await _context.Customers
            .FirstOrDefaultAsync(c => c.Code == code && !c.IsDeleted);
        return entity?.ToModel();
    }

    public async Task<IEnumerable<Customer>> GetByExternalIdsAsync(IEnumerable<string> externalIds)
    {
        var ids = externalIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
        if (!ids.Any())
            return Enumerable.Empty<Customer>();

        var entities = await _context.Customers
            .Where(c => c.ExternalId != null && ids.Contains(c.ExternalId) && !c.IsDeleted)
            .ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<Customer> CreateAsync(CustomerCreateDto dto)
    {
        var customer = new Customer(
            Id: Guid.NewGuid(),
            Code: dto.Code,
            Status: dto.Status
        )
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            ExternalId = dto.ExternalId,
            KeyCardId = dto.KeyCardId,
            Sex = dto.Sex,
            Zipcode = dto.Zipcode,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
            DeletedAt = null,
            DeletedBy = null
        };
        
        var entity = CustomerEntity.FromModel(customer);
        _context.Customers.Add(entity);
        await _context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<Customer> UpdateAsync(Guid id, CustomerUpdateDto dto)
    {
        var entity = await _context.Customers.FindAsync(id);
        if (entity == null)
            throw new ArgumentException($"Customer with ID {id} not found");

        // Update only fields that are provided in the DTO (non-null values)
        if (dto.Code != null) entity.Code = dto.Code;
        if (dto.Status.HasValue) entity.Status = dto.Status.Value;
        if (dto.FirstName != null) entity.FirstName = dto.FirstName;
        if (dto.LastName != null) entity.LastName = dto.LastName;
        if (dto.Phone != null) entity.Phone = dto.Phone;
        if (dto.Email != null) entity.Email = dto.Email;
        if (dto.Address != null) entity.Address = dto.Address;
        if (dto.ExternalId != null) entity.ExternalId = dto.ExternalId;
        if (dto.KeyCardId != null) entity.KeyCardId = dto.KeyCardId;
        if (dto.Sex != null) entity.Sex = dto.Sex;
        if (dto.Zipcode != null) entity.Zipcode = dto.Zipcode;
        if (dto.Latitude.HasValue) entity.Latitude = dto.Latitude;
        if (dto.Longitude.HasValue) entity.Longitude = dto.Longitude;
        
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Customers.FindAsync(id);
        if (entity != null)
        {
            _context.Customers.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
