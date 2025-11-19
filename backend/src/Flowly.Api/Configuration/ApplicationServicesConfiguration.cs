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
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<INoteGroupService, NoteGroupService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITaskItemQueryService, TaskItemQueryService>();
        services.AddScoped<ITaskThemeService, TaskThemeService>();
        
        // Finance Module Services
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IFinancialGoalService, FinancialGoalService>();

        // Links Module Services
        services.AddScoped<ILinkService, LinkService>();

        // Archive Service
        services.AddScoped<IArchiveService, ArchiveService>();
        services.AddScoped<ArchiveMigrationService>();

        // Export Service
        services.AddScoped<IExportService, ExportService>();

        // Dashboard Service
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
