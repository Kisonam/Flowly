#!/bin/bash

# Скрипт для створення нової EF Core міграції

set -e

if [ -z "$1" ]; then
    echo "❌ Помилка: Вкажіть назву міграції"
    echo "Використання: ./tools/create-migration.sh НазваМіграції"
    echo "Приклад: ./tools/create-migration.sh AddUserPhoneNumber"
    exit 1
fi

MIGRATION_NAME=$1

echo "📝 Створення міграції: $MIGRATION_NAME"

cd "$(dirname "$0")/../backend/src/Flowly.Infrastructure"

dotnet ef migrations add "$MIGRATION_NAME" \
  --startup-project ../Flowly.Api/Flowly.Api.csproj \
  --context AppDbContext

echo "✅ Міграція $MIGRATION_NAME створена!"
echo "📂 Файл міграції: backend/src/Flowly.Infrastructure/Migrations/"
echo ""
echo "Наступні кроки:"
echo "1. Перевірте згенеровану міграцію"
echo "2. Застосуйте її: ./tools/apply-migrations.sh"
