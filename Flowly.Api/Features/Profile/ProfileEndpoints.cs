using Flowly.Contracts.Profile;
using Flowly.Infrastructure.Data;
using Flowly.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Api.Features.Profile;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        // GET /api/profile/me
        group.MapGet("/me", async (
            UserManager<AppUser> users,
            AppDbContext db,
            HttpContext http,
            CancellationToken ct) =>
        {
            var userId = users.GetUserId(http.User);
            if (userId is null) return Results.Unauthorized();

            var p = await db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
            if (p is null) return Results.NotFound();

            var resp = new UserProfileResponse(p.UserId, p.FirstName, p.LastName, p.PreferredCulture, p.AvatarPath);
            return Results.Ok(resp);
        });

        // PUT /api/profile/me
        group.MapPut("/me", async (
            UpdateProfileRequest req,
            UserManager<AppUser> users,
            AppDbContext db,
            HttpContext http,
            CancellationToken ct) =>
        {
            var userId = users.GetUserId(http.User);
            if (userId is null) return Results.Unauthorized();

            var p = await db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == userId, ct);
            if (p is null) return Results.NotFound();

            if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
                return Results.BadRequest(new { error = "FirstName and LastName are required." });

            p.FirstName = req.FirstName.Trim();
            p.LastName = req.LastName.Trim();
            p.PreferredCulture = string.IsNullOrWhiteSpace(req.PreferredCulture) ? p.PreferredCulture : req.PreferredCulture.Trim();
            p.AvatarPath = string.IsNullOrWhiteSpace(req.AvatarPath) ? null : req.AvatarPath.Trim();

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return app;
    }
}