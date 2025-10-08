# Research: Enhanced Customer Management

Date: October 5, 2025

## Google Maps Integration for Location Display

**Decision**: Use Google Maps JavaScript API with Blazor JSInterop for customer location mapping.

**Rationale**: 
- Google Maps provides reliable geocoding and map display capabilities
- JavaScript API integrates well with Blazor WebAssembly through IJSRuntime
- Existing project infrastructure supports external API integration patterns
- Manual coordinate entry keeps implementation simple while meeting user requirements

**Alternatives considered**:
- OpenStreetMap/Leaflet: Free alternative but requires more complex setup and has limited geocoding
- Bing Maps: Microsoft integration might be smoother but less familiar to users
- MapBox: Modern API but introduces new vendor dependency

**Implementation notes**:
- Store latitude/longitude as decimal fields in CustomerEntity
- Use IJSRuntime to invoke JavaScript Google Maps functions
- Handle API failures gracefully with fallback to coordinate display only

## Customer Deletion with Referential Integrity

**Decision**: Implement soft deletion with hard delete option after relationship checks.

**Rationale**:
- Constitution requires historical data preservation
- Business relationships (pig pens, transactions) must be validated before deletion
- Soft deletion allows for data recovery and audit trails
- Hard deletion available for true removal after manual confirmation

**Alternatives considered**:
- Hard deletion only: Violates constitutional data integrity requirements
- Soft deletion only: Does not meet user requirement for permanent removal
- Cascade deletion: Too risky for farm operation data

**Implementation notes**:
- Add IsDeleted flag to CustomerEntity (soft delete)
- Create validation service to check active relationships
- Provide manual hard delete after relationship verification
- Maintain audit log of deletion operations

## View Mode Switching (Card vs Table)

**Decision**: Use browser localStorage to persist user preference with component state management.

**Rationale**:
- localStorage provides client-side persistence without server complexity
- MudBlazor supports both card and table layouts naturally
- User preference persistence improves workflow efficiency
- No backend storage required for UI state

**Alternatives considered**:
- Server-side user preferences: Adds unnecessary complexity for single-user system
- Session-only preference: Does not persist across browser sessions
- Cookie storage: More complex than localStorage for this use case

**Implementation notes**:
- Create ViewMode enum (Card, Table)
- Use IJSRuntime for localStorage operations
- Default to Card view as specified
- Toggle component with immediate visual feedback

## POS API Manual Sync Enhancement

**Decision**: Enhance existing PosposImporter with manual trigger capability and field-level update control.

**Rationale**:
- Leverages existing POS integration infrastructure
- Manual triggering provides admin control over sync timing
- Field-level awareness allows location data to remain manual-only
- POS data precedence maintains external system authority

**Alternatives considered**:
- Automatic scheduled sync: Rejected per user clarification - manual trigger preferred
- Real-time sync: Too complex and not requested by user
- Selective field sync: More complex than full sync with field exclusions

**Implementation notes**:
- Extend CustomerService with manual sync endpoints
- Update PosposImporter to exclude location fields from sync
- Add admin UI for triggering manual sync operations
- Maintain existing conflict resolution (POS data wins)

## Risks & Mitigations

**Risk**: Google Maps API rate limiting or failures  
**Mitigation**: Graceful degradation to coordinate-only display, implement client-side caching

**Risk**: Customer deletion affecting system integrity  
**Mitigation**: Comprehensive relationship validation before deletion, soft delete as safety net

**Risk**: View mode preference conflicts across devices  
**Mitigation**: Device-specific preferences acceptable for single-user system

**Risk**: Manual POS sync operation failures  
**Mitigation**: Detailed error reporting and rollback capabilities, leverage existing error handling