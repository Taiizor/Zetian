# Makefile for SMTP Stress Testing Framework

.PHONY: help start stop test build clean logs status

# Default target
help:
	@echo "Zetian SMTP Stress Testing Framework"
	@echo "====================================="
	@echo ""
	@echo "Available commands:"
	@echo "  make start       - Start infrastructure (server, monitoring)"
	@echo "  make stop        - Stop all services"
	@echo "  make test        - Run default load test"
	@echo "  make build       - Build Docker images"
	@echo "  make clean       - Clean up everything"
	@echo "  make logs        - Show service logs"
	@echo "  make status      - Show service status"
	@echo ""
	@echo "Test examples:"
	@echo "  make test-throughput  - Run throughput test"
	@echo "  make test-concurrent  - Run concurrent test"
	@echo "  make test-burst       - Run burst test"
	@echo "  make test-sustained   - Run sustained test"

# Start infrastructure
start:
	@echo "Starting infrastructure..."
	@docker-compose up -d server prometheus grafana
	@echo ""
	@echo "Services available at:"
	@echo "  SMTP: localhost:2525"
	@echo "  Prometheus: http://localhost:9090"
	@echo "  Grafana: http://localhost:3000"

# Stop all services
stop:
	@echo "Stopping all services..."
	@docker-compose down

# Run default test
test:
	@echo "Running default load test..."
	@docker-compose run --rm client

# Run specific test scenarios
test-throughput:
	@echo "Running throughput test..."
	@./run-test.ps1 -Command test -TestType throughput -Duration 60 -Rate 1000

test-concurrent:
	@echo "Running concurrent test..."
	@./run-test.ps1 -Command test -TestType concurrent -Duration 60 -Connections 20

test-burst:
	@echo "Running burst test..."
	@./run-test.ps1 -Command test -TestType burst -Duration 60 -Rate 500

test-sustained:
	@echo "Running sustained test..."
	@./run-test.ps1 -Command test -TestType sustained -Duration 300 -Connections 10

# Build Docker images
build:
	@echo "Building Docker images..."
	@docker-compose build --no-cache

# Clean up everything
clean:
	@echo "Cleaning up..."
	@docker-compose down -v
	@rm -rf results/*
	@echo "Clean up completed"

# Show logs
logs:
	@docker-compose logs -f

# Show service status
status:
	@docker-compose ps