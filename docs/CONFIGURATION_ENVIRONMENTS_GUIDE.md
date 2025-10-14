# Configuration Files Guide - Development vs Production

## 🎯 **Configuration Structure Explained**

### **Development Environment (Local Testing)**

#### **Server Configuration** (`src/server/PigFarmManagement.Server/appsettings.Development.json`)
```json
{
  "Pospos": {
    "ApiKey": "your-dev-api-key",
    "ProductApiBase": "https://go.pospos.co/developer/api",
    "MemberApiBase": "https://go.pospos.co/developer/api"
  },
  "AllowedOrigins": [
    "https://localhost:7000",
    "http://localhost:7000"
  ]
}
```

#### **Client Configuration** (`src/client/PigFarmManagement.Client/wwwroot/appsettings.Development.json`)
```json
{
  "ApiBaseUrl": "http://localhost:5000",
  "Pospos": {
    "ApiBase": "https://go.pospos.co/developer/api"
  }
}
```

### **Production Environment (Railway)**

#### **Server Configuration** (`appsettings.Production.json`)
- Uses environment variables: `${POSPOS_API_KEY}`, `${POSPOS_PRODUCT_API_BASE}`
- Railway environment variables override these placeholders

#### **Client Configuration** 
- Client gets API URLs from server configuration
- No direct POSPOS configuration needed in production client

## 🔧 **Current Configuration Status**

### ✅ **Fixed Issues:**

1. **Created server-side development config** - Now you have proper server configuration for local development
2. **Updated Railway endpoints** - Changed from `/api` to `/developer/api` to match your setup
3. **Added missing CORS origins** - Included your actual Vercel URL

### ⚠️ **Still Needs Your Action:**

1. **Replace API keys** with your actual POSPOS keys:
   
   **For Development:**
   ```bash
   # Edit src/server/PigFarmManagement.Server/appsettings.Development.json
   # Replace "your-pospos-dev-api-key" with your actual key
   ```
   
   **For Production:**
   ```bash
   railway variables --set POSPOS_API_KEY="your-actual-pospos-api-key"
   ```

## 🚀 **How to Use Each Environment**

### **Development (Local Testing):**

1. **Start the server:**
   ```bash
   cd src/server/PigFarmManagement.Server
   dotnet run --urls http://localhost:5000
   ```

2. **Start the client:**
   ```bash
   cd src/client/PigFarmManagement.Client  
   dotnet run --urls http://localhost:7000
   ```

3. **Configuration loaded from:**
   - Server: `appsettings.Development.json` + `launchSettings.json`
   - Client: `wwwroot/appsettings.Development.json`

### **Production (Railway):**

1. **Deploy:**
   ```bash
   railway up --detach
   ```

2. **Configuration loaded from:**
   - Server: `appsettings.Production.json` + Railway environment variables
   - Client: Bundled into the app during build

## 📋 **Environment Variables Summary**

### **Development (Local):**
- Configured in `appsettings.Development.json` files
- No environment variables needed

### **Production (Railway):**
```bash
POSPOS_API_KEY="your-actual-api-key"                    # ⚠️ UPDATE THIS
POSPOS_PRODUCT_API_BASE="https://go.pospos.co/developer/api"  # ✅ UPDATED
POSPOS_MEMBER_API_BASE="https://go.pospos.co/developer/api"   # ✅ UPDATED
ALLOWED_ORIGINS="https://pigfarm-management.vercel.app,..."    # ✅ CONFIGURED
```

## 🔍 **Verification Commands**

### **Check Development Config:**
```bash
# Server logs should show POSPOS configuration
dotnet run --project src/server/PigFarmManagement.Server
```

### **Check Production Config:**
```bash
# Railway logs should show POSPOS configuration
railway logs | Select-String "POSPOS|Pospos"
```

## ✅ **Next Steps**

1. **Update your development API key** in `appsettings.Development.json`
2. **Update your production API key** in Railway:
   ```bash
   railway variables --set POSPOS_API_KEY="your-real-api-key"
   railway up --detach
   ```
3. **Test both environments** to ensure POSPOS integration works

---

**Key Takeaway**: The `appsettings.Development.json` in the client is for client-side configuration, but POSPOS API calls happen from the server, so server-side configuration is what matters for the actual API integration.