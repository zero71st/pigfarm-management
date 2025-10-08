using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PigFarmManagement.Server.Infrastructure.Data.Entities;

/// <summary>
/// Represents an API key for user authentication with lifecycle management
/// </summary>
public class ApiKeyEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string HashedKey { get; set; } = "";

    [Required]
    public Guid UserId { get; set; }

    [MaxLength(100)]
    public string? Label { get; set; }

    /// <summary>
    /// Snapshot of user roles at key creation time (performance optimization)
    /// </summary>
    [MaxLength(255)]
    public string RolesCsv { get; set; } = "";

    // Lifecycle management
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    // Audit trail
    public string? CreatedBy { get; set; }
    public string? RevokedBy { get; set; }

    // Usage statistics
    public int UsageCount { get; set; } = 0;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual UserEntity User { get; set; } = null!;

    // Helper properties
    public string[] Roles => string.IsNullOrWhiteSpace(RolesCsv) 
        ? Array.Empty<string>() 
        : RolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(r => r.Trim())
            .ToArray();

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    
    public bool IsValidForUse => IsActive && !IsExpired && RevokedAt == null;

    public bool HasRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public void MarkAsUsed()
    {
        LastUsedAt = DateTime.UtcNow;
        UsageCount++;
    }

    public void Revoke(string? revokedBy = null)
    {
        IsActive = false;
        RevokedAt = DateTime.UtcNow;
        RevokedBy = revokedBy;
    }
}