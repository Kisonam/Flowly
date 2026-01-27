using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Transactions;

public class TransactionFilterDto
{
    public string? Search { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public TransactionType? Type { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CurrencyCode { get; set; }
    public List<Guid>? TagIds { get; set; }
    public bool? IsArchived { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
