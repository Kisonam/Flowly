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

public static class TestDbContextFactory
{

    public static AppDbContext CreateInMemoryContext()
    {
        
        var connectionString = $"DataSource=:memory:";
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        var context = new AppDbContext(options);

        context.Database.OpenConnection();

        context.Database.EnsureCreated();
        
        return context;
    }

    public static UserManager<ApplicationUser> CreateUserManager(AppDbContext context)
    {
        var store = new UserStore<ApplicationUser, IdentityRole<Guid>, AppDbContext, Guid>(context);
        var options = Options.Create(new IdentityOptions
        {
            
            Password = new PasswordOptions
            {
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false,
                RequireNonAlphanumeric = false,
                RequiredLength = 6
            },
            
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

    public static SignInManager<ApplicationUser> CreateSignInManager(
        UserManager<ApplicationUser> userManager,
        AppDbContext context)
    {
        var services = new ServiceCollection();

        services.AddSingleton(userManager);
        services.AddLogging();
        services.AddHttpContextAccessor();

        services.AddAuthentication();
        services.AddOptions();
        
        services.AddIdentityCore<ApplicationUser>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager();

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
    }
}
