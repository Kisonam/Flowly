#!/bin/bash

# Скрипт для застосування EF Core міграцій до бази даних через Docker

set -e

echo "🔄 Застосування міграцій до бази даних через Docker..."

cd "$(dirname "$0")/.."

# Перевірка, чи запущена база даних
if ! docker compose ps db | grep -q "Up"; then
    echo "⚠️  База даних не запущена. Запускаю..."
    docker compose up -d db
    sleep 5
fi

# Застосування міграцій через тимчасовий Docker контейнер
docker run --rm \
  --network flowly_default \
  -v "$(pwd):/src" \
  -w /src \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  bash -c "dotnet tool install --global dotnet-ef --version 9.0.10 2>/dev/null || true && \
           export PATH=\"\$PATH:/root/.dotnet/tools\" && \
           cd backend/src/Flowly.Infrastructure && \
           dotnet ef database update \
             --startup-project ../Flowly.Api/Flowly.Api.csproj \
             --context AppDbContext \
             --connection 'Host=db;Port=5432;Database=flowly_db;Username=flowly_user;Password=MySecurePass123!'"

echo "✅ Міграції успішно застосовані!"
