namespace Flowly.Domain.Entities;

public class FinancialGoal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal CurrentAmount { get; set; } = 0;
    public DateTime? Deadline { get; set; }
    public string? Description { get; set; }
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public Currency Currency { get; set; } = null!;
    
    public void Update(string title, decimal targetAmount,
     string currencyCode, DateTime? deadline = null, string? description = null)
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
    public void SetCurrentAmount(decimal amount)
    {
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
    public bool IsCompleted()
    {
        return CurrentAmount >= TargetAmount;
    }
    public int GetProgressPercentage()
    {
        if (TargetAmount == 0) return 0;
        return Math.Min(100, (int)((CurrentAmount / TargetAmount) * 100));
    }
    public decimal GetRemainingAmount()
    {
        return Math.Max(0, TargetAmount - CurrentAmount);
    }
    public void Archive()
    {
        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
    }
    public void Restore()
    {
        IsArchived = false;
        UpdatedAt = DateTime.UtcNow;
    }
    public bool IsDeadlineApproaching()
    {
        if (!Deadline.HasValue) return false;
        var daysUntilDeadline = (Deadline.Value - DateTime.UtcNow).Days;
        return daysUntilDeadline > 0 && daysUntilDeadline <= 7;
    }
    public bool IsOverdue()
    {
        return Deadline.HasValue && Deadline.Value < DateTime.UtcNow && !IsCompleted();
    }
}
