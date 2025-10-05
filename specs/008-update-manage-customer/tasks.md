# Tasks: Enhanced Customer Management

**Feature**: Enhanced Customer Management  
**Branch**: `008-update-manage-customer`  
**Input**: Design documents from `/specs/008-update-manage-customer/`  
**Prerequisites**: plan.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

## Execution Flow (main)
```
1. Load plan.md from feature directory ✅
   → Tech stack: C# .NET 8, Blazor WebAssembly, MudBlazor UI, Entity Framework Core, Google Maps JavaScript API
   → Structure: Web application (frontend + backend)
2. Load design documents ✅:
   → data-model.md: CustomerEntity enhancements, location fields, deletion tracking
   → contracts/: Customer deletion, location management, POS sync APIs
   → research.md: Google Maps integration, soft deletion, view switching approach
3. Generate implementation tasks (excluding tests per user request)
   → Database: Migration and entity updates
   → Services: Enhanced customer services, location management
   → API: New endpoints for deletion, location, POS sync
   → UI: Enhanced components, new view modes, Google Maps integration
4. Apply task rules:
   → Different files = mark [P] for parallel execution
   → Same file = sequential dependencies
   → Implementation-focused (no test requirements)
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions
- **Note**: All automated tests skipped per user request (manual testing only)

## Phase 1: Database & Entity Layer

- [x] **T001** [P] Create EF migration for customer location fields in `src/server/PigFarmManagement.Server/Infrastructure/Data/Migrations/AddCustomerLocationFields.cs`
- [x] **T002** [P] Create EF migration for customer soft deletion in `src/server/PigFarmManagement.Server/Infrastructure/Data/Migrations/AddCustomerSoftDeletion.cs`
- [x] **T003** [P] Update CustomerEntity with location properties (Latitude, Longitude) in `src/server/PigFarmManagement.Server/Infrastructure/Data/Entities/CustomerEntity.cs`
- [x] **T004** [P] Update CustomerEntity with deletion tracking (IsDeleted, DeletedAt, DeletedBy) in `src/server/PigFarmManagement.Server/Infrastructure/Data/Entities/CustomerEntity.cs`
- [x] **T005** [P] Update Customer shared model with location properties in `src/shared/PigFarmManagement.Shared/Domain/Entities.cs`
- [x] **T006** [P] Update Customer shared model with deletion tracking in `src/shared/PigFarmManagement.Shared/Domain/Entities.cs`

## Phase 2: New DTOs and Models

- [x] **T007** [P] Create CustomerLocationDto in `src/shared/PigFarmManagement.Shared/Models/CustomerLocationDto.cs`
- [x] **T008** [P] Create CustomerDeletionRequest DTO in `src/shared/PigFarmManagement.Shared/Models/CustomerDeletionRequest.cs`
- [x] **T009** [P] Create CustomerDeletionValidation DTO in `src/shared/PigFarmManagement.Shared/Models/CustomerDeletionValidation.cs`
- [x] **T010** [P] Create ViewMode enum in `src/shared/PigFarmManagement.Shared/Models/ViewMode.cs`

## Phase 3: Enhanced Services

- [ ] **T011** [P] Create CustomerLocationService in `src/server/PigFarmManagement.Server/Services/CustomerLocationService.cs`
- [ ] **T012** [P] Create CustomerDeletionService in `src/server/PigFarmManagement.Server/Services/CustomerDeletionService.cs`
- [ ] **T013** Enhance CustomerRepository with soft deletion queries in `src/server/PigFarmManagement.Server/Infrastructure/Data/Repositories/CustomerRepository.cs`
- [ ] **T014** Enhance CustomerService with location management in `src/server/PigFarmManagement.Server/Features/Customers/CustomerService.cs`
- [ ] **T015** Enhance CustomerService with deletion validation in `src/server/PigFarmManagement.Server/Features/Customers/CustomerService.cs`

## Phase 4: POS Integration Enhancement

- [ ] **T016** Enhance PosposImporter to exclude location fields in `src/server/PigFarmManagement.Server/Services/PosposImporter.cs`
- [ ] **T017** Add manual POS sync trigger endpoint in `src/server/PigFarmManagement.Server/Controllers/ImportPosMemberController.cs`

## Phase 5: New API Endpoints

- [ ] **T018** [P] Create customer deletion validation endpoint in `src/server/PigFarmManagement.Server/Features/Customers/CustomerEndpoints.cs`
- [ ] **T019** [P] Create customer deletion endpoint (soft/hard) in `src/server/PigFarmManagement.Server/Features/Customers/CustomerEndpoints.cs`
- [ ] **T020** [P] Create customer location update endpoint in `src/server/PigFarmManagement.Server/Features/Customers/CustomerEndpoints.cs`

## Phase 6: Client Services

- [ ] **T021** [P] Create CustomerLocationService client in `src/client/PigFarmManagement.Client/Services/CustomerLocationService.cs`
- [ ] **T022** Enhance CustomerService client with deletion methods in `src/client/PigFarmManagement.Client/Features/Customers/Services/CustomerService.cs`
- [ ] **T023** Enhance CustomerService client with location methods in `src/client/PigFarmManagement.Client/Features/Customers/Services/CustomerService.cs`

## Phase 7: New UI Components

- [ ] **T024** [P] Create CustomerLocationMap component in `src/client/PigFarmManagement.Client/Features/Customers/Components/CustomerLocationMap.razor`
- [ ] **T025** [P] Create CustomerTableView component in `src/client/PigFarmManagement.Client/Features/Customers/Components/CustomerTableView.razor`
- [ ] **T026** [P] Create DeleteCustomerDialog component in `src/client/PigFarmManagement.Client/Features/Customers/Components/DeleteCustomerDialog.razor`
- [ ] **T027** [P] Create ViewModeToggle component in `src/client/PigFarmManagement.Client/Features/Customers/Components/ViewModeToggle.razor`

## Phase 8: Enhanced Existing Components

- [ ] **T028** Enhance EditCustomerDialog with location fields in `src/client/PigFarmManagement.Client/Features/Customers/Components/EditCustomerDialog.razor`
- [ ] **T029** Enhance CustomerManagement with delete functionality in `src/client/PigFarmManagement.Client/Features/Customers/Pages/CustomerManagement.razor`
- [ ] **T030** Enhance CustomerManagement with view mode switching in `src/client/PigFarmManagement.Client/Features/Customers/Pages/CustomerManagement.razor`
- [ ] **T031** Enhance CustomerManagement with POS sync trigger in `src/client/PigFarmManagement.Client/Features/Customers/Pages/CustomerManagement.razor`

## Phase 9: Google Maps Integration

- [ ] **T032** [P] Add Google Maps API script reference in `src/client/PigFarmManagement.Client/wwwroot/index.html`
- [ ] **T033** [P] Create Google Maps JavaScript interop in `src/client/PigFarmManagement.Client/wwwroot/js/google-maps.js`
- [ ] **T034** [P] Create GoogleMapsService for IJSRuntime in `src/client/PigFarmManagement.Client/Services/GoogleMapsService.cs`

## Phase 10: Configuration and Setup

- [ ] **T035** [P] Add Google Maps API key configuration in `src/server/PigFarmManagement.Server/appsettings.json`
- [ ] **T036** [P] Update service registration for new services in `src/server/PigFarmManagement.Server/Program.cs`
- [ ] **T037** [P] Update client service registration in `src/client/PigFarmManagement.Client/Program.cs`

## Phase 11: Database Updates

- [ ] **T038** Run EF migrations to update database schema (manual command: `dotnet ef database update`)
- [ ] **T039** Verify database schema changes with sample data insertion

## Phase 12: Manual Testing & Validation

- [ ] **T040** Execute quickstart.md testing scenarios manually
- [ ] **T041** Verify customer deletion flow with relationship validation
- [ ] **T042** Test Google Maps location display and coordinate entry
- [ ] **T043** Validate view mode switching (card ↔ table)
- [ ] **T044** Test POS sync with location preservation
- [ ] **T045** Verify error handling for invalid coordinates and failed API calls

## Dependencies

### Sequential Dependencies
- **Database Layer**: T001, T002 → T003, T004, T005, T006 → T038, T039
- **Services**: T003-T006 → T011, T012, T013 → T014, T015, T016, T017
- **APIs**: T014, T015 → T018, T019, T020
- **Client Services**: T018-T020 → T021, T022, T023
- **Components**: T021-T023 → T024-T031
- **Google Maps**: T032, T033 → T034 → T024
- **Configuration**: T035, T036, T037 → T038
- **Testing**: All implementation tasks → T040-T045

### Parallel Groups
- **Phase 1**: T001, T002 can run together; T003, T004 can run together; T005, T006 can run together
- **Phase 2**: T007, T008, T009, T010 all parallel
- **Phase 5**: T018, T019, T020 all parallel (different endpoints)
- **Phase 7**: T024, T025, T026, T027 all parallel (different components)
- **Phase 9**: T032, T033, T034 mostly parallel (T034 needs T032, T033)
- **Phase 10**: T035, T036, T037 all parallel

## Validation Checklist

- [x] All entities have model update tasks (CustomerEntity, Customer)
- [x] All new services have creation tasks
- [x] All API endpoints have implementation tasks
- [x] All UI components have creation/enhancement tasks
- [x] Google Maps integration has complete setup
- [x] Database migrations are planned
- [x] Manual testing scenarios are defined
- [x] No automated test dependencies (per user request)
- [x] Each task specifies exact file path
- [x] Parallel tasks are truly independent

## Notes

- **Manual Testing Only**: All automated tests (unit, integration, contract) excluded per user request
- **Google Maps**: API key required for location features to work
- **Soft Deletion**: Customers marked as deleted but data preserved for audit
- **POS Sync**: Location data excluded from POS updates (manual entry only)
- **View Persistence**: Session-only (no localStorage per user requirement)
- **Error Handling**: Graceful degradation for Google Maps and POS API failures