using Flowly.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Flowly.IntegrationTests.Helpers;

/// <summary>
/// Custom WebApplicationFactory для інтеграційних тестів.
/// Замінює реальну базу даних на In-Memory для ізоляції тестів.
/// </summary>
public class FlowlyWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureTestServices(services =>
        {
            // Повністю прибираємо PostgreSQL реєстрації DbContext
            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));

            // Додаємо In-Memory базу даних з унікальним ім'ям для кожного тестового хоста
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase($"FlowlyIntegration_{Guid.NewGuid()}");
            });
        });
    }
}
