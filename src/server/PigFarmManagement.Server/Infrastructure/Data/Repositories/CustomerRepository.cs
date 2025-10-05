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

    public async Task<Customer> CreateAsync(Customer customer)
    {
        var entity = CustomerEntity.FromModel(customer);
        _context.Customers.Add(entity);
        await _context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<Customer> UpdateAsync(Customer customer)
    {
        var entity = await _context.Customers.FindAsync(customer.Id);
        if (entity == null)
            throw new ArgumentException($"Customer with ID {customer.Id} not found");

        // Update all fields from the customer model
        entity.Code = customer.Code;
        entity.FirstName = customer.FirstName;
        entity.LastName = customer.LastName;
        entity.Status = customer.Status;
        entity.Phone = customer.Phone;
        entity.Email = customer.Email;
        entity.ExternalId = customer.ExternalId;
        entity.KeyCardId = customer.KeyCardId;
        entity.Address = customer.Address;
        entity.Sex = customer.Sex;
        entity.Zipcode = customer.Zipcode;
        entity.Latitude = customer.Latitude;
        entity.Longitude = customer.Longitude;
        entity.IsDeleted = customer.IsDeleted;
        entity.DeletedAt = customer.DeletedAt;
        entity.DeletedBy = customer.DeletedBy;
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
