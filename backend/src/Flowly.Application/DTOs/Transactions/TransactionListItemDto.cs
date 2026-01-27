using System;
using Flowly.Application.DTOs.Notes;
using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Transactions;

public class TransactionListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BudgetId { get; set; }
    public Guid? GoalId { get; set; }
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Description { get; set; }
    public bool IsArchived { get; set; }

    public CategoryDto? Category { get; set; }
    public BudgetSummaryDto? Budget { get; set; }
    public GoalSummaryDto? Goal { get; set; }
    public List<TagDto> Tags { get; set; } = new();
}
