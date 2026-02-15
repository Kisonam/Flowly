## Wymagania Wstępne

Przed rozpoczęciem upewnij się, że masz zainstalowane następujące narzędzia:

### Wymagane Oprogramowanie

1. **Docker Desktop** (zalecane) - wersja 20.10 lub nowsza
   - Pobierz z: https://www.docker.com/products/docker-desktop
   - Docker Desktop zawiera Docker Compose

**LUB** (dla rozwoju lokalnego bez Dockera):

2. **.NET SDK** - wersja 9.0
   - Pobierz z: https://dotnet.microsoft.com/download
   - Sprawdź instalację: `dotnet --version`

3. **Node.js** - wersja 20.x LTS
   - Pobierz z: https://nodejs.org/
   - Sprawdź instalację: `node --version` i `npm --version`

4. **PostgreSQL** - wersja 16
   - Pobierz z: https://www.postgresql.org/download/
   - Lub użyj Dockera: `docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgres:16-alpine`

### Opcjonalne Narzędzia

- **Git** - do klonowania repozytorium
- **Visual Studio Code** lub **Visual Studio 2022** - do edycji kodu
- **pgAdmin** lub **DBeaver** - do zarządzania bazą danych

## Instalacja od Zera

### Opcja 1: Uruchomienie z Docker (Zalecane)

Jest to najprostszy sposób uruchomienia projektu. Docker automatycznie skonfiguruje wszystkie usługi.

#### Krok 1: Rozpakuj projekt

Rozpakuj projekt na swoje urządzenie

#### Krok 2: Uruchom Docker Desktop

Upewnij się, że Docker Desktop jest uruchomiony na twoim komputerze.

#### Krok 3: Uruchom aplikację w trybie produkcyjnym

```bash
docker-compose -f docker-compose.prod.yml up -d --build
```

To polecenie:
- Zbuduje obrazy Docker dla API (.NET), frontendu (Angular) i bazy danych (PostgreSQL)
- Uruchomi wszystkie kontenery w tle
- Automatycznie zastosuje migracje bazy danych

#### Krok 4: Sprawdź status kontenerów

```bash
docker-compose -f docker-compose.prod.yml ps
```

Powinieneś zobaczyć 3 działające kontenery:
- `flowly-db-prod` - Baza danych PostgreSQL
- `flowly-api-prod` - Backend API (.NET)
- `flowly-web-prod` - Frontend (Angular)

#### Krok 5: Otwórz aplikację

- **Frontend**: http://localhost
- **API**: http://localhost:5001/health
- **Swagger API Docs**: http://localhost:5001/swagger

#### Zatrzymanie aplikacji

```bash
docker-compose -f docker-compose.prod.yml down
```

Aby usunąć również wolumeny (dane bazy):
```bash
docker-compose -f docker-compose.prod.yml down -v
```

### Opcja 2: Uruchomienie Lokalne (Rozwój)

#### Krok 1: Rozpakuj projekt
Rozpakuj projekt na swoje urządzenie.

#### Krok 2: Skonfiguruj bazę danych PostgreSQL

Utwórz bazę danych:
```sql
CREATE DATABASE flowly_db;
```

#### Krok 3: Skonfiguruj zmienne środowiskowe

Utwórz plik `.env` w katalogu głównym projektu:

```env
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=flowly_db;Username=postgres;Password=postgres

JWT__Secret=twoj-super-tajny-klucz-jwt-minimum-32-znaki
JWT__Issuer=FlowlyAPI
JWT__Audience=FlowlyClient
JWT__ExpirationMinutes=60

FileStorage__Path=/app/uploads
FileStorage__MaxFileSizeMB=10

Google__ClientId=twoj-google-client-id
Google__ClientSecret=twoj-google-client-secret
```

#### Krok 4: Zainstaluj zależności backendu

```bash
cd backend/src/Flowly.Api
dotnet restore
```

#### Krok 5: Zastosuj migracje bazy danych

```bash
dotnet ef database update --project ../Flowly.Infrastructure
```

Jeśli `dotnet ef` nie jest zainstalowane:
```bash
dotnet tool install --global dotnet-ef
```

#### Krok 6: Uruchom backend

```bash
dotnet run
```

Backend będzie dostępny pod adresem: http://localhost:5000

#### Krok 7: Zainstaluj zależności frontendu

Otwórz nowy terminal:

```bash
cd frontend
npm install --legacy-peer-deps
```

#### Krok 8: Uruchom frontend

```bash
npm start
```

Frontend będzie dostępny pod adresem: http://localhost:4200

## Konfiguracja

### Konfiguracja Środowiska Produkcyjnego

Edytuj plik `.env.production` dla ustawień produkcyjnych:

```env
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=flowly-db;Port=5432;Database=flowly_db;Username=flowly_user;Password=bezpieczne-haslo

JWT__Secret=super-bezpieczny-klucz-produkcyjny-minimum-32-znaki
JWT__Issuer=FlowlyAPI
JWT__Audience=FlowlyClient
JWT__ExpirationMinutes=60
```

### Konfiguracja Google OAuth (Opcjonalnie)

1. Przejdź do [Google Cloud Console](https://console.cloud.google.com/)
2. Utwórz nowy projekt lub wybierz istniejący
3. Włącz Google+ API
4. Utwórz dane uwierzytelniające OAuth 2.0
5. Dodaj autoryzowane URI przekierowania:
   - http://localhost:4200 (rozwój)
   - https://twoja-domena.com (produkcja)
6. Skopiuj Client ID i Client Secret do pliku `.env`

## Struktura Projektu

```
Flowly/
├── backend/                    # Backend ASP.NET Core
│   ├── src/
│   │   ├── Flowly.Api/        # Warstwa API (Controllers, Configuration)
│   │   ├── Flowly.Application/ # DTOs, Interfaces
│   │   ├── Flowly.Domain/     # Encje, Enums
│   │   └── Flowly.Infrastructure/ # Implementacje serwisów, DbContext
│   └── tests/                 # Testy jednostkowe i integracyjne
├── frontend/                   # Frontend Angular
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/         # Serwisy podstawowe, interceptory
│   │   │   ├── features/     # Moduły funkcjonalne
│   │   │   └── shared/       # Komponenty współdzielone
│   │   └── environments/     # Konfiguracja środowisk
├── docker-compose.yml          # Konfiguracja Docker dla rozwoju
├── docker-compose.prod.yml     # Konfiguracja Docker dla produkcji
└── .env                        # Zmienne środowiskowe (nie commituj!)
```

## Domyślne Konto

Po pierwszym uruchomieniu możesz zarejestrować nowe konto lub użyć funkcji logowania przez Google.

## Uruchomienie Testów

### Testy Backendu

```bash
cd backend
dotnet test
```

### Testy Frontendu

```bash
cd frontend
npm test
```

