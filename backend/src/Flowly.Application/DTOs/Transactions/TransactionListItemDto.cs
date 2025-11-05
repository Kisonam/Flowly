using System;
using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Transactions;

public class TransactionListItemDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public bool IsArchived { get; set; }
}
