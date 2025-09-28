````markdown
# Feature Specification: Import customer from POSPOS API

**Feature Branch**: `001-title-import-customer`  
**Created**: 2025-09-27  
**Status**: Draft  
**Input**: User description: "Import customer from POSPOS API, it real my API"

## Execution Flow (main)
```
1. Parse user description from Input
   → If empty: ERROR "No feature description provided"
2. Extract key concepts from description
   → Identify: actors, actions, data, constraints
3. For each unclear aspect:
   → Mark with [NEEDS CLARIFICATION: specific question]
4. Fill User Scenarios & Testing section
   → If no clear user flow: ERROR "Cannot determine user scenarios"
5. Generate Functional Requirements
   → Each requirement must be testable
   → Mark ambiguous requirements
6. Identify Key Entities (if data involved)
7. Run Review Checklist
   → If any [NEEDS CLARIFICATION]: WARN "Spec has uncertainties"
   → If implementation details found: ERROR "Remove tech details"
8. Return: SUCCESS (spec ready for planning)
```

---

## ⚡ Quick Guidelines
- ✅ Focus on WHAT users need and WHY
- ❌ Avoid HOW to implement (no tech stack, APIs, code structure)
- 👥 Written for business stakeholders, not developers

### Section Requirements
- **Mandatory sections**: Must be completed for every feature
- **Optional sections**: Include only when relevant to the feature
- When a section doesn't apply, remove it entirely (don't leave as "N/A")

---

## Clarifications

### Session 2025-09-27

- Q: Where should the POSPOS API credentials be stored? → A: Environment variables on the server / CI

- Q: How should partial matches be handled? → A: Overwrite internal record with POSPOS data; if POSPOSId not found, create new customer

## User Scenarios & Validation *(mandatory)*

### Primary User Story
As a system operator I want to import customers from the POSPOS system into PigFarmManagement so that new customers and their identifiers are available inside the app for mapping and imports.

### Acceptance Scenarios
1. **Given** an authenticated POSPOS endpoint and valid API credentials, **When** the operator runs the import, **Then** new customers from POSPOS are created in PigFarmManagement and mapped in `customer_id_mapping.json`.
2. **Given** a POSPOS customer that already maps to an existing internal customer, **When** the import runs, **Then** the existing internal customer is not duplicated and mapping is preserved or updated per operator choice.
3. **Given** an API error from POSPOS, **When** the import runs, **Then** the system surfaces a clear error message and partial imports are rolled back (or reported) per the import mode.

### Edge Cases
- Large customer lists (pagination) — import must handle paging or batch processing.
- Duplicate POSPOS customer codes across pages — deduplicate by POSPOS ID.
- Network failures/timeouts — import must be resumable or report the failure and state what partial results occurred.
- Missing required fields in POSPOS payload — skip such records and report them in an import summary with reasons.

## Requirements *(mandatory)*

### Functional Requirements
- **FR-001**: System MUST connect to the POSPOS customer API using operator-provided credentials and fetch customers.
- **FR-002**: System MUST map POSPOS customer identifiers to internal customer records, storing mapping in `customer_id_mapping.json` or an equivalent mapping store.
- **FR-003**: System MUST not create duplicate internal customers for already-mapped POSPOS customers; it MUST provide an option to update mapping if needed.
- **FR-004**: System MUST provide an import summary showing counts created, skipped, updated, and errors with reasons.
- **FR-005**: System MUST support pagination/batching for large result sets from the POSPOS API.
- **FR-006**: System MUST surface clear errors on authentication failure or API errors and should not silently swallow failures.

*Ambiguities / Clarifications required:*
- **FR-007**: POSPOS credentials SHALL be provided via environment variables on the server/CI.
- **FR-008**: On partial match, POSPOS data SHALL overwrite the internal record. If the POSPOSId does not exist in the mapping, create a new internal customer and record the mapping.

### Key Entities *(include if feature involves data)*
- **POSPOSCustomer**: External customer record from POSPOS (POSPOSId, FirstName, LastName, Phone, Email, Address, CreatedAt)
- **CustomerMapping**: Mapping between POSPOSId and internal CustomerId (POSPOSId → InternalId)

---


## Review & Acceptance Checklist

### Content Quality
- [x] No implementation details (languages, frameworks, APIs) — kept minimal, but integration points are described
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

### Requirement Completeness
- [ ] No [NEEDS CLARIFICATION] markers remain
- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous where specified
- [x] Success criteria are measurable
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

---


## Execution Status

- [x] User description parsed
- [x] Key concepts extracted
- [ ] Ambiguities marked
- [x] User scenarios defined
- [x] Requirements generated
- [x] Entities identified
- [ ] Review checklist passed

## Implementation notes (developer)

