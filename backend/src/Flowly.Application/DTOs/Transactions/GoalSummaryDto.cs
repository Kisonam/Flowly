namespace Flowly.Application.DTOs.Transactions;

public class GoalSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
}
