using Flowly.Contracts.Auth;
using Flowly.Domain.Entities;
using Flowly.Infrastructure.Data;
using Flowly.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest req,
            UserManager<AppUser> users,
            SignInManager<AppUser> signIn,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "Email and Password are required." });

            var user = new AppUser { UserName = req.Email, Email = req.Email };
            var result = await users.CreateAsync(user, req.Password);
            if (!result.Succeeded)
                return Results.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

            // Створюємо профіль — відокремлено від Identity
            var profile = new UserProfile
            {
                UserId = user.Id,
                FirstName = req.FirstName?.Trim() ?? string.Empty,
                LastName = req.LastName?.Trim() ?? string.Empty,
                PreferredCulture = "uk"
            };
            db.UserProfiles.Add(profile);
            await db.SaveChangesAsync(ct);

            await signIn.SignInAsync(user, isPersistent: true);

            return Results.Ok(new AuthResponse(user.Id, user.Email!));
        });

        group.MapPost("/login", async (
            LoginRequest req,
            SignInManager<AppUser> signIn,
            UserManager<AppUser> users) =>
        {
            var user = await users.FindByEmailAsync(req.Email);
            if (user is null) return Results.Unauthorized();

            var result = await signIn.PasswordSignInAsync(user, req.Password, req.RememberMe, lockoutOnFailure: false);
            return result.Succeeded
                ? Results.Ok(new AuthResponse(user.Id, user.Email!))
                : Results.Unauthorized();
        });

        group.MapPost("/logout", async (SignInManager<AppUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.Ok();
        }).RequireAuthorization();

        return app;
    }
}