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
    Write-Host "Creating NuGet packages..." -ForegroundColor Yellow

    if (-not (Test-Path "artifacts")) {
        New-Item -ItemType Directory -Path "artifacts" | Out-Null
    }

    # Discover every packable library project under src (those that produce a NuGet package)
    $packableProjects = Get-ChildItem -Path "src" -Recurse -Filter "*.csproj" |
        Where-Object { Select-String -Path $_.FullName -Pattern "<GeneratePackageOnBuild>true</GeneratePackageOnBuild>" -Quiet } |
        Sort-Object FullName

    if (-not $packableProjects) {
        throw "No packable projects found under src."
    }

    foreach ($project in $packableProjects) {
        Write-Host "  Packing $($project.BaseName)..." -ForegroundColor Yellow
        dotnet pack $project.FullName `
            --configuration $Configuration `
            --no-build `
            --output artifacts
    }

    Write-Host "Packages created in artifacts folder" -ForegroundColor Green
    Get-ChildItem -Path "artifacts" -Filter "*.nupkg" | Sort-Object Name | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Build completed successfully!" -ForegroundColor Green
