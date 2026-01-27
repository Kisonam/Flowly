using Flowly.Application.DTOs.Tasks;
using Flowly.Application.Interfaces;
using Flowly.Domain.Entities;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Services;

public class TaskThemeService(AppDbContext db) : ITaskThemeService
{
    private readonly AppDbContext _db = db;

    public async Task<List<TaskThemeDto>> GetAllAsync(Guid userId)
    {
        var themes = await _db.TaskThemes
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Order)
            .ToListAsync();

        return themes.Select(Map).ToList();
    }

    public async Task<TaskThemeDto> GetByIdAsync(Guid userId, Guid id)
    {
        var theme = await _db.TaskThemes.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (theme == null) throw new InvalidOperationException("Theme not found");
        return Map(theme);
    }

    public async Task<TaskThemeDto> CreateAsync(Guid userId, CreateTaskThemeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Title is required", nameof(dto.Title));

        var maxOrder = await _db.TaskThemes.Where(t => t.UserId == userId).MaxAsync(t => (int?)t.Order) ?? -1;

        var theme = new TaskTheme
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = dto.Title.Trim(),
            Color = dto.Color,
            Order = maxOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        _db.TaskThemes.Add(theme);
        await _db.SaveChangesAsync();
        return Map(theme);
    }

    public async Task<TaskThemeDto> UpdateAsync(Guid userId, Guid id, UpdateTaskThemeDto dto)
    {
        var theme = await _db.TaskThemes.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (theme == null) throw new InvalidOperationException("Theme not found");

        if (!string.IsNullOrWhiteSpace(dto.Title)) theme.UpdateTitle(dto.Title);
        if (dto.Color != null) theme.UpdateColor(dto.Color);
        if (dto.Order.HasValue) theme.UpdateOrder(dto.Order.Value);

        await _db.SaveChangesAsync();
        return Map(theme);
    }

    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var theme = await _db.TaskThemes.Include(t => t.Tasks)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (theme == null) throw new InvalidOperationException("Theme not found");

        foreach (var task in theme.Tasks)
        {
            task.TaskThemeId = null;
        }

        _db.TaskThemes.Remove(theme);
        await _db.SaveChangesAsync();
    }

    private static TaskThemeDto Map(TaskTheme t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Order = t.Order,
        Color = t.Color
    };
}
