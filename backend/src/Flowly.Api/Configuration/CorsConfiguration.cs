namespace Flowly.Api.Configuration;

public static class CorsConfiguration
{
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAngular", policy =>
            {
                policy.WithOrigins(
                          "http://localhost:4200",
                          "https://localhost:4200",
                          "http://localhost:5173",
                          "https://localhost:5173")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });

            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }

    public static IApplicationBuilder UseCorsConfiguration(this IApplicationBuilder app, IWebHostEnvironment env)
    {

        if (env.IsDevelopment() || env.IsProduction())
        {
            app.UseCors("AllowAll");
        }
        else
        {
            app.UseCors("AllowAngular");
        }

        return app;
    }
}
