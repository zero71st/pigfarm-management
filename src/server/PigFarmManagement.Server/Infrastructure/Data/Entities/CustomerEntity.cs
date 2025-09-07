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
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public CustomerStatus Status { get; set; }
    
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ExternalId { get; set; }
    public string? KeyCardId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    public virtual ICollection<PigPenEntity> PigPens { get; set; } = new List<PigPenEntity>();
    
    // Convert to shared model
    public Customer ToModel()
    {
        return new Customer(Id, Code, Name, Status)
        {
            Phone = Phone,
            Email = Email,
            ExternalId = ExternalId,
            KeyCardId = KeyCardId,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }
    
    // Create from shared model
    public static CustomerEntity FromModel(Customer customer)
    {
        return new CustomerEntity
        {
            Id = customer.Id,
            Code = customer.Code,
            Name = customer.Name,
            Status = customer.Status,
            Phone = customer.Phone,
            Email = customer.Email,
            ExternalId = customer.ExternalId,
            KeyCardId = customer.KeyCardId,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
    }
}
