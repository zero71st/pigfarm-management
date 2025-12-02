# Feature Specification: Dashboard Thai Translation and Business Metrics Enhancement

**Feature Branch**: `016-update-dashboard-to`  
**Created**: 2025-12-02  
**Status**: Draft  
**Input**: User description: "Update dashboard to show how business and Translate to thai learn from existing dashboard first"

## Execution Flow (main)
```
1. Parse user description from Input
   → Feature request: Translate dashboard to Thai + Enhance business visibility
   → Updated: Focus on active pig pens only, reorganize metrics, add pagination
2. Extract key concepts from description
   → Actors: Business owners, farm managers
   → Actions: View business performance, understand financial metrics
   → Data: Dashboard metrics (active only, no closed pens)
   → Constraints: Must preserve existing functionality, Thai language conversion
3. Analyze existing dashboard structure
   → Current: English labels, basic metrics (pigs, investment, profit/loss, ROI)
   → New: Two metric rows (Activity + Financial), remove Overall Performance section
4. Fill User Scenarios & Testing section
   → Primary: Business owner views dashboard in Thai to understand farm performance
5. Generate Functional Requirements
   → Thai translation for all dashboard text
   → Filter to active pig pens only (exclude closed)
   → Reorganize metrics into Activity and Financial rows
   → Add pagination to customer table (10 per page default)
6. Identify Key Entities
   → Dashboard metrics (active only): TotalActivePigPens, TotalActivePigs, TotalActiveCustomers
   → Financial metrics: TotalInvestment, TotalCost, TotalProfit
7. Run Review Checklist
   → Clear scope: UI translation + metric reorganization + active-only filtering
8. Return: SUCCESS (spec ready for planning)
```

---

## ⚡ Quick Guidelines
- ✅ Focus on WHAT users need and WHY
- ❌ Avoid HOW to implement (no tech stack, APIs, code structure)
- 👥 Written for business stakeholders, not developers

---

## Clarifications

### Session 2025-12-02
- Q: How is an "active" pig pen defined for dashboard filtering purposes? → A: Status field NOT equal to "Closed" (any status except "Closed" is active)
- Q: What should happen when a customer appears in multiple pig pen types (both Cash and Project)? → A: One table showing all customers, with each customer appearing once (their metrics combine all their pens regardless of type)
- Q: What specific statuses exist for pig pens beyond "Closed"? → A: Just Active and Closed
- Q: Should the Cash vs Project breakdown sections show the same 4 metrics, or different metrics? → A: Same 4 metrics in both (Active Pens, Pigs, Investment, Profit/Loss)
- Q: When no active pig pens exist, should pagination controls be hidden or disabled in the customer table? → A: Hidden (pagination UI completely removed when 0 customers)

---

## User Scenarios & Testing

### Primary User Story
As a **farm business owner or manager**, I want to view my dashboard with Thai language labels showing only active pig pens (not closed) so that I can easily understand my farm's current business performance in my native language, focusing on operational metrics and financial health.

### Acceptance Scenarios

1. **Given** I am logged into the dashboard, **When** I view the first row of metric cards, **Then** I should see three activity metrics in Thai:
   - "Total Active Pig Pens" → "คอกสุกรที่ใช้งานทั้งหมด" (excluding closed pens)
   - "Total Active Pigs" → "จำนวนสุกรที่ใช้งานทั้งหมด" (only from active pens)
   - "Total Active Customers" → "จำนวนลูกค้าที่ใช้งานทั้งหมด" (customers with active pens)

2. **Given** I am viewing the dashboard, **When** I view the second row of metric cards, **Then** I should see four financial metrics in Thai:
   - "Total Investment" → "เงินลงทุนทั้งหมด" (calculated as TotalOwnerCapital + TotalCustomerCapital + TotalProfit)
   - "Total Owner's Capital" → "เงินลงทุนส่วนเจ้าของรวม" (calculated as TotalCost - TotalDeposit)
   - "Total Customer's Capital" → "เงินลงทุนส่วนลูกค้า" (= TotalDeposit)
   - "Total Profit/Loss" → "กำไร/ขาดทุนทั้งหมด" (calculated as TotalPriceIncludeDiscount - TotalCost)

