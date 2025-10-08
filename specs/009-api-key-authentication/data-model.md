# Data Model: API-Key Authentication System

**Feature**: 009-api-key-authentication  
**Created**: October 8, 2025  
**Status**: Draft

## Entity Relationship Overview

```
User (1) ←→ (N) ApiKey
User (N) ←→ (N) Role [Optional - can be simplified to string array]
```

## Core Entities

### User Entity

**Purpose**: Represents system users with authentication credentials and role-based permissions

```sql
-- Conceptual Schema
CREATE TABLE Users (
    Id              UNIQUEIDENTIFIER PRIMARY KEY,
    Username        NVARCHAR(50) NOT NULL UNIQUE,
    Email           NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(MAX) NOT NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy       NVARCHAR(50),
    ModifiedAt      DATETIME2,
    ModifiedBy      NVARCHAR(50),
    LastLoginAt     DATETIME2,
    
    -- Role storage - simplified approach
    RolesCsv        NVARCHAR(255) NOT NULL DEFAULT '', -- comma-separated roles
    
    -- Audit fields
    IsDeleted       BIT NOT NULL DEFAULT 0,
    DeletedAt       DATETIME2,
    DeletedBy       NVARCHAR(50)
);
```

**Attributes:**
- **Id**: Primary key, GUID for distributed system compatibility
- **Username**: Unique identifier for login, 3-50 characters
- **Email**: Contact email, must be valid format and unique
- **PasswordHash**: BCrypt or equivalent hashed password, never plain text
- **IsActive**: Account status, inactive users cannot authenticate
- **RolesCsv**: Comma-separated role names (Admin,Manager,Worker,Viewer)
- **Audit Fields**: Standard audit trail for compliance and debugging

**Business Rules:**
- Username: alphanumeric plus underscore/hyphen, case-insensitive uniqueness
- Email: standard email validation, case-insensitive uniqueness
- Password: minimum 8 characters, complexity requirements enforced at application level
- Roles: predefined set (Admin, Manager, Worker, Viewer), hierarchical permissions
- Soft delete: IsDeleted flag preserves audit trail, prevents username reuse

**Indexes:**
```sql
-- Performance indexes
CREATE UNIQUE INDEX IX_Users_Username ON Users(Username) WHERE IsDeleted = 0;
CREATE UNIQUE INDEX IX_Users_Email ON Users(Email) WHERE IsDeleted = 0;
CREATE INDEX IX_Users_IsActive ON Users(IsActive) WHERE IsDeleted = 0;
```

### ApiKey Entity

**Purpose**: Represents authentication tokens with lifecycle management and audit trail

```sql
-- Conceptual Schema
CREATE TABLE ApiKeys (
    Id              UNIQUEIDENTIFIER PRIMARY KEY,
    HashedKey       NVARCHAR(MAX) NOT NULL UNIQUE,
    UserId          UNIQUEIDENTIFIER NOT NULL,
    Label           NVARCHAR(100),
    
    -- Lifecycle management
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt       DATETIME2,
    RevokedAt       DATETIME2,
    LastUsedAt      DATETIME2,
    
    -- Role snapshot (performance optimization)
    RolesCsv        NVARCHAR(255) NOT NULL DEFAULT '',
    
    -- Audit trail
    CreatedBy       NVARCHAR(50),
    RevokedBy       NVARCHAR(50),
    
    -- Usage statistics
    UsageCount      INT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
```

**Attributes:**
- **Id**: Primary key, GUID for unique identification
- **HashedKey**: SHA-256 or stronger hash of the raw API key, indexed for fast lookup
- **UserId**: Foreign key to owning user, cascade delete behavior
- **Label**: User-friendly identifier (e.g., "Mobile App", "Desktop Client")
- **IsActive**: Key status, revoked keys marked inactive immediately
- **ExpiresAt**: Configurable expiration, null for non-expiring keys
- **RolesCsv**: Snapshot of user roles at key creation time (performance optimization)
- **Usage Statistics**: Tracking for monitoring and analytics

**Business Rules:**
- Hashed key must be globally unique across all keys (active and revoked)
- Raw key never stored, only displayed once at generation
- Expiration automatically invalidates key regardless of IsActive status
- Role snapshot prevents need for user lookup on every request
- Usage tracking updates asynchronously to avoid performance impact

