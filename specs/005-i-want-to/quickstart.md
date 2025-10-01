# Quickstart: Import POSPOS Stock to Feed Formula

## Prerequisites
- .NET 8 SDK
- POSPOS API access
- Database setup

## Setup
1. Clone repository and checkout branch `005-i-want-to`
2. Run `dotnet restore`
3. Update POSPOS API configuration

## Test Scenarios

### Scenario 1: Successful Import
1. Prepare POSPOS product JSON data
2. Call POST /api/feed-formulas with import request
3. Verify FeedFormula created with correct fields
4. Check pigpen feed history displays code, name, unit name

### Scenario 2: Handle Network Timeout
1. Simulate POSPOS API timeout
2. Attempt import
3. Verify graceful failure with logged error
4. Check fallback behavior

### Scenario 3: Duplicate Product Codes
1. Import product with existing code
2. Verify duplicate handled (logged, skipped)
3. Check no data corruption

### Scenario 4: Profit Calculation
1. Create FeedFormula with cost
2. Calculate special price using cost
3. Verify profit margin applied correctly