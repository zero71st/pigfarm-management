# Production Security Deployment Configuration

## Environment Variables Template

Copy this template and fill in your actual values for production deployment:

```bash
# Core Application
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80

# Database (use your actual production database connection)
PIGFARM_CONNECTION="Server=your-server;Database=pigfarm_prod;User=your-user;Password=your-password;Encrypt=true;TrustServerCertificate=false"

# External APIs (replace with your actual API keys)
GOOGLE_MAPS_API_KEY=your-actual-google-maps-api-key
POSPOS_API_KEY=your-actual-pospos-api-key
POSPOS_PRODUCT_API_BASE=https://go.pospos.co/developer/api/stock
POSPOS_MEMBER_API_BASE=https://go.pospos.co/developer/api/member?dbname=your-db-name
POSPOS_TRANSACTIONS_API_BASE=https://go.pospos.co/developer/api/transactions

# Security (IMPORTANT: DO NOT set SEED_ADMIN in production)
# SEED_ADMIN should be unset or false in production
```

## Railway Deployment

For Railway.app deployment, add these environment variables in your Railway dashboard:

```bash
# Railway automatically sets ASPNETCORE_ENVIRONMENT to Production
PIGFARM_CONNECTION=${{ Postgres.DATABASE_URL }}
GOOGLE_MAPS_API_KEY=your-google-maps-api-key
POSPOS_API_KEY=your-pospos-api-key
POSPOS_PRODUCT_API_BASE=https://go.pospos.co/developer/api/stock
POSPOS_MEMBER_API_BASE=https://go.pospos.co/developer/api/member?dbname=your-db
POSPOS_TRANSACTIONS_API_BASE=https://go.pospos.co/developer/api/transactions
```

## Security Verification Checklist

After deployment, verify security configuration:

### 1. Test HTTPS and HSTS
```bash
# Should redirect HTTP to HTTPS
curl -I http://your-domain.com

# Should include HSTS header
curl -I https://your-domain.com | grep -i strict-transport-security
```

### 2. Test CORS
```bash
# Should reject unauthorized origins
curl -H "Origin: https://malicious-site.com" \
     -H "Access-Control-Request-Method: GET" \
     -X OPTIONS https://your-domain.com/api/health

# Should accept authorized origins (replace with your actual frontend domain)
curl -H "Origin: https://your-frontend-domain.vercel.app" \
     -H "Access-Control-Request-Method: GET" \
     -X OPTIONS https://your-domain.com/api/health
```

### 3. Verify Security Headers
```bash
# Check all security headers are present
curl -I https://your-domain.com | grep -E "(X-Frame-Options|X-Content-Type-Options|Content-Security-Policy|Referrer-Policy)"
```

### 4. Test Information Disclosure Prevention
```bash
# Should not reveal server information
curl -I https://your-domain.com | grep -E "(Server|X-Powered-By|X-AspNet)"
```

### 5. Verify Admin Seeding is Disabled
```bash
# Check application logs - should NOT see seeding messages in production
# Look for: "Admin seeding skipped" or no seeding messages at all
```