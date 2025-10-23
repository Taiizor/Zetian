# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file
COPY Client.csproj ./

# Restore dependencies
RUN dotnet restore Client.csproj

# Copy source code
COPY . .

# Build application
RUN dotnet build Client.csproj -c Release -o /app/build

# Publish application
FROM build AS publish
RUN dotnet publish Client.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install required packages
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create results directory
RUN mkdir -p /app/results

# Expose metrics port
EXPOSE 9101

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production

# Volume for results
VOLUME ["/app/results"]

# Run the application
ENTRYPOINT ["dotnet", "Zetian.LoadGenerator.dll"]