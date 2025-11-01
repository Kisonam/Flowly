# 🚀 Швидкий старт - Робота з міграціями

## 📋 Зміст

- [Запуск проекту](#запуск-проекту)
- [Робота з міграціями](#робота-з-міграціями)
- [Makefile команди](#makefile-команди)
- [Troubleshooting](#troubleshooting)

---

## 🚀 Запуск проекту

### Перший запуск

```sh
# 1. Переконайтеся, що Docker Desktop запущений

# 2. Запустіть базу даних і застосуйте міграції
docker compose up -d db
./tools/apply-migrations.sh

# 3. Запустіть всі сервіси
docker compose up -d

# 4. Перевірте статус
docker compose ps

# 5. API доступне на: http://localhost:5001
# 6. PgAdmin: http://localhost:5050 (admin@example.com / admin123)
```

### Звичайний запуск

```sh
# Запустити все
docker compose up -d

# Зупинити все
docker compose down

# Зупинити і видалити дані БД
docker compose down -v
```

---

## 🗄️ Робота з міграціями

### 1️⃣ Створення нової міграції

Коли ви змінюєте entity класи у `backend/src/Flowly.Domain/Entities/`:

```sh
# Спосіб 1: Через скрипт (рекомендовано)
./tools/create-migration.sh НазваМіграції

# Спосіб 2: Через Makefile
cd tools && make migrate-add name=НазваМіграції

# Спосіб 3: Вручну
cd backend/src/Flowly.Infrastructure
dotnet ef migrations add НазваМіграції --startup-project ../Flowly.Api
```

**Приклади назв міграцій:**
- `AddUserPhoneNumber`
- `UpdateTransactionTable`
- `AddTaskPriorityEnum`
- `RemoveOldFields`

### 2️⃣ Застосування міграцій до БД

```sh
# Спосіб 1: Через скрипт (рекомендовано)
./tools/apply-migrations.sh

# Спосіб 2: Через Makefile
cd tools && make migrate

# Спосіб 3: Вручну через Docker
docker run --rm \
  --network flowly_default \
  -v "$(pwd):/src" \
  -w /src \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  bash -c "dotnet tool install --global dotnet-ef && \
           export PATH=\"\$PATH:/root/.dotnet/tools\" && \
           cd backend/src/Flowly.Infrastructure && \
           dotnet ef database update \
             --startup-project ../Flowly.Api/Flowly.Api.csproj \
             --context AppDbContext \
             --connection 'Host=db;Port=5432;Database=flowly_db;Username=flowly_user;Password=MySecurePass123!'"
```

### 3️⃣ Перегляд міграцій

```sh
# Список всіх міграцій
cd backend/src/Flowly.Infrastructure
dotnet ef migrations list --startup-project ../Flowly.Api

# Або через Makefile
cd tools && make migrate-list
```

### 4️⃣ Видалення останньої міграції

```sh
# Якщо міграція ще НЕ застосована до БД
cd backend/src/Flowly.Infrastructure
dotnet ef migrations remove --startup-project ../Flowly.Api

# Або через Makefile
cd tools && make migrate-remove
```

### 5️⃣ Відкат міграції в БД

```sh
# Відкотити до конкретної міграції
cd backend/src/Flowly.Infrastructure
dotnet ef database update НазваПопередньоїМіграції --startup-project ../Flowly.Api

# Відкотити ВСІ міграції
dotnet ef database update 0 --startup-project ../Flowly.Api
```

---

## 🔄 Типовий робочий процес

### Сценарій: Додати нове поле до User

```sh
# 1. Відредагуйте entity
# backend/src/Flowly.Domain/Entities/User.cs
# Додайте: public string? PhoneNumber { get; set; }

# 2. Створіть міграцію
./tools/create-migration.sh AddUserPhoneNumber

# 3. Перевірте згенеровану міграцію
code backend/src/Flowly.Infrastructure/Migrations/*_AddUserPhoneNumber.cs

# 4. Застосуйте до БД
./tools/apply-migrations.sh

# 5. Перевірте в PgAdmin
# http://localhost:5050
# Подивіться структуру таблиці Users

# 6. Перезапустіть API (якщо потрібно)
docker compose restart api
```

---

## 🛠️ Makefile команди

```sh
cd tools

# Показати всі доступні команди
make help

# База даних
make db-up              # Запустити PostgreSQL
make db-down            # Зупинити БД
make db-reset           # Пересоздати БД (видалить всі дані!)

# Міграції
make migrate            # Застосувати міграції
make migrate-add name=MyMigration  # Створити міграцію
make migrate-remove     # Видалити останню міграцію
make migrate-list       # Список міграцій

# Docker
make docker-up          # Запустити все в Docker
make docker-down        # Зупинити Docker
make docker-rebuild     # Пересобрати images
make docker-logs        # Показати логи

# Development
make dev-backend        # Запустити backend локально
make dev-frontend       # Запустити frontend локально

# Backup
make backup             # Створити backup БД
make restore file=backup.sql  # Відновити з backup

# Інше
make status             # Статус сервісів
make clean              # Очистити build файли
make info               # Інфо про проєкт
```

---

## 🐛 Troubleshooting

### ❌ "Format of the initialization string does not conform to specification"

**Проблема:** `dotnet ef` не може знайти connection string.

**Рішення:**
```sh
# Використовуйте скрипт замість прямої команди
./tools/apply-migrations.sh

# Або вкажіть connection string явно
cd backend/src/Flowly.Infrastructure
dotnet ef database update --startup-project ../Flowly.Api \
  --connection "Host=localhost;Port=5432;Database=flowly_db;Username=flowly_user;Password=MySecurePass123!"
```

### ❌ "role 'flowly_user' does not exist"

**Проблема:** База даних створена без правильних змінних оточення.

**Рішення:**
```sh
# Пересоздайте БД
docker compose down -v
docker compose up -d db
sleep 5
./tools/apply-migrations.sh
```

### ❌ "Port 5000 already in use"

**Проблема:** macOS ControlCenter використовує порт 5000.

**Рішення:** У `.env` файлі API вже налаштоване на порт 5001, тож проблеми бути не має.

### ❌ Міграції не застосовуються автоматично

**Проблема:** API запускається, але таблиці не створюються.

**Рішення:**
```sh
# Застосуйте міграції вручну
./tools/apply-migrations.sh

# Або налаштуйте автоматичні міграції в Program.cs
```

### ❌ "The model for context has pending changes"

**Проблема:** Модель змінилася, але міграція не створена.

**Рішення:**
```sh
# Створіть нову міграцію
./tools/create-migration.sh FixModelChanges
./tools/apply-migrations.sh
```

---

## 📊 Перевірка стану БД

```sh
# Підключитися до БД через psql
docker exec -it flowly-db psql -U flowly_user -d flowly_db

# Корисні команди в psql:
\dt                      # Список таблиць
\d "Users"               # Структура таблиці Users
\du                      # Список користувачів
SELECT * FROM "Users";   # Дані з таблиці
\q                       # Вийти
```

---

## 🔗 Корисні посилання

- **API:** http://localhost:5001
- **Health Check:** http://localhost:5001/health
- **PgAdmin:** http://localhost:5050
- **PostgreSQL:** localhost:5432

**Логіни PgAdmin:**
- Email: `admin@example.com`
- Password: `admin123`

**PostgreSQL credentials:**
- Host: `localhost` (або `db` всередині Docker)
- Port: `5432`
- Database: `flowly_db`
- User: `flowly_user`
- Password: `MySecurePass123!`

---

## 📝 Чек-лист щоденної роботи

- [ ] `docker compose up -d` - Запустити проект
- [ ] Змінити entities/models у `backend/src/Flowly.Domain/Entities/`
- [ ] `./tools/create-migration.sh НазваМіграції` - Створити міграцію
- [ ] Перевірити згенеровану міграцію
- [ ] `./tools/apply-migrations.sh` - Застосувати міграцію
- [ ] Перевірити зміни в PgAdmin
- [ ] `docker compose restart api` - Перезапустити API
- [ ] `docker compose down` - Зупинити проект (в кінці дня)
