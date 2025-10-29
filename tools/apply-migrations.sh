#!/bin/bash

# Скрипт для застосування EF Core міграцій до бази даних
# Використовується з локальної машини до Docker бази даних

set -e

echo "🔄 Застосування міграцій до бази даних..."

cd "$(dirname "$0")/../backend/src/Flowly.Infrastructure"

CONNECTION_STRING="Host=127.0.0.1;Port=5432;Database=flowly_db;Username=flowly_user;Password=MySecurePass123!"

dotnet ef database update \
  --startup-project ../Flowly.Api/Flowly.Api.csproj \
  --context AppDbContext \
  --connection "$CONNECTION_STRING"

echo "✅ Міграції успішно застосовані!"
