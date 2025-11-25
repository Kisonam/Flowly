#!/bin/bash
# deploy-production.sh
# Production deployment script with validation and safety checks
# Usage: ./scripts/deploy-production.sh [--skip-validation] [--no-build]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
ENV_FILE=".env.production"
COMPOSE_FILE="docker-compose.prod.yml"
SKIP_VALIDATION=false
NO_BUILD=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-validation)
            SKIP_VALIDATION=true
            shift
            ;;
        --no-build)
            NO_BUILD=true
            shift
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            echo "Usage: $0 [--skip-validation] [--no-build]"
            exit 1
            ;;
    esac
done

echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║   Flowly Production Deployment         ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""

# Check if running from project root
if [ ! -f "$COMPOSE_FILE" ]; then
    echo -e "${RED}❌ ERROR: $COMPOSE_FILE not found!${NC}"
    echo "Please run this script from the project root directory."
    exit 1
fi

# Validate environment configuration
if [ "$SKIP_VALIDATION" = false ]; then
    echo -e "${YELLOW}📋 Step 1: Validating environment configuration...${NC}"
    if [ -f "scripts/validate-env.sh" ]; then
        chmod +x scripts/validate-env.sh
        if ! ./scripts/validate-env.sh "$ENV_FILE"; then
            echo -e "${RED}❌ Environment validation failed!${NC}"
            echo "Fix the issues above or use --skip-validation to bypass (not recommended)"
            exit 1
        fi
    else
        echo -e "${YELLOW}⚠️  Warning: validate-env.sh not found, skipping validation${NC}"
    fi
    echo ""
else
    echo -e "${YELLOW}⚠️  Skipping environment validation${NC}"
    echo ""
fi

# Check Docker and Docker Compose
echo -e "${YELLOW}📋 Step 2: Checking Docker installation...${NC}"
if ! command -v docker &> /dev/null; then
    echo -e "${RED}❌ ERROR: Docker is not installed!${NC}"
    exit 1
fi

if ! docker info &> /dev/null; then
    echo -e "${RED}❌ ERROR: Docker daemon is not running!${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Docker is ready${NC}"
echo ""

# Check disk space
echo -e "${YELLOW}📋 Step 3: Checking disk space...${NC}"
AVAILABLE_SPACE=$(df -h . | awk 'NR==2 {print $4}')
echo "Available disk space: $AVAILABLE_SPACE"
echo ""

# Backup existing data (if exists)
echo -e "${YELLOW}📋 Step 4: Checking for existing deployment...${NC}"
if docker ps -a | grep -q "flowly-.*-prod"; then
    echo -e "${YELLOW}⚠️  Existing Flowly containers found${NC}"
    read -p "Do you want to backup data before redeploying? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        BACKUP_DIR="./backups/$(date +%Y%m%d_%H%M%S)"
        mkdir -p "$BACKUP_DIR"
        echo "Creating backup in $BACKUP_DIR..."
        
        # Backup database
        if docker ps | grep -q "flowly-db-prod"; then
            echo "Backing up database..."
            docker exec flowly-db-prod pg_dump -U ${POSTGRES_USER:-flowly_prod_user} ${POSTGRES_DB:-flowly_production} > "$BACKUP_DIR/database.sql"
            echo -e "${GREEN}✅ Database backed up${NC}"
        fi
        
        # Backup uploads
        if [ -d "./data/uploads" ]; then
            echo "Backing up uploads..."
            cp -r ./data/uploads "$BACKUP_DIR/"
            echo -e "${GREEN}✅ Uploads backed up${NC}"
        fi
        
        echo -e "${GREEN}✅ Backup completed: $BACKUP_DIR${NC}"
    fi
    echo ""
fi

# Stop existing containers
echo -e "${YELLOW}📋 Step 5: Stopping existing containers...${NC}"
docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" down
echo -e "${GREEN}✅ Containers stopped${NC}"
echo ""

# Build images
if [ "$NO_BUILD" = false ]; then
    echo -e "${YELLOW}📋 Step 6: Building Docker images...${NC}"
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" build --no-cache
    echo -e "${GREEN}✅ Images built${NC}"
    echo ""
else
    echo -e "${YELLOW}⚠️  Skipping image build${NC}"
    echo ""
fi

# Start containers
echo -e "${YELLOW}📋 Step 7: Starting containers...${NC}"
docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d
echo -e "${GREEN}✅ Containers started${NC}"
echo ""

# Wait for services to be healthy
echo -e "${YELLOW}📋 Step 8: Waiting for services to be healthy...${NC}"
echo "This may take up to 2 minutes..."

MAX_WAIT=120
ELAPSED=0
ALL_HEALTHY=false

while [ $ELAPSED -lt $MAX_WAIT ]; do
    DB_HEALTH=$(docker inspect --format='{{.State.Health.Status}}' flowly-db-prod 2>/dev/null || echo "starting")
    API_HEALTH=$(docker inspect --format='{{.State.Health.Status}}' flowly-api-prod 2>/dev/null || echo "starting")
    WEB_HEALTH=$(docker inspect --format='{{.State.Health.Status}}' flowly-web-prod 2>/dev/null || echo "starting")
    
    echo -ne "\rDatabase: $DB_HEALTH | API: $API_HEALTH | Web: $WEB_HEALTH | Elapsed: ${ELAPSED}s"
    
    if [ "$DB_HEALTH" = "healthy" ] && [ "$API_HEALTH" = "healthy" ] && [ "$WEB_HEALTH" = "healthy" ]; then
        ALL_HEALTHY=true
        break
    fi
    
    sleep 5
    ELAPSED=$((ELAPSED + 5))
done

echo ""
echo ""

if [ "$ALL_HEALTHY" = true ]; then
    echo -e "${GREEN}✅ All services are healthy!${NC}"
else
    echo -e "${YELLOW}⚠️  Some services may not be fully healthy yet${NC}"
    echo "Check logs with: docker-compose -f $COMPOSE_FILE logs -f"
fi

echo ""
echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║   Deployment Summary                   ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""
echo -e "${GREEN}✅ Flowly has been deployed successfully!${NC}"
echo ""
echo "📊 Service Status:"
docker-compose -f "$COMPOSE_FILE" ps
echo ""
echo "🌐 Access Points:"
echo "   Web Application: http://localhost:${WEB_PORT:-80}"
echo "   API Health: http://localhost:${API_PORT:-5000}/health (if exposed)"
echo ""
echo "📝 Useful Commands:"
echo "   View logs:        docker-compose -f $COMPOSE_FILE logs -f"
echo "   Stop services:    docker-compose -f $COMPOSE_FILE down"
echo "   Restart services: docker-compose -f $COMPOSE_FILE restart"
echo "   View status:      docker-compose -f $COMPOSE_FILE ps"
echo ""
echo "🔒 Security Reminders:"
echo "   - Ensure firewall rules are configured"
echo "   - Set up SSL/TLS certificates for HTTPS"
echo "   - Configure regular backups"
echo "   - Monitor logs for suspicious activity"
echo "   - Keep Docker images updated"
echo ""
