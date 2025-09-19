using Flowly.Api.Features.Auth;
using Flowly.Api.Features.Profile;
using Flowly.Infrastructure.Data;
using Flowly.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);
// Читаємо прапорець провайдера (простий перемикач для Dev/Prod).
var useSqlServer = builder.Configuration.GetValue<bool>("Database:UseSqlServer");

// Конекшн-строки
var sqlite = builder.Configuration.GetConnectionString("Sqlite") 
             ?? "Data Source=flowly_dev.db";
var sqlserver = builder.Configuration.GetConnectionString("SqlServer");
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (useSqlServer && !string.IsNullOrWhiteSpace(sqlserver))
        opt.UseSqlServer(sqlserver);
    else
        opt.UseSqlite(sqlite);
});

builder.Services
    .AddIdentityCore<AppUser>(o =>
    {
        o.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme);

builder.Services.AddAuthorization();
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
    opt.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
});

builder.Services
    .AddIdentityCore<AppUser>(o => o.User.RequireUniqueEmail = true)
    .AddRoles<IdentityRole>()
    .AddSignInManager()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapAuthEndpoints();
app.MapProfileEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");

app.Run();
