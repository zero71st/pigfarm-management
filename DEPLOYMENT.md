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
2. Create a new project:

   - Click "New Project"
   - Select "Deploy from GitHub repo"
   - Choose your repository
   - Railway will automatically detect the Dockerfile

3. Configure environment variables (if needed):

   - Go to your project settings
   - Add any required environment variables

4. Your server will be available at: `https://your-app-name.railway.app`
5. Note this URL for the next step

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

### Railway (Server)

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:8080`
- `AllowedOrigins__0=https://your-vercel-app.vercel.app` (replace with actual URL)

### Vercel (Client)

- `ApiBaseUrl=https://your-railway-app.railway.app` (replace with actual URL)

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

## Local Development

To run locally:

1. Server: `dotnet run --project src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj`
2. Client: `dotnet run --project src/client/PigFarmManagement.Client/PigFarmManagement.Client.csproj`