**Indexes:**
```sql
-- Critical performance indexes
CREATE UNIQUE INDEX IX_ApiKeys_HashedKey ON ApiKeys(HashedKey);
CREATE INDEX IX_ApiKeys_UserId ON ApiKeys(UserId);
CREATE INDEX IX_ApiKeys_Active_Expiry ON ApiKeys(IsActive, ExpiresAt) WHERE IsActive = 1;
CREATE INDEX IX_ApiKeys_LastUsed ON ApiKeys(LastUsedAt) WHERE IsActive = 1;
```

### Audit Log Entity (Optional)

**Purpose**: Detailed audit trail for security monitoring and compliance

```sql
-- Conceptual Schema
CREATE TABLE AuthAuditLogs (
    Id              BIGINT IDENTITY PRIMARY KEY,
    EventType       NVARCHAR(50) NOT NULL, -- Login, KeyGenerated, KeyRevoked, etc.
    UserId          UNIQUEIDENTIFIER,
    ApiKeyId        UNIQUEIDENTIFIER,
    ActorId         UNIQUEIDENTIFIER, -- Who performed the action
    
    -- Event details
    Timestamp       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IpAddress       NVARCHAR(45),
    UserAgent       NVARCHAR(500),
    Success         BIT NOT NULL,
    ErrorMessage    NVARCHAR(MAX),
    
    -- Additional context
    AdditionalData  NVARCHAR(MAX), -- JSON for flexible data storage
    
    INDEX IX_AuthAuditLogs_Timestamp (Timestamp),
    INDEX IX_AuthAuditLogs_UserId (UserId),
    INDEX IX_AuthAuditLogs_EventType (EventType)
);
```

## Role System Design

### Simplified Approach (Recommended)
Store roles as comma-separated values in User and ApiKey entities:

**Predefined Roles:**
- **Admin**: Full system access, user management, all operations
- **Manager**: Farm operations, customer management, reporting
- **Worker**: Basic operations, data entry, limited reporting  
- **Viewer**: Read-only access, basic reporting

**Role Hierarchy:**
```
Admin > Manager > Worker > Viewer
```

**Permission Inheritance:**
- Admin: All permissions
- Manager: Worker + Viewer permissions + customer/farm management
- Worker: Viewer permissions + data entry operations
- Viewer: Read-only access only

### Advanced Approach (Future Extension)
Normalize roles into separate entities if more complex permission management needed:

```sql
CREATE TABLE Roles (
    Id              UNIQUEIDENTIFIER PRIMARY KEY,
    Name            NVARCHAR(50) NOT NULL UNIQUE,
    Description     NVARCHAR(255),
    IsSystemRole    BIT NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE UserRoles (
    UserId          UNIQUEIDENTIFIER,
    RoleId          UNIQUEIDENTIFIER,
    AssignedAt      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    AssignedBy      UNIQUEIDENTIFIER,
    
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);
```

## Security Considerations

### Password Security
- **Hashing Algorithm**: BCrypt with work factor 12+ (or Argon2id)
- **Validation**: Minimum 8 characters, complexity requirements
- **Storage**: Never store plain text, hash immediately on receipt
- **Reset**: Admin-initiated only, generates temporary credentials

### API Key Security
- **Generation**: Cryptographically secure random number generator
- **Entropy**: Minimum 256 bits of entropy (32+ byte random value)
- **Storage**: SHA-256 hash minimum, consider stronger algorithms
- **Display**: Raw key shown exactly once, never retrievable
- **Transmission**: HTTPS only, proper HTTP header handling

### Database Security
- **Encryption**: Encrypt sensitive columns at rest
- **Access Control**: Principle of least privilege for database access
- **Audit Trail**: Comprehensive logging of all authentication events
- **Backup Security**: Encrypted backups with secure key management

## Migration Strategy

### Phase 1: Core Schema
```sql
-- Create base tables with minimal feature set
CREATE TABLE Users (...);
CREATE TABLE ApiKeys (...);
-- Seed initial admin user
```

### Phase 2: Audit Enhancement
```sql
-- Add comprehensive audit logging
CREATE TABLE AuthAuditLogs (...);
-- Migrate existing users if any
```

### Phase 3: Advanced Features
```sql
-- Add advanced role management if needed
CREATE TABLE Roles (...);
CREATE TABLE UserRoles (...);
-- Migrate from CSV roles to normalized structure
```

