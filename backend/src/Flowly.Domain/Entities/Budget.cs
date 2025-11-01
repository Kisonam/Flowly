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
    public void Update(DateTime periodStart, DateTime periodEnd, decimal limit, string currencyCode, Guid? categoryId = null)
    {
        if (periodEnd <= periodStart)
            throw new ArgumentException("Period end must be after period start");

        if (limit <= 0)
            throw new ArgumentException("Limit must be positive", nameof(limit));

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
    /// Get days remaining in budget period
    /// </summary>
    public int GetDaysRemaining()
    {
        if (IsExpired()) return 0;
        return (PeriodEnd - DateTime.UtcNow).Days;
    }
}