3. **Given** I am viewing the dashboard, **When** I scroll down past the metric cards, **Then** I should see the Project Operations section first, followed by the Cash Operations section (in that order)

4. **Given** I am viewing the dashboard, **When** I look at the Project and Cash breakdown sections, **Then** both section headers should be in Thai ("Project Operations" → "การดำเนินงานโครงการ", "Cash Operations" → "การดำเนินงานเงินสด") and each section should display 6 metrics in Thai: Active Pig Pens, Total Pigs, Total Investment, Total Owner's Capital, Total Customer's Capital, Profit/Loss, showing only data from active pig pens

5. **Given** the dashboard displays profit/loss amounts, **When** the amount is positive (profit), **Then** it should display in green color, **When** the amount is negative (loss), **Then** it should display in red color (existing behavior preserved)

6. **Given** I view the customer performance table, **When** it displays customer statistics, **Then** all column headers should be in Thai (e.g., "Customer Name" → "ชื่อลูกค้า", "Pig Pen Count" → "จำนวนคอก", "Total Pigs" → "จำนวนสุกร", "Total Investment" → "เงินลงทุนทั้งหมด", "Total Owner's Capital" → "เงินลงทุนส่วนเจ้าของรวม", "Total Customer's Capital" → "รวมเงินมัดจำ", "Total Profit/Loss" → "กำไร/ขาดทุนรวม") and the table should show 10 customers per page by default with pagination controls

7. **Given** I am viewing the customer performance table with more than 10 customers, **When** I interact with the pagination controls, **Then** I should be able to navigate between pages to view all customer data

8. **Given** I have closed pig pens in the system, **When** I view the dashboard, **Then** none of the metrics should include data from closed pig pens (all calculations based on active pens only)

### Edge Cases

- What happens when there are no active pig pens (all closed)?
  → Dashboard should display "0" for all activity metrics (pens, pigs, customers) and "฿0.00" for financial metrics with appropriate Thai labels, no errors
  
- How does the system display very large or very small numbers in Thai?
  → Currency and number formatting should use Thai locale (e.g., ฿1,234,567.89)
  
- What happens when a customer has only closed pig pens?
  → Customer should NOT appear in the customer performance table (active customers only)

- What happens when there are fewer than 10 customers with active pens?
  → Table should display all customers on a single page without pagination controls (pagination UI hidden)
### Functional Requirements

- **FR-001**: System MUST filter all dashboard metrics to show only active pig pens (exclude closed pens)
  - Active defined as: pig pen Status field equals "Active" (only two statuses exist: "Active" and "Closed")
  - All calculations based on pig pens where Status = "Active"
  - Closed pig pens (Status = "Closed") must not contribute to any dashboard metrics

- **FR-002**: System MUST display activity metrics row with 3 cards in Thai language
  - Card 1: "Total Active Pig Pens" → "คอกสุกรที่ใช้งานทั้งหมด" (count of active pens)
  - Card 2: "Total Active Pigs" → "จำนวนสุกรที่ใช้งานทั้งหมด" (sum of pigs in active pens)
  - Card 3: "Total Active Customers" → "จำนวนลูกค้าที่ใช้งานทั้งหมด" (count of customers with active pens)

- **FR-003**: System MUST display financial metrics row with 4 cards in Thai language
  - Card 1: "Total Investment" → "เงินลงทุนทั้งหมด" (calculated as TotalOwnerCapital + TotalCustomerCapital + TotalProfit)
  - Card 2: "Total Owner's Capital" → "เงินลงทุนส่วนเจ้าของรวม" (calculated as TotalCost - TotalDeposit from active pens)
  - Card 3: "Total Customer's Capital" → "เงินลงทุนส่วนลูกค้า" (= TotalDeposit from active pens)
  - Card 4: "Total Profit/Loss" → "กำไร/ขาดทุนทั้งหมด" (calculated as TotalPriceIncludeDiscount - TotalCost from active pens)

