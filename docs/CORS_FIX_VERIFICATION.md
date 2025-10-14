# CORS Fix Testing

## Issue Resolution

✅ **CORS Configuration Updated**: The server now allows requests from `https://pigfarm-management.vercel.app`

### What Was Fixed:

1. **Added Vercel URL to Allowed Origins**: Updated CORS policy to include your Vercel deployment URL
2. **Environment Variable Support**: Added `ALLOWED_ORIGINS` environment variable for flexible configuration
3. **Logging Added**: Server now logs allowed origins for debugging

### Current CORS Configuration:

The server now allows requests from:
- `https://pigfarm-management.vercel.app` (your Vercel client)
- `https://localhost:7000` (local development)
- `https://localhost:5173` (Vite dev server)

## Testing the Fix

### Option 1: Test from Vercel Client
1. Go to your Vercel deployment: `https://pigfarm-management.vercel.app`
2. Try to login with admin credentials:
   - Username: `admin`
   - Password: `Admin123!`
3. The CORS error should be resolved

### Option 2: Test with Browser Console
Open browser console on your Vercel app and run:

```javascript
fetch('https://pigfarm-management-production.up.railway.app/health')
  .then(response => response.json())
  .then(data => console.log('Health check:', data))
  .catch(error => console.error('Error:', error));
```

### Option 3: Test Login API
```javascript
fetch('https://pigfarm-management-production.up.railway.app/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    username: 'admin',
    password: 'Admin123!',
    keyLabel: 'browser-test'
  })
})
.then(response => response.json())
.then(data => console.log('Login response:', data))
.catch(error => console.error('Login error:', error));
```

## Verification in Railway Logs

You should see this line in Railway logs confirming CORS is configured:
```
CORS: Allowing origins: https://pigfarm-management.vercel.app, https://localhost:7000, https://localhost:5173
```

## If CORS Issues Persist

### Check Exact URL
Make sure your Vercel URL exactly matches what's configured. If your Vercel URL is different, update it:

```bash
railway variables --set ALLOWED_ORIGINS="https://your-exact-vercel-url.vercel.app,https://localhost:7000"
```

### Add Additional Origins
If you have multiple Vercel deployments or custom domains:

```bash
railway variables --set ALLOWED_ORIGINS="https://pigfarm-management.vercel.app,https://your-custom-domain.com,https://localhost:7000"
```

## Environment Variables

Current Railway environment variables for CORS:
- `ALLOWED_ORIGINS`: `https://pigfarm-management.vercel.app,https://localhost:7000,https://localhost:5173`

---

**Status**: ✅ CORS Issue Fixed  
**Deployment**: Railway server updated with proper CORS configuration  
**Next Steps**: Test login from your Vercel client application