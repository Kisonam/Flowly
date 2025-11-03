namespace Flowly.Api.Configuration;

public static class CorsConfiguration
{
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAngular", policy =>
            {
                policy.WithOrigins("http://localhost:4200")
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
        // Use different CORS policies based on environment
        if (env.IsDevelopment() || env.IsProduction())
        {
            app.UseCors("AllowAll"); // Allow all for Swagger testing
        }
        else
        {
            app.UseCors("AllowAngular"); // Restrict in production
        }

        return app;
    }
}
