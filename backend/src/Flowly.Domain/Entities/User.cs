using System;
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

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="Note"></typeparam>
    /// <returns></returns>
    public ICollection<Note> Notes { get; set; } = new List<Note>();
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TaskItem"></typeparam>
    /// <returns></returns>
    public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    /// <summary>
    /// User's task themes/lists
    /// </summary>
    // public ICollection<TaskTheme> TaskThemes { get; set; } = new List<TaskTheme>();

    /// <summary>
    /// User's financial transactions
    /// </summary>
    // public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    /// <summary>
    /// User's custom categories
    /// </summary>
    // public ICollection<Category> Categories { get; set; } = new List<Category>();

    /// <summary>
    /// User's budgets
    /// </summary>
    // public ICollection<Budget> Budgets { get; set; } = new List<Budget>();

    /// <summary>
    /// User's financial goals
    /// </summary>
    // public ICollection<FinancialGoal> FinancialGoals { get; set; } = new List<FinancialGoal>();

    /// <summary>
    /// User's custom tags
    /// </summary>
    // public ICollection<Tag> Tags { get; set; } = new List<Tag>();

    /// <summary>
    /// User's uploaded media assets
    /// </summary>
    // public ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();

    /// <summary>
    /// User's archived items
    /// </summary>
    // public ICollection<ArchiveEntry> ArchiveEntries { get; set; } = new List<ArchiveEntry>();

    // ============================================
    // Methods
    // ============================================

    /// <summary>
    /// Update the display name and set UpdatedAt timestamp
    /// </summary>
    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));

        DisplayName = displayName.Trim();
    }

    /// <summary>
    /// Update the avatar path
    /// </summary>
    public void UpdateAvatar(string avatarPath)
    {
        AvatarPath = avatarPath;
    }

    /// <summary>
    /// Change the preferred theme
    /// </summary>
    public void ChangeTheme(ThemeMode theme)
    {
        PreferredTheme = theme;
    }

    /// <summary>
    /// Remove avatar
    /// </summary>
    public void RemoveAvatar()
    {
        AvatarPath = null;
    }
}