- **FR-004**: System MUST remove the "Overall Business Performance" section
  - This section should not appear on the dashboard
  - Relevant metrics are now in the two metric rows (FR-002 and FR-003)

- **FR-005**: System MUST display Project and Cash operations sections in specific order
  - Project Operations section ("การดำเนินงานโครงการ") displayed first
  - Cash Operations section ("การดำเนินงานเงินสด") displayed second
  - Both sections show metrics only from active pig pens
  - Each section displays 6 metrics: Active Pig Pens, Total Pigs, Total Investment, Total Owner's Capital, Total Customer's Capital, Profit/Loss

- **FR-006**: System MUST implement pagination for customer performance table
  - Default page size: 10 customers per page
  - Pagination controls displayed when total customers > 10
  - Pagination controls hidden when total customers ≤ 10
  - Users can navigate between pages

- **FR-007**: System MUST maintain existing visual indicators
  - Profit amounts displayed in green color
  - Loss amounts displayed in red color
  - Appropriate icons for each metric type (home, pets, attach_money, etc.)

- **FR-008**: System MUST use Thai currency formatting for all monetary values
  - Display format: ฿X,XXX.XX (Thai Baht symbol with comma separators and 2 decimal places)

- **FR-009**: System MUST display customer performance table with Thai column headers
  - Single unified table showing all customers (one row per customer)
  - Customer metrics aggregate all their active pig pens regardless of type (Cash + Project combined)
  - Required columns: Customer Name, Pig Pen Count, Total Pigs, Total Investment, Total Owner's Capital, Total Customer's Capital, Total Profit/Loss
  - All table headers translated to Thai
  - Data formatting preserved (currency, numbers, percentages)
  - Only customers with active pig pens shown
### Key Entities

**Updated entities (active pens only):**

- **DashboardOverview**: Aggregated business metrics (active pens only)
  - TotalActivePigPens: Count of active pig pens (exclude closed)
  - TotalActivePigs: Total number of pigs in active pens only
  - TotalActiveCustomers: Count of unique customers with at least one active pen
  - TotalInvestment: Calculated as TotalOwnerCapital + TotalCustomerCapital + TotalProfit
  - TotalOwnerCapital: Calculated as TotalCost - TotalDeposit (from active pens)
  - TotalCustomerCapital: Total deposits (= TotalDeposit from active pens)
  - TotalProfit: Calculated as TotalPriceIncludeDiscount - TotalCost (from active pens)
  - Breakdown by type: Cash vs Project operations (active pens only)
  - CustomerStats: List of per-customer performance metrics (customers with active pens only)
- **CustomerPigPenStats**: Customer-level performance data (active pens only)
  - CustomerName: Name of the customer
  - PigPenCount: Number of active pens owned (exclude closed)
  - TotalPigs: Total number of pigs in customer's active pens
  - TotalInvestment: Customer's total investment (calculated as TotalOwnerCapital + TotalCustomerCapital + TotalProfit, active pens only)
  - TotalOwnerCapital: Customer's owner capital (calculated as TotalCost - TotalDeposit, active pens only)
  - TotalCustomerCapital: Customer's deposits (= TotalDeposit, active pens only)
  - TotalProfitLoss: Customer's profit/loss (calculated as TotalPriceIncludeDiscount - TotalCost, active pens only)only)
  - TotalProfitLoss: Customer's profit/loss (active pens only)
### Non-Functional Requirements

- **NFR-001**: Translation accuracy MUST be business-appropriate and consistent
  - Use professional Thai business terminology
  - Maintain consistency across all dashboard sections

- **NFR-002**: System MUST load dashboard data within existing performance targets
  - Translation should not impact data loading speed
  - Filtering to active pens only should not significantly impact query performance

- **NFR-003**: Pagination MUST provide smooth user experience
  - Page navigation should be responsive (<500ms)
  - Current page state should be clearly indicated
