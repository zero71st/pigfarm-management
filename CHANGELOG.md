# CHANGELOG

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### [Feature 012] POSPOS Import Enhancement - Latest Member Display (2025-11-29)

**Feature**: Enhance POSPOS customer import workflow to display only the latest customer and disable bulk select-all operation

**Changes**:

#### Backend (`CustomerImportEndpoints.cs`)
- Enhanced `GET /api/customers/import/candidates` endpoint with optional `source` query parameter
  - `source=pospos`: Returns only the latest POSPOS member (1 item max, ordered by CreatedAt DESC, Id DESC)
  - `source=all`: Returns all available members (default, backward compatible)
  - Parameter omitted: Defaults to `all` (fully backward compatible)
- Added validation: Invalid source parameter returns 400 Bad Request with descriptive message
- Enhanced error handling:
  - Returns 503 Service Unavailable with message "POSPOS service unavailable. Please try again later." for POSPOS API failures
  - Distinguishes from other 500 errors for better user experience and debugging

#### Frontend (`ImportCandidatesDialog.razor`)
- Added `_source` field to track import context (pospos vs all)
- Modified `LoadCandidates()` method to include source parameter in API URL
- Conditionally hidden select-all checkbox when viewing latest member only (`source=pospos`)
- Individual member row selection remains enabled for both `source` modes
- Enhanced error handling: Shows distinct snackbar message for POSPOS service unavailable vs. other errors
- Selection state remains session-scoped (ephemeral, no database persistence)

#### Documentation (`docs/IMPORT_API.md`)
- Updated `/import/customers/candidates` endpoint documentation with:
  - New `source` query parameter description
  - Usage examples for all query parameter values
  - Response status codes (200, 400, 401, 500, 503)
  - Error response formats with examples

#### Development Reference (`.github/copilot-instructions.md`)
- Added Feature 012 section documenting:
  - Scope and files modified
  - Key API and component changes
  - Pattern used (modifying existing infrastructure)
  - Error handling behavior
  - Selection state lifecycle
  - Reference to validation scenarios

**API Changes**:
- `GET /api/customers/import/candidates?source=pospos` - Returns 1 latest member
- `GET /api/customers/import/candidates?source=all` - Returns all members (new explicit parameter)
- `GET /api/customers/import/candidates` - Returns all members (default, unchanged behavior)

**Breaking Changes**: None (fully backward compatible)

**Migration Required**: No database migrations required

**Testing**:
- Contract validation: All query parameters, response formats, error codes verified
- Manual validation scenarios: 8 user workflow scenarios documented and tested
- Integration tests: Backward compatibility, error handling, performance verified
- See `specs/012-update-search-customer/quickstart.md` for complete validation scenarios

**Implementation Details**:
- No new database tables or schema changes
- No new migrations required
- No new DTOs or models (existing CandidateMember class used)
- Server-side filtering (more efficient than client-side)
- Session-scoped selection state (no persistence)

---

## How to Contribute

When adding new features or changes:
1. Add entry under `[Unreleased]` section
2. Follow the format of existing entries
3. Include: Backend changes, Frontend changes, API changes, Breaking changes, Testing
4. Reference feature number (e.g., Feature 012) and file locations
5. Include date in YYYY-MM-DD format

---

**Last Updated**: 2025-11-29  
**Changelog Maintained By**: Development Team
