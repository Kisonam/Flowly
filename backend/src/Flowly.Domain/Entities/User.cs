using Flowly.Domain.Enums;

namespace Flowly.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? AvatarPath { get; set; }
    public ThemeMode PreferredTheme { get; set; } = ThemeMode.Normal;
    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    public ICollection<TaskTheme> TaskThemes { get; set; } = new List<TaskTheme>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public ICollection<FinancialGoal> FinancialGoals { get; set; } = new List<FinancialGoal>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();
    public ICollection<ArchiveEntry> ArchiveEntries { get; set; } = new List<ArchiveEntry>();
    // Methods
    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));

        DisplayName = displayName.Trim();
    }

    public void UpdateAvatar(string avatarPath)
    {
        AvatarPath = avatarPath;
    }

    public void ChangeTheme(ThemeMode theme)
    {
        PreferredTheme = theme;
    }
    public void RemoveAvatar()
    {
        AvatarPath = null;
    }
}
