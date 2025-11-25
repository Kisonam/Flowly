using Flowly.Infrastructure.Data;
using Flowly.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flowly.UnitTests.Helpers;

/// <summary>
/// Factory для створення тестового DbContext з SQLite In-Memory базою даних.
/// Використовується для ізоляції тестів - кожен тест отримує свою власну базу.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Створює новий AppDbContext з SQLite In-Memory базою даних.
    /// База автоматично створюється та ініціалізується схемою.
    /// </summary>
    /// <returns>Налаштований DbContext готовий до використання в тестах</returns>
    public static AppDbContext CreateInMemoryContext()
    {
        // Створюємо унікальне ім'я для кожної бази даних, щоб тести не впливали один на одного
        var connectionString = $"DataSource=:memory:";
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        var context = new AppDbContext(options);
        
        // ВАЖЛИВО: Для SQLite In-Memory потрібно тримати з'єднання відкритим
        // інакше база буде видалена при закритті з'єднання
        context.Database.OpenConnection();
        
        // Створюємо схему бази даних
        context.Database.EnsureCreated();
        
        return context;
    }

    /// <summary>
    /// Створює UserManager для тестування з мінімальною конфігурацією.
    /// Використовується для тестування AuthService.
    /// </summary>
    public static UserManager<ApplicationUser> CreateUserManager(AppDbContext context)
    {
        var store = new UserStore<ApplicationUser, IdentityRole<Guid>, AppDbContext, Guid>(context);
        var options = Options.Create(new IdentityOptions
        {
            // Вимкнемо складні вимоги до паролів для простоти тестування
            Password = new PasswordOptions
            {
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false,
                RequireNonAlphanumeric = false,
                RequiredLength = 6
            },
            // Налаштування lockout для тестування
            Lockout = new LockoutOptions
            {
                MaxFailedAccessAttempts = 5,
                DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5)
            }
        });
        
        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var userValidators = new List<IUserValidator<ApplicationUser>> { new UserValidator<ApplicationUser>() };
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<UserManager<ApplicationUser>>>();

        return new UserManager<ApplicationUser>(
            store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            serviceProvider,
            logger
        );
    }

    /// <summary>
    /// Створює SignInManager для тестування.
    /// Використовується для тестування логіну та перевірки паролів.
    /// </summary>
    public static SignInManager<ApplicationUser> CreateSignInManager(
        UserManager<ApplicationUser> userManager,
        AppDbContext context)
    {
        var services = new ServiceCollection();
        
        // Додаємо необхідні сервіси для SignInManager
        services.AddSingleton(userManager);
        services.AddLogging();
        services.AddHttpContextAccessor();
        
        // Додаємо authentication services
        services.AddAuthentication();
        services.AddOptions();
        
        services.AddIdentityCore<ApplicationUser>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager();

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
    }
}
