# Multi-stage build for Zetian SMTP Cluster
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Zetian/Zetian.csproj", "src/Zetian/"]
COPY ["src/Zetian.Clustering/Zetian.Clustering.csproj", "src/Zetian.Clustering/"]
COPY ["src/Zetian.HealthCheck/Zetian.HealthCheck.csproj", "src/Zetian.HealthCheck/"]
COPY ["examples/Zetian.Clustering.Examples/Zetian.Clustering.Examples.csproj", "examples/Zetian.Clustering.Examples/"]

# Restore dependencies
RUN dotnet restore "examples/Zetian.Clustering.Examples/Zetian.Clustering.Examples.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/examples/Zetian.Clustering.Examples"
RUN dotnet build "Zetian.Clustering.Examples.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Zetian.Clustering.Examples.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

# Create user for running the application
RUN groupadd -r zetian && useradd -r -g zetian zetian

# Create necessary directories
RUN mkdir -p /app/cluster-data /app/messages /app/logs && \
    chown -R zetian:zetian /app

# Copy published application
COPY --from=publish /app/publish .
RUN chown -R zetian:zetian /app

# Set user
USER zetian

# Expose ports
EXPOSE 25
EXPOSE 7946
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Environment variables with defaults
ENV NODE_ID="node-1" \
    SMTP_PORT="25" \
    CLUSTER_PORT="7946" \
    HEALTH_PORT="8080" \
    DISCOVERY_METHOD="Static" \
    SEED_NODES="" \
    CLUSTER_SECRET="" \
    LOG_LEVEL="Information" \
    ENABLE_METRICS="true" \
    DATA_DIRECTORY="/app/cluster-data" \
    MESSAGE_DIRECTORY="/app/messages" \
    ASPNETCORE_URLS="http://+:8080"

# Volume mounts
VOLUME ["/app/cluster-data", "/app/messages", "/app/logs"]

# Entry point
ENTRYPOINT ["dotnet", "Zetian.Clustering.Examples.dll"]