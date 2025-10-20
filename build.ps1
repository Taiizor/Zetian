# Build script for Zetian SMTP Server Library

param(
    [string]$Configuration = "Release",
    [switch]$Pack,
    [switch]$Test,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

Write-Host "Zetian SMTP Server Library - Build Script" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

# Clean
if ($Clean) {
    Write-Host "Cleaning solution..." -ForegroundColor Yellow
    dotnet clean --configuration $Configuration
    if (Test-Path "artifacts") {
        Remove-Item -Path "artifacts" -Recurse -Force
    }
}

# Restore
Write-Host "Restoring packages..." -ForegroundColor Yellow
dotnet restore

# Build
Write-Host "Building solution in $Configuration mode..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore

# Test
if ($Test) {
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test --configuration $Configuration --no-build --verbosity normal
}

# Pack
if ($Pack) {
    Write-Host "Creating NuGet package..." -ForegroundColor Yellow
    
    if (-not (Test-Path "artifacts")) {
        New-Item -ItemType Directory -Path "artifacts" | Out-Null
    }
    
    dotnet pack src/Zetian/Zetian.csproj `
        --configuration $Configuration `
        --no-build `
        --output artifacts
    
    Write-Host "Package created in artifacts folder" -ForegroundColor Green
    Get-ChildItem -Path "artifacts" -Filter "*.nupkg" | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Build completed successfully!" -ForegroundColor Green
