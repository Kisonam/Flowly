using System.Globalization;
using Flowly.Domain.Entities;
using Flowly.Infrastructure.Data;
using Flowly.Infrastructure.Identity;
using Flowly.Web.Components;
using Flowly.Web.Features.Auth; // ← DTO тут
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DB: той самий провайдер/рядки, що й у API (SQLite за замовчуванням)
var useSqlServer = builder.Configuration.GetValue<bool>("Database:UseSqlServer");
var sqlite = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=flowly_dev.db";
var sqlserver = builder.Configuration.GetConnectionString("SqlServer");

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (useSqlServer && !string.IsNullOrWhiteSpace(sqlserver))
        opt.UseSqlServer(sqlserver);
    else
        opt.UseSqlite(sqlite);
});

builder.Services.AddHttpContextAccessor();

// Razor Components + Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Identity з cookie — Blazor Server працює чудово з цим флоу
builder.Services
    .AddIdentityCore<AppUser>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequiredLength = 6; // мінімально, для деву
        o.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, opt =>
    {
        // Для SPA/API не редіректимо на /Account/Login — залишаємо 401/403 для fetch
        opt.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
        opt.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
    });

builder.Services.AddAuthorization();
builder.Services.AddAntiforgery();

// Локалізація
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supported = new[] { "uk", "en" }.Select(c => new CultureInfo(c)).ToArray();
    options.SupportedCultures = supported;
    options.SupportedUICultures = supported;
    options.SetDefaultCulture("uk");
});

var app = builder.Build();
// === Автоміграція БД на старті (створює таблиці Identity + наші) ===
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Flowly.Infrastructure.Data.AppDbContext>();
    db.Database.Migrate(); // якщо БД/таблиць нема — створить і накотить всі міграції
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// === Same-origin Minimal API for Web (щоб форми /auth/* працювали без CORS і кука була видима Web) ===
var auth = app.MapGroup("/api/auth").WithTags("WebAuth");

// POST /api/auth/register
auth.MapPost("/register", async (
    HttpContext http,
    UserManager<AppUser> users,
    SignInManager<AppUser> signIn,
    AppDbContext db,
    RegisterDto req,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password)
        || string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
        return Results.BadRequest(new { error = "All fields are required." });

    var user = new AppUser { UserName = req.Email, Email = req.Email };
    var result = await users.CreateAsync(user, req.Password);
    if (!result.Succeeded)
        return Results.BadRequest(new { error = string.Join("; ", result.Errors.Select(e => e.Description)) });

    db.UserProfiles.Add(new UserProfile
    {
        UserId = user.Id,
        FirstName = req.FirstName.Trim(),
        LastName  = req.LastName.Trim(),
        PreferredCulture = "uk"
    });
    await db.SaveChangesAsync(ct);

    await signIn.SignInAsync(user, isPersistent: true); // кука ставиться на origin Web
    return Results.Ok(new { user.Id, user.Email });
})
.Accepts<RegisterDto>("application/json")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

// POST /api/auth/login
auth.MapPost("/login", async (
    UserManager<AppUser> users,
    SignInManager<AppUser> signIn,
    LoginDto req) =>
{
    var user = await users.FindByEmailAsync(req.Email);
    if (user is null) return Results.Unauthorized();

    var signInRes = await signIn.PasswordSignInAsync(user, req.Password, req.RememberMe, lockoutOnFailure: false);
    return signInRes.Succeeded ? Results.Ok(new { user.Id, user.Email }) : Results.Unauthorized();
})
.Accepts<LoginDto>("application/json")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized);

// POST /api/auth/logout
auth.MapPost("/logout", async (SignInManager<AppUser> signIn) =>
{
    await signIn.SignOutAsync();
    return Results.Ok();
}).RequireAuthorization();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();