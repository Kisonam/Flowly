using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// DTO for updating an existing transaction
/// </summary>
public class UpdateTransactionDto
{
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BudgetId { get; set; }
    public Guid? GoalId { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public List<Guid>? TagIds { get; set; }
}
