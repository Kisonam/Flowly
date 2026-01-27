namespace Flowly.Application.DTOs.Transactions;

public class FinancialGoalDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal CurrentAmount { get; set; }
    public DateTime? Deadline { get; set; }
    public string? Description { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int ProgressPercentage => TargetAmount > 0 ? Math.Min(100, (int)((CurrentAmount / TargetAmount) * 100)) : 0;
    public decimal RemainingAmount => Math.Max(0, TargetAmount - CurrentAmount);
    public bool IsCompleted => CurrentAmount >= TargetAmount;
    public bool IsOverdue { get; set; }
    public bool IsDeadlineApproaching { get; set; }
}
