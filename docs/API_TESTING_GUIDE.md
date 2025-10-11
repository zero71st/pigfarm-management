# PigFarm API Testing Guide - Railway Deployment

**Base URL**: `https://pigfarm-management-production.up.railway.app`  
**Date**: October 11, 2025

## Authentication Setup

### Step 1: Login to Get API Key

First, login with your admin credentials to get an API key:

```bash
# Login request
curl -X POST "https://pigfarm-management-production.up.railway.app/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "Admin123!",
    "keyLabel": "testing-session",
    "expirationDays": 30
  }'
```

**Expected Response**:
```json
{
  "message": "Login successful",
  "apiKey": "your-generated-api-key-here",
  "user": {
    "id": "...",
    "username": "admin",
    "email": "kasem.dev79@gmail.com",
    "roles": ["Admin", "User"]
  },
  "expiresAt": "2024-11-10T..."
}
```

### Step 2: Use API Key for Subsequent Requests

Copy the `apiKey` from the login response and use it in the `X-Api-Key` header for all authenticated requests.

## API Endpoints Testing

### 🔍 Health Check (Public)

```bash
curl "https://pigfarm-management-production.up.railway.app/health"
```

**Expected Response**:
```json
{
  "status": "healthy",
  "timestamp": "2025-10-11T..."
}
```

### 👤 Authentication Endpoints

#### Get Current User Info
```bash
curl "https://pigfarm-management-production.up.railway.app/api/auth/me" \
  -H "X-Api-Key: YOUR_API_KEY_HERE"
```

### 🐷 PigPen Endpoints

#### Get All PigPens
```bash
curl "https://pigfarm-management-production.up.railway.app/api/pigpens" \
  -H "X-Api-Key: YOUR_API_KEY_HERE"
```

#### Create New PigPen
```bash
curl -X POST "https://pigfarm-management-production.up.railway.app/api/pigpens" \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: YOUR_API_KEY_HERE" \
  -d '{
    "penCode": "PEN001",
    "customerId": "00000000-0000-0000-0000-000000000000",
    "pigCount": 50,
    "feedCost": 1500.00,
    "investment": 25000.00,
    "notes": "Test pen created via API"
  }'
```

#### Get PigPen by ID
```bash
curl "https://pigfarm-management-production.up.railway.app/api/pigpens/{pigpen-id}" \
  -H "X-Api-Key: YOUR_API_KEY_HERE"
```

### 👥 Customer Endpoints

#### Get All Customers
```bash
curl "https://pigfarm-management-production.up.railway.app/api/customers" \
  -H "X-Api-Key: YOUR_API_KEY_HERE"
```

#### Create New Customer
```bash
curl -X POST "https://pigfarm-management-production.up.railway.app/api/customers" \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: YOUR_API_KEY_HERE" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "code": "CUST001",
    "phone": "123-456-7890",
    "address": "123 Farm Road",
    "latitude": 13.7563,
    "longitude": 100.5018,
    "status": "Active"
  }'
```

### 🍖 Feed Formula Endpoints

#### Get Feed Formulas by Category
```bash
curl "https://pigfarm-management-production.up.railway.app/api/feed-formulas/by-category/starter" \
  -H "X-Api-Key: YOUR_API_KEY_HERE"
```

#### Calculate Feed Requirements
```bash
curl -X POST "https://pigfarm-management-production.up.railway.app/api/feed-formulas/calculate-requirements" \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: YOUR_API_KEY_HERE" \
  -d '{
    "pigCount": 100,
    "avgWeight": 25.5,
    "category": "grower"
  }'
```

### 📊 Dashboard Endpoints

#### Get Dashboard Summary
```bash
curl "https://pigfarm-management-production.up.railway.app/api/dashboard/summary" \
  -H "X-Api-Key: YOUR_API_KEY_HERE"
```

### 🔧 Admin Endpoints

#### Get All Users (Admin Only)
```bash
curl "https://pigfarm-management-production.up.railway.app/api/admin/users" \
  -H "X-Api-Key: YOUR_API_KEY_HERE"
```

#### Create New User (Admin Only)
```bash
curl -X POST "https://pigfarm-management-production.up.railway.app/api/admin/users" \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: YOUR_API_KEY_HERE" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "TestPassword123!",
    "roles": ["User"]
  }'
```

## Import Endpoints

### Import Customers from POSPOS
```bash
curl -X POST "https://pigfarm-management-production.up.railway.app/api/customers/import" \
  -H "X-Api-Key: YOUR_API_KEY_HERE"
```

### Import Feed Products
```bash
curl -X POST "https://pigfarm-management-production.up.railway.app/api/feeds/import" \
  -H "X-Api-Key: YOUR_API_KEY_HERE"
```

## Testing with PowerShell

If you prefer PowerShell, here are equivalent commands:

### Login with PowerShell
```powershell
$loginBody = @{
    username = "admin"
    password = "Admin123!"
    keyLabel = "testing-session"
    expirationDays = 30
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://pigfarm-management-production.up.railway.app/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"

# Store the API key
$apiKey = $response.apiKey
Write-Host "API Key: $apiKey"
```

### Test Endpoints with PowerShell
```powershell
# Get all customers
$headers = @{ "X-Api-Key" = $apiKey }
$customers = Invoke-RestMethod -Uri "https://pigfarm-management-production.up.railway.app/api/customers" -Headers $headers
$customers | ConvertTo-Json -Depth 3
```

## Expected Error Responses

### Unauthorized (Missing or Invalid API Key)
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

### Forbidden (Insufficient Permissions)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403
}
```

### Bad Request (Invalid Data)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "field": ["validation error message"]
  }
}
```

## Swagger Documentation

You can also test the API interactively using Swagger UI:

**URL**: `https://pigfarm-management-production.up.railway.app/swagger`

1. Visit the Swagger URL
2. Click "Authorize" 
3. Enter your API key (without any prefix)
4. Test endpoints directly from the browser

## Testing Checklist

- [ ] Health endpoint responds
- [ ] Login with admin credentials works
- [ ] API key is returned from login
- [ ] Authenticated endpoints work with API key
- [ ] Unauthorized requests are properly rejected
- [ ] CRUD operations work for main entities
- [ ] Import endpoints function correctly
- [ ] Admin endpoints work for admin users
- [ ] Swagger documentation is accessible

## Troubleshooting

### Common Issues

1. **401 Unauthorized**: Check that you're including the `X-Api-Key` header
2. **403 Forbidden**: Verify your user has the required role for the endpoint
3. **404 Not Found**: Ensure the endpoint URL is correct
4. **500 Internal Server Error**: Check Railway logs for detailed error information

### Check Railway Logs
```bash
railway logs --tail 20
```

### Test Health Endpoint First
Always start by testing the health endpoint to ensure the service is running.

---

**Last Updated**: October 11, 2025  
**Service URL**: https://pigfarm-management-production.up.railway.app