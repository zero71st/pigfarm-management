# Data Model: Enhanced Customer Management

## Enhanced Entities

### CustomerEntity (Database)
**Enhanced fields for location support:**
```csharp
public class CustomerEntity
{
    // Existing fields...
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public CustomerStatus Status { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ExternalId { get; set; }
    public string? KeyCardId { get; set; }
    public string? Address { get; set; }
    public string? Sex { get; set; }
    public string? Zipcode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // NEW: Location fields
    public decimal? Latitude { get; set; }  // -90 to 90
    public decimal? Longitude { get; set; } // -180 to 180
    public bool IsDeleted { get; set; } = false; // Soft deletion
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // Navigation properties
    public virtual ICollection<PigPenEntity> PigPens { get; set; }
}
```

### Customer (Shared Model)
**Enhanced with location and deletion tracking:**
```csharp
public record Customer(Guid Id, string Code, CustomerStatus Status)
{
    // Existing properties...
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? ExternalId { get; init; }
    public string? KeyCardId { get; init; }
    public string? Sex { get; init; }
    public string? Zipcode { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    
    // NEW: Location properties
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public bool HasLocation => Latitude.HasValue && Longitude.HasValue;
    
    // NEW: Deletion tracking
    public bool IsDeleted { get; init; }
    public DateTime? DeletedAt { get; init; }
    public string? DeletedBy { get; init; }
    
    // Enhanced business logic
    public bool IsActive => Status == CustomerStatus.Active && !IsDeleted;
    public string DisplayName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName) 
        ? Code 
        : $"{FirstName} {LastName} ({Code})";
}
```

## New DTOs

### CustomerLocationDto
```csharp
public class CustomerLocationDto
{
    public Guid CustomerId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Address { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### CustomerDeletionRequest
```csharp
public class CustomerDeletionRequest
{
    public Guid CustomerId { get; set; }
    public bool ForceDelete { get; set; } = false; // Hard delete override
    public string? Reason { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}
```

### CustomerDeletionValidation
```csharp
public class CustomerDeletionValidation
{
    public bool CanDelete { get; set; }
    public List<string> BlockingReasons { get; set; } = new();
    public int ActivePigPenCount { get; set; }
    public int ActiveTransactionCount { get; set; }
    public bool HasRecent活动 { get; set; }
}
```

### ViewModePreference
```csharp
public class ViewModePreference
{
    public ViewMode Mode { get; set; } = ViewMode.Card;
    public DateTime LastUpdated { get; set; }
}

public enum ViewMode
{
    Card = 0,
    Table = 1
}
```

## Validation Rules

### Location Coordinates
- **Latitude**: Must be between -90 and 90 (inclusive)
- **Longitude**: Must be between -180 and 180 (inclusive)
- **Precision**: Support up to 6 decimal places for ~1 meter accuracy
- **Nullable**: Location is optional for customers

### Customer Deletion
- **Soft Delete**: Always allowed, sets IsDeleted = true, DeletedAt = now
- **Hard Delete**: Only allowed when:
  - No active pig pens (Status != Completed)
  - No recent transactions (within last 30 days)
  - Explicit ForceDelete = true confirmation
- **Audit Trail**: All deletions logged with timestamp and user

### Field Update Restrictions
- **POS Sync**: Updates all fields EXCEPT Latitude, Longitude, IsDeleted
- **Manual Edit**: Updates all fields including location coordinates
- **Conflict Resolution**: POS data always wins over manual edits (except location)

## State Transitions

### Customer Lifecycle
```
Created → Active → [Optional: Soft Deleted] → [Optional: Hard Deleted]
         ↓
       Inactive (Status change only)
```

### Location Data
```
No Location → Manual Entry → [Coordinates Set] → [Manual Update Allowed]
```

### Deletion Flow
```
Active Customer → Validation Check → Soft Delete → [Optional: Hard Delete]
                      ↓
                [Blocking Relationships] → Deletion Prevented
```

## Relationships

### Customer ← → PigPen
- **One-to-Many**: Customer can have multiple PigPens
- **Deletion Impact**: Active PigPens block customer hard deletion
- **Cascade Rule**: Customer soft deletion does not affect PigPens

### Customer ← → Transaction (Implied)
- **One-to-Many**: Customer can have multiple transactions
- **Deletion Impact**: Recent transactions block customer hard deletion
- **Audit Requirement**: Transaction history preserved even after customer deletion

## Migration Requirements

### Database Schema Changes
```sql
-- Add location and deletion tracking columns
ALTER TABLE Customers ADD COLUMN Latitude DECIMAL(10,8) NULL;
ALTER TABLE Customers ADD COLUMN Longitude DECIMAL(11,8) NULL;
ALTER TABLE Customers ADD COLUMN IsDeleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE Customers ADD COLUMN DeletedAt DATETIME NULL;
ALTER TABLE Customers ADD COLUMN DeletedBy NVARCHAR(255) NULL;

-- Add indexes for performance
CREATE INDEX IX_Customers_Location ON Customers(Latitude, Longitude) WHERE Latitude IS NOT NULL;
CREATE INDEX IX_Customers_IsDeleted ON Customers(IsDeleted);
```

### Data Preservation
- **Existing Customers**: All current customer data preserved
- **Location Backfill**: New location fields default to NULL (no location)
- **Deletion State**: All existing customers default to IsDeleted = false
- **Backward Compatibility**: Existing APIs continue to work without modification