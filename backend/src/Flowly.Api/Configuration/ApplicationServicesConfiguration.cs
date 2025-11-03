using Flowly.Application.Common.Settings;
using Flowly.Application.DTOs.Common.Settings;
using Flowly.Application.Interfaces;
using Flowly.Infrastructure.Services;

namespace Flowly.Api.Configuration;

public static class ApplicationServicesConfiguration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<GoogleSettings>(configuration.GetSection("Google"));
        // Register application services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
