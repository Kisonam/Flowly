#!/bin/bash
# tools/seed.sh
# Скрипт для заповнення БД початковими даними

set -e

echo "🌱 Starting database seeding..."

# Кольори
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Завантажити .env
if [ ! -f .env ]; then
    echo -e "${RED}❌ Error: .env file not found!${NC}"
    exit 1
fi

export $(cat .env | grep -v '^#' | xargs)

# Перевірка PostgreSQL
echo -e "${YELLOW}📡 Checking PostgreSQL connection...${NC}"
if ! docker-compose ps db | grep -q "Up"; then
    echo -e "${RED}❌ PostgreSQL is not running!${NC}"
    echo "Run: docker-compose up -d db"
    exit 1
fi

# SQL скрипт для seed даних
echo -e "${YELLOW}🌱 Inserting seed data...${NC}"

# Виконати SQL через docker exec
docker exec -i flowly-db psql -U $POSTGRES_USER -d $POSTGRES_DB <<-EOSQL
-- ============================================
-- Currencies (валюти)
-- ============================================
INSERT INTO "Currencies" ("Code", "Name", "Symbol") VALUES
    ('USD', 'US Dollar', '\$'),
    ('EUR', 'Euro', '€'),
    ('UAH', 'Ukrainian Hryvnia', '₴'),
    ('PLN', 'Polish Zloty', 'zł')
ON CONFLICT ("Code") DO NOTHING;

-- ============================================
-- Default Categories (базові категорії)
-- ============================================
-- Ці категорії будуть мати UserId = NULL (глобальні)
-- Користувачі можуть створювати власні
INSERT INTO "Categories" ("Id", "UserId", "Name") VALUES
    (gen_random_uuid(), NULL, 'Food & Drinks'),
    (gen_random_uuid(), NULL, 'Transport'),
    (gen_random_uuid(), NULL, 'Shopping'),
    (gen_random_uuid(), NULL, 'Entertainment'),
    (gen_random_uuid(), NULL, 'Health'),
    (gen_random_uuid(), NULL, 'Education'),
    (gen_random_uuid(), NULL, 'Utilities'),
    (gen_random_uuid(), NULL, 'Salary'),
    (gen_random_uuid(), NULL, 'Freelance'),
    (gen_random_uuid(), NULL, 'Other')
ON CONFLICT DO NOTHING;

-- ============================================
-- Test User (для розробки)
-- ============================================
-- Password: Test123! (hashed)
-- Цей користувач тільки для dev середовища!
DO \$\$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "AspNetUsers" WHERE "Email" = 'test@flowly.com') THEN
        INSERT INTO "AspNetUsers" (
            "Id", 
            "UserName", 
            "NormalizedUserName", 
            "Email", 
            "NormalizedEmail", 
            "EmailConfirmed",
            "PasswordHash",
            "SecurityStamp",
            "ConcurrencyStamp",
            "PhoneNumberConfirmed",
            "TwoFactorEnabled",
            "LockoutEnabled",
            "AccessFailedCount",
            "DisplayName",
            "PreferredTheme",
            "CreatedAt"
        ) VALUES (
            gen_random_uuid(),
            'test@flowly.com',
            'TEST@FLOWLY.COM',
            'test@flowly.com',
            'TEST@FLOWLY.COM',
            true,
            'AQAAAAIAAYagAAAAEFake0Hash0ForDevelopment0Only',
            'FAKESECURITYSTAMP',
            gen_random_uuid()::text,
            false,
            false,
            true,
            0,
            'Test User',
            'Normal',
            NOW()
        );
        RAISE NOTICE 'Test user created: test@flowly.com / Test123!';
    ELSE
        RAISE NOTICE 'Test user already exists';
    END IF;
END \$\$;

EOSQL

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Seed data inserted successfully!${NC}"
    echo ""
    echo -e "${GREEN}📊 Seeded data:${NC}"
    echo "  • 4 Currencies (USD, EUR, UAH, PLN)"
    echo "  • 10 Default Categories"
    echo "  • 1 Test User (test@flowly.com / Test123!)"
    echo ""
    echo -e "${YELLOW}⚠️  Note: Test user is for development only!${NC}"
else
    echo -e "${RED}❌ Seeding failed!${NC}"
    exit 1
fi

echo -e "${GREEN}🎉 Database seeding complete!${NC}"