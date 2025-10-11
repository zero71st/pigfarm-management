# Deployment Guide

This guide explains how to deploy the PigFarm Management System to Vercel (client) and Railway (server).

## Prerequisites

1. GitHub account
2. Vercel account (free tier available)
3. Railway account (free tier available)
4. Git installed locally

## Step 1: Push to GitHub

1. Initialize git repository (if not done):

```bash
git init
git add .
git commit -m "Initial commit"
```

2. Create a new repository on GitHub:

   - Go to https://github.com/new
   - Name your repository (e.g., "pigfarm-management")
   - Don't initialize with README (we already have one)

3. Add GitHub remote and push:

```bash
git remote add origin https://github.com/YOUR_USERNAME/pigfarm-management.git
git branch -M main
git push -u origin main
```

## Step 2: Deploy Server to Railway

1. Go to https://railway.app and sign up/login

2. Create a PostgreSQL database:
   - Click "New Project"
   - Select "Provision PostgreSQL"
   - Note down the `DATABASE_URL` from the Variables tab

3. Create the server application:
   - Click "New Project" again (or add service to existing project)
   - Select "Deploy from GitHub repo"
   - Choose your repository
   - Railway will automatically detect the Dockerfile

4. Configure environment variables (REQUIRED for production):
   Go to your project settings → Variables and add:

   ```
   ASPNETCORE_ENVIRONMENT=Production
   ASPNETCORE_URLS=http://+:8080
   DATABASE_URL=<your-postgresql-connection-string-from-step-2>
   ADMIN_USERNAME=admin
   ADMIN_EMAIL=admin@yourcompany.com
   ADMIN_PASSWORD=<secure-password-here>
   ADMIN_APIKEY=<secure-api-key-here>
   AllowedOrigins__0=<your-vercel-domain>
   ```

   **CRITICAL**: The `ADMIN_PASSWORD` and `ADMIN_APIKEY` must be provided in production or the application will fail to start.

5. Run database migrations BEFORE first deployment:
   ```bash
   # Install Railway CLI if not already installed
   npm install -g @railway/cli
   
   # Login to Railway
   railway login
   
   # Link to your project
   railway link
   
   # Run migrations
   railway run "dotnet ef database update --project src/server/PigFarmManagement.Server --connection \"$DATABASE_URL\""
   ```

6. Deploy the application:
   - Railway will build and deploy automatically
   - Your server will be available at: `https://your-app-name.railway.app`

7. Verify deployment:
   - Check health endpoint: `https://your-app-name.railway.app/health`
   - Check logs for successful admin seeding message

## Step 3: Deploy Client to Vercel

### Option A: Using Vercel CLI (Recommended)

1. Install Vercel CLI:

```bash
npm install -g vercel
```

2. Login to Vercel:

```bash
vercel login
```

3. Deploy:

```bash
cd "d:\dz Projects\PigFarmManagement"
vercel --prod
```

### Option B: Using Vercel Dashboard

1. Go to https://vercel.com and sign up/login
2. Click "New Project"
3. Import your GitHub repository
4. Configure build settings:
   - Framework Preset: Other
   - Build Command: `cd src && dotnet publish client/PigFarmManagement.Client/PigFarmManagement.Client.csproj -c Release -o client/PigFarmManagement.Client/bin/Release/net8.0/publish`
   - Output Directory: `src/client/PigFarmManagement.Client/bin/Release/net8.0/publish/wwwroot`

## Step 4: Update Configuration

After deployment, update the client configuration:

1. Update the API base URL in `src/client/PigFarmManagement.Client/Program.cs`:

   - Replace `"https://your-railway-app.railway.app"` with your actual Railway URL

2. Update Railway CORS settings if needed:

   - Add your Vercel domain to allowed origins

3. Redeploy both applications

## Environment Variables

### Railway (Server) - REQUIRED

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
DATABASE_URL=postgresql://user:password@host:port/database?sslmode=require
ADMIN_USERNAME=admin
ADMIN_EMAIL=admin@yourcompany.com
ADMIN_PASSWORD=<secure-password-here>
ADMIN_APIKEY=<secure-api-key-here>
AllowedOrigins__0=https://your-vercel-app.vercel.app
```

**Security Notes:**
- Generate a strong `ADMIN_PASSWORD` (at least 12 characters with mixed case, numbers, symbols)
- Generate a secure `ADMIN_APIKEY` (64+ random characters)
- Keep these secrets secure and rotate them periodically
- The application will refuse to start in production if `ADMIN_PASSWORD` or `ADMIN_APIKEY` are missing

### Vercel (Client)

```bash
ApiBaseUrl=https://your-railway-app.railway.app
```

## Custom Domains (Optional)

### Vercel

1. Go to your project settings
2. Click "Domains"
3. Add your custom domain

### Railway

1. Go to your project settings
2. Click "Custom Domain"
3. Add your custom domain

## Monitoring and Logs

- **Railway**: Check logs in the Railway dashboard
- **Vercel**: Check logs in the Vercel dashboard
- **Health Check**: Visit `https://your-railway-app.railway.app/health` to check server status

## Troubleshooting

1. **CORS Issues**: Make sure the client domain is added to Railway's allowed origins
2. **Build Failures**: Check the build logs in respective platforms
3. **API Connection**: Verify the API base URL in client configuration
4. **Health Check**: Railway expects the health endpoint to respond on `/health`
5. **Admin Seeding Failures**: 
   - Check Railway logs for "ADMIN_PASSWORD" or "ADMIN_APIKEY" missing errors
   - Ensure secrets are set in Railway Variables tab
   - Application will exit with code 1 if production secrets are missing
6. **Database Connection Issues**:
   - Verify `DATABASE_URL` is correctly set and includes SSL mode
   - Check PostgreSQL service is running in Railway
   - Run migrations before first deployment
7. **Migration Issues**:
   - Use Railway CLI: `railway run "dotnet ef database update --project src/server/PigFarmManagement.Server"`
   - Or use the `/migrations/run` endpoint with admin API key after deployment
8. **API Authentication**:
   - Use the seeded admin API key for protected endpoints
   - Check logs for "Admin created" messages to confirm seeding worked

## Local Development

To run locally:

1. Server: `dotnet run --project src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj`
2. Client: `dotnet run --project src/client/PigFarmManagement.Client/PigFarmManagement.Client.csproj`
