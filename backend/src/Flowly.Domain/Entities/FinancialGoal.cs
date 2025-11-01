// backend/src/Flowly.Domain/Entities/FinancialGoal.cs

namespace Flowly.Domain.Entities;

/// <summary>
/// Financial goal - target amount to save
/// </summary>
public class FinancialGoal
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Owner of the goal
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Goal title (e.g., "New Laptop", "Vacation", "Emergency Fund")
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Target amount to reach
    /// </summary>
    public decimal TargetAmount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Current saved amount
    /// </summary>
    public decimal CurrentAmount { get; set; } = 0;

    /// <summary>
    /// Optional deadline
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the goal is archived
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// When the goal was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the goal was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the goal was completed (if reached)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    // ============================================
    // Navigation Properties
    // ============================================

    /// <summary>
    /// Currency reference
    /// </summary>
    public Currency Currency { get; set; } = null!;

    // ============================================
    // Methods
    // ============================================

    /// <summary>
    /// Update goal details
    /// </summary>
    public void Update(string title, decimal targetAmount, string currencyCode, DateTime? deadline = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        if (targetAmount <= 0)
            throw new ArgumentException("Target amount must be positive", nameof(targetAmount));

        Title = title.Trim();
        TargetAmount = targetAmount;
        CurrencyCode = currencyCode;
        Deadline = deadline;
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add to current amount
    /// </summary>
    public void AddAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        CurrentAmount += amount;
        UpdatedAt = DateTime.UtcNow;

        if (IsCompleted() && CompletedAt == null)
        {
            CompletedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Subtract from current amount
    /// </summary>
    public void SubtractAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        CurrentAmount = Math.Max(0, CurrentAmount - amount);
        UpdatedAt = DateTime.UtcNow;

        if (!IsCompleted())
        {
            CompletedAt = null;
        }
    }

    /// <summary>
    /// Set current amount directly
    /// </summary>
    public void SetCurrentAmount(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        CurrentAmount = amount;
        UpdatedAt = DateTime.UtcNow;

        if (IsCompleted() && CompletedAt == null)
        {
            CompletedAt = DateTime.UtcNow;
        }
        else if (!IsCompleted())
        {
            CompletedAt = null;
        }
    }

    /// <summary>
    /// Check if goal is completed
    /// </summary>
    public bool IsCompleted()
    {
        return CurrentAmount >= TargetAmount;
    }

    /// <summary>
    /// Get completion percentage
    /// </summary>
    public int GetProgressPercentage()
    {
        if (TargetAmount == 0) return 0;
        return Math.Min(100, (int)((CurrentAmount / TargetAmount) * 100));
    }

    /// <summary>
    /// Get remaining amount to reach goal
    /// </summary>
    public decimal GetRemainingAmount()
    {
        return Math.Max(0, TargetAmount - CurrentAmount);
    }

    /// <summary>
    /// Archive goal
    /// </summary>
    public void Archive()
    {
        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restore from archive
    /// </summary>
    public void Restore()
    {
        IsArchived = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if deadline is approaching (within 7 days)
    /// </summary>
    public bool IsDeadlineApproaching()
    {
        if (!Deadline.HasValue) return false;
        var daysUntilDeadline = (Deadline.Value - DateTime.UtcNow).Days;
        return daysUntilDeadline > 0 && daysUntilDeadline <= 7;
    }

    /// <summary>
    /// Check if deadline has passed
    /// </summary>
    public bool IsOverdue()
    {
        return Deadline.HasValue && Deadline.Value < DateTime.UtcNow && !IsCompleted();
    }
}