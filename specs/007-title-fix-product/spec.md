```markdown
# Feature Specification: Fix product import - search & selection

**Feature Branch**: `007-title-fix-product`  
**Created**: 2025-10-05  
**Status**: Draft  
**Input**: Users should be able to search for products by code or name. Before searching, no results should be displayed. The results must match the search criteria only. Users must select one or more products before importing — importing without selection should not be allowed. Also, remove the current category filter.

## Execution Flow (main)

1. The user opens the product import UI.
2. Initially the product results list is empty / hidden.
3. The user types a search term (product code or product name) and triggers search.
4. The system performs a search constrained to the entered term and returns matching products only.
5. The UI displays the matched results; user can select one or more products.
6. Import action is enabled only when at least one product is selected.
7. Category filtering UI is removed for this workflow.

## Summary

Improve the product import workflow so that product discovery is explicit and deterministic. The UI must not show any results before the user performs a search. Search must match only on the provided code or name (no fuzzy/all results). Users must explicitly select products to import; importing without selection is prohibited. The existing category filter must be removed from the import UI.

## User Scenarios & Testing *(mandatory)*

### Primary User Story
As a user importing products, I want to search by product code or name, see only relevant matches, select specific products, and import only those selected items. This prevents accidental imports and improves performance by avoiding pre-loading the entire product catalog.

### Acceptance Scenarios
1. Given the import page is opened, when the user has not run a search, then the product results list is empty or hidden.
2. Given the user enters an exact product code and submits the search, when matching products exist, then only products whose code equals the query are returned.
3. Given the user enters a product name and submits the search, when matching products exist, then only products whose name contains the query (case-insensitive) are returned.
4. Given the search returns results, when no products are selected, then the Import button is disabled and an appropriate message or tooltip indicates selection is required.
5. Given one or more products are selected, when the user clicks Import, then the selected products are sent to the import endpoint and a success/failure summary is shown.
6. Given the user clears the search input, when no active search term exists, then the results list returns to empty/hidden.
7. Given the user attempts to filter by category, then the category filter is not present (removed from UI).

- Slow network / server error: show error message and allow retry. Import should be idempotent or guarded to avoid duplicates.

## Requirements *(mandatory)*

- FR-004: Search results MUST strictly match the query:
  - For product codes: exact match (case-insensitive).
- FR-010: The import endpoint MUST validate the payload and return per-item success/failure details.
 - FR-011: The import endpoint MUST perform upsert behavior for duplicates (idempotent): update existing records when identifiers match and report which items were created vs updated.

## Clarifications

### Session 2025-10-05

- Q: Should the product search support server-side pagination (recommended for larger catalogs), or should it return all matching products in one response?
   → A: Return all matching products in one response.
 
- Q: Should product selections persist across repeated searches or be cleared when a new search runs?
   → A: Clear selections on each new search.

- Q: How should duplicates be handled during import?
   → A: Upsert (update existing records, idempotent).
- NFR-002: The UI must be accessible: selections must be keyboard-navigable and screen-reader friendly.
- If the current UI implementation uses an automatic category-based pre-load, that logic must be removed or disabled.
- Ensure the client still uses the shared `Product` DTO shape used across the app to avoid mapping mismatches.
- [ ] Selected product ids are sent to import endpoint and validated server-side
- [ ] Accessibility and keyboard navigation verified

## Execution Status
- [x] User description parsed
- [x] Key concepts extracted
- [x] Ambiguities marked (none)
- [x] User scenarios defined
- [x] Requirements generated
- [x] Entities identified
- [ ] Review checklist ready for validation

``` # Feature Specification: [FEATURE NAME]

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
