FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/server/PigFarmManagement.Server/PigFarmManagement.Server.csproj", "server/PigFarmManagement.Server/"]
COPY ["src/shared/PigFarmManagement.Shared/PigFarmManagement.Shared.csproj", "shared/PigFarmManagement.Shared/"]

# Restore dependencies
RUN dotnet restore "server/PigFarmManagement.Server/PigFarmManagement.Server.csproj"

# Copy source code
COPY src/ .

# Build the application
RUN dotnet build "server/PigFarmManagement.Server/PigFarmManagement.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "server/PigFarmManagement.Server/PigFarmManagement.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "PigFarmManagement.Server.dll"]
