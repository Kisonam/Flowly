namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// Budget information DTO
/// </summary>
public class BudgetDto
{
    public Guid Id { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal Limit { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public decimal CurrentSpent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Related entities
    public CategoryDto? Category { get; set; }

    // Computed properties
    public decimal RemainingAmount => Math.Max(0, Limit - CurrentSpent);
    public int ProgressPercentage => Limit > 0 ? Math.Min(100, (int)((CurrentSpent / Limit) * 100)) : 0;
    public bool IsExceeded => CurrentSpent > Limit;
    public bool IsActive { get; set; }
    public int DaysRemaining { get; set; }
}
