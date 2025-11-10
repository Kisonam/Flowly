using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// DTO for creating a new transaction
/// </summary>
public class CreateTransactionDto
{
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public Guid CategoryId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public List<Guid>? TagIds { get; set; }
}
