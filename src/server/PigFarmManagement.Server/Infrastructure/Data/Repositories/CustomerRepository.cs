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
        var entities = await _context.Customers.ToListAsync();
        return entities.Select(e => e.ToModel());
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Customers.FindAsync(id);
        return entity?.ToModel();
    }

    public async Task<Customer?> GetByCodeAsync(string code)
    {
        var entity = await _context.Customers.FirstOrDefaultAsync(c => c.Code == code);
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

    entity.Code = customer.Code;
    // Name removed in favor of FirstName/LastName
    entity.FirstName = customer.FirstName;
    entity.LastName = customer.LastName;
    entity.Status = customer.Status;
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
