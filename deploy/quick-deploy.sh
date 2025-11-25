#!/bin/bash
# Quick deployment script for DigitalOcean
# Run this on your LOCAL machine to deploy to server

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🚀 Flowly Quick Deploy Script${NC}"
echo ""

# Check if server IP is provided
if [ -z "$1" ]; then
    echo -e "${RED}❌ Error: Server IP not provided${NC}"
    echo "Usage: ./quick-deploy.sh YOUR_SERVER_IP"
    exit 1
fi

SERVER_IP=$1
SERVER_USER=${2:-root}

echo -e "${YELLOW}📡 Deploying to: $SERVER_USER@$SERVER_IP${NC}"
echo ""

# Test SSH connection
echo -e "${YELLOW}🔐 Testing SSH connection...${NC}"
if ! ssh -o ConnectTimeout=5 $SERVER_USER@$SERVER_IP "echo 'Connection successful'"; then
    echo -e "${RED}❌ Cannot connect to server${NC}"
    exit 1
fi
echo -e "${GREEN}✅ SSH connection successful${NC}"
echo ""

# Copy files to server
echo -e "${YELLOW}📦 Copying files to server...${NC}"
rsync -avz --exclude 'node_modules' --exclude 'bin' --exclude 'obj' --exclude '.git' \
    ../ $SERVER_USER@$SERVER_IP:/var/www/flowly/

echo -e "${GREEN}✅ Files copied${NC}"
echo ""

# Build and start containers
echo -e "${YELLOW}🐳 Building and starting Docker containers...${NC}"
ssh $SERVER_USER@$SERVER_IP << 'ENDSSH'
cd /var/www/flowly
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml build
docker-compose -f docker-compose.prod.yml up -d
ENDSSH

echo -e "${GREEN}✅ Containers started${NC}"
echo ""

# Wait for services to be ready
echo -e "${YELLOW}⏳ Waiting for services to be ready...${NC}"
sleep 10

# Check health
echo -e "${YELLOW}🏥 Checking health...${NC}"
ssh $SERVER_USER@$SERVER_IP << 'ENDSSH'
cd /var/www/flowly
docker-compose -f docker-compose.prod.yml ps
ENDSSH

echo ""
echo -e "${GREEN}✅ Deployment complete!${NC}"
echo ""
echo -e "${YELLOW}📊 Check status:${NC}"
echo "  ssh $SERVER_USER@$SERVER_IP 'cd /var/www/flowly && docker-compose -f docker-compose.prod.yml logs -f'"
echo ""
echo -e "${YELLOW}🌐 Open in browser:${NC}"
echo "  http://$SERVER_IP"
