# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY stress/server/Server.csproj stress/server/
COPY src/Zetian/Zetian.csproj src/Zetian/

# Restore dependencies
RUN dotnet restore stress/server/Server.csproj

# Copy source code
COPY stress/ stress/
COPY src/ src/

# Build application
RUN dotnet build stress/server/Server.csproj -c Release -o /app/build

# Publish application
FROM build AS publish
RUN dotnet publish stress/server/Server.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install required packages
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Expose ports
EXPOSE 25 9100

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnostics=0

# Health check
HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:9100/metrics || exit 1

# Run the application
ENTRYPOINT ["dotnet", "Zetian.StressTestServer.dll"]