#!/bin/bash
# tools/migrate.sh
# Скрипт для застосування EF Core міграцій

set -e  # Зупинитись при помилці

echo "🔄 Starting database migration..."

# Кольори для output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Перевірка чи є .env файл
if [ ! -f .env ]; then
    echo -e "${RED}❌ Error: .env file not found!${NC}"
    echo "Please create .env file from .env.example"
    exit 1
fi

# Завантажити змінні з .env
export $(cat .env | grep -v '^#' | xargs)

# Перевірка чи працює PostgreSQL
echo -e "${YELLOW}📡 Checking PostgreSQL connection...${NC}"
if ! docker-compose ps db | grep -q "Up"; then
    echo -e "${RED}❌ PostgreSQL is not running!${NC}"
    echo "Starting PostgreSQL..."
    docker-compose up -d db
    echo "Waiting for PostgreSQL to be ready..."
    sleep 5
fi

# Перевірка чи встановлений dotnet-ef
if ! dotnet tool list -g | grep -q "dotnet-ef"; then
    echo -e "${YELLOW}📦 Installing dotnet-ef tool...${NC}"
    dotnet tool install --global dotnet-ef
fi

# Перейти в папку Infrastructure (де DbContext)
cd backend/src/Flowly.Infrastructure

# Застосувати міграції
echo -e "${YELLOW}🚀 Applying migrations...${NC}"
dotnet ef database update --startup-project ../Flowly.Api/Flowly.Api.csproj --context AppDbContext

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Migrations applied successfully!${NC}"
    
    # Показати список міграцій
    echo -e "${YELLOW}📋 Applied migrations:${NC}"
    dotnet ef migrations list --startup-project ../Flowly.Api/Flowly.Api.csproj --context AppDbContext
else
    echo -e "${RED}❌ Migration failed!${NC}"
    exit 1
fi

echo -e "${GREEN}🎉 Database is ready!${NC}"