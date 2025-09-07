using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Server.Infrastructure.Data.Entities;

public class DepositEntity
{
    public Guid Id { get; set; }
    
    public Guid PigPenId { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    public DateTime Date { get; set; }
    
    public string? Remark { get; set; }
    
    // Navigation Properties
    [ForeignKey("PigPenId")]
    public virtual PigPenEntity PigPen { get; set; } = null!;
    
    // Convert to shared model
    public Deposit ToModel()
    {
        return new Deposit(Id, PigPenId, Amount, Date, Remark);
    }
    
    // Create from shared model
    public static DepositEntity FromModel(Deposit deposit)
    {
        return new DepositEntity
        {
            Id = deposit.Id,
            PigPenId = deposit.PigPenId,
            Amount = deposit.Amount,
            Date = deposit.Date,
            Remark = deposit.Remark
        };
    }
}
