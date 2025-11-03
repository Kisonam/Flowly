using System.Text;
using Flowly.Application.Common.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Flowly.Api.Configuration;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Bind JWT settings
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        // Get JWT settings
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings not found in appsettings.json");

        // Configure JWT Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false; // Set to true in production
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero // Remove 5 min default tolerance
            };

            // JWT event logging
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"❌ JWT Authentication failed: {context.Exception.Message}");
                    if (context.Exception.InnerException != null)
                    {
                        Console.WriteLine($"   Inner: {context.Exception.InnerException.Message}");
                    }
                    Console.WriteLine($"   Exception Type: {context.Exception.GetType().Name}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                    Console.WriteLine($"✅ JWT Token validated for user: {email} (ID: {userId})");
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    Console.WriteLine($"⚠️ JWT Challenge triggered");
                    Console.WriteLine($"   Error: {context.Error}");
                    Console.WriteLine($"   ErrorDescription: {context.ErrorDescription}");
                    Console.WriteLine($"   AuthenticateFailure: {context.AuthenticateFailure?.Message}");
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader))
                    {
                        Console.WriteLine($"📨 Authorization header: {authHeader.Substring(0, Math.Min(60, authHeader.Length))}...");
                        
                        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("⚠️ WARNING: Token does not start with 'Bearer '");
                        }
                    }
                    else
                    {
                        Console.WriteLine("📭 No Authorization header found");
                    }
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}
