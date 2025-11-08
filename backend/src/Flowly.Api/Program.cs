using Flowly.Api.Configuration;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables
DotNetEnv.Env.Load();
builder.Configuration.AddEnvironmentVariables();

// ============================================
// Configure Services
// ============================================

// Database
builder.Services.AddDatabaseConfiguration(builder.Configuration);

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
app.UseStaticFiles();
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
