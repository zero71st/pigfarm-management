using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Entities;

public class CustomerEntity
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;
    
    // Name has been split into FirstName/LastName per POSPOS integration
    
    public CustomerStatus Status { get; set; }
    
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ExternalId { get; set; }
    public string? KeyCardId { get; set; }
    public string? Address { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Sex { get; set; }
    public string? Zipcode { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    public virtual ICollection<PigPenEntity> PigPens { get; set; } = new List<PigPenEntity>();
    
    // Convert to shared model
    public Customer ToModel()
    {
        var customer = new Customer(Id, Code, Status)
        {
            Phone = Phone,
            Email = Email,
            ExternalId = ExternalId,
            KeyCardId = KeyCardId,
            Address = Address,
            FirstName = FirstName,
            LastName = LastName,
            Sex = Sex,
            Zipcode = Zipcode,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };

        return customer;
    }
    
    // Create from shared model
    public static CustomerEntity FromModel(Customer customer)
    {
        return new CustomerEntity
        {
            Id = customer.Id,
            Code = customer.Code,
            Status = customer.Status,
            Phone = customer.Phone,
            Email = customer.Email,
            ExternalId = customer.ExternalId,
            KeyCardId = customer.KeyCardId,
            Address = customer.Address,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Sex = customer.Sex,
            Zipcode = customer.Zipcode,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
    }
}
