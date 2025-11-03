using Flowly.Api.Configuration;

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
builder.Services.AddControllers();

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
// app.UseHttpsRedirection();
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
