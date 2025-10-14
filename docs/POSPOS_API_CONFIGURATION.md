# POSPOS API Configuration Guide

**Last Updated**: October 11, 2025  
**Purpose**: Configure POSPOS API connection for customer and product import functionality

## Required Environment Variables

The PigFarm Management System requires the following environment variables to connect to the POSPOS API:

### 1. POSPOS API Key
```bash
railway variables --set POSPOS_API_KEY="your-actual-pospos-api-key"
```

### 2. POSPOS API Base URLs
```bash
# Product API endpoint
railway variables --set POSPOS_PRODUCT_API_BASE="https://go.pospos.co/api"

# Member/Customer API endpoint  
railway variables --set POSPOS_MEMBER_API_BASE="https://go.pospos.co/api"

# Transactions API endpoint (optional)
railway variables --set POSPOS_TRANSACTIONS_API_BASE="https://go.pospos.co/api"
```

## Where to Get Your POSPOS API Information

### 1. API Key
1. Log into your POSPOS admin panel
2. Navigate to **Settings** → **API Settings**
3. Generate or copy your API key
4. The key typically looks like: `pk_live_abcd1234...` or similar

### 2. API Endpoints
- **Base URL**: Usually `https://go.pospos.co/api`
- **Full Product Endpoint**: `https://go.pospos.co/api/products`
- **Full Member Endpoint**: `https://go.pospos.co/api/members`

> **Note**: Replace `go.pospos.co` with your actual POSPOS domain if different

## Configuration in Railway

### Set All Variables at Once
```bash
# Replace with your actual values
railway variables --set POSPOS_API_KEY="pk_live_your_api_key_here"
railway variables --set POSPOS_PRODUCT_API_BASE="https://go.pospos.co/api"
railway variables --set POSPOS_MEMBER_API_BASE="https://go.pospos.co/api"
railway variables --set POSPOS_TRANSACTIONS_API_BASE="https://go.pospos.co/api"
```

### Verify Configuration
```bash
# Check all environment variables
railway variables
```

You should see output like:
```
║ POSPOS_API_KEY                         │ pk_live_your_api_key_here         ║
║ POSPOS_PRODUCT_API_BASE                │ https://go.pospos.co/api          ║
║ POSPOS_MEMBER_API_BASE                 │ https://go.pospos.co/api          ║
║ POSPOS_TRANSACTIONS_API_BASE           │ https://go.pospos.co/api          ║
```

## Deploy Configuration Changes

After setting the environment variables, redeploy the application:

```bash
railway up --detach
```

## Verify POSPOS Connection

### Check Logs for Configuration
```bash
railway logs | Select-String "POSPOS|Pospos"
```

**Expected Output**:
```
info: PosposMemberClient configured. MemberApiBase='https://go.pospos.co/api', ApiKeySet=True
info: PosposProductClient configured. ProductApiBase='https://go.pospos.co/api', ApiKeySet=True
```

### Test Import Endpoints

1. **Import Customers**:
```bash
curl -X POST "https://pigfarm-management-production.up.railway.app/api/customers/import" \
  -H "X-Api-Key: YOUR_API_KEY"
```

2. **Import Products/Feeds**:
```bash
curl -X POST "https://pigfarm-management-production.up.railway.app/api/feeds/import" \
  -H "X-Api-Key: YOUR_API_KEY"
```

## Troubleshooting

### Issue 1: "POSPOS MemberApiBase is not configured"

**Error in Logs**:
```
POSPOS MemberApiBase is not configured or invalid. No candidates will be returned.
```

**Solution**: Set the missing environment variables:
```bash
railway variables --set POSPOS_MEMBER_API_BASE="https://go.pospos.co/api"
railway variables --set POSPOS_API_KEY="your-api-key"
railway up --detach
```

### Issue 2: "Unauthorized" or "403 Forbidden"

**Cause**: Invalid or expired API key

**Solution**: 
1. Verify your API key in POSPOS admin panel
2. Update the environment variable:
```bash
railway variables --set POSPOS_API_KEY="your-new-api-key"
railway up --detach
```

### Issue 3: "Connection timeout" or "Host not found"

**Cause**: Incorrect API base URL

**Solution**: Verify your POSPOS domain and update:
```bash
railway variables --set POSPOS_PRODUCT_API_BASE="https://your-actual-domain.pospos.co/api"
railway up --detach
```

### Issue 4: Empty Import Results

**Check Logs**:
```bash
railway logs --tail 20
```

**Common Causes**:
- API key doesn't have proper permissions
- No data available in POSPOS for the timeframe
- Incorrect endpoint configuration

## Security Considerations

1. **Never commit API keys to source control**
2. **Use environment variables only** for sensitive configuration
3. **Regularly rotate API keys** as per your security policy
4. **Monitor API usage** in POSPOS admin panel

## Import Functionality

Once configured, the following import features will work:

### Customer Import
- **Endpoint**: `POST /api/customers/import`
- **Function**: Imports customers from POSPOS members API
- **Mapping**: POSPOS members → PigFarm customers

### Feed/Product Import  
- **Endpoint**: `POST /api/feeds/import`
- **Function**: Imports products from POSPOS products API
- **Mapping**: POSPOS products → PigFarm feed formulas

### Transaction Import
- **Endpoint**: Available for future implementation
- **Function**: Import sales/transaction data
- **Mapping**: POSPOS transactions → PigFarm records

## Configuration Summary

| Environment Variable | Required | Description | Example |
|---------------------|----------|-------------|---------|
| `POSPOS_API_KEY` | ✅ Yes | Your POSPOS API key | `pk_live_abc123...` |
| `POSPOS_PRODUCT_API_BASE` | ✅ Yes | Products API endpoint | `https://go.pospos.co/api` |
| `POSPOS_MEMBER_API_BASE` | ✅ Yes | Members API endpoint | `https://go.pospos.co/api` |
| `POSPOS_TRANSACTIONS_API_BASE` | ⚠️ Optional | Transactions API endpoint | `https://go.pospos.co/api` |

---

**Status**: ⚠️ Requires Configuration  
**Next Steps**: Set your actual POSPOS API key and verify endpoints  
**Documentation**: See Railway deployment guide for complete setup