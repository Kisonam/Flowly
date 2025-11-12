namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// Goal summary DTO for transaction display
/// </summary>
public class GoalSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
}
