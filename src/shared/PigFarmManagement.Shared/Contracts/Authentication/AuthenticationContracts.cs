using System.ComponentModel.DataAnnotations;

namespace PigFarmManagement.Shared.Contracts.Authentication;

public record LoginRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; init; } = "";

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; init; } = "";

    [StringLength(100)]
    public string? KeyLabel { get; init; }

    [Range(1, 365)]
    public int? ExpirationDays { get; init; } = 30;
}

public record LoginResponse
{
    public string ApiKey { get; init; } = "";
    public DateTime ExpiresAt { get; init; }
    public UserInfo User { get; init; } = null!;
}

public record UserInfo
{
    public Guid Id { get; init; }
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public string[] Roles { get; init; } = Array.Empty<string>();
    public bool IsActive { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateUserRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; init; } = "";

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; init; } = "";

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; init; } = "";

    [Required]
    public string[] Roles { get; init; } = Array.Empty<string>();

    public bool IsActive { get; init; } = true;
}

public record UpdateUserRequest
{
    [StringLength(255)]
    [EmailAddress]
    public string? Email { get; init; }

    public string[]? Roles { get; init; }

    public bool? IsActive { get; init; }
}

public record ChangePasswordRequest
{
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string CurrentPassword { get; init; } = "";

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; init; } = "";
}

public record ApiKeyRequest
{
    [StringLength(100)]
    public string? Label { get; init; }

    [Range(1, 365)]
    public int ExpirationDays { get; init; } = 30;
}

public record ApiKeyInfo
{
    public Guid Id { get; init; }
    public string? Label { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public int UsageCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Status { get; init; } = "";
}

public record ApiKeyResponse
{
    public string ApiKey { get; init; } = "";
    public ApiKeyInfo KeyInfo { get; init; } = null!;
}

public record ErrorResponse
{
    public string Message { get; init; } = "";
    public string? Details { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string[]? ValidationErrors { get; init; }
}

public record ValidationErrorResponse : ErrorResponse
{
    public Dictionary<string, string[]> Errors { get; init; } = new();
}

// Extension methods for mapping (these will be implemented in the server project)
// Removed to avoid circular dependencies - mapping logic should be in server layer