namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// DTO for filtering financial goals
/// </summary>
public class GoalFilterDto
{
    public bool? IsCompleted { get; set; }
    public bool? IsArchived { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTime? DeadlineFrom { get; set; }
    public DateTime? DeadlineTo { get; set; }
}