**Existing entities to be preserved:**

- **DashboardOverview**: Aggregated business metrics
  - TotalActivePigPens: Count of active pig pens
  - TotalPigs: Total number of pigs across all pens
  - TotalInvestment: Sum of all investments
  - TotalProfitLoss: Net profit or loss
  - Breakdown by type: Cash vs Project operations
  - CustomerStats: List of per-customer performance metrics
### Content Quality
- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs (Thai language accessibility, focus on active operations)
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

### Requirement Completeness
- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous  
- [x] Success criteria are measurable (active-only filtering, Thai labels, metric reorganization, pagination)
- [x] Scope is clearly bounded (UI translation + metric filtering + layout reorganization)
- [x] Dependencies and assumptions identified (existing dashboard structure, pig pen status field)
  - Maintain consistency across all dashboard sections

- **NFR-002**: System MUST load dashboard data within existing performance targets
## Execution Status

- [x] User description parsed
- [x] Key concepts extracted
- [x] Ambiguities marked (none - clear requirements)
- [x] User scenarios defined (8 scenarios + 5 edge cases)
- [x] Requirements generated (FR-001 to FR-010, NFR-001 to NFR-003)
- [x] Entities identified and updated for active-only filtering
- [x] Review checklist passed
- [x] Updated per user feedback (focus on active pens, reorganize metrics, add pagination)al stakeholders
- [x] All mandatory sections completed

## Thai Translation Reference

### Core Dashboard Terms (Activity Metrics Row)
- Dashboard → แดชบอร์ด
- Total Active Pig Pens → คอกสุกรที่ใช้งานทั้งหมด
- Total Active Pigs → จำนวนสุกรที่ใช้งานทั้งหมด
- Total Active Customers → จำนวนลูกค้าที่ใช้งานทั้งหมด

### Financial Metrics Row
- Total Investment → เงินลงทุนทั้งหมด
- Total Owner's Capital → เงินลงทุนส่วนเจ้าของรวม
- Total Customer's Capital → เงินลงทุนส่วนลูกค้า
- Total Profit/Loss → กำไร/ขาดทุนทั้งหมด

### Section Headers (Order: Project first, Cash second)
- Project Operations → การดำเนินงานโครงการ
- Cash Operations → การดำเนินงานเงินสด

### Metric Labels (within Cash/Project sections)
- Active Pig Pens → คอกสุกรที่ใช้งาน
- Total Pigs → จำนวนสุกร
- Total Investment → เงินลงทุน
- Owner's Capital → เงินลงทุนส่วนเจ้าของ
- Customer's Capital → เงินลงทุนส่วนลูกค้า
- Profit/Loss → กำไร/ขาดทุน

### Customer Performance Table Headers
- Customer Name → ชื่อลูกค้า
- Status → สถานะ
- Pig Pen Count → จำนวนคอก (active only)
- Total Pigs → จำนวนสุกร (from active pens)
- Total Investment → เงินลงทุนทั้งหมด (active pens)
- Total Owner's Capital → เงินลงทุนส่วนเจ้าของรวม (active pens)
- Total Customer's Capital → รวมเงินมัดจำ (active pens)
- Total Profit/Loss → กำไร/ขาดทุนรวม (active pens)
- ROI → ผลตอบแทน (%)

### Pagination Controls
- Rows per page → แถวต่อหน้า
- Page X of Y → หน้า X จาก Y

### Formatting
- Currency: ฿1,234.56 (Thai Baht)
- Percentage: 12.5% (one decimal place)
- Numbers: 1,234 (comma separator)

### Business Calculations
- Total Customer's Capital = Total Deposit
- Total Owner's Capital = Total Cost - Total Deposit
- Total Profit/Loss = Total PriceIncludeDiscount - Total Cost
- Total Investment = Total Owner's Capital + Total Customer's Capital + Total Profit/Loss
- All metrics: Active pig pens only (exclude closed)
- Section order: Project Operations first, then Cash Operations
