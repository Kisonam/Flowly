using Flowly.Api.Configuration;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Load configuration files (appsettings + environment specific)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Load environment variables (supports .env + system vars)
DotNetEnv.Env.Load();
builder.Configuration.AddEnvironmentVariables();

// ============================================
// Configure Services
// ============================================

// Database: пропускаємо PostgreSQL у тестовому середовищі (інтеграційні тести самі реєструють InMemory)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDatabaseConfiguration(builder.Configuration);
}

// Identity
builder.Services.AddIdentityConfiguration();

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Validation
builder.Services.AddValidationConfiguration();

// Application Services
builder.Services.AddApplicationServices(builder.Configuration);

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize/deserialize enums as strings (e.g., "High" instead of 3)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

// CORS
builder.Services.AddCorsConfiguration();

// ============================================
// Configure Pipeline
// ============================================

var app = builder.Build();

// Swagger Documentation
app.UseSwaggerDocumentation();

// Apply Database Migrations
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    await app.ApplyMigrationsAsync();
}

// Middleware Pipeline
// Only use HTTPS redirection in Development when not in Docker
if (!app.Environment.IsProduction())
{
    // app.UseHttpsRedirection(); // Disabled for Docker compatibility
}

// Serve static files from wwwroot
app.UseStaticFiles();

// Serve uploaded files from /app/uploads through /uploads URL
var uploadsPath = builder.Configuration["FileStorage:Path"] ?? "/app/uploads";
if (Directory.Exists(uploadsPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });
}

app.UseCorsConfiguration(app.Environment);
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// Health Check Endpoint
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}));

Console.WriteLine("🚀 Flowly API is running!");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
