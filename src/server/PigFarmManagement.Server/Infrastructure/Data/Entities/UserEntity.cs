using System.ComponentModel.DataAnnotations;

namespace PigFarmManagement.Server.Infrastructure.Data.Entities;

/// <summary>
/// Represents a system user with authentication credentials and role-based permissions
/// </summary>
public class UserEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = "";

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    /// <summary>
    /// Comma-separated roles (Admin,Manager,Worker,Viewer)
    /// </summary>
    [MaxLength(255)]
    public string RolesCsv { get; set; } = "";

    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public virtual ICollection<ApiKeyEntity> ApiKeys { get; set; } = new List<ApiKeyEntity>();

    // Helper properties
    public string[] Roles => string.IsNullOrWhiteSpace(RolesCsv) 
        ? Array.Empty<string>() 
        : RolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(r => r.Trim())
            .ToArray();

    /// <summary>
    /// Primary role for the user (first role in RolesCsv)
    /// </summary>
    public string Role => Roles.FirstOrDefault() ?? "ReadOnly";

    /// <summary>
    /// Display name for the user (alias for Username)
    /// </summary>
    public string Name => Username;

    public bool HasRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool IsAdmin => HasRole("Admin");
    public bool IsManager => HasRole("Manager") || IsAdmin;
    public bool IsWorker => HasRole("Worker") || IsManager;
    public bool IsViewer => HasRole("Viewer") || IsWorker;
}