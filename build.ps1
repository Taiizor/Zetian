# Build script for Zetian SMTP Server Library

param(
    [string]$Configuration = "Release",
    [switch]$Pack,
    [switch]$Push,
    [switch]$Test,
    [switch]$Clean,
    [string]$ApiKey = "",
    [string]$Source = "https://api.nuget.org/v3/index.json"
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
    dotnet test --solution Zetian.slnx --configuration $Configuration --no-build --verbosity normal --max-parallel-test-modules 1 -- -parallel none
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

# Push
if ($Push) {
    Write-Host "Pushing NuGet packages to $Source..." -ForegroundColor Yellow

    # Fall back to the NUGET_API_KEY environment variable when no key is passed
    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        $ApiKey = $env:NUGET_API_KEY
    }

    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        throw "No NuGet API key provided. Pass -ApiKey <key> or set the NUGET_API_KEY environment variable."
    }

    $packages = Get-ChildItem -Path "artifacts" -Filter "*.nupkg" | Sort-Object Name
    if (-not $packages) {
        throw "No .nupkg files found in artifacts. Run with -Pack first."
    }

    foreach ($package in $packages) {
        Write-Host "  Pushing $($package.Name)..." -ForegroundColor Yellow
        # Symbol packages (.snupkg) in the same folder are pushed automatically alongside each .nupkg.
        # --skip-duplicate keeps the run going when a version already exists on the feed.
        dotnet nuget push $package.FullName `
            --api-key $ApiKey `
            --source $Source `
            --skip-duplicate
    }

    Write-Host "All packages pushed to $Source" -ForegroundColor Green
}

Write-Host ""
Write-Host "Build completed successfully!" -ForegroundColor Green
