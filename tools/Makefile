# Makefile для Flowly проєкту
# Використання: make [command]

.PHONY: help dev db-up db-down migrate seed clean test docker-up docker-down docker-rebuild

# Кольори для виводу
GREEN  := $(shell tput -Txterm setaf 2)
YELLOW := $(shell tput -Txterm setaf 3)
WHITE  := $(shell tput -Txterm setaf 7)
CYAN   := $(shell tput -Txterm setaf 6)
RESET  := $(shell tput -Txterm sgr0)

help: ## Показати це меню
	@echo ''
	@echo '${CYAN}Flowly - Routine Organizer${RESET}'
	@echo ''
	@echo 'Usage:'
	@echo '  ${YELLOW}make${RESET} ${GREEN}<target>${RESET}'
	@echo ''
	@echo 'Targets:'
	@awk 'BEGIN {FS = ":.*?## "} { \
		if (/^[a-zA-Z_-]+:.*?##.*$$/) {printf "  ${YELLOW}%-20s${GREEN}%s${RESET}\n", $$1, $$2} \
		else if (/^## .*$$/) {printf "  ${CYAN}%s${RESET}\n", substr($$1,4)} \
		}' $(MAKEFILE_LIST)

## Development
dev: ## Запустити проєкт локально (backend + frontend + db)
	@echo "${GREEN}🚀 Starting Flowly in development mode...${RESET}"
	@make db-up
	@sleep 2
	@echo "${YELLOW}Starting backend...${RESET}"
	@cd backend && dotnet watch run --project src/Flowly.Api/Flowly.Api.csproj &
	@echo "${YELLOW}Starting frontend...${RESET}"
	@cd frontend && npm start

dev-backend: ## Запустити тільки backend
	@echo "${GREEN}🔧 Starting backend...${RESET}"
	@cd backend && dotnet watch run --project src/Flowly.Api/Flowly.Api.csproj

dev-frontend: ## Запустити тільки frontend
	@echo "${GREEN}🎨 Starting frontend...${RESET}"
	@cd frontend && npm start

## Database
db-up: ## Підняти PostgreSQL + pgAdmin
	@echo "${GREEN}🐘 Starting PostgreSQL...${RESET}"
	@docker-compose up -d db pgadmin

db-down: ## Зупинити БД
	@echo "${YELLOW}⏹️  Stopping database...${RESET}"
	@docker-compose down

db-reset: ## Видалити БД і створити заново (ВИДАЛИТЬ ВСІ ДАНІ!)
	@echo "${YELLOW}⚠️  This will DELETE all data! Press Ctrl+C to cancel...${RESET}"
	@sleep 3
	@docker-compose down -v
	@make db-up
	@sleep 3
	@make migrate
	@make seed

## Migrations
migrate: ## Застосувати міграції через Docker
	@chmod +x ../tools/apply-migrations.sh
	@../tools/apply-migrations.sh

migrate-add: ## Створити нову міграцію (use: make migrate-add name=MigrationName)
	@if [ -z "$(name)" ]; then \
		echo "${YELLOW}❌ Помилка: Вкажіть назву міграції${RESET}"; \
		echo "Використання: make migrate-add name=НазваМіграції"; \
		exit 1; \
	fi
	@chmod +x ../tools/create-migration.sh
	@../tools/create-migration.sh $(name)

migrate-remove: ## Видалити останню міграцію
	@echo "${YELLOW}🗑️  Removing last migration...${RESET}"
	@cd ../backend/src/Flowly.Infrastructure && \
		dotnet ef migrations remove --startup-project ../Flowly.Api/Flowly.Api.csproj --context AppDbContext

migrate-list: ## Показати список міграцій
	@cd ../backend/src/Flowly.Infrastructure && \
		dotnet ef migrations list --startup-project ../Flowly.Api/Flowly.Api.csproj --context AppDbContext

seed: ## Заповнити БД початковими даними
	@chmod +x tools/seed.sh
	@./tools/seed.sh

## Testing
test: ## Запустити всі тести
	@echo "${GREEN}🧪 Running tests...${RESET}"
	@cd backend && dotnet test
	@cd frontend && npm test

test-backend: ## Тести backend
	@echo "${GREEN}🧪 Running backend tests...${RESET}"
	@cd backend && dotnet test

test-frontend: ## Тести frontend
	@echo "${GREEN}🧪 Running frontend tests...${RESET}"
	@cd frontend && npm test

## Docker (Production)
docker-up: ## Запустити ВСЕ через Docker (production mode)
	@echo "${GREEN}🐳 Starting all services in Docker...${RESET}"
	@docker-compose up -d --build

docker-down: ## Зупинити Docker контейнери
	@echo "${YELLOW}⏹️  Stopping Docker containers...${RESET}"
	@docker-compose down

docker-rebuild: ## Rebuild Docker images
	@echo "${GREEN}🔨 Rebuilding Docker images...${RESET}"
	@docker-compose build --no-cache

docker-logs: ## Показати логи Docker
	@docker-compose logs -f

## Cleanup
clean: ## Очистити build артефакти
	@echo "${YELLOW}🧹 Cleaning build artifacts...${RESET}"
	@find backend -type d -name "bin" -o -name "obj" | xargs rm -rf
	@rm -rf frontend/dist frontend/.angular frontend/node_modules/.cache

clean-all: clean ## Очистити ВСЕ (включно з node_modules)
	@echo "${YELLOW}🧹 Deep cleaning...${RESET}"
	@rm -rf frontend/node_modules
	@rm -rf backend/src/*/bin backend/src/*/obj

## Build
build: ## Build проєкту
	@echo "${GREEN}🔨 Building backend...${RESET}"
	@cd backend && dotnet build
	@echo "${GREEN}🔨 Building frontend...${RESET}"
	@cd frontend && npm run build

## Install
install: ## Встановити всі залежності
	@echo "${GREEN}📦 Installing backend dependencies...${RESET}"
	@cd backend && dotnet restore
	@echo "${GREEN}📦 Installing frontend dependencies...${RESET}"
	@cd frontend && npm install --legacy-peer-deps

## Backup
backup: ## Створити backup БД
	@echo "${GREEN}💾 Creating database backup...${RESET}"
	@mkdir -p backups
	@docker exec flowly-db pg_dump -U $(POSTGRES_USER) $(POSTGRES_DB) > backups/backup_$(shell date +%Y%m%d_%H%M%S).sql
	@echo "${GREEN}✅ Backup created in backups/${RESET}"

restore: ## Відновити БД з backup (use: make restore file=backup.sql)
	@echo "${YELLOW}📥 Restoring database from $(file)...${RESET}"
	@docker exec -i flowly-db psql -U $(POSTGRES_USER) $(POSTGRES_DB) < $(file)
	@echo "${GREEN}✅ Database restored${RESET}"

## Info
status: ## Показати статус сервісів
	@echo "${CYAN}📊 Services Status:${RESET}"
	@docker-compose ps

logs: ## Показати логи БД
	@docker-compose logs -f db

info: ## Показати інформацію про проєкт
	@echo "${CYAN}ℹ️  Flowly Project Info${RESET}"
	@echo ""
	@echo "${YELLOW}Backend:${RESET}"
	@cd backend && dotnet --version
	@echo ""
	@echo "${YELLOW}Frontend:${RESET}"
	@cd frontend && node --version && npm --version
	@echo ""
	@echo "${YELLOW}Docker:${RESET}"
	@docker --version
	@docker-compose --version