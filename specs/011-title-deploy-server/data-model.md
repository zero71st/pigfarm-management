# Data Model

Entities

- User
  - Id: GUID (PK)
  - Username: string (unique)
  - Email: string
  - PasswordHash: string
  - RolesCsv: string (e.g., "Admin,User")
  - CreatedAt: DateTime

- ApiKey
  - Id: GUID (PK)
  - UserId: GUID (FK -> User.Id)
  - HashedKey: string
  - CreatedAt: DateTime
  - ExpiresAt: DateTime? (optional)

- MigrationJob
  - Id: GUID (PK)
  - StartedAt: DateTime
  - FinishedAt: DateTime?
  - Status: enum { Pending, Running, Success, Failed }
  - ErrorMessage: string? (for failure diagnosis)

Validation & Constraints

- Username must be unique.
- PasswordHash is stored only after hashing with a secure algorithm (BCrypt, ASP.NET Identity hasher, or similar).
- ApiKey raw values are never stored; only salted hashes are persisted.

Lifecycle Notes

- User creation by seeder runs only when no admin user exists.
- MigrationJob entries are optional but useful for observability and diagnostics.
