using Flowly.Api.Configuration;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

DotNetEnv.Env.Load();
builder.Configuration.AddEnvironmentVariables();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDatabaseConfiguration(builder.Configuration);
}

builder.Services.AddIdentityConfiguration();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddValidationConfiguration();

builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

builder.Services.AddCorsConfiguration();

var app = builder.Build();

app.UseSwaggerDocumentation();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    await app.ApplyMigrationsAsync();
}

if (!app.Environment.IsProduction())
{
}

app.UseStaticFiles();

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

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}));

Console.WriteLine("🚀 Flowly API is running!");

app.Run();

public partial class Program { }