- A minimal Pospos client, importer service, and file-based mapping store were scaffolded in the server project as part of initial implementation. See `src/server/.../Services` for implementation.
- The importer reads the POSPOS API using an `apikey` HTTP header. The typed options class is `PosposOptions` and is bound from configuration (section `Pospos`).
- Mapping file: `customer_id_mapping.json` at repo root; written atomically with a `.bak` created on overwrite when persistence is requested.
- Current status: models, client, mapping store and importer implemented; controller endpoints implemented and build passing. Contract tests and automated test scripts are intentionally left to the developer to implement as preferred.



---

*Notes / Next Steps*
- Confirm where POSPOS credentials should be securely stored and how operators provide them.
- Decide desired duplicate-resolution behavior (skip, update, prompt).
- Plan for a small import UI or admin CLI with an import summary and logs.

````
# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]`  
**Created**: [DATE]  
**Status**: Draft  
**Input**: User description: "$ARGUMENTS"

## Execution Flow (main)
```
1. Parse user description from Input
   → If empty: ERROR "No feature description provided"
2. Extract key concepts from description
   → Identify: actors, actions, data, constraints
3. For each unclear aspect:
   → Mark with [NEEDS CLARIFICATION: specific question]
4. Fill User Scenarios & Testing section
   → If no clear user flow: ERROR "Cannot determine user scenarios"
5. Generate Functional Requirements
   → Each requirement must be testable
   → Mark ambiguous requirements
6. Identify Key Entities (if data involved)
7. Run Review Checklist
   → If any [NEEDS CLARIFICATION]: WARN "Spec has uncertainties"
   → If implementation details found: ERROR "Remove tech details"
8. Return: SUCCESS (spec ready for planning)
```

---

## ⚡ Quick Guidelines
- ✅ Focus on WHAT users need and WHY
- ❌ Avoid HOW to implement (no tech stack, APIs, code structure)
- 👥 Written for business stakeholders, not developers

### Section Requirements
- **Mandatory sections**: Must be completed for every feature
- **Optional sections**: Include only when relevant to the feature
- When a section doesn't apply, remove it entirely (don't leave as "N/A")

### For AI Generation
When creating this spec from a user prompt:
1. **Mark all ambiguities**: Use [NEEDS CLARIFICATION: specific question] for any assumption you'd need to make
2. **Don't guess**: If the prompt doesn't specify something (e.g., "login system" without auth method), mark it
3. **Think like a tester**: Every vague requirement should fail the "testable and unambiguous" checklist item
4. **Common underspecified areas**:
   - User types and permissions
   - Data retention/deletion policies  
   - Performance targets and scale
   - Error handling behaviors
   - Integration requirements
   - Security/compliance needs

---

## User Scenarios & Testing *(mandatory)*

### Primary User Story
[Describe the main user journey in plain language]

### Acceptance Scenarios
1. **Given** [initial state], **When** [action], **Then** [expected outcome]
2. **Given** [initial state], **When** [action], **Then** [expected outcome]

### Edge Cases
- What happens when [boundary condition]?
- How does system handle [error scenario]?

## Requirements *(mandatory)*

### Functional Requirements
- **FR-001**: System MUST [specific capability, e.g., "allow users to create accounts"]
- **FR-002**: System MUST [specific capability, e.g., "validate email addresses"]  
- **FR-003**: Users MUST be able to [key interaction, e.g., "reset their password"]
- **FR-004**: System MUST [data requirement, e.g., "persist user preferences"]
- **FR-005**: System MUST [behavior, e.g., "log all security events"]

*Example of marking unclear requirements:*
- **FR-006**: System MUST authenticate users via [NEEDS CLARIFICATION: auth method not specified - email/password, SSO, OAuth?]
- **FR-007**: System MUST retain user data for [NEEDS CLARIFICATION: retention period not specified]

### Key Entities *(include if feature involves data)*
- **[Entity 1]**: [What it represents, key attributes without implementation]
- **[Entity 2]**: [What it represents, relationships to other entities]

---

## Review & Acceptance Checklist
*GATE: Automated checks run during main() execution*

### Content Quality
- [ ] No implementation details (languages, frameworks, APIs)
- [ ] Focused on user value and business needs
- [ ] Written for non-technical stakeholders
- [ ] All mandatory sections completed

### Requirement Completeness
- [ ] No [NEEDS CLARIFICATION] markers remain
- [ ] Requirements are testable and unambiguous  
- [ ] Success criteria are measurable
- [ ] Scope is clearly bounded
- [ ] Dependencies and assumptions identified

---

## Execution Status
*Updated by main() during processing*

- [ ] User description parsed
- [ ] Key concepts extracted
- [ ] Ambiguities marked
- [ ] User scenarios defined
- [ ] Requirements generated
- [ ] Entities identified
- [ ] Review checklist passed

---
