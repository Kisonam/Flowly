# Flowly

Personal routine and budget management application built with ASP.NET Core and Angular.

## ✨ Features

- 🔐 **Authentication & Authorization**
  - Email/Password registration and login
  - **Google OAuth 2.0** (Login & Sign up) 🆕
  - JWT tokens (Access & Refresh)
  - User profile management
  - Avatar upload

- 📊 **Dashboard** (Coming soon)
- 💰 **Budget Management** (Coming soon)
- 📝 **Routine Tracking** (Coming soon)

## 🚀 Google OAuth Integration

Flowly now supports Google Sign-In! Users can login or register using their Google account.

**Setup Guide:** [docs/GOOGLE_OAUTH_QUICKSTART.md](./docs/GOOGLE_OAUTH_QUICKSTART.md)

## 🛠️ Tech Stack

### Backend
- ASP.NET Core 9.0
- Entity Framework Core
- PostgreSQL
- JWT Authentication
- Google.Apis.Auth
- Swagger/OpenAPI

### Frontend
- Angular 19
- TypeScript
- Bootstrap 5
- RxJS
- Google Identity Services

## 📦 Getting Started

### Prerequisites
- .NET 9.0 SDK
- Node.js 18+ & npm
- PostgreSQL 14+
- Docker & Docker Compose (optional)

### Quick Start with Docker

```bash
# Clone repository
git clone <repository-url>
cd Flowly

# Start all services
docker compose up --build
```

- Frontend: http://localhost:4200
- Backend API: http://localhost:5001
- Swagger: http://localhost:5001/swagger

### Manual Setup

#### Backend

```bash
cd backend/src/Flowly.Api

# Restore dependencies
dotnet restore

# Update database
dotnet ef database update

# Run
dotnet run
```

#### Frontend

```bash
cd frontend

# Install dependencies
npm install

# Run development server
ng serve
```

## 🔐 Google OAuth Setup

To enable Google Sign-In:

1. Get Google OAuth credentials from [Google Cloud Console](https://console.cloud.google.com/)
2. Configure backend:
   ```bash
   cd backend/src/Flowly.Api
   dotnet user-secrets set "Google:ClientId" "YOUR_CLIENT_ID"
   dotnet user-secrets set "Google:ClientSecret" "YOUR_CLIENT_SECRET"
   ```
3. Configure frontend in `frontend/src/environments/environment.ts`:
   ```typescript
   googleClientId: 'YOUR_CLIENT_ID.apps.googleusercontent.com'
   ```

**Full documentation:** [docs/GOOGLE_OAUTH_SETUP.md](./docs/GOOGLE_OAUTH_SETUP.md)

## 📚 Documentation

- [Google OAuth Quick Start](./docs/GOOGLE_OAUTH_QUICKSTART.md)
- [Google OAuth Setup Guide](./docs/GOOGLE_OAUTH_SETUP.md)
- [Google OAuth Summary](./docs/GOOGLE_OAUTH_SUMMARY.md)
- [Database Migrations](./MIGRATIONS.md)

## 🔧 Development

### Database Migrations

```bash
# Create new migration
cd backend/src/Flowly.Infrastructure
dotnet ef migrations add MigrationName -s ../Flowly.Api

# Apply migrations
cd ../Flowly.Api
dotnet ef database update
```

### Testing

```bash
# Backend tests (coming soon)
cd backend
dotnet test

# Frontend tests
cd frontend
ng test
```

## 📝 License

See [LICENSE](./LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.