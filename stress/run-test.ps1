# SMTP Stress Test Runner for Windows
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("start", "stop", "test", "logs", "clean", "build")]
    [string]$Command = "start",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("throughput", "concurrent", "burst", "sustained")]
    [string]$TestType = "throughput",
    
    [Parameter(Mandatory=$false)]
    [int]$Duration = 60,
    
    [Parameter(Mandatory=$false)]
    [int]$Connections = 10,
    
    [Parameter(Mandatory=$false)]
    [int]$Rate = 1000,
    
    [Parameter(Mandatory=$false)]
    [int]$MessageSize = 1024
)

$ErrorActionPreference = "Stop"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host " Zetian SMTP Stress Testing Framework" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

function Update-ClientConfig {
    $configPath = "./config/client.yml"
    $config = @"
# Load Generator Configuration

# Target SMTP server
Target:
  Host: server
  Port: 25

# Test scenario configuration
Scenario:
  Type: $TestType
  Duration: $Duration
  Connections: $Connections
  Rate: $Rate

# Message configuration
Message:
  Size: $MessageSize
  Recipients: 1
"@
    Set-Content -Path $configPath -Value $config
    Write-Host "✓ Client configuration updated" -ForegroundColor Green
}

switch ($Command) {
    "start" {
        Write-Host "Starting infrastructure services..." -ForegroundColor Yellow
        docker-compose up -d server prometheus grafana
        
        Write-Host ""
        Write-Host "Services started:" -ForegroundColor Green
        Write-Host "  • SMTP Server: localhost:2525" -ForegroundColor White
        Write-Host "  • Prometheus: http://localhost:9090" -ForegroundColor White
        Write-Host "  • Grafana: http://localhost:3000 (admin/admin)" -ForegroundColor White
        Write-Host ""
        Write-Host "Run './run-test.ps1 -Command test' to start a load test" -ForegroundColor Cyan
    }
    
    "stop" {
        Write-Host "Stopping all services..." -ForegroundColor Yellow
        docker-compose down
        Write-Host "✓ All services stopped" -ForegroundColor Green
    }
    
    "test" {
        Write-Host "Starting load test..." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Test Configuration:" -ForegroundColor Cyan
        Write-Host "  Type: $TestType" -ForegroundColor White
        Write-Host "  Duration: $Duration seconds" -ForegroundColor White
        Write-Host "  Connections: $Connections" -ForegroundColor White
        Write-Host "  Target Rate: $Rate msg/s" -ForegroundColor White
        Write-Host "  Message Size: $MessageSize bytes" -ForegroundColor White
        Write-Host ""
        
        Update-ClientConfig
        
        Write-Host "Running test..." -ForegroundColor Yellow
        docker-compose run --rm client
        
        Write-Host ""
        Write-Host "✓ Test completed!" -ForegroundColor Green
        Write-Host "View results in Grafana: http://localhost:3000" -ForegroundColor Cyan
    }
    
    "logs" {
        Write-Host "Showing logs (Ctrl+C to exit)..." -ForegroundColor Yellow
        docker-compose logs -f
    }
    
    "clean" {
        Write-Host "Cleaning up..." -ForegroundColor Yellow
        docker-compose down -v
        if (Test-Path "./results") {
            Remove-Item -Path "./results/*" -Force -ErrorAction SilentlyContinue
        }
        Write-Host "✓ Cleanup completed" -ForegroundColor Green
    }
    
    "build" {
        Write-Host "Building Docker images..." -ForegroundColor Yellow
        docker-compose build --no-cache
        Write-Host "✓ Build completed" -ForegroundColor Green
    }
}

Write-Host ""