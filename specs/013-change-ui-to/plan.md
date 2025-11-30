# Implementation Plan: Thai Language UI Conversion

**Branch**: `013-change-ui-to` | **Date**: 2025-11-30 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/013-change-ui-to/spec.md`

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
Convert all user-facing UI text from English to Thai language (hardcoded, no language switcher). Includes buttons, labels, forms, tables, dialogs, notifications, and validation messages. Dates use ISO format, numbers use Arabic numerals with Thai separators, and currency displays with ฿ symbol. Technical errors and logs remain in English.

## Technical Context
**Language/Version**: C# .NET 8, Blazor WebAssembly  
**Primary Dependencies**: MudBlazor 7.x (UI components), ASP.NET Core 8  
**Storage**: N/A (UI translation only, no database changes)  
**Testing**: Manual validation via quickstart scenarios (no automated tests per user request)  
**Target Platform**: Web browsers (Blazor WASM client)
**Project Type**: web (frontend + backend structure)  
**Performance Goals**: No performance impact (static text replacement)  
**Constraints**: Must preserve existing UI layouts, Thai Unicode support required  
**Scale/Scope**: ~50-100 UI components across customer, pig pen, feed management features

**User-provided context**: generate plan without test plan, test docs

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

✅ **Simplicity**: Pure UI translation with no new abstractions or frameworks (hardcoded Thai text)  
✅ **Data Integrity**: No database schema changes, no data migrations  
✅ **Feature-Based**: Changes isolated to existing feature components (customers, pig pens, feeds)  
✅ **Single Owner**: UI layer changes only (Blazor components)  
✅ **No Over-Engineering**: Direct string replacement, no i18n framework overhead

**Status**: ✅ No violations detected - proceed to Phase 0

## Project Structure

### Documentation (this feature)
```
specs/[###-feature]/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
src/
├── client/                              # Blazor WebAssembly frontend
│   └── Features/
│       ├── Customers/
│       │   ├── Pages/
│       │   └── Components/
│       ├── PigPens/
│       │   ├── Pages/
│       │   └── Components/
│       ├── Feeds/
│       │   ├── Pages/
│       │   └── Components/
│       └── Shared/
│           └── Components/
├── server/                              # ASP.NET Core backend
│   └── Features/
│       ├── Customers/
│       ├── PigPens/
│       └── Feeds/
└── shared/                              # DTOs and domain models
    └── DTOs/
```

**Structure Decision**: Web application (Option 2) - Feature-based architecture with Blazor client and ASP.NET Core server. Thai translation applies primarily to `src/client/Features/` Razor components. No backend API changes required (UI-only modification).

## Phase 0: Outline & Research
1. **Extract unknowns from Technical Context** above:
   - None - Technical context fully resolved (C# .NET 8, Blazor WASM, MudBlazor 7.x, manual testing)
   
2. **Generate research.md** with sections:
   - **Thai Unicode Rendering**: Research browser support for Thai characters, font fallback requirements, layout handling for Thai script (longer character widths)
   - **MudBlazor Localization Patterns**: Investigate MudBlazor built-in localization (date pickers, validation messages) and how to override with Thai equivalents
   - **Blazor Hardcoded Translation Strategy**: Document pattern for replacing English strings in Razor markup and C# code-behind without resource files
   - **Number/Currency Formatting**: Research .NET CultureInfo for Thai locale (th-TH) for number separators and Baht symbol (฿)
   - **Layout Overflow Prevention**: Strategies for handling Thai text being longer than English in fixed-width buttons/labels
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:
   ```
   For each unknown in Technical Context:
     Task: "Research {unknown} for {feature context}"
   For each technology choice:
     Task: "Find best practices for {tech} in {domain}"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

**Output**: research.md with all NEEDS CLARIFICATION resolved

## Phase 1: Design & Contracts
*Prerequisites: research.md complete ✅*

1. **Extract entities from feature spec** → `data-model.md`:
   - **No new entities required** - Thai UI conversion is pure presentation layer change
   - Document: Global culture configuration (CultureInfo("th-TH") in Program.cs)
   - Document: Translation mapping strategy (hardcoded strings, no resource files)
   - Output: Minimal data-model.md documenting culture setup (not database entities)

2. **Generate API contracts** from functional requirements:
   - **No API contract changes** - UI-only modification, backend APIs unchanged
   - Existing endpoints serve same data, only client presentation layer changes
   - Output: contracts/ directory with note "No API changes for UI translation"

3. **Produce contract artifacts** from contracts:
   - **N/A** - No contracts to document (UI-only feature)

4. **Extract manual validation scenarios** from user stories:
   - Scenario 1: Verify all customer management pages display Thai labels/buttons
   - Scenario 2: Verify all pig pen pages display Thai text with correct date/number formats
   - Scenario 3: Verify all feed management dialogs show Thai validation messages
   - Scenario 4: Verify currency displays with ฿ symbol in correct format (฿1,234.56)
   - Scenario 5: Verify Thai text does not overflow in dialogs/tables (responsive check)
   - Scenario 6: Verify English remains in logs, technical errors, API responses
   - Output: `quickstart.md` with step-by-step manual validation checklist

5. **Update agent file incrementally** (O(1) operation):
   - Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot`
     **IMPORTANT**: Execute it exactly as specified above. Do not add or remove any arguments.
   - Add entry for Feature 013: Thai Language UI Conversion
   - Preserve manual additions between markers
   - Update recent changes (keep last 3)
   - Keep under 150 lines for token efficiency
   - Output to `.github/copilot-instructions.md`

**Output**: data-model.md (minimal), contracts/ (empty/note), quickstart.md, .github/copilot-instructions.md

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Load `.specify/templates/tasks-template.md` as base
- Generate tasks from Phase 1 design docs (quickstart.md primarily, since no contracts/entities)
- **No contract tests**: No API changes (UI-only feature)
- **No model creation**: No database entities (presentation layer only)
- **No integration tests**: Per user requirement "without test plan, test docs"
- **Translation tasks**: One task per feature area (Customers, PigPens, Feeds, Shared)
- **Configuration task**: Global culture setup in Program.cs
- **Validation tasks**: Manual validation per quickstart.md scenarios

**Task Categories**:
1. **Configuration** [P]: Set up CultureInfo in Program.cs
2. **Customer Feature Translation** [P]: Translate all customer-related Razor components and DTOs
3. **Pig Pen Feature Translation** [P]: Translate all pig pen-related Razor components and DTOs
4. **Feed Feature Translation** [P]: Translate all feed-related Razor components and DTOs
5. **Shared Components Translation** [P]: Translate navigation, layout, shared dialogs
6. **Manual Validation**: Execute quickstart.md scenarios (6 scenarios)

**Ordering Strategy**:
- Configuration first (blocks all other tasks - culture must be set before testing)
- Translation tasks in parallel [P] (independent feature slices)
- Manual validation last (requires all translations complete)

**Estimated Output**: 7-10 tasks in tasks.md (simplified due to no tests)

**Note**: User explicitly requested "generate plan without test plan, test docs" - so no automated testing tasks will be generated. Validation is purely manual via quickstart.md.

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

**No violations detected** - This section is empty.

Thai language UI conversion adheres to all constitutional principles:
- ✅ Simplicity: Direct string replacement, no new abstractions
- ✅ Data Integrity: No database changes
- ✅ Feature-Based: Changes isolated to existing feature components
- ✅ Single Owner: UI layer only
- ✅ No Over-Engineering: No i18n framework, hardcoded approach


## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete ✅ (research.md generated)
- [x] Phase 1: Design complete ✅ (data-model.md, contracts/README.md, quickstart.md, .github/copilot-instructions.md updated)
- [x] Phase 2: Task planning complete ✅ (approach documented above)
- [x] Phase 3: Tasks generated ✅ (tasks.md created with 26 tasks)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS ✅
- [x] Post-Design Constitution Check: PASS ✅ (re-evaluated after Phase 1)
- [x] All NEEDS CLARIFICATION resolved ✅ (no unknowns in Technical Context)
- [x] Complexity deviations documented ✅ (none - no violations)

**Artifacts Generated**:
- [x] research.md (5 research areas documented)
- [x] data-model.md (minimal - cultural configuration only)
- [x] contracts/README.md (documents no API changes)
- [x] quickstart.md (6 manual validation scenarios)
- [x] .github/copilot-instructions.md (Feature 013 entry added)
- [x] tasks.md (26 tasks: 3 setup, 16 translation, 7 validation)

**Next Command**: Begin implementation - Execute tasks T001-T003 (setup), then parallel translation groups

---
*Based on Constitution v2.1.1 - See `/memory/constitution.md`*
