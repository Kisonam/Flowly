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
    private const string TestDatabaseName = "FlowlyIntegrationTestDb";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureTestServices(services =>
        {
            // Повністю прибираємо PostgreSQL реєстрації DbContext
            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));

            // Додаємо In-Memory базу даних зі спільною назвою для всіх тестів
            // Це дозволяє зберігати дані між запитами в межах одного тесту
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(TestDatabaseName);
            });

            // Ініціалізуємо базу даних
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
