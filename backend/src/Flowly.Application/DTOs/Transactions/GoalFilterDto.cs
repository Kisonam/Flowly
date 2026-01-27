namespace Flowly.Application.DTOs.Transactions;

public class GoalFilterDto
{
    public bool? IsCompleted { get; set; }
    public bool? IsArchived { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTime? DeadlineFrom { get; set; }
    public DateTime? DeadlineTo { get; set; }
}
