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
                          "http://localhost:5001",
                          "https://localhost:5001")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
            
            // Policy for Swagger and development
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
        // Use AllowAll policy for Development and Production (Docker)
        // This allows testing from different origins including Google OAuth redirects
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
