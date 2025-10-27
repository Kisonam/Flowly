# Dockerfile для .NET API
# Місце: backend/Dockerfile

# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Flowly.sln", "./"]
COPY ["src/Flowly.Api/Flowly.Api.csproj", "src/Flowly.Api/"]
COPY ["src/Flowly.Application/Flowly.Application.csproj", "src/Flowly.Application/"]
COPY ["src/Flowly.Domain/Flowly.Domain.csproj", "src/Flowly.Domain/"]
COPY ["src/Flowly.Infrastructure/Flowly.Infrastructure.csproj", "src/Flowly.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Flowly.sln"

# Copy all source code
COPY . .

# Build the API project
WORKDIR "/src/src/Flowly.Api"
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

# Install curl for healthchecks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create uploads directory
RUN mkdir -p /app/uploads && chmod 755 /app/uploads

# Copy published app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "Flowly.Api.dll"]