# Production Security Configuration

## Overview
This document outlines the security hardening measures implemented for production deployment of the PigFarm Management System.

## Security Features Implemented

### 1. HTTPS Enforcement
- **HSTS (HTTP Strict Transport Security)**: Enforces HTTPS connections for 365 days
- **HTTPS Redirection**: Automatically redirects HTTP traffic to HTTPS
- **Preload Support**: HSTS preload list compatible

Configuration:
```csharp
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});
```

### 2. CORS (Cross-Origin Resource Sharing)
- **Development**: Permissive policy for local development
- **Production**: Strict whitelist of allowed origins
- **Credentials**: Supports API key authentication across origins

Production Configuration:
```json
{
  "AllowedOrigins": [
    "https://pigfarm-management-client.vercel.app",
    "https://zero71st-pigfarm-management.vercel.app"
  ]
}
```

### 3. Security Headers
Comprehensive security headers are applied in production:

#### Clickjacking Protection
- `X-Frame-Options: DENY` - Prevents embedding in frames/iframes

#### Content Type Security
- `X-Content-Type-Options: nosniff` - Prevents MIME type sniffing attacks

#### Referrer Policy
- `Referrer-Policy: strict-origin-when-cross-origin` - Controls referrer information

#### Cross-Origin Policies
- `Cross-Origin-Embedder-Policy: require-corp` - Isolation protection
- `Cross-Origin-Opener-Policy: same-origin` - Window isolation
- `Cross-Origin-Resource-Policy: cross-origin` - Resource sharing control

#### Content Security Policy (CSP)
Restrictive CSP that allows necessary functionality:
```
default-src 'self';
script-src 'self' 'unsafe-inline' 'unsafe-eval' https://maps.googleapis.com;
style-src 'self' 'unsafe-inline' https://fonts.googleapis.com;
font-src 'self' https://fonts.gstatic.com;
img-src 'self' data: https:;
connect-src 'self' https://go.pospos.co https://maps.googleapis.com;
frame-ancestors 'none';
base-uri 'self';
form-action 'self'
```

### 4. Information Disclosure Prevention
Removes server information headers:
- `Server`
- `X-Powered-By`
- `X-AspNet-Version`
- `X-AspNetMvc-Version`

### 5. Reverse Proxy Support
- **Forwarded Headers**: Properly handles X-Forwarded-For and X-Forwarded-Proto
- **Trusted Proxies**: Configured for deployment behind reverse proxies

## Environment Variables for Production

### Required Environment Variables
```bash
# Database
PIGFARM_CONNECTION="Server=...;Database=...;..."

# External APIs
GOOGLE_MAPS_API_KEY="your-google-maps-api-key"
POSPOS_API_KEY="your-pospos-api-key"
POSPOS_PRODUCT_API_BASE="https://go.pospos.co/developer/api/stock"
POSPOS_MEMBER_API_BASE="https://go.pospos.co/developer/api/member?dbname=..."
POSPOS_TRANSACTIONS_API_BASE="https://go.pospos.co/developer/api/transactions"

# Security
ASPNETCORE_ENVIRONMENT="Production"
ASPNETCORE_URLS="https://+:443;http://+:80"

# Admin Seeding (should be false or unset in production)
# SEED_ADMIN="false"
```

## Deployment Checklist

### Pre-Deployment Security
- [ ] Update `AllowedOrigins` in appsettings.Production.json with actual production URLs
- [ ] Ensure all secrets are stored in environment variables, not configuration files
- [ ] Remove or disable admin seeding (`SEED_ADMIN` should be unset)
- [ ] Configure proper SSL certificates
- [ ] Set up reverse proxy (Nginx, Cloudflare, etc.) if needed

### Post-Deployment Verification
- [ ] Verify HTTPS redirection works: `curl -I http://yourdomain.com`
- [ ] Check HSTS header: `curl -I https://yourdomain.com | grep -i strict`
- [ ] Test CORS from allowed origins
- [ ] Verify CSP is working (check browser dev tools)
- [ ] Confirm no server information is leaked in headers

### Security Testing Commands
```bash
# Test HTTPS redirection
curl -I http://yourdomain.com

# Check security headers
curl -I https://yourdomain.com

# Test CORS (replace with your actual domain)
curl -H "Origin: https://unauthorized-domain.com" \
     -H "Access-Control-Request-Method: GET" \
     -X OPTIONS https://yourdomain.com/api/health

# Verify no information disclosure
curl -I https://yourdomain.com | grep -E "(Server|X-Powered-By|X-AspNet)"
```

## Additional Security Recommendations

### 1. Rate Limiting (Next Phase)
Consider implementing rate limiting for:
- Authentication endpoints
- API key generation
- Admin operations

### 2. Monitoring & Alerting
- Set up monitoring for failed authentication attempts
- Alert on admin seeding attempts in production
- Monitor CSP violations

### 3. Regular Security Updates
- Keep dependencies updated
- Regular security audits
- Monitor security advisories for .NET and dependencies

## Security Headers Testing
Use tools like:
- [Security Headers Analyzer](https://securityheaders.com/)
- [Mozilla Observatory](https://observatory.mozilla.org/)
- [SSL Labs Test](https://www.ssllabs.com/ssltest/)

## Compliance
This configuration helps meet security requirements for:
- OWASP Top 10 protection
- PCI DSS compliance (if handling payment data)
- General data protection best practices