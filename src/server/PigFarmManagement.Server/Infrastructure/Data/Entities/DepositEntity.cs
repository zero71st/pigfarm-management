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

    private static DateTime AsUnspecifiedDate(DateTime value)
        => DateTime.SpecifyKind(value.Date, DateTimeKind.Unspecified);

    // Workaround for storing date-only values in PostgreSQL timestamptz:
    // store as UTC noon to prevent timezone conversions from shifting the calendar day.
    private static DateTime ToUtcNoon(DateTime value)
    {
        var date = value.Date;
        return new DateTime(date.Year, date.Month, date.Day, 12, 0, 0, DateTimeKind.Utc);
    }
    
    // Convert to shared model
    public Deposit ToModel()
    {
        return new Deposit(Id, PigPenId, Amount, AsUnspecifiedDate(Date), Remark);
    }
    
    // Create from shared model
    public static DepositEntity FromModel(Deposit deposit)
    {
        return new DepositEntity
        {
            Id = deposit.Id,
            PigPenId = deposit.PigPenId,
            Amount = deposit.Amount,
            Date = ToUtcNoon(deposit.Date),
            Remark = deposit.Remark
        };
    }
}
