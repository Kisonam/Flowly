using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Api.Configuration;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options => 
            options.UseNpgsql(connectionString));

        return services;
    }

    public static async Task ApplyMigrationsAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        try
        {
            Console.WriteLine("🔄 Applying database migrations...");
            await dbContext.Database.MigrateAsync();
            Console.WriteLine("✅ Database migrations applied successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error applying migrations: {ex.Message}");
        }
    }
}