## Data Validation Rules

### User Entity Validation
```csharp
// Conceptual validation rules
public class UserValidation
{
    public static bool ValidateUsername(string username)
    {
        // 3-50 characters, alphanumeric plus underscore/hyphen
        var pattern = @"^[a-zA-Z0-9_-]{3,50}$";
        return Regex.IsMatch(username, pattern);
    }
    
    public static bool ValidateEmail(string email)
    {
        // Standard email validation
        return EmailAddressAttribute.IsValid(email) && email.Length <= 255;
    }
    
    public static bool ValidatePassword(string password)
    {
        // Minimum 8 characters, at least one uppercase, lowercase, digit
        return password.Length >= 8 &&
               password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit);
    }
    
    public static bool ValidateRoles(string rolesCsv)
    {
        var validRoles = new[] { "Admin", "Manager", "Worker", "Viewer" };
        var roles = rolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return roles.All(role => validRoles.Contains(role.Trim()));
    }
}
```

### API Key Entity Validation
```csharp
// Conceptual validation rules
public class ApiKeyValidation
{
    public static bool ValidateLabel(string label)
    {
        // Optional, 1-100 characters if provided
        return string.IsNullOrEmpty(label) || 
               (label.Length >= 1 && label.Length <= 100);
    }
    
    public static bool ValidateExpiration(DateTime? expiresAt)
    {
        // Must be future date if provided
        return !expiresAt.HasValue || expiresAt.Value > DateTime.UtcNow;
    }
    
    public static string GenerateRawKey()
    {
        // Generate 32 bytes of secure random data
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
    
    public static string HashKey(string rawKey)
    {
        // SHA-256 hash of raw key
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(rawKey);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
```

## Performance Considerations

### Database Optimization
- **Primary Keys**: GUID vs. Identity considerations for distributed scenarios
- **Indexing Strategy**: Covering indexes for common query patterns
- **Query Optimization**: Efficient lookups for authentication middleware
- **Connection Pooling**: Proper connection management for high throughput

### Caching Strategy
- **User Data**: Cache user role information with proper invalidation
- **API Key Validation**: Consider in-memory cache for hot keys
- **Negative Caching**: Cache invalid key attempts to prevent repeated database hits
- **Cache Invalidation**: Immediate invalidation on key revocation

### Scalability Planning
- **Read Replicas**: Separate read/write concerns for authentication
- **Partitioning**: Strategies for very large user bases
- **Rate Limiting**: Prevent brute force attacks and abuse
- **Monitoring**: Performance metrics and alerting

---

## Entity Framework Configuration

### User Entity Configuration
```csharp
// Conceptual EF configuration
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Username)
               .IsRequired()
               .HasMaxLength(50);
        
        builder.Property(u => u.Email)
               .IsRequired()
               .HasMaxLength(255);
        
        builder.Property(u => u.PasswordHash)
               .IsRequired();
        
        builder.Property(u => u.RolesCsv)
               .HasMaxLength(255)
               .HasDefaultValue("");
        
        builder.HasIndex(u => u.Username)
               .IsUnique()
               .HasFilter("IsDeleted = 0");
        
        builder.HasIndex(u => u.Email)
               .IsUnique()
               .HasFilter("IsDeleted = 0");
        
        builder.HasMany(u => u.ApiKeys)
               .WithOne(k => k.User)
               .HasForeignKey(k => k.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### API Key Entity Configuration
```csharp
// Conceptual EF configuration
public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.HasKey(k => k.Id);
        
        builder.Property(k => k.HashedKey)
               .IsRequired();
        
        builder.Property(k => k.Label)
               .HasMaxLength(100);
        
        builder.Property(k => k.RolesCsv)
               .HasMaxLength(255)
               .HasDefaultValue("");
        
        builder.HasIndex(k => k.HashedKey)
               .IsUnique();
        
        builder.HasIndex(k => new { k.IsActive, k.ExpiresAt })
               .HasFilter("IsActive = 1");
        
        builder.HasOne(k => k.User)
               .WithMany(u => u.ApiKeys)
               .HasForeignKey(k => k.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

This data model provides a solid foundation for the API-Key authentication system while maintaining flexibility for future enhancements and ensuring proper security, performance, and maintainability characteristics.