#!/bin/bash

# Install .NET 8 SDK
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$HOME/.dotnet

# Build the Blazor WebAssembly app
echo "Building Blazor WebAssembly application..."
dotnet publish src/client/PigFarmManagement.Client/PigFarmManagement.Client.csproj -c Release -o build-output

# Create public directory and copy ALL wwwroot contents including _framework
echo "Preparing files for Vercel..."
mkdir -p public
cp -r build-output/wwwroot/* public/

# List contents to verify
echo "Public directory contents:"
ls -la public/

echo "Build completed! Files are in public/ directory"
