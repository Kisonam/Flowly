using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;

namespace Flowly.Api.Configuration;

public static class ValidationConfiguration
{
    public static IServiceCollection AddValidationConfiguration(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssembly(Assembly.Load("Flowly.Application"));

        return services;
    }
}
