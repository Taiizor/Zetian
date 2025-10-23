#!/bin/bash

# SMTP Stress Test Runner for Linux/Mac

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[0;37m'
NC='\033[0m' # No Color

# Default values
COMMAND="start"
TEST_TYPE="throughput"
DURATION=60
CONNECTIONS=10
RATE=1000
MESSAGE_SIZE=1024

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --command)
            COMMAND="$2"
            shift 2
            ;;
        --test-type)
            TEST_TYPE="$2"
            shift 2
            ;;
        --duration)
            DURATION="$2"
            shift 2
            ;;
        --connections)
            CONNECTIONS="$2"
            shift 2
            ;;
        --rate)
            RATE="$2"
            shift 2
            ;;
        --message-size)
            MESSAGE_SIZE="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo -e "${CYAN}================================================${NC}"
echo -e "${CYAN} Zetian SMTP Stress Testing Framework${NC}"
echo -e "${CYAN}================================================${NC}"
echo ""

update_client_config() {
    cat > ./config/client.yml <<EOF
# Load Generator Configuration

# Target SMTP server
Target:
  Host: server
  Port: 25

# Test scenario configuration
Scenario:
  Type: $TEST_TYPE
  Duration: $DURATION
  Connections: $CONNECTIONS
  Rate: $RATE

# Message configuration
Message:
  Size: $MESSAGE_SIZE
  Recipients: 1
EOF
    echo -e "${GREEN}✓ Client configuration updated${NC}"
}

case $COMMAND in
    start)
        echo -e "${YELLOW}Starting infrastructure services...${NC}"
        docker-compose up -d server prometheus grafana
        
        echo ""
        echo -e "${GREEN}Services started:${NC}"
        echo -e "  ${WHITE}• SMTP Server: localhost:2525${NC}"
        echo -e "  ${WHITE}• Prometheus: http://localhost:9090${NC}"
        echo -e "  ${WHITE}• Grafana: http://localhost:3000 (admin/admin)${NC}"
        echo ""
        echo -e "${CYAN}Run './run-test.sh --command test' to start a load test${NC}"
        ;;
    
    stop)
        echo -e "${YELLOW}Stopping all services...${NC}"
        docker-compose down
        echo -e "${GREEN}✓ All services stopped${NC}"
        ;;
    
    test)
        echo -e "${YELLOW}Starting load test...${NC}"
        echo ""
        echo -e "${CYAN}Test Configuration:${NC}"
        echo -e "  ${WHITE}Type: $TEST_TYPE${NC}"
        echo -e "  ${WHITE}Duration: $DURATION seconds${NC}"
        echo -e "  ${WHITE}Connections: $CONNECTIONS${NC}"
        echo -e "  ${WHITE}Target Rate: $RATE msg/s${NC}"
        echo -e "  ${WHITE}Message Size: $MESSAGE_SIZE bytes${NC}"
        echo ""
        
        update_client_config
        
        echo -e "${YELLOW}Running test...${NC}"
        docker-compose run --rm client
        
        echo ""
        echo -e "${GREEN}✓ Test completed!${NC}"
        echo -e "${CYAN}View results in Grafana: http://localhost:3000${NC}"
        ;;
    
    logs)
        echo -e "${YELLOW}Showing logs (Ctrl+C to exit)...${NC}"
        docker-compose logs -f
        ;;
    
    clean)
        echo -e "${YELLOW}Cleaning up...${NC}"
        docker-compose down -v
        rm -rf ./results/*
        echo -e "${GREEN}✓ Cleanup completed${NC}"
        ;;
    
    build)
        echo -e "${YELLOW}Building Docker images...${NC}"
        docker-compose build --no-cache
        echo -e "${GREEN}✓ Build completed${NC}"
        ;;
    
    *)
        echo -e "${RED}Invalid command: $COMMAND${NC}"
        echo "Usage: $0 --command [start|stop|test|logs|clean|build]"
        echo ""
        echo "Options for test command:"
        echo "  --test-type [throughput|concurrent|burst|sustained]"
        echo "  --duration <seconds>"
        echo "  --connections <number>"
        echo "  --rate <messages per second>"
        echo "  --message-size <bytes>"
        exit 1
        ;;
esac

echo ""