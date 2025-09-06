#!/bin/bash

# Install .NET 8 SDK
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$HOME/.dotnet

# Build the Blazor WebAssembly app
echo "Building Blazor WebAssembly application..."
dotnet publish src/client/PigFarmManagement.Client/PigFarmManagement.Client.csproj -c Release -o dist

# Copy wwwroot contents to dist root
echo "Preparing files for Vercel..."
cp -r dist/wwwroot/* dist/
rm -rf dist/wwwroot

echo "Build completed!"
