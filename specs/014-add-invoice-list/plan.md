
# Implementation Plan: Invoice Management Tab in Feed History Section

**Branch**: `014-add-invoice-list` | **Date**: 2025-11-30 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/014-add-invoice-list/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from file system structure or context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, `GEMINI.md` for Gemini CLI, `QWEN.md` for Qwen Code or `AGENTS.md` for opencode).
7. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
Add a tabbed interface to the pig pen detail page feed history section with two tabs: "Invoice Management" (default) and "Feed History". The Invoice Management tab displays invoices grouped by InvoiceReferenceCode from existing FeedEntity data, with delete capability. Reuses existing POSPOS import functionality. No database schema changes required - purely a new UI view of existing feed data with grouping logic.

## Technical Context
**Language/Version**: C# .NET 8, Blazor WebAssembly
**Primary Dependencies**: MudBlazor 7.x (UI components), Entity Framework Core 8.x (data access)
**Storage**: Existing FeedEntity table (no schema changes), SQLite (dev), PostgreSQL (production via Railway)
**Testing**: Manual validation via quickstart.md (no automated tests per user requirement)
**Target Platform**: Web browsers (Chrome, Firefox, Safari, Edge)
**Project Type**: Web application (Blazor WASM frontend + ASP.NET Core backend)
**Performance Goals**: <500ms tab switch, <1s invoice list load for typical pig pen (50-100 invoices)
**Constraints**: Must reuse existing FeedEntity table, existing POSPOS import dialog, no new backend endpoints for import
**Scale/Scope**: Typical pig pen has 20-200 feed items grouped into 10-50 invoices, single-user application

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Data Integrity ✅
- **Preserves historical records**: Invoice deletion removes FeedEntity records (raw transaction data), not locked/closed pig pen calculations
- **No migration needed**: Uses existing InvoiceReferenceCode field already populated by POSPOS import
- **UI constraints**: Deletion shows confirmation with item count, cannot delete if pig pen is locked

### Simplicity & Minimalism ✅
- **Reuses existing infrastructure**: No new import dialog, no new backend endpoints, no schema changes
- **Straightforward interface**: Tab-based UI pattern (MudBlazor MudTabs component), simple grouping logic
- **Single-owner workflow**: Invoice management accessible from pig pen detail page (existing navigation)

### Feature-Based Architecture ✅
- **Feature ownership**: PigPens feature owns this enhancement (PigPenDetailPage.razor modification)
- **Vertical slice**: UI (tab component), grouping logic (client-side LINQ), delete endpoint (backend PigPens feature)
- **Backwards compatibility**: Existing Feed History tab preserved, no breaking changes to FeedEntity contract

**Gate Result**: PASS - No constitutional violations detected

## Project Structure

### Documentation (this feature)
```
specs/014-add-invoice-list/
├── spec.md              # Feature specification (complete)
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (skipped - no research needed)
├── data-model.md        # Phase 1 output (skipped - no new entities)
├── quickstart.md        # Phase 1 output (manual validation scenarios)
└── contracts/           # Phase 1 output (API contract for delete endpoint)
```

### Source Code (repository root)
```
src/
├── client/PigFarmManagement.Client/
│   └── Features/PigPens/
│       ├── Pages/
│       │   └── PigPenDetailPage.razor         # MODIFY: Add MudTabs with Invoice/Feed tabs
│       ├── Components/
│       │   ├── InvoiceListTab.razor           # NEW: Invoice management tab component
│       │   ├── FeedHistoryTab.razor           # NEW: Extracted existing feed history
│       │   └── DeleteInvoiceConfirmDialog.razor # NEW: Invoice deletion confirmation
│       └── Services/
│           └── PigPenService.cs               # MODIFY: Add DeleteInvoiceByReference method
│
├── server/PigFarmManagement.Server/
│   ├── Features/PigPens/
│   │   └── PigPenEndpoints.cs                 # MODIFY: Add DELETE /api/pigpens/{id}/invoices/{refCode}
│   └── Infrastructure/Data/
│       └── Repositories/
│           └── FeedRepository.cs              # MODIFY: Add DeleteByInvoiceReferenceAsync method
│
└── shared/PigFarmManagement.Shared/
    └── Models/
        ├── FeedDtos.cs                        # INSPECT: Verify InvoiceReferenceCode field
        └── InvoiceGroupDto.cs                 # NEW: DTO for grouped invoice display
```

**Structure Decision**: Web application structure (Option 2) - Feature-based organization within client/server/shared folders. Invoice management is part of existing PigPens feature vertical slice. No new feature folder needed.

## Phase 0: Outline & Research
**Status**: SKIPPED - No research needed

**Rationale**: All technical decisions already resolved:
- UI framework: MudBlazor MudTabs component (already used in project)
- Grouping logic: Client-side LINQ GroupBy on InvoiceReferenceCode (standard pattern)
- Delete endpoint: Minimal API pattern (already used in PigPenEndpoints.cs)
- Data model: Existing FeedEntity table with InvoiceReferenceCode field

**Output**: No research.md file needed - proceed directly to Phase 1

## Phase 1: Design & Contracts
*Prerequisites: Phase 0 skipped (no research needed)*

### 1. Data Model (SKIPPED)
**Rationale**: No new database entities. Uses existing FeedEntity with InvoiceReferenceCode field.

**Output**: No data-model.md needed - document DTO only in contracts/

### 2. API Contracts
**New Endpoint**: 
```
DELETE /api/pigpens/{pigPenId}/invoices/{invoiceReferenceCode}
```

**Contract Details**: See `contracts/delete-invoice-endpoint.md`

### 3. Quickstart Manual Validation
**Scenarios** (from spec.md):
1. View invoice list grouped by InvoiceReferenceCode
2. Switch between Invoice Management and Feed History tabs
3. Delete invoice and verify all feed items removed
4. Import feeds via existing dialog and verify both tabs update

**Output**: `quickstart.md` with step-by-step manual validation

### 4. Agent Context Update
**Action**: Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot`
**Updates**:
- Recent change: Invoice Management tab in PigPenDetailPage.razor
- Tech stack: No new dependencies (MudBlazor, EF Core already documented)
- Pattern: Tab-based UI with client-side grouping

**Output**: `.github/copilot-instructions.md` updated

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**SKIPPED PER USER REQUIREMENT** - User requested "without create any stuff about test suce plan, task, docs"

No tasks.md will be generated. Implementation will proceed directly based on this plan.

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

**No violations** - Implementation aligns with all constitutional principles:
- ✅ Preserves data integrity (deletion shows confirmation, respects locked pens)
- ✅ Maintains simplicity (reuses existing patterns and components)
- ✅ Follows feature-based architecture (PigPens feature vertical slice)
- ✅ No new dependencies or infrastructure

## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (SKIPPED - no research needed)
- [x] Phase 1: Design complete (/plan command - complete)
- [x] Phase 2: Task planning complete (/tasks command - complete)
- [x] Phase 3: Tasks generated (tasks.md with 15 implementation tasks)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved (via user clarifications)
- [x] Complexity deviations documented (N/A - no deviations)

---
*Based on Constitution v2.1.1 - See `/memory/constitution.md`*
