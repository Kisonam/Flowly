// backend/src/Flowly.Domain/Entities/Budget.cs

namespace Flowly.Domain.Entities;

/// <summary>
/// Budget - spending limit for a period (optionally per category)
/// </summary>
public class Budget
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Owner of the budget
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Budget title/name
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Budget period start date
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Budget period end date
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Budget limit amount
    /// </summary>
    public decimal Limit { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Optional category (null = overall budget for all categories)
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// When the budget was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the budget was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Whether the budget is archived
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// When the budget was archived
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    // ============================================
    // Navigation Properties
    // ============================================

    /// <summary>
    /// Currency reference
    /// </summary>
    public Currency Currency { get; set; } = null!;

    /// <summary>
    /// Category reference (if category-specific budget)
    /// </summary>
    public Category? Category { get; set; }

    // ============================================
    // Methods
    // ============================================

    /// <summary>
    /// Update budget details
    /// </summary>
    public void Update(string title, string? description, DateTime periodStart, DateTime periodEnd, decimal limit, string currencyCode, Guid? categoryId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));

        if (periodEnd <= periodStart)
            throw new ArgumentException("Period end must be after period start");

        if (limit <= 0)
            throw new ArgumentException("Limit must be positive", nameof(limit));

        Title = title;
        Description = description;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        Limit = limit;
        CurrencyCode = currencyCode;
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if budget is currently active
    /// </summary>
    public bool IsActive()
    {
        var now = DateTime.UtcNow;
        return now >= PeriodStart && now <= PeriodEnd;
    }

    /// <summary>
    /// Check if budget period has expired
    /// </summary>
    public bool IsExpired()
    {
        return DateTime.UtcNow > PeriodEnd;
    }

    /// <summary>
    /// Get days remaining in budget period (negative if expired)
    /// </summary>
    public int GetDaysRemaining()
    {
        return (PeriodEnd - DateTime.UtcNow).Days;
    }

    /// <summary>
    /// Archive the budget
    /// </summary>
    public void Archive()
    {
        IsArchived = true;
        ArchivedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restore the budget from archive
    /// </summary>
    public void Restore()
    {
        IsArchived = false;
        ArchivedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}