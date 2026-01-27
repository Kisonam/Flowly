using Flowly.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Flowly.Api.Configuration;

public static class IdentityConfiguration
{
    public static IServiceCollection AddIdentityConfiguration(
        this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            options.User.RequireUniqueEmail = true;

            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<Flowly.Infrastructure.Data.AppDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }
}
