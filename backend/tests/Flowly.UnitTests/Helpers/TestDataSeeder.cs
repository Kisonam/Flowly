using Flowly.Domain.Entities;
using Flowly.Domain.Enums;
using Flowly.Infrastructure.Data;
using Flowly.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Flowly.UnitTests.Helpers;

/// <summary>
/// Допоміжний клас для створення тестових даних.
/// Забезпечує консистентність тестових даних між різними тестами.
/// </summary>
public static class TestDataSeeder
{
    /// <summary>
    /// Створює тестового користувача з заданими параметрами.
    /// </summary>
    public static async Task<ApplicationUser> CreateTestUserAsync(
        UserManager<ApplicationUser> userManager,
        string email = "test@example.com",
        string password = "Test123!",
        string displayName = "Test User")
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    /// <summary>
    /// Створює тестові теги для користувача.
    /// </summary>
    public static async Task<List<Tag>> CreateTestTagsAsync(AppDbContext context, Guid userId, int count = 3)
    {
        var tags = new List<Tag>();
        for (int i = 0; i < count; i++)
        {
            tags.Add(new Tag
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = $"Tag{i + 1}",
                Color = $"#00{i}{i}{i}{i}00",
                CreatedAt = DateTime.UtcNow
            });
        }

        context.Tags.AddRange(tags);
        await context.SaveChangesAsync();
        return tags;
    }

    /// <summary>
    /// Створює тестові категорії для фінансів.
    /// </summary>
    public static async Task<List<Category>> CreateTestCategoriesAsync(AppDbContext context, Guid userId)
    {
        var categories = new List<Category>
        {
            new Category
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Food",
                Color = "#FF5733",
                Icon = "🍔"
            },
            new Category
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Transport",
                Color = "#33FF57",
                Icon = "🚗"
            },
            new Category
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Salary",
                Color = "#3357FF",
                Icon = "💰"
            }
        };

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();
        return categories;
    }

    /// <summary>
    /// Створює тестову валюту.
    /// </summary>
    public static async Task<Currency> CreateTestCurrencyAsync(AppDbContext context, string code = "USD")
    {
        // Перевіряємо чи вже існує така валюта
        var existing = await context.Currencies.FindAsync(code);
        if (existing != null)
        {
            return existing;
        }

        var currency = new Currency
        {
            Code = code,
            Name = code == "USD" ? "US Dollar" : code,
            Symbol = code == "USD" ? "$" : code
        };

        context.Currencies.Add(currency);
        await context.SaveChangesAsync();
        return currency;
    }

    /// <summary>
    /// Створює тестову тему для задач.
    /// </summary>
    public static async Task<TaskTheme> CreateTestTaskThemeAsync(
        AppDbContext context,
        Guid userId,
        string title = "Work",
        string? color = "#FF5733")
    {
        var theme = new TaskTheme
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Color = color,
            Order = 0,
            CreatedAt = DateTime.UtcNow
        };

        context.TaskThemes.Add(theme);
        await context.SaveChangesAsync();
        return theme;
    }

    /// <summary>
    /// Створює тестовий бюджет.
    /// </summary>
    public static async Task<Budget> CreateTestBudgetAsync(
        AppDbContext context,
        Guid userId,
        string currencyCode = "USD",
        decimal limit = 1000m)
    {
        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Monthly Budget",
            Description = "Test budget",
            PeriodStart = DateTime.UtcNow.Date,
            PeriodEnd = DateTime.UtcNow.Date.AddMonths(1),
            Limit = limit,
            CurrencyCode = currencyCode,
            CreatedAt = DateTime.UtcNow
        };

        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        return budget;
    }
}
