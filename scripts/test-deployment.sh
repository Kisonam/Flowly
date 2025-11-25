#!/bin/bash
# test-deployment.sh
# Test production deployment locally
# Usage: ./scripts/test-deployment.sh

set -e

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo "🧪 Testing Production Deployment"
echo "================================"
echo ""

# Check if containers are running
echo "📦 Checking container status..."
if ! docker ps | grep -q "flowly-.*-prod"; then
    echo -e "${RED}❌ No production containers running${NC}"
    echo "Start deployment first: ./scripts/deploy-production.sh"
    exit 1
fi

echo -e "${GREEN}✅ Containers are running${NC}"
echo ""

# Test database
echo "🗄️  Testing database connection..."
if docker exec flowly-db-prod pg_isready -U flowly_prod_user > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Database is ready${NC}"
else
    echo -e "${RED}❌ Database is not ready${NC}"
    exit 1
fi

# Test API health
echo "🔌 Testing API health endpoint..."
API_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/health 2>/dev/null || echo "000")
if [ "$API_HEALTH" = "200" ]; then
    echo -e "${GREEN}✅ API health check passed (HTTP $API_HEALTH)${NC}"
    curl -s http://localhost:5000/health | jq '.' 2>/dev/null || curl -s http://localhost:5000/health
else
    echo -e "${RED}❌ API health check failed (HTTP $API_HEALTH)${NC}"
    echo "Checking if API port is exposed..."
    if ! docker ps | grep "flowly-api-prod" | grep -q "5000"; then
        echo -e "${YELLOW}⚠️  API port is not exposed (internal only)${NC}"
        echo "Testing through web proxy instead..."
    fi
fi
echo ""

# Test web frontend
echo "🌐 Testing web frontend..."
WEB_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost/ 2>/dev/null || echo "000")
if [ "$WEB_HEALTH" = "200" ]; then
    echo -e "${GREEN}✅ Web frontend is accessible (HTTP $WEB_HEALTH)${NC}"
else
    echo -e "${RED}❌ Web frontend is not accessible (HTTP $WEB_HEALTH)${NC}"
    exit 1
fi
echo ""

# Test API through web proxy
echo "🔄 Testing API through web proxy..."
API_PROXY=$(curl -s -o /dev/null -w "%{http_code}" http://localhost/api/health 2>/dev/null || echo "000")
if [ "$API_PROXY" = "200" ]; then
    echo -e "${GREEN}✅ API accessible through web proxy (HTTP $API_PROXY)${NC}"
else
    echo -e "${YELLOW}⚠️  API proxy returned HTTP $API_PROXY${NC}"
fi
echo ""

# Check container health status
echo "💚 Checking container health status..."
DB_HEALTH=$(docker inspect --format='{{.State.Health.Status}}' flowly-db-prod 2>/dev/null || echo "unknown")
API_HEALTH_STATUS=$(docker inspect --format='{{.State.Health.Status}}' flowly-api-prod 2>/dev/null || echo "unknown")
WEB_HEALTH_STATUS=$(docker inspect --format='{{.State.Health.Status}}' flowly-web-prod 2>/dev/null || echo "unknown")

echo "Database: $DB_HEALTH"
echo "API: $API_HEALTH_STATUS"
echo "Web: $WEB_HEALTH_STATUS"
echo ""

if [ "$DB_HEALTH" = "healthy" ] && [ "$API_HEALTH_STATUS" = "healthy" ] && [ "$WEB_HEALTH_STATUS" = "healthy" ]; then
    echo -e "${GREEN}✅ All containers are healthy!${NC}"
else
    echo -e "${YELLOW}⚠️  Some containers may still be starting...${NC}"
fi
echo ""

# Check resource usage
echo "📊 Resource usage:"
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}" | grep flowly
echo ""

# Check logs for errors
echo "📝 Checking logs for errors (last 50 lines)..."
ERROR_COUNT=$(docker-compose -f docker-compose.prod.yml logs --tail=50 2>&1 | grep -i "error\|exception\|fatal" | wc -l)
if [ "$ERROR_COUNT" -gt 0 ]; then
    echo -e "${YELLOW}⚠️  Found $ERROR_COUNT error messages in logs${NC}"
    echo "Review logs with: docker-compose -f docker-compose.prod.yml logs"
else
    echo -e "${GREEN}✅ No errors found in recent logs${NC}"
fi
echo ""

# Summary
echo "================================"
echo "🎉 Deployment Test Summary"
echo "================================"
echo ""
echo "Access your application at:"
echo "  🌐 Web: http://localhost"
echo "  📡 API: http://localhost:5000 (if exposed)"
echo ""
echo "Useful commands:"
echo "  📋 View logs: docker-compose -f docker-compose.prod.yml logs -f"
echo "  📊 View stats: docker stats"
echo "  🔄 Restart: docker-compose -f docker-compose.prod.yml restart"
echo ""
