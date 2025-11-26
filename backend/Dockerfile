# Dockerfile для .NET API
# Місце: backend/Dockerfile

# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Flowly.sln", "./"]
COPY ["backend/src/Flowly.Api/Flowly.Api.csproj", "backend/src/Flowly.Api/"]
COPY ["backend/src/Flowly.Application/Flowly.Application.csproj", "backend/src/Flowly.Application/"]
COPY ["backend/src/Flowly.Domain/Flowly.Domain.csproj", "backend/src/Flowly.Domain/"]
COPY ["backend/src/Flowly.Infrastructure/Flowly.Infrastructure.csproj", "backend/src/Flowly.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Flowly.sln"

# Copy all source code
COPY backend/ backend/

# Build the API project
WORKDIR "/src/backend/src/Flowly.Api"
RUN dotnet build "Flowly.Api.csproj" -c Release -o /app/build

# ============================================
# Stage 2: Publish
# ============================================
FROM build AS publish
RUN dotnet publish "Flowly.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ============================================
# Stage 3: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for healthchecks and gosu for user switching
RUN apt-get update && apt-get install -y curl gosu && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r flowly && useradd -r -g flowly flowly

# Copy published app
COPY --from=publish /app/publish .

# Copy entrypoint script
COPY backend/docker-entrypoint.sh /usr/local/bin/
RUN chmod +x /usr/local/bin/docker-entrypoint.sh

# Change ownership of app files
RUN chown -R flowly:flowly /app

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000
ENV COMPlus_EnableDiagnostics=0

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

# Run the application via entrypoint script
ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]