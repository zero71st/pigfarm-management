using System.ComponentModel.DataAnnotations;

namespace PigFarmManagement.Server.Infrastructure.Data.Entities;

/// <summary>
/// Entity to track database migration execution for observability and audit purposes.
/// See specs/011-title-deploy-server/data-model.md for requirements.
/// </summary>
public class MigrationJobEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? FinishedAt { get; set; }

    [Required]
    public MigrationJobStatus Status { get; set; } = MigrationJobStatus.Pending;

    /// <summary>
    /// Error message for failure diagnosis. Null for successful migrations.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Source of the migration trigger (e.g., "startup", "endpoint", "ci")
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Number of migrations applied in this job
    /// </summary>
    public int MigrationsApplied { get; set; } = 0;
}

public enum MigrationJobStatus
{
    Pending = 0,
    Running = 1,
    Success = 2,
    Failed = 3
}